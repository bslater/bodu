// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLDsaReference.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides the ML-DSA arithmetic that <see cref="MLDsaEngine" />'s Montgomery reduction and multiply-and-shift
/// decomposition replaced - every coefficient reduced with the remainder operator, and the decomposition computed by
/// division - kept as an independent oracle that the engine's arithmetic tests hold it to.
/// </summary>
internal static class MLDsaReference
{
    /// <summary>The polynomial degree n = 256.</summary>
    private const int N = 256;

    /// <summary>The coefficient modulus q = 8380417.</summary>
    private const int Q = 8380417;

    /// <summary>The twiddle factors ζ^BitRev₈(m) mod q, in plain form.</summary>
    private static readonly int[] s_zetas = BuildZetaTable();

    /// <summary>
    /// Applies the forward NTT (FIPS 204 Algorithm 41) in place, reducing every step with the remainder operator.
    /// </summary>
    /// <param name="w">The 256 coefficients in [0, q).</param>
    internal static void Ntt(Span<int> w)
    {
        int m = 0;
        for (int len = 128; len >= 1; len >>= 1)
        {
            for (int start = 0; start < N; start += 2 * len)
            {
                int zeta = s_zetas[++m];
                for (int j = start; j < start + len; j++)
                {
                    int t = (int)(((long)zeta * w[j + len]) % Q);
                    w[j + len] = (w[j] - t + Q) % Q;
                    w[j] = (w[j] + t) % Q;
                }
            }
        }
    }

    /// <summary>
    /// Applies the inverse NTT (FIPS 204 Algorithm 42) in place, reducing every step with the remainder operator.
    /// </summary>
    /// <param name="w">The 256 NTT coefficients in [0, q).</param>
    internal static void InvNtt(Span<int> w)
    {
        int m = 256;
        for (int len = 1; len < N; len <<= 1)
        {
            for (int start = 0; start < N; start += 2 * len)
            {
                int zeta = s_zetas[--m];
                for (int j = start; j < start + len; j++)
                {
                    int t = w[j];
                    w[j] = (t + w[j + len]) % Q;
                    w[j + len] = (int)(((long)zeta * ((w[j + len] - t + Q) % Q)) % Q);
                }
            }
        }

        for (int j = 0; j < N; j++)
            w[j] = (int)((w[j] * 8347681L) % Q);
    }

    /// <summary>
    /// Multiplies two NTT-domain polynomials coefficient-wise (FIPS 204 Algorithm 45) with the remainder operator.
    /// </summary>
    /// <param name="left">The first polynomial, coefficients in [0, q).</param>
    /// <param name="right">The second polynomial, coefficients in [0, q).</param>
    /// <param name="destination">The span receiving the product in [0, q).</param>
    internal static void MultiplyNtt(ReadOnlySpan<int> left, ReadOnlySpan<int> right, Span<int> destination)
    {
        for (int i = 0; i < N; i++)
            destination[i] = (int)(((long)left[i] * right[i]) % Q);
    }

    /// <summary>
    /// Decomposes a coefficient relative to α = 2γ₂ (FIPS 204 Algorithm 36) by division.
    /// </summary>
    /// <param name="gamma2">The parameter γ₂.</param>
    /// <param name="r">The coefficient in [0, q).</param>
    /// <param name="r1">Receives the high part.</param>
    /// <param name="r0">Receives the centered low part.</param>
    internal static void Decompose(int gamma2, int r, out int r1, out int r0)
    {
        int alpha = 2 * gamma2;

        r0 = r % alpha;
        if (r0 > gamma2)
            r0 -= alpha;

        if (r - r0 == Q - 1)
        {
            r1 = 0;
            r0--;
            return;
        }

        r1 = (r - r0) / alpha;
    }

    /// <summary>
    /// Returns the high part of a coefficient (FIPS 204 Algorithm 37).
    /// </summary>
    /// <param name="gamma2">The parameter γ₂.</param>
    /// <param name="r">The coefficient in [0, q).</param>
    /// <returns>The high part.</returns>
    internal static int HighBits(int gamma2, int r)
    {
        Decompose(gamma2, r, out int r1, out _);
        return r1;
    }

    /// <summary>
    /// Computes the hint bit (FIPS 204 Algorithm 39).
    /// </summary>
    /// <param name="gamma2">The parameter γ₂.</param>
    /// <param name="z">The perturbation in [0, q).</param>
    /// <param name="r">The base coefficient in [0, q).</param>
    /// <returns>1 when adding <paramref name="z" /> changes the high part; otherwise, 0.</returns>
    internal static int MakeHint(int gamma2, int z, int r) =>
        HighBits(gamma2, r) != HighBits(gamma2, (r + z) % Q) ? 1 : 0;

    /// <summary>
    /// Recovers the high part of a coefficient using a hint bit (FIPS 204 Algorithm 40).
    /// </summary>
    /// <param name="gamma2">The parameter γ₂.</param>
    /// <param name="hint">The hint bit.</param>
    /// <param name="r">The coefficient in [0, q).</param>
    /// <returns>The corrected high part.</returns>
    internal static int UseHint(int gamma2, int hint, int r)
    {
        int m = (Q - 1) / (2 * gamma2);
        Decompose(gamma2, r, out int r1, out int r0);

        if (hint == 0)
            return r1;

        return r0 > 0 ? (r1 + 1) % m : (r1 - 1 + m) % m;
    }

    /// <summary>
    /// Builds the twiddle table ζ^BitRev₈(m) mod q.
    /// </summary>
    /// <returns>The table.</returns>
    private static int[] BuildZetaTable()
    {
        int[] table = new int[256];
        for (int m = 0; m < 256; m++)
            table[m] = LatticeCommon.PowMod(1753, LatticeCommon.BitReverse(m, 8), Q);

        return table;
    }
}
