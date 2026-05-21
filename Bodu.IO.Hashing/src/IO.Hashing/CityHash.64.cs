// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CityHash.64.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using Bodu.Extensions;

namespace Bodu.IO.Hashing;

/// <summary>
/// Computes a 64-bit (8-byte) non-cryptographic hash using the <c>CityHash64</c> variant by Google. This class cannot
/// be inherited.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="CityHash64" /> selects one of four internal mixing paths depending on the input length: a compact path
/// for 0–16 bytes, a four-word path for 17–32 bytes, an eight-word path with byte-swap finalization for 33–64 bytes,
/// and a full iterative path that consumes 64-byte blocks using two pairs of seeded weak hash accumulators for inputs
/// of 65 bytes or more. All paths converge through the shared <c>HashLen16</c> finalizer, which applies two rounds of
/// multiply-shift-XOR to distribute entropy across all output bits.
/// </para>
/// <para>
/// <strong>Parameters at a glance.</strong>
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// Output size: 64 bits (8 bytes), little-endian.
/// </description>
/// </item>
/// <item>
/// <description>
/// Variant: <c>CityHash64</c>.
/// </description>
/// </item>
/// <item>
/// <description>
/// Length-dispatched mixing: 0–16, 17–32, 33–64, and 65+ byte paths.
/// </description>
/// </item>
/// <item>
/// <description>
/// Block size on the long path: 64 bytes.
/// </description>
/// </item>
/// </list>
/// <para>
/// <strong>When to choose CityHash64.</strong> The general-purpose default for 64-bit non-cryptographic hashing —
/// fingerprints, content-based sharding, deduplication keys. <see cref="MurmurHash3_128" /> gives twice the bits at
/// slightly lower throughput on long inputs; <see cref="Fnv1a64" /> is preferable only for very small fixed-length keys
/// where simplicity matters more than distribution.
/// </para>
/// <note type="important"> This algorithm is <b>not</b> cryptographically secure and must <b>not</b> be used for
/// password hashing, digital signatures, or any application requiring adversarial collision resistance. </note>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using Bodu.IO.Hashing;
/// using Bodu.IO.Hashing.Extensions;
///
/// var city = new CityHash64();
/// byte[] fingerprint = city.ComputeHash(blob);
///]]>
/// </code>
/// </example>
/// </remarks>
/// <seealso cref="CityHash{T}"/> <seealso cref="CityHash32"/> <seealso cref="CityHash128"/>
public sealed class CityHash64
    : CityHash<CityHash64>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CityHash64" /> class with a fixed 64-bit (8-byte) hash output size.
    /// </summary>
    public CityHash64()
        : base(64)
    {
    }

    /// <summary>
    /// Computes the 64-bit CityHash of the provided input span, selecting the optimal mixing path based on input
    /// length.
    /// </summary>
    /// <param name="source">The input bytes to hash.</param>
    /// <returns>An 8-byte array containing the little-endian encoded 64-bit hash value.</returns>
    protected override byte[] ComputeHashCore(ReadOnlySpan<byte> source)
    {
        var result = source.Length switch
        {
            <= 16 => Hash64Len0to16(source),
            <= 32 => Hash64Len17to32(source),
            <= 64 => Hash64Len33to64(source),
            _ => Hash64Long(source)
        };

        var buffer = new byte[8];
        BinaryPrimitives.WriteUInt64LittleEndian(buffer, result);
        return buffer;
    }

    /// <summary>
    /// Hashes 17 to 32 bytes by reading four 64-bit words from the start and end of the input.
    /// </summary>
    /// <param name="s">The input span. Length must be in the range [17, 32].</param>
    /// <returns>The 64-bit hash value.</returns>
    private static ulong Hash64Len17to32(ReadOnlySpan<byte> s)
    {
        var len = s.Length;
        var mul = K2 + (ulong)(len * 2);

        var a = BinaryPrimitives.ReadUInt64LittleEndian(s) * K1;
        var b = BinaryPrimitives.ReadUInt64LittleEndian(s.Slice(8));
        var c = BinaryPrimitives.ReadUInt64LittleEndian(s.Slice(len - 8)) * mul;
        var d = BinaryPrimitives.ReadUInt64LittleEndian(s.Slice(len - 16)) * K2;

        return HashLen16(
            u: (a + b).RotateBitsRightUnchecked(43) + c.RotateBitsRightUnchecked(30) + d,
            v: a + (b + K2).RotateBitsRightUnchecked(18) + c,
            mul: mul);
    }

    /// <summary>
    /// Hashes 33 to 64 bytes by reading eight 64-bit words spread across the full input span, including a byte-swap
    /// finalization step.
    /// </summary>
    /// <param name="s">The input span. Length must be in the range [33, 64].</param>
    /// <returns>The 64-bit hash value.</returns>
    private static ulong Hash64Len33to64(ReadOnlySpan<byte> s)
    {
        var len = s.Length;
        var mul = K2 + (ulong)(len * 2);

        var a = BinaryPrimitives.ReadUInt64LittleEndian(s) * K2;
        var b = BinaryPrimitives.ReadUInt64LittleEndian(s.Slice(8));
        var c = BinaryPrimitives.ReadUInt64LittleEndian(s.Slice(len - 24));
        var d = BinaryPrimitives.ReadUInt64LittleEndian(s.Slice(len - 32));
        var e = BinaryPrimitives.ReadUInt64LittleEndian(s.Slice(16)) * K2;
        var f = BinaryPrimitives.ReadUInt64LittleEndian(s.Slice(24)) * 9;
        var g = BinaryPrimitives.ReadUInt64LittleEndian(s.Slice(len - 8));
        var h = BinaryPrimitives.ReadUInt64LittleEndian(s.Slice(len - 16)) * mul;

        var u = (a + g).RotateBitsRightUnchecked(43) + ((b.RotateBitsRightUnchecked(30) + c) * 9);
        var v = ((a + g) ^ d) + f + 1;
        var w = ((u + v) * mul).ReverseBytesUnchecked() + h;
        var x = (e + f).RotateBitsRightUnchecked(42) + c;
        var y = (((v + w) * mul).ReverseBytesUnchecked() + g) * mul;
        var z = e + f + c;

        a = (((x + z) * mul) + y).ReverseBytesUnchecked() + b;
        b = ShiftMix(((z + a) * mul) + d + h) * mul;

        return b + x;
    }

    /// <summary>
    /// Hashes inputs of 65 bytes or more using two pairs of seeded weak-hash accumulators that consume the input in
    /// 64-byte blocks.
    /// </summary>
    /// <param name="s">The input span. Length must be 65 or greater.</param>
    /// <returns>The 64-bit hash value.</returns>
    /// <remarks>
    /// <para>
    /// The method seeds the four accumulators (<c>v</c>, <c>w</c>) and the three mixing variables (<c>x</c>, <c>y</c>,
    /// <c>z</c>) from the tail of the input before processing, ensuring that long and short inputs produce
    /// well-distributed results.
    /// </para>
    /// <para>
    /// Each 64-byte iteration updates all five variables and swaps <c>x</c> and <c>z</c> to prevent positional bias.
    /// The final result combines both accumulator pairs through two nested <c>HashLen16</c> calls.
    /// </para>
    /// </remarks>
    private static ulong Hash64Long(ReadOnlySpan<byte> s)
    {
        var len = s.Length;

        // Seed the mixing variables and accumulators from the tail of the input so that length
        // differences always produce distinct starting states, regardless of head content.
        var x = BinaryPrimitives.ReadUInt64LittleEndian(s.Slice(len - 40));
        var y = BinaryPrimitives.ReadUInt64LittleEndian(s.Slice(len - 16))
                + BinaryPrimitives.ReadUInt64LittleEndian(s.Slice(len - 56));
        var z = HashLen16(
            BinaryPrimitives.ReadUInt64LittleEndian(s.Slice(len - 48)) + (ulong)len,
            BinaryPrimitives.ReadUInt64LittleEndian(s.Slice(len - 24)));

        var (v0, v1) = WeakHashLen32WithSeeds(s.Slice(len - 64), (ulong)len, z);
        var (w0, w1) = WeakHashLen32WithSeeds(s.Slice(len - 32), y + K1, x);

        // Fold the first 8 bytes of the head into x to anchor the start of the input.
        x = (x * K1) + BinaryPrimitives.ReadUInt64LittleEndian(s);

        // Align remaining length down to a 64-byte boundary for the main loop.
        var remaining = (len - 1) & ~63;
        var offset = 0;

        do
        {
            x = (x + y + v0 + BinaryPrimitives.ReadUInt64LittleEndian(s.Slice(offset + 8))).RotateBitsRightUnchecked(37) * K1;
            y = (y + v1 + BinaryPrimitives.ReadUInt64LittleEndian(s.Slice(offset + 48))).RotateBitsRightUnchecked(42) * K1;

            x ^= w1;
            y += v0 + BinaryPrimitives.ReadUInt64LittleEndian(s.Slice(offset + 40));
            z = (z + w0).RotateBitsRightUnchecked(33) * K1;

            (v0, v1) = WeakHashLen32WithSeeds(s.Slice(offset), v1 * K1, x + w0);
            (w0, w1) = WeakHashLen32WithSeeds(s.Slice(offset + 32), z + w1, y + BinaryPrimitives.ReadUInt64LittleEndian(s.Slice(offset + 16)));

            // Swap x and z each iteration to prevent accumulator position bias.
            (z, x) = (x, z);

            offset += 64;
            remaining -= 64;
        }
        while (remaining != 0);

        return HashLen16(
            HashLen16(v0, w0) + (ShiftMix(y) * K1) + z,
            HashLen16(v1, w1) + x);
    }
}
