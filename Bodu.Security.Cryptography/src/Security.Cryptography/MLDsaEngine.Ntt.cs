// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLDsaEngine.Ntt.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides the complete number-theoretic transform over Z₈₃₈₀₄₁₇ used by ML-DSA (FIPS 204 Algorithms 41–45).
/// </summary>
internal static partial class MLDsaEngine
{
    /// <summary>The primitive 512th root of unity ζ = 1753 modulo q from which the twiddle table is derived.</summary>
    private const int Zeta = 1753;

    /// <summary>256⁻¹ mod q, applied as the final scaling of the inverse transform.</summary>
    private const long InverseOf256 = 8347681;

    /// <summary>Twiddle factors ζ^BitRev₈(m) mod q for m = 0–255, computed once at type initialization rather than transcribed, eliminating table-copy defects.</summary>
    private static readonly int[] s_zetas = BuildZetaTable();

    /// <summary>
    /// Applies the forward NTT (FIPS 204 Algorithm 41) to a polynomial in place.
    /// </summary>
    /// <param name="w">The 256 coefficients in [0, q), replaced by their NTT representation in [0, q).</param>
    private static void Ntt(Span<int> w)
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
    /// Applies the inverse NTT (FIPS 204 Algorithm 42) to a polynomial in place, including the final scaling by 256⁻¹
    /// mod q.
    /// </summary>
    /// <param name="w">The 256 NTT coefficients in [0, q), replaced by the standard representation in [0, q).</param>
    private static void InvNtt(Span<int> w)
    {
        int m = 256;
        for (int len = 1; len < N; len <<= 1)
        {
            for (int start = 0; start < N; start += 2 * len)
            {
                // FIPS 204 negates the twiddle and computes ζ·(t − w); using the positive twiddle with the
                // (w − t) ordering below is the algebraically identical form.
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
            w[j] = (int)((w[j] * InverseOf256) % Q);
    }

    /// <summary>
    /// Multiplies two NTT-domain polynomials coefficient-wise modulo q (FIPS 204 Algorithm 45).
    /// </summary>
    /// <param name="left">The first NTT-domain polynomial. Coefficients in [0, q).</param>
    /// <param name="right">The second NTT-domain polynomial. Coefficients in [0, q).</param>
    /// <param name="destination">The span receiving the product. May alias <paramref name="left" />.</param>
    private static void MultiplyNtt(ReadOnlySpan<int> left, ReadOnlySpan<int> right, Span<int> destination)
    {
        for (int i = 0; i < N; i++)
            destination[i] = (int)(((long)left[i] * right[i]) % Q);
    }

    /// <summary>
    /// Adds <paramref name="source" /> into <paramref name="accumulator" /> coefficient-wise modulo q.
    /// </summary>
    /// <param name="accumulator">The polynomial updated in place. Coefficients in [0, q).</param>
    /// <param name="source">The polynomial to add. Coefficients in [0, q).</param>
    private static void AddInto(Span<int> accumulator, ReadOnlySpan<int> source)
    {
        for (int i = 0; i < N; i++)
            accumulator[i] = (accumulator[i] + source[i]) % Q;
    }

    /// <summary>
    /// Subtracts <paramref name="source" /> from <paramref name="accumulator" /> coefficient-wise modulo q.
    /// </summary>
    /// <param name="accumulator">The polynomial updated in place. Coefficients in [0, q).</param>
    /// <param name="source">The polynomial to subtract. Coefficients in [0, q).</param>
    private static void SubtractFrom(Span<int> accumulator, ReadOnlySpan<int> source)
    {
        for (int i = 0; i < N; i++)
            accumulator[i] = (accumulator[i] - source[i] + Q) % Q;
    }

    /// <summary>
    /// Builds the twiddle table ζ^BitRev₈(m) mod q.
    /// </summary>
    /// <returns>The 256-entry table.</returns>
    private static int[] BuildZetaTable()
    {
        int[] table = new int[256];
        for (int m = 0; m < 256; m++)
            table[m] = LatticeCommon.PowMod(Zeta, LatticeCommon.BitReverse(m, 8), Q);

        return table;
    }

}
