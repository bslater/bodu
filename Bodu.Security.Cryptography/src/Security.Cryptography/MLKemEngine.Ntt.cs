// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLKemEngine.Ntt.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides the number-theoretic transform over Z₃₃₂₉ used by ML-KEM (FIPS 203 Algorithms 9–12).
/// </summary>
internal static partial class MLKemEngine
{
    /// <summary>The primitive 256th root of unity ζ = 17 modulo q from which the twiddle tables are derived.</summary>
    private const int Zeta = 17;

    /// <summary>Twiddle factors ζ^BitRev₇(i) mod q for i = 0–127, computed once at type initialization rather than transcribed, eliminating table-copy defects.</summary>
    private static readonly int[] s_zetas = BuildZetaTable();

    /// <summary>Base-case multipliers γ[i] = ζ^(2·BitRev₇(i) + 1) mod q for the degree-two pairwise products.</summary>
    private static readonly int[] s_gammas = BuildGammaTable();

    /// <summary>
    /// Applies the forward NTT (FIPS 203 Algorithm 9) to a polynomial in place.
    /// </summary>
    /// <param name="f">The 256 coefficients in [0, q), replaced by their NTT representation in [0, q).</param>
    private static void Ntt(Span<int> f)
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
    /// Applies the inverse NTT (FIPS 203 Algorithm 10) to a polynomial in place, including the final scaling by 128⁻¹
    /// mod q.
    /// </summary>
    /// <param name="f">The 256 NTT coefficients in [0, q), replaced by the standard representation in [0, q).</param>
    private static void InvNtt(Span<int> f)
    {
        // 3303 = 128⁻¹ mod q.
        const int InverseOf128 = 3303;

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
            f[j] = (f[j] * InverseOf128) % Q;
    }

    /// <summary>
    /// Multiplies two polynomials in the NTT domain (FIPS 203 Algorithms 11–12) using the 128 degree-two base-case
    /// products.
    /// </summary>
    /// <param name="left">The first NTT-domain polynomial. Coefficients in [0, q).</param>
    /// <param name="right">The second NTT-domain polynomial. Coefficients in [0, q).</param>
    /// <param name="destination">The span receiving the NTT-domain product. May not alias the inputs.</param>
    private static void MultiplyNtt(ReadOnlySpan<int> left, ReadOnlySpan<int> right, Span<int> destination)
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
    /// Builds the forward-NTT twiddle table ζ^BitRev₇(i) mod q.
    /// </summary>
    /// <returns>The 128-entry table.</returns>
    private static int[] BuildZetaTable()
    {
        int[] table = new int[128];
        for (int i = 0; i < 128; i++)
            table[i] = PowMod(Zeta, BitReverse7(i));

        return table;
    }

    /// <summary>
    /// Builds the base-case multiplier table ζ^(2·BitRev₇(i) + 1) mod q.
    /// </summary>
    /// <returns>The 128-entry table.</returns>
    private static int[] BuildGammaTable()
    {
        int[] table = new int[128];
        for (int i = 0; i < 128; i++)
            table[i] = PowMod(Zeta, (2 * BitReverse7(i)) + 1);

        return table;
    }

    /// <summary>
    /// Computes base^exponent mod q by square-and-multiply over public constants.
    /// </summary>
    /// <param name="value">The base value.</param>
    /// <param name="exponent">The non-negative exponent.</param>
    /// <returns>The modular power in [0, q).</returns>
    private static int PowMod(int value, int exponent)
    {
        long result = 1;
        long basis = value % Q;

        while (exponent > 0)
        {
            if ((exponent & 1) != 0)
                result = (result * basis) % Q;

            basis = (basis * basis) % Q;
            exponent >>= 1;
        }

        return (int)result;
    }

    /// <summary>
    /// Reverses the low seven bits of an index.
    /// </summary>
    /// <param name="value">The index in [0, 128).</param>
    /// <returns>The bit-reversed index.</returns>
    private static int BitReverse7(int value)
    {
        int result = 0;
        for (int bit = 0; bit < 7; bit++)
            result |= ((value >> bit) & 1) << (6 - bit);

        return result;
    }
}
