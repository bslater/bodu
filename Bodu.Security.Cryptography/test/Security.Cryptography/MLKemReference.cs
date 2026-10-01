// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLKemReference.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides the ML-KEM arithmetic that <see cref="MLKemEngine" />'s Montgomery and Barrett reductions replaced - every
/// coefficient reduced with the remainder operator, and the binomial noise counted a bit at a time - kept as an
/// independent oracle that the engine's arithmetic tests hold it to.
/// </summary>
internal static class MLKemReference
{
    /// <summary>The polynomial degree n = 256.</summary>
    private const int N = 256;

    /// <summary>The coefficient modulus q = 3329.</summary>
    private const int Q = 3329;

    /// <summary>The twiddle factors ζ^BitRev₇(i) mod q, in plain form.</summary>
    private static readonly int[] s_zetas = BuildTable(i => LatticeCommon.BitReverse(i, 7));

    /// <summary>The base-case multipliers ζ^(2·BitRev₇(i) + 1) mod q.</summary>
    private static readonly int[] s_gammas = BuildTable(i => (2 * LatticeCommon.BitReverse(i, 7)) + 1);

    /// <summary>
    /// Applies the forward NTT (FIPS 203 Algorithm 9) in place, reducing every step with the remainder operator.
    /// </summary>
    /// <param name="f">The 256 coefficients in [0, q).</param>
    internal static void Ntt(Span<int> f)
    {
        int i = 1;
        for (int len = 128; len >= 2; len >>= 1)
        {
            for (int start = 0; start < N; start += 2 * len)
            {
                int zeta = s_zetas[i++];
                for (int j = start; j < start + len; j++)
                {
                    int t = (zeta * f[j + len]) % Q;
                    f[j + len] = (f[j] - t + Q) % Q;
                    f[j] = (f[j] + t) % Q;
                }
            }
        }
    }

    /// <summary>
    /// Applies the inverse NTT (FIPS 203 Algorithm 10) in place, reducing every step with the remainder operator.
    /// </summary>
    /// <param name="f">The 256 NTT coefficients in [0, q).</param>
    internal static void InvNtt(Span<int> f)
    {
        int i = 127;
        for (int len = 2; len <= 128; len <<= 1)
        {
            for (int start = 0; start < N; start += 2 * len)
            {
                int zeta = s_zetas[i--];
                for (int j = start; j < start + len; j++)
                {
                    int t = f[j];
                    f[j] = (t + f[j + len]) % Q;
                    f[j + len] = (zeta * ((f[j + len] - t + Q) % Q)) % Q;
                }
            }
        }

        for (int j = 0; j < N; j++)
            f[j] = (f[j] * 3303) % Q;
    }

    /// <summary>
    /// Multiplies two NTT-domain polynomials (FIPS 203 Algorithms 11-12), reducing with the remainder operator.
    /// </summary>
    /// <param name="left">The first polynomial.</param>
    /// <param name="right">The second polynomial.</param>
    /// <param name="destination">The span receiving the product.</param>
    internal static void MultiplyNtt(ReadOnlySpan<int> left, ReadOnlySpan<int> right, Span<int> destination)
    {
        for (int i = 0; i < 128; i++)
        {
            int a0 = left[2 * i];
            int a1 = left[(2 * i) + 1];
            int b0 = right[2 * i];
            int b1 = right[(2 * i) + 1];

            destination[2 * i] = (int)((((long)a0 * b0) + ((((long)a1 * b1) % Q) * s_gammas[i])) % Q);
            destination[(2 * i) + 1] = (int)((((long)a0 * b1) + ((long)a1 * b0)) % Q);
        }
    }

    /// <summary>
    /// Samples a centered-binomial polynomial (FIPS 203 Algorithm 8) from SHAKE256(seed ‖ counter), counting the stream
    /// a bit at a time.
    /// </summary>
    /// <param name="eta">The CBD parameter η.</param>
    /// <param name="seed">The 32-byte seed.</param>
    /// <param name="counter">The counter byte.</param>
    /// <param name="destination">The span receiving 256 coefficients in [0, q).</param>
    internal static void SamplePolyCbd(int eta, ReadOnlySpan<byte> seed, byte counter, Span<int> destination)
    {
        byte[] bytes = new byte[64 * eta];
        KeccakSponge.Shake256(seed, [counter], bytes);

        for (int i = 0; i < N; i++)
        {
            int positive = 0;
            int negative = 0;

            for (int j = 0; j < eta; j++)
            {
                positive += GetBit(bytes, (2 * i * eta) + j);
                negative += GetBit(bytes, (2 * i * eta) + eta + j);
            }

            destination[i] = (positive - negative + Q) % Q;
        }
    }

    /// <summary>
    /// Reads one bit of a little-endian bit stream.
    /// </summary>
    /// <param name="bytes">The stream.</param>
    /// <param name="bitIndex">The bit's index.</param>
    /// <returns>0 or 1.</returns>
    private static int GetBit(ReadOnlySpan<byte> bytes, int bitIndex) =>
        (bytes[bitIndex >> 3] >> (bitIndex & 7)) & 1;

    /// <summary>
    /// Builds a 128-entry table of powers of ζ = 17 modulo q.
    /// </summary>
    /// <param name="exponent">The exponent of each entry.</param>
    /// <returns>The table.</returns>
    private static int[] BuildTable(Func<int, int> exponent)
    {
        int[] table = new int[128];
        for (int i = 0; i < 128; i++)
            table[i] = LatticeCommon.PowMod(17, exponent(i), Q);

        return table;
    }
}
