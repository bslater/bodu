// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CityHash.32.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using Bodu.Extensions;

namespace Bodu.IO.Hashing;

/// <summary>
/// Computes a 32-bit (4-byte) non-cryptographic hash using the <c>CityHash32</c> variant by Google. This class cannot
/// be inherited.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="CityHash32" /> dispatches to one of four internal mixing paths based on input length: a loop over
/// individual bytes for 0–4 bytes; a four-word path for 5–12 bytes; a six-word path for 13–24 bytes; and a full
/// iterative path consuming 20-byte blocks with three interleaved accumulators for 25 or more bytes. All paths converge
/// through the <c>Mur</c> and <c>Mix</c> primitives defined in <see cref="CityHash{T}" />.
/// </para>
/// <para>
/// <strong>Parameters at a glance.</strong>
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// Output size: 32 bits (4 bytes), little-endian.
/// </description>
/// </item>
/// <item>
/// <description>
/// Variant: <c>CityHash32</c>.
/// </description>
/// </item>
/// <item>
/// <description>
/// Length-dispatched mixing: 0–4, 5–12, 13–24, and 25+ byte paths.
/// </description>
/// </item>
/// <item>
/// <description>
/// Seedless — for seeded variants prefer <see cref="MurmurHash3_32" />.
/// </description>
/// </item>
/// </list>
/// <para>
/// <strong>When to choose CityHash32.</strong> Pick <see cref="CityHash32" /> for 32-bit slot indexes when throughput
/// on long inputs matters — it edges <see cref="MurmurHash3_32" /> on 64-bit hosts. For short fixed-length keys
/// <see cref="Fnv1a32" /> is simpler and competitive; if a seed is needed (bloom filters, hash-flooding mitigation)
/// prefer <see cref="MurmurHash3_32" />.
/// </para>
/// <note type="important"> This algorithm is <b>not</b> cryptographically secure and must <b>not</b> be used for
/// password hashing, digital signatures, or any application requiring adversarial collision resistance. </note>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using Bodu.IO.Hashing; using Bodu.IO.Hashing.Extensions; var city = new CityHash32();
/// byte[] digest = city.ComputeHash(System.Text.Encoding.UTF8.GetBytes("session-key"));
///]]>
/// </code>
/// </example>
/// </remarks>
/// <seealso cref="CityHash{T}"/> <seealso cref="CityHash64"/> <seealso cref="CityHash128"/>
public sealed class CityHash32
    : CityHash<CityHash32>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CityHash32" /> class with a fixed 32-bit (4-byte) hash output size.
    /// </summary>
    public CityHash32()
        : base(32)
    {
    }

    /// <summary>
    /// Computes the 32-bit CityHash of the provided input span, selecting the optimal mixing path based on input
    /// length.
    /// </summary>
    /// <param name="source">The input bytes to hash.</param>
    /// <returns>A 4-byte array containing the little-endian encoded 32-bit hash value.</returns>
    protected override byte[] ComputeHashCore(ReadOnlySpan<byte> source)
    {
        var result = source.Length switch
        {
            <= 4 => Hash32Len0to4(source),
            <= 12 => Hash32Len5to12(source),
            <= 24 => Hash32Len13to24(source),
            _ => Hash32Len25Plus(source)
        };

        var buffer = new byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(buffer, result);
        return buffer;
    }

    /// <summary>
    /// Hashes 0 to 4 bytes using an accumulating byte-level multiply-XOR loop.
    /// </summary>
    /// <param name="s">The input span. Length must be in the range [0, 4].</param>
    /// <returns>The 32-bit hash value.</returns>
    private static uint Hash32Len0to4(ReadOnlySpan<byte> s)
    {
        uint b = 0;
        uint c = 9;

        unchecked
        {
            foreach (var t in s)
            {
                // Google casts each char to signed char, then to uint32.
                var v = unchecked((uint)(sbyte)t);

                b = (b * C1) + v;
                c ^= b;
            }

            return Mix(Mur(b, Mur((uint)s.Length, c)));
        }
    }

    /// <summary>
    /// Hashes 13 to 24 bytes by reading six evenly distributed 4-byte words across the input.
    /// </summary>
    /// <param name="s">The input span. Length must be in the range [13, 24].</param>
    /// <returns>The 32-bit hash value.</returns>
    private static uint Hash32Len13to24(ReadOnlySpan<byte> s)
    {
        var a = BinaryPrimitives.ReadUInt32LittleEndian(s.Slice((s.Length >> 1) - 4, 4));
        var b = BinaryPrimitives.ReadUInt32LittleEndian(s.Slice(4, 4));
        var c = BinaryPrimitives.ReadUInt32LittleEndian(s.Slice(s.Length - 8, 4));
        var d = BinaryPrimitives.ReadUInt32LittleEndian(s.Slice(s.Length >> 1, 4));
        var e = BinaryPrimitives.ReadUInt32LittleEndian(s.Slice(0, 4));
        var f = BinaryPrimitives.ReadUInt32LittleEndian(s.Slice(s.Length - 4, 4));
        var h = (uint)s.Length;

        return Mix(Mur(f, Mur(e, Mur(d, Mur(c, Mur(b, Mur(a, h)))))));
    }

    /// <summary>
    /// Hashes inputs of 25 bytes or more using three interleaved 32-bit accumulators over 20-byte iterative blocks.
    /// </summary>
    /// <param name="s">The input span. Length must be 25 or greater.</param>
    /// <returns>The 32-bit hash value.</returns>
    /// <remarks>
    /// The algorithm seeds three accumulators (<c>h</c>, <c>g</c>, <c>f</c>) from the tail of the input, then iterates
    /// over 20-byte blocks from the start. A three-way permutation is applied after each block to rotate accumulator
    /// roles. After the loop, each accumulator undergoes a double rotate-multiply finalization before the results are
    /// merged.
    /// </remarks>
    private static uint Hash32Len25Plus(ReadOnlySpan<byte> s)
    {
        var len = s.Length;

        // Seed accumulators from the tail of the input.
        var h = (uint)len;
        var g = h * C1;
        var f = g;

        var a0 = RotateMix32(BinaryPrimitives.ReadUInt32LittleEndian(s.Slice(len - 4, 4)));
        var a1 = RotateMix32(BinaryPrimitives.ReadUInt32LittleEndian(s.Slice(len - 8, 4)));
        var a2 = RotateMix32(BinaryPrimitives.ReadUInt32LittleEndian(s.Slice(len - 16, 4)));
        var a3 = RotateMix32(BinaryPrimitives.ReadUInt32LittleEndian(s.Slice(len - 12, 4)));
        var a4 = RotateMix32(BinaryPrimitives.ReadUInt32LittleEndian(s.Slice(len - 20, 4)));

        h ^= a0;
        h = (h.RotateBitsRightUnchecked(19) * 5) + HashMagic;
        h ^= a2;
        h = (h.RotateBitsRightUnchecked(19) * 5) + HashMagic;

        g ^= a1;
        g = (g.RotateBitsRightUnchecked(19) * 5) + HashMagic;
        g ^= a3;
        g = (g.RotateBitsRightUnchecked(19) * 5) + HashMagic;

        f += a4;
        f = (f.RotateBitsRightUnchecked(19) * 5) + HashMagic;

        // Iteratively consume 20-byte blocks from the start of the input.
        var iters = (len - 1) / 20;

        for (var i = 0; i < iters; i++)
        {
            var offset = i * 20;

            var b0 = RotateMix32(BinaryPrimitives.ReadUInt32LittleEndian(s.Slice(offset + 0, 4)));
            var b1 = BinaryPrimitives.ReadUInt32LittleEndian(s.Slice(offset + 4, 4));
            var b2 = RotateMix32(BinaryPrimitives.ReadUInt32LittleEndian(s.Slice(offset + 8, 4)));
            var b3 = RotateMix32(BinaryPrimitives.ReadUInt32LittleEndian(s.Slice(offset + 12, 4)));
            var b4 = BinaryPrimitives.ReadUInt32LittleEndian(s.Slice(offset + 16, 4));

            h ^= b0;
            h = (h.RotateBitsRightUnchecked(18) * 5) + HashMagic;

            f += b1;
            f = f.RotateBitsRightUnchecked(19) * C1;

            g += b2;
            g = (g.RotateBitsRightUnchecked(18) * 5) + HashMagic;

            h ^= b3 + b1;
            h = (h.RotateBitsRightUnchecked(19) * 5) + HashMagic;

            g ^= b4;
            g = BinaryPrimitives.ReverseEndianness(g) * 5;

            h += b4 * 5;
            h = BinaryPrimitives.ReverseEndianness(h);

            f += b0;

            // Rotate accumulator roles to prevent bias across successive blocks.
            Permute3(ref f, ref h, ref g);
        }

        // Finalize each accumulator with a double rotate-multiply pass.
        g = g.RotateBitsRightUnchecked(11) * C1;
        g = g.RotateBitsRightUnchecked(17) * C1;

        f = f.RotateBitsRightUnchecked(11) * C1;
        f = f.RotateBitsRightUnchecked(17) * C1;

        h = ((h + g).RotateBitsRightUnchecked(19) * 5) + HashMagic;
        h = h.RotateBitsRightUnchecked(17) * C1;
        h = ((h + f).RotateBitsRightUnchecked(19) * 5) + HashMagic;
        h = h.RotateBitsRightUnchecked(17) * C1;

        return h;
    }

    /// <summary>
    /// Hashes 5 to 12 bytes by reading 4-byte words from the start, end, and center of the input.
    /// </summary>
    /// <param name="s">The input span. Length must be in the range [5, 12].</param>
    /// <returns>The 32-bit hash value.</returns>
    private static uint Hash32Len5to12(ReadOnlySpan<byte> s)
    {
        var a = (uint)s.Length;
        var b = a * 5;
        uint c = 9;
        var d = b;

        a += BinaryPrimitives.ReadUInt32LittleEndian(s.Slice(0, 4));
        b += BinaryPrimitives.ReadUInt32LittleEndian(s.Slice(s.Length - 4, 4));
        c += BinaryPrimitives.ReadUInt32LittleEndian(s.Slice((s.Length >> 1) & 4, 4));

        return Mix(Mur(c, Mur(b, Mur(a, d))));
    }

    private static uint RotateMix32(uint value)
    {
        unchecked
        {
            return (value * C1).RotateBitsRightUnchecked(17) * C2;
        }
    }
}
