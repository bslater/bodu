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
/// <see cref="Snefru{T}"/> is one of the earliest cryptographic hash functions developed and is now considered broken: collision
/// attacks against the two- and four-pass variants are known, and it should not be used for any new security-sensitive application.
/// It remains implemented here for interoperability with legacy data and academic study.
/// </para>
/// <para>This base class is extended by:</para>
/// <list type="bullet">
/// <item>
/// <description><see cref="Snefru128"/> produces a 128-bit (16-byte) hash with a 4-word internal state.</description>
/// </item>
/// <item>
/// <description><see cref="Snefru256"/> produces a 256-bit (32-byte) hash with an 8-word internal state.</description>
/// </item>
/// </list>
/// <para>
/// Each input block is processed by 8 rounds consisting of an S-box substitution step followed by a word-wise circular rotation.
/// After all input has been absorbed, the internal state is serialised in big-endian byte order to produce the final digest.
/// </para>
/// <para>
/// <strong>When to choose Snefru.</strong> Academic study and legacy interop only — Snefru has practical
/// collision attacks against both the 2-pass and 4-pass variants and is one of the earliest cryptographic
/// hashes ever published. Pick <see cref="Snefru128"/> for 128-bit output and <see cref="Snefru256"/> for
/// 256-bit output. For any new security-sensitive cryptographic hashing use SHA-2, SHA-3, or
/// <see cref="Blake2b"/>; for non-cryptographic fingerprinting use a member of <c>Bodu.IO.Hashing</c>.
/// </para>
/// <note type="important">This algorithm is <b>not</b> considered secure by modern cryptographic standards and should <b>not</b> be
/// used for password hashing, digital signatures, or integrity validation in security-sensitive applications.</note>
/// </remarks>
/// <seealso cref="Snefru128"/>
/// <seealso cref="Snefru256"/>
/// <seealso cref="BlockHashAlgorithm{T}"/>
public abstract partial class Snefru<T>
    : BlockHashAlgorithm<T>
    where T : Snefru<T>, new()
{
    private const int TotalWords = 16;                              // number of 32-bit words in the working buffer.
    private const int Mask = TotalWords - 1;                        // bitmask to constrain index calculations to the buffer length; inlined as an immediate by the JIT.
    private static readonly int[] s_shifts = [16, 8, 16, 24];       // fixed bitwise rotation amounts applied after each S-box round.
    private static readonly int[] s_permittedHashSizes = [128, 256];

    private readonly uint[] _buffer = new uint[TotalWords];         // internal working buffer used for permutation and round processing.
    private readonly uint[] _state;                                 // internal state used to accumulate the hash output across input blocks.

    /// <summary>
    /// Initializes a new instance of the <see cref="Snefru{T}"/> class with the specified output hash size.
    /// </summary>
    /// <param name="hashSize">The size of the output hash, in bits. Must be either 128 or 256.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="hashSize"/> is not one of the supported values.</exception>
    protected Snefru(int hashSize)
        : base((64 - (hashSize >> 3)) * 8) // BlockSizeBits = 8 * (64 - outputBytes)
    {
        CryptoHelpers.ThrowIfInvalidHashSize(hashSize, s_permittedHashSizes);

        // _state is zero-filled by `new`; Initialize re-clears it on every reset.
        this._state = new uint[hashSize >> 5];
        HashSizeValue = hashSize;
    }

    /// <inheritdoc />
    public override bool CanReuseTransform => true;

    /// <inheritdoc />
    public override bool CanTransformMultipleBlocks => true;

    /// <inheritdoc />
    /// <remarks>The format is <c>"Snefru/<i>n</i>"</c>, where <i>n</i> is the configured output size in bits.</remarks>
    public override string AlgorithmName
    {
        get
        {
            this.ThrowIfDisposed();
            return $"Snefru/{this.HashSizeValue}";
        }
    }

    /// <inheritdoc />
    /// <remarks>Clears the Snefru chaining state to all zeros, as required by the algorithm specification.</remarks>
    public override void Initialize()
    {
        base.Initialize();
        Array.Clear(this._state);
    }

    /// <summary>
    /// Releases resources used by the algorithm and clears the internal state and working buffer.
    /// </summary>
    /// <param name="disposing">
    /// <see langword="true"/> to release both managed and unmanaged resources; <see langword="false"/> to release only unmanaged resources.
    /// </param>
    protected override void Dispose(bool disposing)
    {
        if (this.IsDisposed) return;

        if (disposing)
        {
            CryptoHelpers.Clear(this._buffer);
            CryptoHelpers.Clear(this._state);
        }

        base.Dispose(disposing);
    }

    /// <summary>
    /// Pads the final input block for the <c>Snefru</c> hash algorithm by appending zeros and encoding the total message length.
    /// </summary>
    /// <param name="block">The final block of unprocessed input, typically containing fewer than <c>BlockSize</c> bytes.</param>
    /// <param name="messageLength">The total number of bytes processed prior to this block (excluding the current partial block).</param>
    /// <returns>
    /// A padded byte array of exactly <c>2 × BlockSize</c> bytes, containing the input block followed by zeros and an 8-byte big-endian
    /// length field. The result is aligned for final compression and ready for use by <see cref="ProcessBlock(ReadOnlySpan{byte})"/>.
    /// </returns>
    /// <remarks>
    /// Snefru's final padding block is double the standard block size to support its dual-block internal buffer design. The method pads
    /// the input block with zeros and appends a 64-bit big-endian integer representing the total message length (in bits).
    /// </remarks>
    protected override byte[] PadBlock(ReadOnlySpan<byte> block, ulong messageLength)
    {
        // paddedLength is always ≤ 96 for Snefru128 (2 × 48) and ≤ 64 for Snefru256 (2 × 32),
        // so stackalloc is always safe and appropriately sized here.
        var paddedLength = 2 * (this.BlockSize / 8);
        Span<byte> padded = stackalloc byte[paddedLength];
        block.CopyTo(padded);
        BinaryPrimitives.WriteUInt64BigEndian(padded[(paddedLength - 8)..], messageLength << 3);
        return padded[..paddedLength].ToArray();
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
        this._state.AsSpan().CopyTo(this._buffer);
        LoadBlockToBuffer(block, this._buffer.AsSpan(this._state.Length));

        for (var round = 0; round < 8; round++)
        {
            foreach (var shift in s_shifts)
            {
                ApplySBoxRounds(round);
                RotateWords(shift);
            }
        }

        for (var i = 0; i < this._state.Length; i++)
            this._state[i] ^= this._buffer[Mask - i];
    }

    /// <summary>
    /// Finalizes the hash computation by serialising the internal state to a byte array in big-endian format.
    /// </summary>
    /// <returns>The computed hash as a byte array.</returns>
    protected override byte[] ProcessFinalBlock()
    {
        var output = new byte[this._state.Length * sizeof(uint)];
        WriteStateBigEndian(this._state, output);

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
        for (var i = 0; i < destination.Length; i++)
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
        for (var i = 0; i < source.Length; i++)
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
        var baseBox = round << 1;

        for (var kk = 0; kk < TotalWords; kk++)
        {
            var next = (kk + 1) & Mask;
            var last = (kk + Mask) & Mask;

            // Flat array layout: each table occupies 256 consecutive entries.
            // Index = (tableIndex << 8) | byteValue, avoiding the double indirection of a jagged array.
            var sBoxIndex = ((baseBox + ((kk >> 1) & 0x01)) << 8) | (int)(this._buffer[kk] & 0xff);
            var sboxEntry = s_constants[sBoxIndex];

            this._buffer[next] ^= sboxEntry;
            this._buffer[last] ^= sboxEntry;
        }
    }

    /// <summary>
    /// Performs a circular right bitwise rotation on each word in the internal buffer.
    /// </summary>
    /// <param name="shiftAmount">The number of bits to rotate right by.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RotateWords(int shiftAmount)
    {
        for (var i = 0; i < TotalWords; i++)
            this._buffer[i] = this._buffer[i].RotateBitsRightUnchecked(shiftAmount);
    }
}
