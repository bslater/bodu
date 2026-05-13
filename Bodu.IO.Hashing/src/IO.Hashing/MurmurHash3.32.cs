// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MurmurHash3.32.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
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
/// <see cref="MurmurHash3_32" /> processes input in 4-byte blocks, applying a pair of multiply-rotate-XOR mixing
/// steps per block, followed by a tail pass for any remaining 1–3 bytes. The output is finalized using
/// <see cref="MurmurHash3{T}.FMix32(uint)" /> to ensure strong avalanche properties.
/// </para>
/// <para>
/// A 32-bit seed may be supplied at construction time to produce independent hash families for identical input,
/// which is useful for building distributed hash tables and bloom filters.
/// </para>
/// <para>
/// <strong>Parameters at a glance.</strong>
/// </para>
/// <list type="bullet">
///   <item><description>Output size: 32 bits (4 bytes), little-endian.</description></item>
///   <item><description>Variant: <c>MurmurHash3_x86_32</c>.</description></item>
///   <item><description>Block size: 4 bytes; tail pass for remaining 1–3 bytes.</description></item>
///   <item><description>Seed: 32 bits, defaults to <c>0</c>.</description></item>
/// </list>
/// <para>
/// <strong>When to choose MurmurHash3_32.</strong> The default 32-bit hash for in-memory hash tables and
/// bloom filters. Choose it over <see cref="Fnv1a32"/> when input length exceeds ~16 bytes or when SMHasher
/// quality matters. Reach for <see cref="MurmurHash3_128"/> when collision pressure (large key spaces,
/// fingerprinting) calls for more bits, or for <see cref="CityHash32"/> for slightly better throughput on
/// long inputs on 64-bit CPUs.
/// </para>
/// <note type="important">
/// This algorithm is <b>not</b> cryptographically secure and must <b>not</b> be used for password hashing, digital
/// signatures, or any application requiring adversarial collision resistance.
/// </note>
/// <example>
/// <code language="csharp">
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
/// </code>
/// </example>
/// </remarks>
public sealed class MurmurHash3_32
    : MurmurHash3<MurmurHash3_32>
{
    private const uint C1 = 0xCC9E2D51u;
    private const uint C2 = 0x1B873593u;

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
        : base(32, seed)
    {
    }

    /// <summary>
    /// Computes the 32-bit MurmurHash3 of the provided input span.
    /// </summary>
    /// <param name="source">The input bytes to hash.</param>
    /// <returns>A 4-byte array containing the little-endian encoded 32-bit hash value.</returns>
    protected override byte[] ComputeHashCore(ReadOnlySpan<byte> source)
    {
        uint h1 = this.Seed;
        int len = source.Length;
        int nblocks = len / 4;

        // Body: process 4-byte blocks.
        for (int i = 0; i < nblocks; i++)
        {
            uint k1 = BinaryPrimitives.ReadUInt32LittleEndian(source.Slice(i * 4, 4));

            k1 = unchecked(k1 * C1);
            k1 = RotateLeft(k1, 15);
            k1 = unchecked(k1 * C2);

            h1 ^= k1;
            h1 = RotateLeft(h1, 13);
            h1 = unchecked((h1 * 5u) + 0xE6546B64u);
        }

        // Tail: process remaining 1–3 bytes.
        ReadOnlySpan<byte> tail = source.Slice(nblocks * 4);
        uint k = 0;

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

        // Finalization.
        h1 = unchecked(h1 ^ (uint)len);
        h1 = FMix32(h1);

        byte[] result = new byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(result, h1);
        return result;
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
