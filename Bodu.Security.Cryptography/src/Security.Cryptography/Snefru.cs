// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Snefru.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Bodu.Extensions;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Base class for the <c>Snefru</c> family of unkeyed hash functions designed by Ralph Merkle, implementing the core compression
/// routine using S-box substitutions and word rotations over 512-bit blocks.
/// </summary>
/// <typeparam name="T">The concrete Snefru variant derived from this class. Must expose a public parameterless constructor.</typeparam>
/// <remarks>
/// <para>
/// <see cref="Snefru{T}" /> is one of the earliest cryptographic hash functions developed and is now considered broken: collision
/// attacks against the two- and four-pass variants are known, and it should not be used for any new security-sensitive application.
/// It remains implemented here for interoperability with legacy data and academic study.
/// </para>
/// <para>This base class is extended by:</para>
/// <list type="bullet">
/// <item>
/// <description><see cref="Snefru128" /> produces a 128-bit (16-byte) hash with a 4-word internal state.</description>
/// </item>
/// <item>
/// <description><see cref="Snefru256" /> produces a 256-bit (32-byte) hash with an 8-word internal state.</description>
/// </item>
/// </list>
/// <para>
/// Each input block is processed by 8 rounds consisting of an S-box substitution step followed by a word-wise circular rotation.
/// After all input has been absorbed, the internal state is serialised in big-endian byte order to produce the final digest.
/// </para>
/// <note type="important">This algorithm is <b>not</b> considered secure by modern cryptographic standards and should <b>not</b> be
/// used for password hashing, digital signatures, or integrity validation in security-sensitive applications.</note>
/// </remarks>
public abstract partial class Snefru<T>
    : BlockHashAlgorithm<T>
    where T : Snefru<T>, new()
{
    private const int TotalWords = 16;                              // number of 32-bit words in the working buffer.
    private const int Mask = TotalWords - 1;                        // bitmask to constrain index calculations to the buffer length; inlined as an immediate by the JIT.
    private static readonly int[] Shifts = [16, 8, 16, 24];         // fixed bitwise rotation amounts applied after each S-box round.
    private static readonly int[] ValidHashSizes = { 128, 256 };

    private readonly uint[] buffer = new uint[TotalWords];          // internal working buffer used for permutation and round processing.
    private readonly uint[] state;                                  // internal state used to accumulate the hash output across input blocks.

    private bool disposed = false;

#if !NET6_0_OR_GREATER

    // Required for .NET Standard 2.0 or older frameworks
    private bool finalized;
#endif

    /// <summary>
    /// Initializes a new instance of the <see cref="Snefru{T}" /> class with the specified output hash size.
    /// </summary>
    /// <param name="hashSize">The size of the output hash, in bits. Must be either 128 or 256.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="hashSize" /> is not one of the supported values.</exception>
    protected Snefru(int hashSize)
        : base(64 - (hashSize >> 3)) // BlockSizeBytes = 64 - outputBytes
    {
        if (Array.IndexOf(ValidHashSizes, hashSize) == -1)
            throw new ArgumentOutOfRangeException(nameof(hashSize),
                string.Format(ResourceStrings.CryptographicException_InvalidHashSize, hashSize, string.Join(", ", ValidHashSizes)));

        this.state = new uint[hashSize >> 5];
        HashSizeValue = hashSize;

        InitializeState();
    }

    /// <inheritdoc />
    public override bool CanReuseTransform => true;

    /// <inheritdoc />
    public override bool CanTransformMultipleBlocks => true;

    /// <inheritdoc />
    public override void Initialize()
    {
        this.ThrowIfDisposed();

        base.Initialize();
        InitializeState();
    }

    /// <summary>
    /// Releases resources used by the algorithm and clears the internal state and working buffer.
    /// </summary>
    /// <param name="disposing">
    /// <see langword="true" /> to release both managed and unmanaged resources; <see langword="false" /> to release only unmanaged resources.
    /// </param>
    protected override void Dispose(bool disposing)
    {
        if (this.disposed) return;

        if (disposing)
        {
            CryptoHelpers.Clear(this.buffer);
            CryptoHelpers.Clear(this.state);
            CryptoHelpers.ClearAndNullify(ref HashValue);

            this.HashSizeValue = 0;
        }

        this.disposed = true;
        base.Dispose(disposing);
    }

    /// <summary>
    /// Pads the final input block for the <c>Snefru</c> hash algorithm by appending zeros and encoding the total message length.
    /// </summary>
    /// <param name="block">The final block of unprocessed input, typically containing fewer than <c>BlockSize</c> bytes.</param>
    /// <param name="messageLength">The total number of bytes processed prior to this block (excluding the current partial block).</param>
    /// <returns>
    /// A padded byte array of exactly <c>2 × BlockSize</c> bytes, containing the input block followed by zeros and an 8-byte big-endian
    /// length field. The result is aligned for final compression and ready for use by <see cref="ProcessBlock(ReadOnlySpan{byte})" />.
    /// </returns>
    /// <remarks>
    /// Snefru's final padding block is double the standard block size to support its dual-block internal buffer design. The method pads
    /// the input block with zeros and appends a 64-bit big-endian integer representing the total message length (in bits).
    /// </remarks>
    protected override byte[] PadBlock(ReadOnlySpan<byte> block, ulong messageLength)
    {
        // paddedLength is always ≤ 96 for Snefru128 (2 × 48) and ≤ 64 for Snefru256 (2 × 32),
        // so stackalloc is always safe and appropriately sized here.
        int paddedLength = 2 * BlockSizeBytes;
        Span<byte> padded = stackalloc byte[paddedLength];
        block.CopyTo(padded);
        BinaryPrimitives.WriteUInt64BigEndian(padded.Slice(paddedLength - 8), messageLength << 3);
        return padded.Slice(0, paddedLength).ToArray();
    }

    /// <summary>
    /// Transforms a single 512-bit block using Snefru S-box and rotation rounds. Updates internal state via XOR with permuted buffer values.
    /// </summary>
    /// <param name="block">The 64-byte input block to hash.</param>
    /// <remarks>
    /// The method performs 8 rounds, each consisting of 4 shifts and S-box applications, to mix input entropy into the state.
    /// </remarks>
    protected override void ProcessBlock(ReadOnlySpan<byte> block)
    {
        this.state.AsSpan().CopyTo(this.buffer);
        LoadBlockToBuffer(block, this.buffer.AsSpan(this.state.Length));

        for (int round = 0; round < 8; round++)
        {
            foreach (int shift in Shifts)
            {
                ApplySBoxRounds(round);
                RotateWords(shift);
            }
        }

        for (int i = 0; i < this.state.Length; i++)
            this.state[i] ^= this.buffer[Mask - i];
    }

    /// <summary>
    /// Finalizes the hash computation by serializing the internal state to a byte array in big-endian format.
    /// </summary>
    /// <returns>The computed hash as a byte array.</returns>
    protected override byte[] ProcessFinalBlock()
    {
        byte[] output = new byte[this.state.Length * sizeof(uint)];
        WriteStateBigEndian(this.state, output);

        return output;
    }

    /// <summary>
    /// Converts a 64-byte input block into a sequence of 32-bit words in big-endian format.
    /// </summary>
    /// <param name="block">The input block to convert.</param>
    /// <param name="destination">The destination span for storing converted words.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void LoadBlockToBuffer(ReadOnlySpan<byte> block, Span<uint> destination)
    {
        ReadOnlySpan<uint> inputWords = MemoryMarshal.Cast<byte, uint>(block);
        for (int i = 0; i < destination.Length; i++)
            destination[i] = inputWords[i].ReverseBytesUnchecked();
    }

    /// <summary>
    /// Writes a big-endian byte representation of a 32-bit word array to the given destination span.
    /// </summary>
    /// <param name="source">The source 32-bit word span.</param>
    /// <param name="destination">The output byte span to populate.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteStateBigEndian(ReadOnlySpan<uint> source, Span<byte> destination)
    {
        // Cast the destination to uint words to avoid per-element Slice calls and their associated bounds checks.
        Span<uint> dest = MemoryMarshal.Cast<byte, uint>(destination);
        for (int i = 0; i < source.Length; i++)
            dest[i] = source[i].ReverseBytesUnchecked();
    }

    /// <summary>
    /// Applies Snefru's S-box substitution rounds using the configured constants for the given round.
    /// </summary>
    /// <param name="round">The current round index (0–7).</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ApplySBoxRounds(int round)
    {
        // Hoist the round-invariant portion of the S-box index out of the inner loop.
        // sBoxNumber alternates between baseBox and baseBox+1 every two iterations of kk,
        // so only the low bit of (kk >> 1) varies; baseBox accounts for the round offset.
        int baseBox = round << 1;

        for (int kk = 0; kk < TotalWords; kk++)
        {
            int next = (kk + 1) & Mask;
            int last = (kk + Mask) & Mask;

            // Flat array layout: each table occupies 256 consecutive entries.
            // Index = (tableIndex << 8) | byteValue, avoiding the double indirection of a jagged array.
            int sBoxIndex = ((baseBox + ((kk >> 1) & 0x01)) << 8) | (int)(this.buffer[kk] & 0xff);
            uint sboxEntry = Constants[sBoxIndex];

            this.buffer[next] ^= sboxEntry;
            this.buffer[last] ^= sboxEntry;
        }
    }

    /// <summary>
    /// Clears the internal state array to prepare for new input.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void InitializeState() => Array.Clear(this.state);

    /// <summary>
    /// Performs a circular right bitwise rotation on each word in the internal buffer.
    /// </summary>
    /// <param name="shiftAmount">The number of bits to rotate right by.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RotateWords(int shiftAmount)
    {
        for (int i = 0; i < TotalWords; i++)
            this.buffer[i] = this.buffer[i].RotateBitsRightUnchecked(shiftAmount);
    }
}
