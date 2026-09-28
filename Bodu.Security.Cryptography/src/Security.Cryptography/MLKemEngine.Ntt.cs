// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLKemEngine.Ntt.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides the number-theoretic transform over Z₃₃₂₉ used by ML-KEM (FIPS 203 Algorithms 9–12).
/// </summary>
internal static partial class MLKemEngine
{
    /// <summary>The primitive 256th root of unity ζ = 17 modulo q from which the twiddle tables are derived.</summary>
    private const int Zeta = 17;

    /// <summary>128⁻¹ · 2^16 mod q: the inverse transform's final scaling by 128⁻¹, in Montgomery form.</summary>
    private const int InverseOf128Montgomery = 512;

    /// <summary>Twiddle factors ζ^BitRev₇(i) · 2^16 mod q for i = 0–127, in Montgomery form so that <see cref="MontgomeryReduce" /> of a product with one yields the plain product. Computed once at type initialization rather than transcribed, eliminating table-copy defects.</summary>
    private static readonly int[] s_zetas = BuildZetaTable();

    /// <summary>Base-case multipliers γ[i] = ζ^(2·BitRev₇(i) + 1) mod q for the degree-two pairwise products.</summary>
    private static readonly int[] s_gammas = BuildGammaTable();

    /// <summary>
    /// Applies the forward NTT (FIPS 203 Algorithm 9) to a polynomial in place.
    /// </summary>
    /// <param name="f">The 256 coefficients in [0, q), replaced by their NTT representation in [0, q).</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="f" /> holds fewer than 256 coefficients.
    /// </exception>
    /// <remarks>
    /// Each butterfly multiplies by its twiddle through <see cref="MontgomeryReduce" /> and leaves the sum and
    /// difference unreduced: a layer grows the coefficients by at most q in magnitude, so after the seven layers they
    /// lie within (−8q, 8q) and one reduction per coefficient at the end brings them back to [0, q).
    /// </remarks>
    internal static void Ntt(Span<int> f)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(f.Length, N, nameof(f));

        // The length is checked above, so the butterflies address the coefficients by reference, without a bounds
        // check on each access.
        ref int coefficients = ref MemoryMarshal.GetReference(f);

        int i = 1;
        for (int len = 128; len >= 2; len >>= 1)
        {
            for (int start = 0; start < N; start += 2 * len)
            {
                int zeta = s_zetas[i++];
                ref int low = ref Unsafe.Add(ref coefficients, start);
                ref int high = ref Unsafe.Add(ref low, len);

                for (int j = 0; j < len; j++)
                {
                    int t = MontgomeryReduce(zeta * Unsafe.Add(ref high, j));
                    Unsafe.Add(ref high, j) = Unsafe.Add(ref low, j) - t;
                    Unsafe.Add(ref low, j) += t;
                }
            }
        }

        for (int j = 0; j < N; j++)
        {
            ref int coefficient = ref Unsafe.Add(ref coefficients, j);
            coefficient = Canonicalize(BarrettReduce(coefficient));
        }
    }

    /// <summary>
    /// Applies the inverse NTT (FIPS 203 Algorithm 10) to a polynomial in place, including the final scaling by 128⁻¹
    /// mod q.
    /// </summary>
    /// <param name="f">The 256 NTT coefficients in [0, q), replaced by the standard representation in [0, q).</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="f" /> holds fewer than 256 coefficients.
    /// </exception>
    /// <remarks>
    /// Each butterfly reduces its sum with <see cref="BarrettReduce" /> and its twiddle product with
    /// <see cref="MontgomeryReduce" />, which keeps every coefficient within (−q, q) from layer to layer.
    /// </remarks>
    internal static void InvNtt(Span<int> f)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(f.Length, N, nameof(f));

        ref int coefficients = ref MemoryMarshal.GetReference(f);

        int i = 127;
        for (int len = 2; len <= 128; len <<= 1)
        {
            for (int start = 0; start < N; start += 2 * len)
            {
                int zeta = s_zetas[i--];
                ref int low = ref Unsafe.Add(ref coefficients, start);
                ref int high = ref Unsafe.Add(ref low, len);

                for (int j = 0; j < len; j++)
                {
                    int t = Unsafe.Add(ref low, j);
                    int u = Unsafe.Add(ref high, j);
                    Unsafe.Add(ref low, j) = BarrettReduce(t + u);
                    Unsafe.Add(ref high, j) = MontgomeryReduce(zeta * (u - t));
                }
            }
        }

        for (int j = 0; j < N; j++)
        {
            ref int coefficient = ref Unsafe.Add(ref coefficients, j);
            coefficient = Canonicalize(MontgomeryReduce(InverseOf128Montgomery * coefficient));
        }
    }

    /// <summary>
    /// Multiplies two polynomials in the NTT domain (FIPS 203 Algorithms 11–12) using the 128 degree-two base-case
    /// products.
    /// </summary>
    /// <param name="left">The first NTT-domain polynomial. Coefficients in [0, q).</param>
    /// <param name="right">The second NTT-domain polynomial. Coefficients in [0, q).</param>
    /// <param name="destination">The span receiving the NTT-domain product. May not alias the inputs.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="left" />, <paramref name="right" /> or <paramref name="destination" /> holds fewer than 256
    /// coefficients.
    /// </exception>
    /// <remarks>
    /// Each output coefficient is a sum of products below q³ + q² &lt; 2^36, reduced once with
    /// <see cref="ReduceWide" />.
    /// </remarks>
    internal static void MultiplyNtt(ReadOnlySpan<int> left, ReadOnlySpan<int> right, Span<int> destination)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(left.Length, N, nameof(left));
        ArgumentOutOfRangeException.ThrowIfLessThan(right.Length, N, nameof(right));
        ArgumentOutOfRangeException.ThrowIfLessThan(destination.Length, N, nameof(destination));

        for (int i = 0; i < 128; i++)
        {
            ulong a0 = (uint)left[2 * i];
            ulong a1 = (uint)left[(2 * i) + 1];
            ulong b0 = (uint)right[2 * i];
            ulong b1 = (uint)right[(2 * i) + 1];

            destination[2 * i] = ReduceWide((a0 * b0) + (a1 * b1 * (uint)s_gammas[i]));
            destination[(2 * i) + 1] = ReduceWide((a0 * b1) + (a1 * b0));
        }
    }

    /// <summary>
    /// Builds the forward-NTT twiddle table ζ^BitRev₇(i) · 2^16 mod q.
    /// </summary>
    /// <returns>The 128-entry table.</returns>
    private static int[] BuildZetaTable()
    {
        int[] table = new int[128];
        for (int i = 0; i < 128; i++)
            table[i] = (int)(((long)LatticeCommon.PowMod(Zeta, LatticeCommon.BitReverse(i, 7), Q) << 16) % Q);

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
            table[i] = LatticeCommon.PowMod(Zeta, (2 * LatticeCommon.BitReverse(i, 7)) + 1, Q);

        return table;
    }
}
