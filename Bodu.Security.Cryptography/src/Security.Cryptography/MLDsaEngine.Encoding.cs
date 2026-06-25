// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLDsaEngine.Encoding.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides the ML-DSA bit-packing codecs and the hint encoding (FIPS 204 Algorithms 16–21).
/// </summary>
internal static partial class MLDsaEngine
{
    /// <summary>
    /// Serializes 256 unsigned coefficients of the given bit width in little-endian bit order (FIPS 204 SimpleBitPack).
    /// </summary>
    /// <param name="bits">The bit width of each coefficient.</param>
    /// <param name="coefficients">The 256 coefficients, each below 2^bits.</param>
    /// <param name="destination">The span receiving 32·bits bytes.</param>
    private static void SimpleBitPack(int bits, ReadOnlySpan<int> coefficients, Span<byte> destination)
    {
        ulong accumulator = 0;
        int bitCount = 0;
        int index = 0;

        for (int i = 0; i < N; i++)
        {
            accumulator |= (ulong)(uint)coefficients[i] << bitCount;
            bitCount += bits;

            while (bitCount >= 8)
            {
                destination[index++] = (byte)accumulator;
                accumulator >>= 8;
                bitCount -= 8;
            }
        }
    }

    /// <summary>
    /// Deserializes 32·bits bytes into 256 unsigned coefficients in little-endian bit order (FIPS 204 SimpleBitUnpack).
    /// </summary>
    /// <param name="bits">The bit width of each coefficient.</param>
    /// <param name="source">The 32·bits source bytes.</param>
    /// <param name="destination">The span receiving the 256 coefficients.</param>
    private static void SimpleBitUnpack(int bits, ReadOnlySpan<byte> source, Span<int> destination)
    {
        uint mask = (1u << bits) - 1;
        ulong accumulator = 0;
        int bitCount = 0;
        int index = 0;

        for (int i = 0; i < N; i++)
        {
            while (bitCount < bits)
            {
                accumulator |= (ulong)source[index++] << bitCount;
                bitCount += 8;
            }

            destination[i] = (int)(accumulator & mask);
            accumulator >>= bits;
            bitCount -= bits;
        }
    }

    /// <summary>
    /// Serializes 256 centered coefficients by packing <paramref name="bound" /> − centered(coefficient) in the given
    /// bit width (FIPS 204 BitPack with b = <paramref name="bound" />).
    /// </summary>
    /// <param name="bits">The bit width of each packed value.</param>
    /// <param name="bound">The upper bound b of the centered range [b − 2^bits + 1, b].</param>
    /// <param name="coefficients">The 256 coefficients in [0, q) holding centered values folded modulo q.</param>
    /// <param name="destination">The span receiving 32·bits bytes.</param>
    private static void BitPackSigned(int bits, int bound, ReadOnlySpan<int> coefficients, Span<byte> destination)
    {
        Span<int> packed = stackalloc int[N];
        for (int i = 0; i < N; i++)
        {
            int centered = coefficients[i];
            if (centered > (Q - 1) / 2)
                centered -= Q;

            packed[i] = bound - centered;
        }

        SimpleBitPack(bits, packed, destination);
        CryptographyHelper.Clear(packed);
    }

    /// <summary>
    /// Deserializes 32·bits bytes into 256 centered coefficients folded into [0, q) (FIPS 204 BitUnpack with b =
    /// <paramref name="bound" />).
    /// </summary>
    /// <param name="bits">The bit width of each packed value.</param>
    /// <param name="bound">The upper bound b of the centered range.</param>
    /// <param name="source">The 32·bits source bytes.</param>
    /// <param name="destination">The span receiving the 256 coefficients in [0, q).</param>
    /// <remarks>
    /// Used only for full-range packings (z at the γ₁ width, t₀ at the d width) where every <paramref name="bits" />-bit
    /// code point maps to a valid coefficient; bounded fields use <see cref="TryBitUnpackSigned" /> to reject
    /// non-canonical encodings.
    /// </remarks>
    private static void BitUnpackSigned(int bits, int bound, ReadOnlySpan<byte> source, Span<int> destination) =>
        _ = TryBitUnpackSigned(bits, bound, (1 << bits) - 1, source, destination);

    /// <summary>
    /// Deserializes 32·bits bytes into 256 centered coefficients folded into [0, q) (FIPS 204 BitUnpack with b =
    /// <paramref name="bound" />), rejecting any packed code point that exceeds <paramref name="maxEncoded" />.
    /// </summary>
    /// <param name="bits">The bit width of each packed value.</param>
    /// <param name="bound">The upper bound b of the centered range.</param>
    /// <param name="maxEncoded">
    /// The largest packed value that encodes a valid coefficient; larger code points are non-canonical.
    /// </param>
    /// <param name="source">The 32·bits source bytes.</param>
    /// <param name="destination">The span receiving the 256 coefficients in [0, q).</param>
    /// <returns>
    /// <see langword="true" /> when every packed value is within range; otherwise, <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// The whole vector is unpacked regardless of any out-of-range code point so the running time reveals only that a
    /// canonical-encoding check occurred, not which coefficient — if any — drove the rejection.
    /// </remarks>
    private static bool TryBitUnpackSigned(int bits, int bound, int maxEncoded, ReadOnlySpan<byte> source, Span<int> destination)
    {
        SimpleBitUnpack(bits, source, destination);

        bool valid = true;
        for (int i = 0; i < N; i++)
        {
            int raw = destination[i];
            valid &= raw <= maxEncoded;

            int centered = bound - raw;
            destination[i] = ((centered % Q) + Q) % Q;
        }

        return valid;
    }

    /// <summary>
    /// Serializes the hint vector into ω + k bytes: the ascending hint positions per polynomial followed by the running
    /// cumulative counts (FIPS 204 Algorithm 20 / HintBitPack).
    /// </summary>
    /// <param name="parameters">The parameter set supplying ω and k.</param>
    /// <param name="hints">The k hint polynomials with entries 0 or 1.</param>
    /// <param name="destination">The span receiving ω + k bytes.</param>
    private static void HintBitPack(MLDsaParameters parameters, int[][] hints, Span<byte> destination)
    {
        destination.Clear();

        int index = 0;
        for (int i = 0; i < parameters.K; i++)
        {
            for (int j = 0; j < N; j++)
            {
                if (hints[i][j] != 0)
                    destination[index++] = (byte)j;
            }

            destination[parameters.Omega + i] = (byte)index;
        }
    }

    /// <summary>
    /// Deserializes and strictly validates a hint encoding (FIPS 204 Algorithm 21 / HintBitUnpack): cumulative counts
    /// must be monotone and bounded by ω, positions within each polynomial must be strictly ascending, and all unused
    /// position bytes must be zero.
    /// </summary>
    /// <param name="parameters">The parameter set supplying ω and k.</param>
    /// <param name="source">The ω + k encoded bytes.</param>
    /// <param name="hints">The k hint polynomials receiving entries of 0 or 1.</param>
    /// <returns><see langword="true" /> when the encoding is canonical; otherwise, <see langword="false" />.</returns>
    private static bool TryHintBitUnpack(MLDsaParameters parameters, ReadOnlySpan<byte> source, int[][] hints)
    {
        int index = 0;
        for (int i = 0; i < parameters.K; i++)
        {
            Array.Clear(hints[i]);

            int limit = source[parameters.Omega + i];
            if (limit < index || limit > parameters.Omega)
                return false;

            int first = index;
            while (index < limit)
            {
                if (index > first && source[index] <= source[index - 1])
                    return false;

                hints[i][source[index]] = 1;
                index++;
            }
        }

        // Any trailing position bytes beyond the final count must be zero for the encoding to be canonical.
        for (int j = index; j < parameters.Omega; j++)
        {
            if (source[j] != 0)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Encodes the high-bits commitment w₁ for hashing: SimpleBitPack of every polynomial at the parameter set's w₁
    /// width (FIPS 204 Algorithm 28 / w1Encode).
    /// </summary>
    /// <param name="parameters">The parameter set supplying the packing width and k.</param>
    /// <param name="w1">The k high-bits polynomials.</param>
    /// <param name="destination">The span receiving 32·bits·k bytes.</param>
    private static void W1Encode(MLDsaParameters parameters, int[][] w1, Span<byte> destination)
    {
        int bytesPerPoly = 32 * parameters.W1Bits;
        for (int i = 0; i < parameters.K; i++)
            SimpleBitPack(parameters.W1Bits, w1[i], destination.Slice(i * bytesPerPoly, bytesPerPoly));
    }
}
