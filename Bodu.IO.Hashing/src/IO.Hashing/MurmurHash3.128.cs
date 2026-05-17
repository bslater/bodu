// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MurmurHash3.128.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace Bodu.IO.Hashing;

/// <summary>
/// Computes a 128-bit (16-byte) non-cryptographic hash using the <c>MurmurHash3_x64_128</c> variant by Austin Appleby,
/// optimized for 64-bit platforms. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="MurmurHash3_128" /> maintains two independent 64-bit accumulators (<c>h1</c> and <c>h2</c>) seeded from
/// the constructor-supplied value. Input is consumed in 16-byte blocks; each block word is mixed through
/// multiply-rotate-multiply passes before being folded into the appropriate accumulator. Remaining 1–15 bytes are
/// handled by a tail switch. Both accumulators are cross-mixed and finalized via
/// <see cref="MurmurHash3{T}.FMix64(ulong)" /> to produce the 128-bit output.
/// </para>
/// <para>
/// A 32-bit seed may be supplied at construction time. Both accumulators are initialized to the same seed value,
/// matching the behavior of the reference <c>MurmurHash3_x64_128</c> implementation.
/// </para>
/// <para>
/// <strong>Parameters at a glance.</strong>
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// Output size: 128 bits (16 bytes), little-endian.
/// </description>
/// </item>
/// <item>
/// <description>
/// Variant: <c>MurmurHash3_x64_128</c> — optimized for 64-bit platforms.
/// </description>
/// </item>
/// <item>
/// <description>
/// Block size: 16 bytes; tail pass for remaining 1–15 bytes.
/// </description>
/// </item>
/// <item>
/// <description>
/// Seed: 32 bits, applied to both accumulators; defaults to <c>0</c>.
/// </description>
/// </item>
/// </list>
/// <para>
/// <strong>When to choose MurmurHash3_128.</strong> Pick this when collision pressure makes 32 or 64 bits inadequate —
/// content fingerprinting, deduplication keys, large bloom filters. Output is also useful as a pair of 64-bit halves
/// for two-hash cuckoo or split-key schemes. <see cref="CityHash128" /> is a comparable alternative with similar
/// quality and slightly better throughput on long inputs on modern 64-bit CPUs.
/// </para>
/// <note type="important"> This algorithm is <b>not</b> cryptographically secure and must <b>not</b> be used for
/// password hashing, digital signatures, or any application requiring adversarial collision resistance. </note>
/// <example>
/// <code language="csharp"> using Bodu.IO.Hashing; using Bodu.IO.Hashing.Extensions; // 128-bit fingerprint of a
/// content blob. var m = new MurmurHash3_128(); byte[] fingerprint = m.ComputeHash(blob); // Custom seed to isolate a
/// second hash family for cuckoo / bloom-filter use. var m2 = new MurmurHash3_128(seed: 0xC2B2AE35u); </code>
/// </example>
/// </remarks>
/// <seealso cref="MurmurHash3{T}"/> <seealso cref="MurmurHash3_32"/>
public sealed class MurmurHash3_128
    : MurmurHash3<MurmurHash3_128>
{

    private const ulong C1 = 0x87C37B91114253D5uL;
    private const ulong C2 = 0x4CF5AD432745937FuL;

    /// <summary>
    /// Initializes a new instance of the <see cref="MurmurHash3_128" /> class with a seed of zero.
    /// </summary>
    public MurmurHash3_128()
        : this(0u)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MurmurHash3_128" /> class with the specified seed.
    /// </summary>
    /// <param name="seed">The 32-bit seed value used to initialize both 64-bit accumulators.</param>
    public MurmurHash3_128(uint seed)
        : base(128, seed)
    {
    }

    /// <summary>
    /// Computes the 128-bit MurmurHash3 (x64 variant) of the provided input span.
    /// </summary>
    /// <param name="source">The input bytes to hash.</param>
    /// <returns>
    /// A 16-byte array containing the little-endian encoded 128-bit hash value, with <c>h1</c> in bytes 0–7 and
    /// <c>h2</c> in bytes 8–15.
    /// </returns>
    protected override byte[] ComputeHashCore(ReadOnlySpan<byte> source)
    {
        ulong h1 = this.Seed;
        ulong h2 = this.Seed;
        int len = source.Length;
        int nblocks = len / 16;

        // Body: process 16-byte blocks as two 64-bit words.
        for (int i = 0; i < nblocks; i++)
        {
            ulong k1 = BinaryPrimitives.ReadUInt64LittleEndian(source.Slice(i * 16, 8));
            ulong k2 = BinaryPrimitives.ReadUInt64LittleEndian(source.Slice((i * 16) + 8, 8));

            k1 = unchecked(k1 * C1);
            k1 = RotateLeft(k1, 31);
            k1 = unchecked(k1 * C2);
            h1 ^= k1;

            h1 = RotateLeft(h1, 27);
            h1 = unchecked(h1 + h2);
            h1 = unchecked((h1 * 5uL) + 0x52DCE729uL);

            k2 = unchecked(k2 * C2);
            k2 = RotateLeft(k2, 33);
            k2 = unchecked(k2 * C1);
            h2 ^= k2;

            h2 = RotateLeft(h2, 31);
            h2 = unchecked(h2 + h1);
            h2 = unchecked((h2 * 5uL) + 0x38495AB5uL);
        }

        // Tail: process remaining 1–15 bytes.
        ReadOnlySpan<byte> tail = source.Slice(nblocks * 16);
        ulong t1 = 0, t2 = 0;

        switch (tail.Length)
        {
            case 15: t2 ^= (ulong)tail[14] << 48; goto case 14;
            case 14: t2 ^= (ulong)tail[13] << 40; goto case 13;
            case 13: t2 ^= (ulong)tail[12] << 32; goto case 12;
            case 12: t2 ^= (ulong)tail[11] << 24; goto case 11;
            case 11: t2 ^= (ulong)tail[10] << 16; goto case 10;
            case 10: t2 ^= (ulong)tail[9] << 8; goto case 9;
            case 9:
                t2 ^= (ulong)tail[8];
                t2 = unchecked(t2 * C2);
                t2 = RotateLeft(t2, 33);
                t2 = unchecked(t2 * C1);
                h2 ^= t2;
                goto case 8;
            case 8: t1 ^= (ulong)tail[7] << 56; goto case 7;
            case 7: t1 ^= (ulong)tail[6] << 48; goto case 6;
            case 6: t1 ^= (ulong)tail[5] << 40; goto case 5;
            case 5: t1 ^= (ulong)tail[4] << 32; goto case 4;
            case 4: t1 ^= (ulong)tail[3] << 24; goto case 3;
            case 3: t1 ^= (ulong)tail[2] << 16; goto case 2;
            case 2: t1 ^= (ulong)tail[1] << 8; goto case 1;
            case 1:
                t1 ^= tail[0];
                t1 = unchecked(t1 * C1);
                t1 = RotateLeft(t1, 31);
                t1 = unchecked(t1 * C2);
                h1 ^= t1;
                break;
        }

        // Finalization: cross-mix accumulators then apply fmix64.
        h1 = unchecked(h1 ^ (ulong)len);
        h2 = unchecked(h2 ^ (ulong)len);

        h1 = unchecked(h1 + h2);
        h2 = unchecked(h2 + h1);

        h1 = FMix64(h1);
        h2 = FMix64(h2);

        h1 = unchecked(h1 + h2);
        h2 = unchecked(h2 + h1);

        byte[] result = new byte[16];
        BinaryPrimitives.WriteUInt64LittleEndian(result.AsSpan(0, 8), h1);
        BinaryPrimitives.WriteUInt64LittleEndian(result.AsSpan(8, 8), h2);
        return result;
    }

    /// <summary>
    /// Rotates a 64-bit unsigned integer left by the specified number of bits.
    /// </summary>
    /// <param name="value">The value to rotate.</param>
    /// <param name="bits">The number of bit positions to rotate left.</param>
    /// <returns>The rotated value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong RotateLeft(ulong value, int bits) =>
        (value << bits) | (value >> (64 - bits));

}
