// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Snefru.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Bodu.Extensions;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Base class for the <c>Snefru</c> family of unkeyed hash functions designed by Ralph Merkle, implementing the core
/// compression routine using S-box substitutions and word rotations over 512-bit blocks.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Snefru" /> is one of the earliest cryptographic hash functions developed and is now considered broken:
/// collision attacks against the two- and four-pass variants are known, and it should not be used for any new
/// security-sensitive application. It remains implemented here for interoperability with legacy data and academic
/// study.
/// </para>
/// <para>
/// This base class is extended by:
/// </para>
/// <list type="bullet">
/// <item>
/// <description><see cref="Snefru128" /> produces a 128-bit (16-byte) hash with a 4-word internal state.</description>
/// </item>
/// <item>
/// <description><see cref="Snefru256" /> produces a 256-bit (32-byte) hash with an 8-word internal state.</description>
/// </item>
/// </list>
/// <para>
/// Each input block is processed by 8 rounds consisting of an S-box substitution step followed by a word-wise circular
/// rotation. After all input has been absorbed, the internal state is serialized in big-endian byte order to produce
/// the final digest.
/// </para>
/// <para>
/// <strong>When to choose Snefru.</strong> Academic study and legacy interop only — Snefru has practical collision
/// attacks against both the 2-pass and 4-pass variants and is one of the earliest cryptographic hashes ever published.
/// Pick <see cref="Snefru128" /> for 128-bit output and <see cref="Snefru256" /> for 256-bit output. For any new
/// security-sensitive cryptographic hashing use SHA-2, SHA-3, or <see cref="Blake2b" />; for non-cryptographic
/// fingerprinting use a member of <c>Bodu.IO.Hashing</c>.
/// </para>
/// <note type="important">This algorithm is <b>not</b> considered secure by modern cryptographic standards and should
/// <b>not</b> be used for password hashing, digital signatures, or integrity validation in security-sensitive
/// applications.</note>
/// </remarks>
/// <seealso cref="Snefru128"/> <seealso cref="Snefru256"/> <seealso cref="BlockHashAlgorithm"/>
public abstract partial class Snefru
    : BlockHashAlgorithm
{
    /// <summary>The number of 32-bit words in the working buffer.</summary>
    private const int TotalWords = 16;

    /// <summary>The bitmask that constrains index calculations to the buffer length.</summary>
    private const int Mask = TotalWords - 1;

    /// <summary>The fixed bitwise rotation amounts applied after each S-box round.</summary>
    private static readonly int[] s_shifts = [16, 8, 16, 24];

    /// <summary>The supported output hash sizes, in bits.</summary>
    private static readonly int[] s_permittedHashSizes = [128, 256];

    /// <summary>The internal working buffer used for permutation and round processing.</summary>
    private readonly uint[] _buffer = new uint[TotalWords];

    /// <summary>The internal state used to accumulate the hash output across input blocks.</summary>
    private readonly uint[] _state;

    /// <summary>
    /// Initializes a new instance of the <see cref="Snefru" /> class with the specified output hash size.
    /// </summary>
    /// <param name="hashSize">The size of the output hash, in bits. Must be either 128 or 256.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="hashSize" /> is not one of the supported values.
    /// </exception>
    protected Snefru(int hashSize)
        : base((64 - (hashSize >> 3)) * 8) // BlockSizeBits = 8 * (64 - outputBytes)
    {
        CryptographyThrowHelper.ThrowIfInvalidHashSize(hashSize, s_permittedHashSizes);

        // _state is zero-filled by `new`; Initialize re-clears it on every reset.
        _state = new uint[hashSize >> 5];
        HashSizeValue = hashSize;
    }

    /// <inheritdoc />
    public override bool CanReuseTransform => true;

    /// <inheritdoc />
    public override bool CanTransformMultipleBlocks => true;

    /// <inheritdoc />
    /// <remarks>
    /// The format is <c>"Snefru/<i>n</i>"</c>, where <i>n</i> is the configured output size in bits.
    /// </remarks>
    public override string AlgorithmName
    {
        get
        {
            ThrowIfDisposed();
            return $"Snefru/{HashSizeValue}";
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Clears the Snefru chaining state to all zeros, as required by the algorithm specification.
    /// </remarks>
    public override void Initialize()
    {
        base.Initialize();
        CryptographyHelper.Clear(_state);
    }

    /// <summary>
    /// Releases resources used by the algorithm and clears the internal state and working buffer.
    /// </summary>
    /// <param name="disposing">
    /// <see langword="true" /> to release both managed and unmanaged resources; <see langword="false" /> to release
    /// only unmanaged resources.
    /// </param>
    protected override void Dispose(bool disposing)
    {
        if (IsDisposed) return;

        if (disposing)
        {
            CryptographyHelper.Clear(_buffer);
            CryptographyHelper.Clear(_state);
        }

        base.Dispose(disposing);
    }

    /// <summary>
    /// Pads the final input block for the <c>Snefru</c> hash algorithm by appending zeros and encoding the total
    /// message length.
    /// </summary>
    /// <param name="block">
    /// The final block of unprocessed input, typically containing fewer than <c>BlockSize</c> bytes.
    /// </param>
    /// <param name="messageLength">
    /// The total number of bytes processed prior to this block (excluding the current partial block).
    /// </param>
    /// <returns>
    /// A padded byte array of exactly <c>2 × BlockSize</c> bytes, containing the input block followed by zeros and an
    /// 8-byte big-endian length field. The result is aligned for final compression and ready for use by
    /// <see cref="ProcessBlock(ReadOnlySpan{byte})" />.
    /// </returns>
    /// <remarks>
    /// Snefru's final padding block is double the standard block size to support its dual-block internal buffer design.
    /// The method pads the input block with zeros and appends a 64-bit big-endian integer representing the total
    /// message length (in bits).
    /// </remarks>
    /// <param name="destination">The span receiving the padded block or blocks; at least two blocks long.</param>
    protected override int PadBlock(ReadOnlySpan<byte> block, ulong messageLength, Span<byte> destination)
    {
        // paddedLength is always ≤ 96 for Snefru128 (2 × 48) and ≤ 64 for Snefru256 (2 × 32).
        int paddedLength = 2 * (BlockSize / 8);
        Span<byte> padded = destination[..paddedLength];
        padded.Clear();
        block.CopyTo(padded);
        BinaryPrimitives.WriteUInt64BigEndian(padded[(paddedLength - 8)..], messageLength << 3);
        return paddedLength;
    }

    /// <summary>
    /// Transforms a single 512-bit block using Snefru S-box and rotation rounds. Updates internal state via XOR with
    /// permuted buffer values.
    /// </summary>
    /// <param name="block">The 64-byte input block to hash.</param>
    /// <remarks>
    /// The method performs 8 rounds, each consisting of 4 shifts and S-box applications, to mix input entropy into the
    /// state.
    /// </remarks>
    protected override void ProcessBlock(ReadOnlySpan<byte> block)
    {
        _state.AsSpan().CopyTo(_buffer);
        LoadBlockToBuffer(block, _buffer.AsSpan(_state.Length));

        for (int round = 0; round < 8; round++)
        {
            foreach (int shift in s_shifts)
            {
                ApplySBoxRounds(round);
                RotateWords(shift);
            }
        }

        for (int i = 0; i < _state.Length; i++)
            _state[i] ^= _buffer[Mask - i];
    }

    /// <summary>
    /// Finalizes the hash computation by serializing the internal state to a byte array in big-endian format.
    /// </summary>
    /// <returns>The computed hash as a byte array.</returns>
    protected override byte[] ProcessFinalBlock()
    {
        byte[] output = new byte[_state.Length * sizeof(uint)];
        WriteStateBigEndian(_state, output);

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
        // Read each word big-endian directly so the result is correct regardless of host endianness. The previous
        // MemoryMarshal.Cast + byte-reverse only yielded big-endian words on little-endian hosts.
        for (int i = 0; i < destination.Length; i++)
            destination[i] = BinaryPrimitives.ReadUInt32BigEndian(block.Slice(i * 4, 4));
    }

    /// <summary>
    /// Writes a big-endian byte representation of a 32-bit word array to the given destination span.
    /// </summary>
    /// <param name="source">The source 32-bit word span.</param>
    /// <param name="destination">The output byte span to populate.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteStateBigEndian(ReadOnlySpan<uint> source, Span<byte> destination)
    {
        // Write each word big-endian directly so the output is correct regardless of host endianness. The previous
        // MemoryMarshal.Cast + byte-reverse only produced big-endian bytes on little-endian hosts.
        for (int i = 0; i < source.Length; i++)
            BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(i * 4, 4), source[i]);
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

        // Interior refs bypass bounds checks: next/last are non-monotonic ((kk+1)&15, (kk+15)&15)
        // so the JIT cannot hoist the check out of the loop; sBoxIndex involves a dynamic byte value
        // so the s_constants check is also never elided without this pattern.
        ref uint bufRef = ref MemoryMarshal.GetArrayDataReference(_buffer);
        ref uint sBoxRef = ref MemoryMarshal.GetArrayDataReference(s_constants);

        for (int kk = 0; kk < TotalWords; kk++)
        {
            int next = (kk + 1) & Mask;
            int last = (kk + Mask) & Mask;

            // Flat array layout: each table occupies 256 consecutive entries.
            // Index = (tableIndex << 8) | byteValue, avoiding the double indirection of a jagged array.
            int sBoxIndex = ((baseBox + ((kk >> 1) & 0x01)) << 8) | (int)(Unsafe.Add(ref bufRef, kk) & 0xff);
            uint sboxEntry = Unsafe.Add(ref sBoxRef, sBoxIndex);

            Unsafe.Add(ref bufRef, next) ^= sboxEntry;
            Unsafe.Add(ref bufRef, last) ^= sboxEntry;
        }
    }

    /// <summary>
    /// Performs a circular right bitwise rotation on each word in the internal buffer.
    /// </summary>
    /// <param name="shiftAmount">The number of bits to rotate right by.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RotateWords(int shiftAmount)
    {
        ref uint bufRef = ref MemoryMarshal.GetArrayDataReference(_buffer);
        for (int i = 0; i < TotalWords; i++)
            Unsafe.Add(ref bufRef, i) = Unsafe.Add(ref bufRef, i).RotateBitsRightUnchecked(shiftAmount);
    }
}
