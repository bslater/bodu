// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CityHash.128.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using Bodu.Extensions;

namespace Bodu.IO.Hashing;

/// <summary>
/// Computes a 128-bit (16-byte) non-cryptographic hash using the <c>CityHash128</c> variant by Google. This class
/// cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="CityHash128" /> chooses its initial 128-bit seed from the first 16 bytes of input (or from the fixed
/// constants <c>K0</c>, <c>K1</c> for shorter inputs), then delegates to <c>CityHash128WithSeed</c>. For inputs shorter
/// than 128 bytes, <c>CityMurmur</c> folds the message into the seeded accumulator; for longer inputs, a main loop
/// consumes 128-byte blocks using two pairs of seeded weak-hash accumulators before a tail pass over the remaining
/// 0–127 bytes.
/// </para>
/// <para>
/// The digest is emitted as two consecutive little-endian 64-bit words (<c>First</c> followed by <c>Second</c>),
/// matching the encoding convention used by <see cref="CityHash64" />.
/// </para>
/// <para>
/// <strong>Parameters at a glance.</strong>
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// Output size: 128 bits (16 bytes), little-endian, two 64-bit words.
/// </description>
/// </item>
/// <item>
/// <description>
/// Variant: <c>CityHash128</c>.
/// </description>
/// </item>
/// <item>
/// <description>
/// Length-dispatched mixing: &lt;128-byte and 128+-byte paths.
/// </description>
/// </item>
/// <item>
/// <description>
/// Block size on the long path: 128 bytes.
/// </description>
/// </item>
/// </list>
/// <para>
/// <strong>When to choose CityHash128.</strong> Pick <see cref="CityHash128" /> for low-collision fingerprinting of
/// large key spaces — content-addressed storage, deduplication, two-hash cuckoo / split-key schemes that can use the
/// two 64-bit halves independently. <see cref="MurmurHash3_128" /> is the closest alternative; it supports a
/// constructor seed but is typically slightly slower on long inputs on 64-bit CPUs.
/// </para>
/// <note type="important"> This algorithm is <b>not</b> cryptographically secure and must <b>not</b> be used for
/// password hashing, digital signatures, or any application requiring adversarial collision resistance. </note>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using Bodu.IO.Hashing;
/// using Bodu.IO.Hashing.Extensions;
///
/// var city = new CityHash128();
/// byte[] fingerprint = city.ComputeHash(blob);
/// fingerprint = [low 64 bits || high 64 bits], each little-endian.
///]]>
/// </code>
/// </example>
/// </remarks>
/// <seealso cref="CityHash{T}"/> <seealso cref="CityHash32"/> <seealso cref="CityHash64"/>
public sealed class CityHash128
    : CityHash<CityHash128>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CityHash128" /> class with a fixed 128-bit (16-byte) hash output
    /// size.
    /// </summary>
    public CityHash128()
        : base(128)
    {
    }

    /// <summary>
    /// Computes the 128-bit CityHash of the provided input span, selecting the optimal mixing path based on input
    /// length.
    /// </summary>
    /// <param name="source">The input bytes to hash.</param>
    /// <returns>A 16-byte array containing the little-endian encoded 128-bit hash value.</returns>
    protected override byte[] ComputeHashCore(ReadOnlySpan<byte> source)
    {
        (var first, var second) = CityHash128Core(source);

        var buffer = new byte[16];
        BinaryPrimitives.WriteUInt64LittleEndian(buffer.AsSpan(0, 8), first);
        BinaryPrimitives.WriteUInt64LittleEndian(buffer.AsSpan(8, 8), second);
        return buffer;
    }

    /// <summary>
    /// Selects a 128-bit seed for <see cref="CityHash128WithSeed(ReadOnlySpan{byte}, ulong, ulong)" /> based on the
    /// first 16 bytes of the input, or on fixed constants when the input is shorter.
    /// </summary>
    /// <param name="s">The complete input span.</param>
    /// <returns>The 128-bit hash as a <c>(First, Second)</c> tuple.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static (ulong First, ulong Second) CityHash128Core(ReadOnlySpan<byte> s)
    {
        if (s.Length >= 16)
        {
            var low = BinaryPrimitives.ReadUInt64LittleEndian(s);
            var high = BinaryPrimitives.ReadUInt64LittleEndian(s.Slice(8)) + K0;

            return CityHash128WithSeed(s.Slice(16), low, high);
        }

        return CityHash128WithSeed(s, K0, K1);
    }

    /// <summary>
    /// Computes <c>CityHash128WithSeed</c>: for short inputs (&lt; 128 bytes) it delegates to
    /// <see cref="CityMurmur(ReadOnlySpan{byte}, ulong, ulong)" />; for longer inputs it runs the iterative main loop
    /// over 128-byte blocks followed by a tail reduction.
    /// </summary>
    /// <param name="source">The input span to hash.</param>
    /// <param name="seedLow">The low 64 bits of the seed.</param>
    /// <param name="seedHigh">The high 64 bits of the seed.</param>
    /// <returns>The 128-bit hash as a <c>(First, Second)</c> tuple.</returns>
    private static (ulong First, ulong Second) CityHash128WithSeed(
        ReadOnlySpan<byte> source, ulong seedLow, ulong seedHigh)
    {
        if (source.Length < 128)
            return CityMurmur(source, seedLow, seedHigh);

        // Keep 56 bytes of running state across the main loop.
        var x = seedLow;
        var y = seedHigh;
        var z = (ulong)source.Length * K1;

        var v0 = ((y ^ K1).RotateBitsRightUnchecked(49) * K1) + BinaryPrimitives.ReadUInt64LittleEndian(source);
        var v1 = (v0.RotateBitsRightUnchecked(42) * K1) + BinaryPrimitives.ReadUInt64LittleEndian(source.Slice(8));
        var w0 = ((y + z).RotateBitsRightUnchecked(35) * K1) + x;
        var w1 = (x + BinaryPrimitives.ReadUInt64LittleEndian(source.Slice(88))).RotateBitsRightUnchecked(53) * K1;

        var offset = 0;
        var remaining = source.Length;

        do
        {
            // Two unrolled iterations per 128-byte block, matching the Google reference.
            x = (x + y + v0 + BinaryPrimitives.ReadUInt64LittleEndian(source.Slice(offset + 8))).RotateBitsRightUnchecked(37) * K1;
            y = (y + v1 + BinaryPrimitives.ReadUInt64LittleEndian(source.Slice(offset + 48))).RotateBitsRightUnchecked(42) * K1;
            x ^= w1;
            y += v0 + BinaryPrimitives.ReadUInt64LittleEndian(source.Slice(offset + 40));
            z = (z + w0).RotateBitsRightUnchecked(33) * K1;
            (v0, v1) = WeakHashLen32WithSeeds(source.Slice(offset), v1 * K1, x + w0);
            (w0, w1) = WeakHashLen32WithSeeds(source.Slice(offset + 32), z + w1, y + BinaryPrimitives.ReadUInt64LittleEndian(source.Slice(offset + 16)));
            (z, x) = (x, z);
            offset += 64;

            x = (x + y + v0 + BinaryPrimitives.ReadUInt64LittleEndian(source.Slice(offset + 8))).RotateBitsRightUnchecked(37) * K1;
            y = (y + v1 + BinaryPrimitives.ReadUInt64LittleEndian(source.Slice(offset + 48))).RotateBitsRightUnchecked(42) * K1;
            x ^= w1;
            y += v0 + BinaryPrimitives.ReadUInt64LittleEndian(source.Slice(offset + 40));
            z = (z + w0).RotateBitsRightUnchecked(33) * K1;
            (v0, v1) = WeakHashLen32WithSeeds(source.Slice(offset), v1 * K1, x + w0);
            (w0, w1) = WeakHashLen32WithSeeds(source.Slice(offset + 32), z + w1, y + BinaryPrimitives.ReadUInt64LittleEndian(source.Slice(offset + 16)));
            (z, x) = (x, z);
            offset += 64;

            remaining -= 128;
        }
        while (remaining >= 128);

        x += (v0 + z).RotateBitsRightUnchecked(49) * K0;
        y = (y * K0) + w1.RotateBitsRightUnchecked(37);
        z = (z * K0) + w0.RotateBitsRightUnchecked(27);
        w0 *= 9;
        v0 *= K0;

        // Tail: hash up to four 32-byte chunks taken from the end of the input.
        var tailOffset = offset;
        var tailEnd = offset + remaining;
        for (var tailDone = 0; tailDone < remaining;)
        {
            tailDone += 32;
            y = ((x + y).RotateBitsRightUnchecked(42) * K0) + v1;
            w0 += BinaryPrimitives.ReadUInt64LittleEndian(source.Slice(tailEnd - tailDone + 16));
            x = (x * K0) + w0;
            z += w1 + BinaryPrimitives.ReadUInt64LittleEndian(source.Slice(tailEnd - tailDone));
            w1 += v0;
            (v0, v1) = WeakHashLen32WithSeeds(source.Slice(tailEnd - tailDone), v0 + z, v1);
            v0 *= K0;
        }

        x = HashLen16(x, v0);
        y = HashLen16(y + z, w0);
        return (HashLen16(x + v1, w1) + y, HashLen16(x + w1, y + v1));
    }

    /// <summary>
    /// Computes a 128-bit hash of a &lt; 128-byte input by folding it into the 128-bit seed using a Murmur-style mixing
    /// loop. This is the short-input specialization invoked by
    /// <see cref="CityHash128WithSeed(ReadOnlySpan{byte}, ulong, ulong)" />.
    /// </summary>
    /// <param name="source">The input span. Length must be less than 128 bytes.</param>
    /// <param name="seedLow">The low 64 bits of the seed.</param>
    /// <param name="seedHigh">The high 64 bits of the seed.</param>
    /// <returns>The 128-bit hash as a <c>(First, Second)</c> tuple.</returns>
    private static (ulong First, ulong Second) CityMurmur(
        ReadOnlySpan<byte> source, ulong seedLow, ulong seedHigh)
    {
        var a = seedLow;
        var b = seedHigh;
        var c = 0UL;
        var d = 0UL;
        var len = source.Length;
        var l = len - 16;

        if (l <= 0)
        {
            a = ShiftMix(a * K1) * K1;
            c = (b * K1) + Hash64Len0to16(source);
            d = ShiftMix(a + (len >= 8 ? BinaryPrimitives.ReadUInt64LittleEndian(source) : c));
        }
        else
        {
            c = HashLen16(BinaryPrimitives.ReadUInt64LittleEndian(source.Slice(len - 8)) + K1, a);
            d = HashLen16(b + (ulong)len, c + BinaryPrimitives.ReadUInt64LittleEndian(source.Slice(len - 16)));
            a += d;

            var offset = 0;
            while (l > 0)
            {
                a ^= ShiftMix(BinaryPrimitives.ReadUInt64LittleEndian(source.Slice(offset)) * K1) * K1;
                a *= K1;
                b ^= a;
                c ^= ShiftMix(BinaryPrimitives.ReadUInt64LittleEndian(source.Slice(offset + 8)) * K1) * K1;
                c *= K1;
                d ^= c;

                offset += 16;
                l -= 16;
            }
        }

        a = HashLen16(a, c);
        b = HashLen16(d, b);
        return (a ^ b, HashLen16(b, a));
    }
}
