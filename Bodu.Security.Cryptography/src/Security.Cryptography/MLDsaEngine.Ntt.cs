// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLDsaEngine.Ntt.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides the complete number-theoretic transform over Z₈₃₈₀₄₁₇ used by ML-DSA (FIPS 204 Algorithms 41–45).
/// </summary>
internal static partial class MLDsaEngine
{
    /// <summary>The primitive 512th root of unity ζ = 1753 modulo q from which the twiddle table is derived.</summary>
    private const int Zeta = 1753;

    /// <summary>256⁻¹ · 2^32 mod q: the inverse transform's final scaling by 256⁻¹, in Montgomery form.</summary>
    private const long InverseOf256Montgomery = 16382;

    /// <summary>Twiddle factors ζ^BitRev₈(m) · 2^32 mod q for m = 0–255, in Montgomery form so that <see cref="MontgomeryReduce" /> of a product with one yields the plain product. Computed once at type initialization rather than transcribed, eliminating table-copy defects.</summary>
    private static readonly int[] s_zetas = BuildZetaTable();

    /// <summary>
    /// Applies the forward NTT (FIPS 204 Algorithm 41) to a polynomial in place with the kernel dispatch selects.
    /// </summary>
    /// <param name="w">
    /// The 256 coefficients, each below q in magnitude, replaced by their NTT representation in [0, q).
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="w" /> holds fewer than 256 coefficients.
    /// </exception>
    internal static void Ntt(Span<int> w) =>
        Ntt(KernelKind.Auto, w);

    /// <summary>
    /// Applies the forward NTT (FIPS 204 Algorithm 41) to a polynomial in place with the specified kernel.
    /// </summary>
    /// <param name="kernel">
    /// The kernel; <see cref="KernelKind.Auto" /> for the one dispatch selects. Any other kind must be one the
    /// processor supports.
    /// </param>
    /// <param name="w">
    /// The 256 coefficients, each below q in magnitude, replaced by their NTT representation in [0, q).
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="w" /> holds fewer than 256 coefficients.
    /// </exception>
    /// <remarks>
    /// Each butterfly multiplies by its twiddle through <see cref="MontgomeryReduce" /> and leaves the sum and
    /// difference unreduced: a layer grows the coefficients by at most q in magnitude, so after the eight layers they
    /// lie within (−9q, 9q) and one reduction per coefficient at the end brings them back to [0, q). A polynomial in
    /// Montgomery form transforms into the Montgomery form of its transform, since the transform is linear. Every
    /// kernel produces the same coefficients.
    /// </remarks>
    internal static void Ntt(KernelKind kernel, Span<int> w)
    {
        ThrowHelper.ThrowIfLessThan(w.Length, N, nameof(w));

        // The length is checked above, so the kernels address the coefficients by reference, without a bounds check on
        // each access.
        ref int coefficients = ref MemoryMarshal.GetReference(w);
        if (Resolve(kernel) == KernelKind.Avx2)
            Vector256Kernel.Ntt(ref coefficients);
        else
            NttScalar(ref coefficients);
    }

    /// <summary>
    /// Applies the forward NTT with the portable scalar code.
    /// </summary>
    /// <param name="coefficients">The first of the 256 coefficients, transformed in place.</param>
    private static void NttScalar(ref int coefficients)
    {
        int m = 0;
        for (int len = 128; len >= 1; len >>= 1)
        {
            for (int start = 0; start < N; start += 2 * len)
            {
                long zeta = s_zetas[++m];
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
            coefficient = Freeze(coefficient);
        }
    }

    /// <summary>
    /// Applies the inverse NTT (FIPS 204 Algorithm 42) to a polynomial in place, including the final scaling by 256⁻¹
    /// mod q, with the kernel dispatch selects.
    /// </summary>
    /// <param name="w">
    /// The 256 NTT coefficients, each below q in magnitude, replaced by the standard representation in [0, q).
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="w" /> holds fewer than 256 coefficients.
    /// </exception>
    internal static void InvNtt(Span<int> w) =>
        InvNtt(KernelKind.Auto, w);

    /// <summary>
    /// Applies the inverse NTT (FIPS 204 Algorithm 42) to a polynomial in place, including the final scaling by 256⁻¹
    /// mod q, with the specified kernel.
    /// </summary>
    /// <param name="kernel">
    /// The kernel; <see cref="KernelKind.Auto" /> for the one dispatch selects. Any other kind must be one the
    /// processor supports.
    /// </param>
    /// <param name="w">
    /// The 256 NTT coefficients, each below q in magnitude, replaced by the standard representation in [0, q).
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="w" /> holds fewer than 256 coefficients.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Each butterfly leaves its sum unreduced and reduces its twiddle product with <see cref="MontgomeryReduce" />.
    /// Sums at most double from layer to layer, so coefficients below q in magnitude stay below 256q &lt; 2^31 through
    /// the eighth. A caller holding larger values, such as sums of products from
    /// <see cref="MultiplyAccumulateNtt(ReadOnlySpan{int}, ReadOnlySpan{int}, Span{int})" />, reduces them first with
    /// <see cref="Reduce32(int)" />.
    /// </para>
    /// <para>
    /// The last layer carries the final scaling: its sums are multiplied by 256⁻¹ and its differences by ζ·256⁻¹, each
    /// with a single Montgomery reduction, so no separate pass over the coefficients is needed.
    /// </para>
    /// <para>
    /// FIPS 204 negates the twiddle and computes ζ·(t − w); using the positive twiddle with the (w − t) ordering below
    /// is the algebraically identical form. Every kernel produces the same coefficients.
    /// </para>
    /// </remarks>
    internal static void InvNtt(KernelKind kernel, Span<int> w)
    {
        ThrowHelper.ThrowIfLessThan(w.Length, N, nameof(w));

        ref int coefficients = ref MemoryMarshal.GetReference(w);
        if (Resolve(kernel) == KernelKind.Avx2)
            Vector256Kernel.InvNtt(ref coefficients);
        else
            InvNttScalar(ref coefficients);
    }

    /// <summary>
    /// Applies the inverse NTT with the portable scalar code.
    /// </summary>
    /// <param name="coefficients">The first of the 256 coefficients, transformed in place.</param>
    private static void InvNttScalar(ref int coefficients)
    {
        int m = 256;
        for (int len = 1; len < N / 2; len <<= 1)
        {
            for (int start = 0; start < N; start += 2 * len)
            {
                long zeta = s_zetas[--m];
                ref int low = ref Unsafe.Add(ref coefficients, start);
                ref int high = ref Unsafe.Add(ref low, len);

                for (int j = 0; j < len; j++)
                {
                    int t = Unsafe.Add(ref low, j);
                    int u = Unsafe.Add(ref high, j);
                    Unsafe.Add(ref low, j) = t + u;
                    Unsafe.Add(ref high, j) = MontgomeryReduce(zeta * (u - t));
                }
            }
        }

        // The last layer's twiddle, ζ = s_zetas[1], combined with 256⁻¹; both stay in Montgomery form.
        long scaledZeta = MontgomeryReduce(s_zetas[1] * InverseOf256Montgomery);
        ref int lowHalf = ref coefficients;
        ref int highHalf = ref Unsafe.Add(ref coefficients, N / 2);

        for (int j = 0; j < N / 2; j++)
        {
            int t = Unsafe.Add(ref lowHalf, j);
            int u = Unsafe.Add(ref highHalf, j);
            Unsafe.Add(ref lowHalf, j) = Canonicalize(MontgomeryReduce(InverseOf256Montgomery * (t + u)));
            Unsafe.Add(ref highHalf, j) = Canonicalize(MontgomeryReduce(scaledZeta * (u - t)));
        }
    }

    /// <summary>
    /// Multiplies two NTT-domain polynomials coefficient-wise (FIPS 204 Algorithm 45), one of them in Montgomery form,
    /// with the kernel dispatch selects.
    /// </summary>
    /// <param name="montgomeryLeft">
    /// The first polynomial, in Montgomery form. Coefficients below q in magnitude.
    /// </param>
    /// <param name="right">The second polynomial. Coefficients below q in magnitude.</param>
    /// <param name="destination">The span receiving the plain product, each coefficient in (−q, q).</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="montgomeryLeft" />, <paramref name="right" /> or <paramref name="destination" /> holds fewer
    /// than 256 coefficients.
    /// </exception>
    internal static void MultiplyNtt(ReadOnlySpan<int> montgomeryLeft, ReadOnlySpan<int> right, Span<int> destination) =>
        MultiplyNtt(KernelKind.Auto, montgomeryLeft, right, destination);

    /// <summary>
    /// Multiplies two NTT-domain polynomials coefficient-wise (FIPS 204 Algorithm 45), one of them in Montgomery form,
    /// with the specified kernel.
    /// </summary>
    /// <param name="kernel">
    /// The kernel; <see cref="KernelKind.Auto" /> for the one dispatch selects. Any other kind must be one the
    /// processor supports.
    /// </param>
    /// <param name="montgomeryLeft">
    /// The first polynomial, in Montgomery form. Coefficients below q in magnitude.
    /// </param>
    /// <param name="right">The second polynomial. Coefficients below q in magnitude.</param>
    /// <param name="destination">The span receiving the plain product, each coefficient in (−q, q).</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="montgomeryLeft" />, <paramref name="right" /> or <paramref name="destination" /> holds fewer
    /// than 256 coefficients.
    /// </exception>
    /// <remarks>
    /// Each product is reduced once by <see cref="MontgomeryReduce" />, which removes the 2^32 that
    /// <paramref name="montgomeryLeft" /> carries. The result is exact but not canonical;
    /// <see cref="InvNtt(Span{int})" /> accepts it as it is. Every kernel produces the same coefficients.
    /// </remarks>
    internal static void MultiplyNtt(KernelKind kernel, ReadOnlySpan<int> montgomeryLeft, ReadOnlySpan<int> right, Span<int> destination)
    {
        ThrowHelper.ThrowIfLessThan(montgomeryLeft.Length, N, nameof(montgomeryLeft));
        ThrowHelper.ThrowIfLessThan(right.Length, N, nameof(right));
        ThrowHelper.ThrowIfLessThan(destination.Length, N, nameof(destination));

        ref int left = ref MemoryMarshal.GetReference(montgomeryLeft);
        ref int factor = ref MemoryMarshal.GetReference(right);
        ref int product = ref MemoryMarshal.GetReference(destination);
        if (Resolve(kernel) == KernelKind.Avx2)
        {
            Vector256Kernel.MultiplyNtt(ref left, ref factor, ref product);
            return;
        }

        for (int i = 0; i < N; i++)
            Unsafe.Add(ref product, i) = MontgomeryReduce((long)Unsafe.Add(ref left, i) * Unsafe.Add(ref factor, i));
    }

    /// <summary>
    /// Adds the coefficient-wise product of two NTT-domain polynomials, one of them in Montgomery form, into an
    /// accumulator without reducing the sum, with the kernel dispatch selects.
    /// </summary>
    /// <param name="montgomeryLeft">
    /// The first polynomial, in Montgomery form. Coefficients below q in magnitude.
    /// </param>
    /// <param name="right">The second polynomial. Coefficients below q in magnitude.</param>
    /// <param name="accumulator">
    /// The polynomial the product is added into; each addition grows it by less than q.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="montgomeryLeft" />, <paramref name="right" /> or <paramref name="accumulator" /> holds fewer
    /// than 256 coefficients.
    /// </exception>
    internal static void MultiplyAccumulateNtt(ReadOnlySpan<int> montgomeryLeft, ReadOnlySpan<int> right, Span<int> accumulator) =>
        MultiplyAccumulateNtt(KernelKind.Auto, montgomeryLeft, right, accumulator);

    /// <summary>
    /// Adds the coefficient-wise product of two NTT-domain polynomials, one of them in Montgomery form, into an
    /// accumulator without reducing the sum, with the specified kernel.
    /// </summary>
    /// <param name="kernel">
    /// The kernel; <see cref="KernelKind.Auto" /> for the one dispatch selects. Any other kind must be one the
    /// processor supports.
    /// </param>
    /// <param name="montgomeryLeft">
    /// The first polynomial, in Montgomery form. Coefficients below q in magnitude.
    /// </param>
    /// <param name="right">The second polynomial. Coefficients below q in magnitude.</param>
    /// <param name="accumulator">
    /// The polynomial the product is added into; each addition grows it by less than q.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="montgomeryLeft" />, <paramref name="right" /> or <paramref name="accumulator" /> holds fewer
    /// than 256 coefficients.
    /// </exception>
    /// <remarks>
    /// Every kernel produces the same coefficients.
    /// </remarks>
    internal static void MultiplyAccumulateNtt(KernelKind kernel, ReadOnlySpan<int> montgomeryLeft, ReadOnlySpan<int> right, Span<int> accumulator)
    {
        ThrowHelper.ThrowIfLessThan(montgomeryLeft.Length, N, nameof(montgomeryLeft));
        ThrowHelper.ThrowIfLessThan(right.Length, N, nameof(right));
        ThrowHelper.ThrowIfLessThan(accumulator.Length, N, nameof(accumulator));

        ref int left = ref MemoryMarshal.GetReference(montgomeryLeft);
        ref int factor = ref MemoryMarshal.GetReference(right);
        ref int sum = ref MemoryMarshal.GetReference(accumulator);
        if (Resolve(kernel) == KernelKind.Avx2)
        {
            Vector256Kernel.MultiplyAccumulateNtt(ref left, ref factor, ref sum);
            return;
        }

        for (int i = 0; i < N; i++)
            Unsafe.Add(ref sum, i) += MontgomeryReduce((long)Unsafe.Add(ref left, i) * Unsafe.Add(ref factor, i));
    }

    /// <summary>
    /// Selects the widest kernel the processor supports and the process allows: AVX2, then the scalar code.
    /// </summary>
    /// <returns>The kernel dispatch runs; never <see cref="KernelKind.Auto" />.</returns>
    /// <remarks>
    /// The gate honors the <see cref="SimdCapabilities.DisableSimdSwitchName" /> switch, which pins the scalar code.
    /// </remarks>
    internal static KernelKind SelectKernel() =>
        SimdCapabilities.Avx2 ? KernelKind.Avx2 : KernelKind.Scalar;

    /// <summary>
    /// Determines whether the processor can run the specified kernel, whether or not the process allows vector code.
    /// </summary>
    /// <param name="kernel">The kernel.</param>
    /// <returns>
    /// <see langword="true" /> if the processor supports every instruction the kernel uses; otherwise,
    /// <see langword="false" />.
    /// </returns>
    internal static bool IsSupported(KernelKind kernel) => kernel switch
    {
        KernelKind.Auto or KernelKind.Scalar => true,
        KernelKind.Avx2 => System.Runtime.Intrinsics.X86.Avx2.IsSupported,
        _ => false,
    };

    /// <summary>
    /// Resolves <see cref="KernelKind.Auto" /> to the kernel dispatch selects.
    /// </summary>
    /// <param name="kernel">The requested kernel.</param>
    /// <returns>The kernel to run; never <see cref="KernelKind.Auto" />.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static KernelKind Resolve(KernelKind kernel) =>
        kernel == KernelKind.Auto ? SelectKernel() : kernel;

    /// <summary>
    /// Builds the twiddle table ζ^BitRev₈(m) · 2^32 mod q.
    /// </summary>
    /// <returns>The 256-entry table.</returns>
    private static int[] BuildZetaTable()
    {
        int[] table = new int[256];
        for (int m = 0; m < 256; m++)
            table[m] = (int)(((long)LatticeCommon.PowMod(Zeta, LatticeCommon.BitReverse(m, 8), Q) << 32) % Q);

        return table;
    }
}
