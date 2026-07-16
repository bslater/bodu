// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MurmurHash3.32.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace Bodu.IO.Hashing;

/// <summary>
/// Computes a 32-bit (4-byte) non-cryptographic hash using the <c>MurmurHash3_x86_32</c> variant by Austin Appleby.
/// This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="MurmurHash3_32" /> processes input in 4-byte blocks, applying a pair of multiply-rotate-XOR mixing steps
/// per block, followed by a tail pass for any remaining 1–3 bytes. The output is finalized using
/// <see cref="MurmurHash3.FMix32(uint)" /> to ensure strong avalanche properties.
/// </para>
/// <para>
/// A 32-bit seed may be supplied at construction time to produce independent hash families for identical input, which
/// is useful for building distributed hash tables and bloom filters.
/// </para>
/// <para>
/// <strong>Parameters at a glance.</strong>
/// </para>
/// <list type="bullet">
/// <item>
/// <description>Output size: 32 bits (4 bytes), little-endian.</description>
/// </item>
/// <item>
/// <description>Variant: <c>MurmurHash3_x86_32</c>.</description>
/// </item>
/// <item>
/// <description>Block size: 4 bytes; tail pass for remaining 1–3 bytes.</description>
/// </item>
/// <item>
/// <description>Seed: 32 bits, defaults to <c>0</c>.</description>
/// </item>
/// </list>
/// <para>
/// <strong>When to choose MurmurHash3_32.</strong> The default 32-bit hash for in-memory hash tables and bloom filters.
/// Choose it over <see cref="Fnv1a32" /> when input length exceeds ~16 bytes or when SMHasher quality matters. Reach
/// for <see cref="MurmurHash3_128" /> when collision pressure (large key spaces, fingerprinting) calls for more bits,
/// or for <see cref="CityHash32" /> for slightly better throughput on long inputs on 64-bit CPUs.
/// </para>
/// <note type="important"> This algorithm is <b>not</b> cryptographically secure and must <b>not</b> be used for
/// password hashing, digital signatures, or any application requiring adversarial collision resistance. </note>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using Bodu.IO.Hashing;
/// using Bodu.IO.Hashing.Extensions;
///
/// // Default seed, suitable for an in-memory hash table.
/// var m = new MurmurHash3_32();
/// byte[] digest = m.ComputeHash(System.Text.Encoding.UTF8.GetBytes("hash-table-key"));
///
/// // Custom seed for a second, independent hash family (useful in bloom filters).
/// var m2 = new MurmurHash3_32(seed: 0x9E3779B1u);
/// byte[] digest2 = m2.ComputeHash(System.Text.Encoding.UTF8.GetBytes("hash-table-key"));
///]]>
/// </code>
/// </example>
/// </remarks>
/// <seealso cref="MurmurHash3"/> <seealso cref="MurmurHash3_128"/>
public sealed class MurmurHash3_32
    : MurmurHash3
{
    /// <summary>The first mixing constant applied to each block during the MurmurHash3 32-bit body pass.</summary>
    private const uint C1 = 0xCC9E2D51u;

    /// <summary>The second mixing constant applied to each block during the MurmurHash3 32-bit body pass.</summary>
    private const uint C2 = 0x1B873593u;

    /// <summary>The running 32-bit accumulator, seeded at construction and updated as each 4-byte block is mixed in.</summary>
    private uint _h1;

    /// <summary>
    /// Initializes a new instance of the <see cref="MurmurHash3_32" /> class with a seed of zero.
    /// </summary>
    public MurmurHash3_32()
        : this(0u)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MurmurHash3_32" /> class with the specified seed.
    /// </summary>
    /// <param name="seed">The 32-bit seed value used to initialize the hash state.</param>
    public MurmurHash3_32(uint seed)
        : base(32, blockSizeBytes: 4, seed)
    {
        _h1 = seed;
    }

    /// <inheritdoc />
    private protected override void ResetCore() =>
        _h1 = Seed;

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _h1 = 0;

        base.Dispose(disposing);
    }

    /// <inheritdoc />
    private protected override void MixBlocks(ReadOnlySpan<byte> blocks)
    {
        uint h1 = _h1;

        for (int i = 0; i < blocks.Length; i += 4)
        {
            uint k1 = BinaryPrimitives.ReadUInt32LittleEndian(blocks.Slice(i, 4));

            k1 = unchecked(k1 * C1);
            k1 = RotateLeft(k1, 15);
            k1 = unchecked(k1 * C2);

            h1 ^= k1;
            h1 = RotateLeft(h1, 13);
            h1 = unchecked((h1 * 5u) + 0xE6546B64u);
        }

        _h1 = h1;
    }

    /// <inheritdoc />
    private protected override void FinalizeCore(ReadOnlySpan<byte> tail, ulong totalBytes, Span<byte> destination)
    {
        uint h1 = _h1;
        uint k = 0;

        // Tail: fold in the remaining 1–3 bytes.
        switch (tail.Length)
        {
            case 3: k ^= (uint)tail[2] << 16; goto case 2;
            case 2: k ^= (uint)tail[1] << 8; goto case 1;
            case 1:
                k ^= tail[0];
                k = unchecked(k * C1);
                k = RotateLeft(k, 15);
                k = unchecked(k * C2);
                h1 ^= k;
                break;
        }

        // Finalization over a local copy — the running accumulator is left untouched.
        h1 = unchecked(h1 ^ (uint)totalBytes);
        h1 = FMix32(h1);

        BinaryPrimitives.WriteUInt32LittleEndian(destination, h1);
    }

    /// <summary>
    /// Rotates a 32-bit unsigned integer left by the specified number of bits.
    /// </summary>
    /// <param name="value">The value to rotate.</param>
    /// <param name="bits">The number of bit positions to rotate left.</param>
    /// <returns>The rotated value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint RotateLeft(uint value, int bits) =>
        (value << bits) | (value >> (32 - bits));
}
