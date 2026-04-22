// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CityHash.64.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Extensions;
using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Computes a 64-bit (8-byte) non-cryptographic hash using the <c>CityHash64</c> variant by Google. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="CityHash64" /> selects one of four internal mixing paths depending on the input length: a compact path for 0–16 bytes,
/// a four-word path for 17–32 bytes, an eight-word path with byte-swap finalisation for 33–64 bytes, and a full iterative path that
/// consumes 64-byte blocks using two pairs of seeded weak hash accumulators for inputs of 65 bytes or more. All paths converge through
/// the internal <c>HashLen16</c> finaliser, which applies two rounds of multiply-shift-XOR to distribute entropy across all output bits.
/// </para>
/// <note type="important">
/// This algorithm is <b>not</b> cryptographically secure and must <b>not</b> be used for password hashing, digital signatures, or any
/// application requiring adversarial collision resistance.
/// </note>
/// </remarks>
public sealed class CityHash64
    : CityHash<CityHash64>
{
    /// <summary>
    /// The prime multiplier used in the 64-bit <c>HashLen16</c> finalisation step.
    /// </summary>
    private const ulong KMul = 0x9ddfea08eb382d69UL;

    /// <summary>
    /// Initialises a new instance of the <see cref="CityHash64" /> class with a fixed 64-bit (8-byte) hash output size.
    /// </summary>
    public CityHash64()
        : base(64)
    {
    }

    /// <summary>
    /// Computes the 64-bit CityHash of the provided input span, selecting the optimal mixing path based on input length.
    /// </summary>
    /// <param name="source">The input bytes to hash.</param>
    /// <returns>An 8-byte array containing the little-endian encoded 64-bit hash value.</returns>
    protected override byte[] ComputeHashCore(ReadOnlySpan<byte> source)
    {
        ulong result = source.Length switch
        {
            <= 16 => Hash64Len0to16(source),
            <= 32 => Hash64Len17to32(source),
            <= 64 => Hash64Len33to64(source),
            _ => Hash64Long(source)
        };

        byte[] buffer = new byte[8];
        BinaryPrimitives.WriteUInt64LittleEndian(buffer, result);
        return buffer;
    }

    /// <summary>
    /// Applies a final 64-bit entropy spreading step by XOR-ing the value with its own upper half-shift.
    /// </summary>
    /// <param name="val">The 64-bit value to mix.</param>
    /// <returns>The mixed 64-bit result.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong ShiftMix(ulong val) =>
        val ^ (val >> 47);

    /// <summary>
    /// Combines two 64-bit values into a single 64-bit hash using the default <see cref="KMul" /> multiplier.
    /// </summary>
    /// <param name="u">The first input value.</param>
    /// <param name="v">The second input value.</param>
    /// <returns>The combined 64-bit hash value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong HashLen16(ulong u, ulong v) =>
        HashLen16(u, v, KMul);

    /// <summary>
    /// Combines two 64-bit values into a single 64-bit hash using a caller-supplied multiplier. Applies two rounds of
    /// multiply-shift-XOR to thoroughly distribute entropy across all output bits.
    /// </summary>
    /// <param name="u">The first input value.</param>
    /// <param name="v">The second input value.</param>
    /// <param name="mul">The multiplier to apply during mixing. Typically a large odd prime.</param>
    /// <returns>The combined 64-bit hash value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong HashLen16(ulong u, ulong v, ulong mul)
    {
        ulong a = (u ^ v) * mul;
        a ^= a >> 47;

        ulong b = (v ^ a) * mul;
        b ^= b >> 47;

        return b * mul;
    }

    /// <summary>
    /// Hashes 0 to 16 bytes. Selects a byte-, word-, or double-word path based on the exact input length.
    /// </summary>
    /// <param name="s">The input span. Length must be in the range [0, 16].</param>
    /// <returns>The 64-bit hash value.</returns>
    /// <remarks>
    /// For inputs of 8 bytes or more, two overlapping 64-bit words are read and mixed with the length-adjusted <see cref="K2" />
    /// constant. For 4–7 bytes, two 32-bit words span the input. For 1–3 bytes, individual bytes seed a <see cref="ShiftMix" /> step.
    /// An empty input returns <see cref="CityHash{T}.K2" /> directly.
    /// </remarks>
    private static ulong Hash64Len0to16(ReadOnlySpan<byte> s)
    {
        int len = s.Length;

        if (len >= 8)
        {
            ulong mul = K2 + (ulong)(len * 2);
            ulong a = BinaryPrimitives.ReadUInt64LittleEndian(s) + K2;
            ulong b = BinaryPrimitives.ReadUInt64LittleEndian(s.Slice(len - 8));
            ulong c = b.RotateBitsRightUnchecked(37) * mul + a;
            ulong d = (a.RotateBitsRightUnchecked(25) + b) * mul;
            return HashLen16(c, d, mul);
        }

        if (len >= 4)
        {
            ulong mul = K2 + (ulong)(len * 2);
            ulong a = BinaryPrimitives.ReadUInt32LittleEndian(s);
            return HashLen16((ulong)len + (a << 3), BinaryPrimitives.ReadUInt32LittleEndian(s.Slice(len - 4)), mul);
        }

        if (len > 0)
        {
            byte a = s[0];
            byte b = s[len >> 1];
            byte c = s[len - 1];
            uint y = (uint)a + ((uint)b << 8);
            uint z = (uint)len + ((uint)c << 2);
            return ShiftMix((ulong)y * K2 ^ (ulong)z * K0) * K2;
        }

        // An empty input is defined to return K2.
        return K2;
    }

    /// <summary>
    /// Hashes 17 to 32 bytes by reading four 64-bit words from the start and end of the input.
    /// </summary>
    /// <param name="s">The input span. Length must be in the range [17, 32].</param>
    /// <returns>The 64-bit hash value.</returns>
    private static ulong Hash64Len17to32(ReadOnlySpan<byte> s)
    {
        int len = s.Length;
        ulong mul = K2 + (ulong)(len * 2);

        ulong a = BinaryPrimitives.ReadUInt64LittleEndian(s) * K1;
        ulong b = BinaryPrimitives.ReadUInt64LittleEndian(s.Slice(8));
        ulong c = BinaryPrimitives.ReadUInt64LittleEndian(s.Slice(len - 8)) * mul;
        ulong d = BinaryPrimitives.ReadUInt64LittleEndian(s.Slice(len - 16)) * K2;

        return HashLen16(
            u: (a + b).RotateBitsRightUnchecked(43) + c.RotateBitsRightUnchecked(30) + d,
            v: a + (b + K2).RotateBitsRightUnchecked(18) + c,
            mul: mul);
    }

    /// <summary>
    /// Hashes 33 to 64 bytes by reading eight 64-bit words spread across the full input span, including a byte-swap finalisation step.
    /// </summary>
    /// <param name="s">The input span. Length must be in the range [33, 64].</param>
    /// <returns>The 64-bit hash value.</returns>
    private static ulong Hash64Len33to64(ReadOnlySpan<byte> s)
    {
        int len = s.Length;
        ulong mul = K2 + (ulong)(len * 2);

        ulong a = BinaryPrimitives.ReadUInt64LittleEndian(s) * K2;
        ulong b = BinaryPrimitives.ReadUInt64LittleEndian(s.Slice(8));
        ulong c = BinaryPrimitives.ReadUInt64LittleEndian(s.Slice(len - 24));
        ulong d = BinaryPrimitives.ReadUInt64LittleEndian(s.Slice(len - 32));
        ulong e = BinaryPrimitives.ReadUInt64LittleEndian(s.Slice(16)) * K2;
        ulong f = BinaryPrimitives.ReadUInt64LittleEndian(s.Slice(24)) * 9;
        ulong g = BinaryPrimitives.ReadUInt64LittleEndian(s.Slice(len - 8));
        ulong h = BinaryPrimitives.ReadUInt64LittleEndian(s.Slice(len - 16)) * mul;

        ulong u = (a + g).RotateBitsRightUnchecked(43) + (b.RotateBitsRightUnchecked(30) + c) * 9;
        ulong v = ((a + g) ^ d) + f + 1;
        ulong w = (u + v).ReverseBytesUnchecked() * mul;
        ulong x = (e + f).RotateBitsRightUnchecked(42) + c;
        ulong y = ((v + w).ReverseBytesUnchecked() + h) * mul;
        ulong z = e + f + c;

        a = ((x + z) * mul + y).ReverseBytesUnchecked() + b;
        b = ShiftMix((z + a) * mul + d + h) * mul;

        return b + x;
    }

    /// <summary>
    /// Computes a weak 64-bit hash of 32 bytes using six provided seed values, returning a pair of independent 64-bit outputs.
    /// </summary>
    /// <param name="w">The first 64-bit word of the input block.</param>
    /// <param name="x">The second 64-bit word of the input block.</param>
    /// <param name="y">The third 64-bit word of the input block.</param>
    /// <param name="z">The fourth 64-bit word of the input block.</param>
    /// <param name="a">The first accumulator seed.</param>
    /// <param name="b">The second accumulator seed.</param>
    /// <returns>
    /// A tuple containing two independent 64-bit hash values derived from the mixed input and seeds.
    /// </returns>
    /// <remarks>
    /// This helper is used by both <see cref="Hash64Len33to64" /> and the iterative loop in <see cref="Hash64Long" /> to process
    /// 32-byte halves of each 64-byte block. Each call folds all four input words and both seeds into two output values using
    /// additions and a single rotation.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static (ulong First, ulong Second) WeakHashLen32WithSeeds(
        ulong w, ulong x, ulong y, ulong z, ulong a, ulong b)
    {
        a += w;
        b = (b + a + z).RotateBitsRightUnchecked(21);

        ulong c = a;
        a += x;
        a += y;
        b += a.RotateBitsRightUnchecked(44);

        return (a + z, b + c);
    }

    /// <summary>
    /// Reads four consecutive 64-bit little-endian words from the specified span and forwards them to
    /// <see cref="WeakHashLen32WithSeeds(ulong, ulong, ulong, ulong, ulong, ulong)" /> along with the provided seeds.
    /// </summary>
    /// <param name="s">The input span. Must contain at least 32 bytes starting at offset 0.</param>
    /// <param name="a">The first accumulator seed.</param>
    /// <param name="b">The second accumulator seed.</param>
    /// <returns>
    /// A tuple containing two independent 64-bit hash values derived from the 32-byte block and seeds.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static (ulong First, ulong Second) WeakHashLen32WithSeeds(
        ReadOnlySpan<byte> s, ulong a, ulong b) =>
        WeakHashLen32WithSeeds(
            BinaryPrimitives.ReadUInt64LittleEndian(s),
            BinaryPrimitives.ReadUInt64LittleEndian(s.Slice(8)),
            BinaryPrimitives.ReadUInt64LittleEndian(s.Slice(16)),
            BinaryPrimitives.ReadUInt64LittleEndian(s.Slice(24)),
            a, b);

    /// <summary>
    /// Hashes inputs of 65 bytes or more using two pairs of seeded <see cref="WeakHashLen32WithSeeds" /> accumulators that consume
    /// the input in 64-byte blocks.
    /// </summary>
    /// <param name="s">The input span. Length must be 65 or greater.</param>
    /// <returns>The 64-bit hash value.</returns>
    /// <remarks>
    /// <para>
    /// The method seeds the four accumulators (<c>v</c>, <c>w</c>) and the three mixing variables (<c>x</c>, <c>y</c>, <c>z</c>)
    /// from the tail of the input before processing, ensuring that long and short inputs produce well-distributed results.
    /// </para>
    /// <para>
    /// Each 64-byte iteration updates all five variables and swaps <c>x</c> and <c>z</c> to prevent positional bias. The final
    /// result combines both accumulator pairs through two nested <see cref="HashLen16" /> calls.
    /// </para>
    /// </remarks>
    private static ulong Hash64Long(ReadOnlySpan<byte> s)
    {
        int len = s.Length;

        // Seed the mixing variables and accumulators from the tail of the input so that length
        // differences always produce distinct starting states, regardless of head content.
        ulong x = BinaryPrimitives.ReadUInt64LittleEndian(s.Slice(len - 40));
        ulong y = BinaryPrimitives.ReadUInt64LittleEndian(s.Slice(len - 16))
                + BinaryPrimitives.ReadUInt64LittleEndian(s.Slice(len - 56));
        ulong z = HashLen16(
            BinaryPrimitives.ReadUInt64LittleEndian(s.Slice(len - 48)) + (ulong)len,
            BinaryPrimitives.ReadUInt64LittleEndian(s.Slice(len - 24)));

        var (v0, v1) = WeakHashLen32WithSeeds(s.Slice(len - 64), (ulong)len, z);
        var (w0, w1) = WeakHashLen32WithSeeds(s.Slice(len - 32), y + K1, x);

        // Fold the first 8 bytes of the head into x to anchor the start of the input.
        x = x * K1 + BinaryPrimitives.ReadUInt64LittleEndian(s);

        // Align remaining length down to a 64-byte boundary for the main loop.
        int remaining = (len - 1) & ~63;
        int offset = 0;

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
            HashLen16(v0, w0) + ShiftMix(y) * K1 + z,
            HashLen16(v1, w1) + x);
    }
}
