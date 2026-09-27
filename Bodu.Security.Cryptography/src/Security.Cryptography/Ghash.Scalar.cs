// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Ghash.Scalar.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace Bodu.Security.Cryptography;

internal static partial class Ghash
{
    /// <summary>
    /// Folds <paramref name="data" /> into <paramref name="state" /> with the portable scalar multiply.
    /// </summary>
    /// <param name="key">The prepared key.</param>
    /// <param name="state">The 16-byte running state, in the function's byte order; updated in place.</param>
    /// <param name="data">The data to fold in; a final partial block is padded with zeros.</param>
    /// <remarks>
    /// <para>
    /// This is the constant-time 64-bit method of Thomas Pornin's BearSSL (<c>ghash_ctmul64</c>). Each 64 × 64-bit
    /// carry-less product comes from <see cref="MultiplyCarryless" />, which uses ordinary integer multiplications on
    /// operands whose set bits are four apart, so every carry either lands in a masked-off bit or leaves the word. The
    /// high half of each product is the low half of the product of the bit-reversed operands, reversed back. Karatsuba
    /// takes the 128-bit product from three such pairs, and the result is shifted and reduced as in the carry-less
    /// kernels.
    /// </para>
    /// <para>
    /// Integer multiplication runs in time independent of its operands on the processors this library targets, and
    /// nothing here branches on or indexes by data.
    /// </para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    [SkipLocalsInit]
    private static void UpdateScalar(in Key key, Span<byte> state, ReadOnlySpan<byte> data)
    {
        bool polyval = key.IsPolyval;

        // Hold the state as the GHASH-domain element's big-endian halves; POLYVAL's state is its byte reversal.
        ulong y1 = polyval ? BinaryPrimitives.ReadUInt64LittleEndian(state[8..]) : BinaryPrimitives.ReadUInt64BigEndian(state);
        ulong y0 = polyval ? BinaryPrimitives.ReadUInt64LittleEndian(state) : BinaryPrimitives.ReadUInt64BigEndian(state[8..]);

        ulong h1 = key.High;
        ulong h0 = key.Low;
        ulong h0r = ReverseBits(h0);
        ulong h1r = ReverseBits(h1);
        ulong h2 = h0 ^ h1;
        ulong h2r = h0r ^ h1r;

        Span<byte> last = stackalloc byte[BlockBytes];
        int offset = 0;

        while (offset < data.Length)
        {
            scoped ReadOnlySpan<byte> block;
            if (data.Length - offset >= BlockBytes)
            {
                block = data.Slice(offset, BlockBytes);
            }
            else
            {
                last.Clear();
                data[offset..].CopyTo(last);
                block = last;
            }

            offset += BlockBytes;

            if (polyval)
            {
                y1 ^= BinaryPrimitives.ReadUInt64LittleEndian(block[8..]);
                y0 ^= BinaryPrimitives.ReadUInt64LittleEndian(block);
            }
            else
            {
                y1 ^= BinaryPrimitives.ReadUInt64BigEndian(block);
                y0 ^= BinaryPrimitives.ReadUInt64BigEndian(block[8..]);
            }

            // Karatsuba over the 64-bit halves: the low halves of the three products directly, the high halves through
            // the bit-reversed operands.
            ulong y0r = ReverseBits(y0);
            ulong y1r = ReverseBits(y1);
            ulong y2 = y0 ^ y1;
            ulong y2r = y0r ^ y1r;

            ulong z0 = MultiplyCarryless(y0, h0);
            ulong z1 = MultiplyCarryless(y1, h1);
            ulong z2 = MultiplyCarryless(y2, h2);
            ulong z0h = MultiplyCarryless(y0r, h0r);
            ulong z1h = MultiplyCarryless(y1r, h1r);
            ulong z2h = MultiplyCarryless(y2r, h2r);
            z2 ^= z0 ^ z1;
            z2h ^= z0h ^ z1h;
            z0h = ReverseBits(z0h) >> 1;
            z1h = ReverseBits(z1h) >> 1;
            z2h = ReverseBits(z2h) >> 1;

            ulong v0 = z0;
            ulong v1 = z0h ^ z2;
            ulong v2 = z1 ^ z2h;
            ulong v3 = z1h;

            // Shift the 256-bit product left one bit for the reflected bit order, then reduce.
            v3 = (v3 << 1) | (v2 >> 63);
            v2 = (v2 << 1) | (v1 >> 63);
            v1 = (v1 << 1) | (v0 >> 63);
            v0 <<= 1;

            v2 ^= v0 ^ (v0 >> 1) ^ (v0 >> 2) ^ (v0 >> 7);
            v1 ^= (v0 << 63) ^ (v0 << 62) ^ (v0 << 57);
            v3 ^= v1 ^ (v1 >> 1) ^ (v1 >> 2) ^ (v1 >> 7);
            v2 ^= (v1 << 63) ^ (v1 << 62) ^ (v1 << 57);

            y0 = v2;
            y1 = v3;
        }

        CryptographyHelper.Clear(last);

        if (polyval)
        {
            BinaryPrimitives.WriteUInt64LittleEndian(state, y0);
            BinaryPrimitives.WriteUInt64LittleEndian(state[8..], y1);
        }
        else
        {
            BinaryPrimitives.WriteUInt64BigEndian(state, y1);
            BinaryPrimitives.WriteUInt64BigEndian(state[8..], y0);
        }
    }

    /// <summary>
    /// Returns the low 64 bits of the carry-less product of two 64-bit values, using integer multiplications.
    /// </summary>
    /// <param name="x">The first factor.</param>
    /// <param name="y">The second factor.</param>
    /// <returns>The low 64 bits of the carry-less product.</returns>
    /// <remarks>
    /// Each factor is split into four parts whose set bits are four positions apart. Multiplying two parts sums, at
    /// every position of the result class, at most sixteen one-bit products; the only column that can reach sixteen is
    /// the last, whose carry leaves the 64-bit word, so every column's lowest bit is its carry-less sum. Masking keeps
    /// those bits.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong MultiplyCarryless(ulong x, ulong y)
    {
        ulong x0 = x & 0x1111111111111111UL;
        ulong x1 = x & 0x2222222222222222UL;
        ulong x2 = x & 0x4444444444444444UL;
        ulong x3 = x & 0x8888888888888888UL;
        ulong y0 = y & 0x1111111111111111UL;
        ulong y1 = y & 0x2222222222222222UL;
        ulong y2 = y & 0x4444444444444444UL;
        ulong y3 = y & 0x8888888888888888UL;

        ulong z0 = (x0 * y0) ^ (x1 * y3) ^ (x2 * y2) ^ (x3 * y1);
        ulong z1 = (x0 * y1) ^ (x1 * y0) ^ (x2 * y3) ^ (x3 * y2);
        ulong z2 = (x0 * y2) ^ (x1 * y1) ^ (x2 * y0) ^ (x3 * y3);
        ulong z3 = (x0 * y3) ^ (x1 * y2) ^ (x2 * y1) ^ (x3 * y0);

        return (z0 & 0x1111111111111111UL)
            | (z1 & 0x2222222222222222UL)
            | (z2 & 0x4444444444444444UL)
            | (z3 & 0x8888888888888888UL);
    }

    /// <summary>
    /// Reverses the order of the 64 bits of a value.
    /// </summary>
    /// <param name="value">The value to reverse.</param>
    /// <returns>The bit-reversed value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong ReverseBits(ulong value)
    {
        value = ((value & 0x5555555555555555UL) << 1) | ((value >> 1) & 0x5555555555555555UL);
        value = ((value & 0x3333333333333333UL) << 2) | ((value >> 2) & 0x3333333333333333UL);
        value = ((value & 0x0F0F0F0F0F0F0F0FUL) << 4) | ((value >> 4) & 0x0F0F0F0F0F0F0F0FUL);
        return BinaryPrimitives.ReverseEndianness(value);
    }
}
