// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLDsaEngine.Rounding.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.InteropServices;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides the ML-DSA rounding and hint primitives (FIPS 204 Algorithms 35–40).
/// </summary>
internal static partial class MLDsaEngine
{
    /// <summary>The dropped-bits parameter d = 13 of Power2Round.</summary>
    private const int D = 13;

    /// <summary>
    /// Splits a coefficient into high and low parts r = r₁·2ᵈ + r₀ with r₀ ∈ (−2ᵈ⁻¹, 2ᵈ⁻¹] (FIPS 204 Algorithm 35).
    /// </summary>
    /// <param name="r">The coefficient in [0, q).</param>
    /// <param name="r1">Receives the high part.</param>
    /// <param name="r0">Receives the centered low part.</param>
    private static void Power2Round(int r, out int r1, out int r0)
    {
        r0 = r & ((1 << D) - 1);

        // r0 > 2^(d-1) ? r0 -= 2^d. Folded through a sign-bit mask so the coefficient (secret key material during
        // key generation) drives no branch.
        int mask = ((1 << (D - 1)) - r0) >> 31; // -1 when r0 > 2^(d-1); otherwise 0
        r0 -= (1 << D) & mask;

        r1 = (r - r0) >> D;
    }

    /// <summary>
    /// Decomposes a coefficient relative to α = 2γ₂ into r = r₁·α + r₀ with r₀ ∈ (−γ₂, γ₂], folding the wrap-around
    /// case r − r₀ = q − 1 onto r₁ = 0 (FIPS 204 Algorithm 36).
    /// </summary>
    /// <param name="gamma2">The parameter γ₂: (q − 1) / 32 or (q − 1) / 88.</param>
    /// <param name="r">The coefficient in [0, q).</param>
    /// <param name="r1">Receives the high part in [0, (q − 1)/α).</param>
    /// <param name="r0">Receives the centered low part.</param>
    /// <remarks>
    /// <para>
    /// FIPS 204 divides by α. The division is replaced by the multiply-and-shift estimates the reference implementation
    /// uses: r is first rounded to a multiple of 2^7, whose quotient by α the constants 1025 / 2^22 (for γ₂ = (q − 1) /
    /// 32) or 11275 / 2^24 (for γ₂ = (q − 1) / 88) give exactly. The high part that would reach (q − 1)/α wraps to 0,
    /// and the low part that would then exceed (q − 1) / 2 is folded down by q, which is the wrap-around case of the
    /// standard.
    /// </para>
    /// <para>
    /// Only the choice between the two parameter sets, which is public, is a branch; the coefficient drives no branch
    /// and no division, whose timing on some processors depends on the operands.
    /// </para>
    /// </remarks>
    internal static void Decompose(int gamma2, int r, out int r1, out int r0)
    {
        r1 = HighBits(gamma2, r);
        r0 = r - (r1 * 2 * gamma2);
        r0 -= ((((Q - 1) / 2) - r0) >> 31) & Q;
    }

    /// <summary>
    /// Returns the high part of a coefficient under the γ₂ decomposition (FIPS 204 Algorithm 37).
    /// </summary>
    /// <param name="gamma2">The parameter γ₂: (q − 1) / 32 or (q − 1) / 88.</param>
    /// <param name="r">The coefficient in [0, q).</param>
    /// <returns>The high part r₁.</returns>
    internal static int HighBits(int gamma2, int r)
    {
        int r1 = (r + 127) >> 7;

        if (gamma2 == (Q - 1) / 32)
        {
            r1 = ((r1 * 1025) + (1 << 21)) >> 22;
            return r1 & 15;
        }

        r1 = ((r1 * 11275) + (1 << 23)) >> 24;
        return r1 ^ (((43 - r1) >> 31) & r1);
    }

    /// <summary>
    /// Computes the hint bit indicating whether adding <paramref name="z" /> to <paramref name="r" /> changes the high
    /// part (FIPS 204 Algorithm 39).
    /// </summary>
    /// <param name="gamma2">The parameter γ₂.</param>
    /// <param name="z">The perturbation coefficient in [0, q).</param>
    /// <param name="r">The base coefficient in [0, q).</param>
    /// <returns>1 when the high parts differ; otherwise, 0.</returns>
    internal static int MakeHint(int gamma2, int z, int r)
    {
        // (r + z) mod q via a single branch-free conditional subtract (both operands are in [0, q), so the sum is
        // in [0, 2q)), keeping the perturbed coefficient off any secret-dependent branch.
        int sum = r + z;
        sum -= Q & ((Q - 1 - sum) >> 31);

        int diff = HighBits(gamma2, r) - HighBits(gamma2, sum);

        // 1 when the high parts differ, 0 otherwise, without a comparison branch.
        return (int)((uint)(diff | -diff) >> 31);
    }

    /// <summary>
    /// Recovers the high part of <paramref name="r" /> using a hint bit (FIPS 204 Algorithm 40).
    /// </summary>
    /// <param name="gamma2">The parameter γ₂: (q − 1) / 32 or (q − 1) / 88.</param>
    /// <param name="hint">The hint bit (0 or 1).</param>
    /// <param name="r">The coefficient in [0, q).</param>
    /// <returns>The corrected high part.</returns>
    /// <remarks>
    /// Verification is the only caller and every input to it is public, so the branches here reveal nothing secret. The
    /// high part moves one step up or down modulo m = (q − 1) / 2γ₂, which is 16 or 44.
    /// </remarks>
    internal static int UseHint(int gamma2, int hint, int r)
    {
        Decompose(gamma2, r, out int r1, out int r0);
        if (hint == 0)
            return r1;

        if (gamma2 == (Q - 1) / 32)
            return r0 > 0 ? (r1 + 1) & 15 : (r1 - 1) & 15;

        if (r0 > 0)
            return r1 == 43 ? 0 : r1 + 1;

        return r1 == 0 ? 43 : r1 - 1;
    }

    /// <summary>
    /// Replaces each coefficient of a polynomial by its high part under the γ₂ decomposition (FIPS 204 Algorithm 37),
    /// with the kernel dispatch selects.
    /// </summary>
    /// <param name="gamma2">The parameter γ₂: (q − 1) / 32 or (q − 1) / 88.</param>
    /// <param name="r">The 256 coefficients, each in [0, q).</param>
    /// <param name="r1">The span receiving the 256 high parts. May be <paramref name="r" />.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="r" /> or <paramref name="r1" /> holds fewer than 256 coefficients.
    /// </exception>
    internal static void HighBits(int gamma2, ReadOnlySpan<int> r, Span<int> r1) =>
        HighBits(KernelKind.Auto, gamma2, r, r1);

    /// <summary>
    /// Replaces each coefficient of a polynomial by its high part under the γ₂ decomposition (FIPS 204 Algorithm 37),
    /// with the specified kernel.
    /// </summary>
    /// <param name="kernel">
    /// The kernel; <see cref="KernelKind.Auto" /> for the one dispatch selects. Any other kind must be one the
    /// processor supports.
    /// </param>
    /// <param name="gamma2">The parameter γ₂: (q − 1) / 32 or (q − 1) / 88.</param>
    /// <param name="r">The 256 coefficients, each in [0, q).</param>
    /// <param name="r1">The span receiving the 256 high parts. May be <paramref name="r" />.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="r" /> or <paramref name="r1" /> holds fewer than 256 coefficients.
    /// </exception>
    /// <remarks>
    /// Every kernel produces the high parts <see cref="HighBits(int, int)" /> does.
    /// </remarks>
    internal static void HighBits(KernelKind kernel, int gamma2, ReadOnlySpan<int> r, Span<int> r1)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(r.Length, N, nameof(r));
        ArgumentOutOfRangeException.ThrowIfLessThan(r1.Length, N, nameof(r1));

        if (Resolve(kernel) == KernelKind.Avx2)
        {
            Vector256Kernel.HighBits(gamma2, ref MemoryMarshal.GetReference(r), ref MemoryMarshal.GetReference(r1));
            return;
        }

        for (int i = 0; i < N; i++)
            r1[i] = HighBits(gamma2, r[i]);
    }

    /// <summary>
    /// Returns the largest magnitude of a polynomial's low parts under the γ₂ decomposition, with the kernel dispatch
    /// selects.
    /// </summary>
    /// <param name="gamma2">The parameter γ₂: (q − 1) / 32 or (q − 1) / 88.</param>
    /// <param name="r">The 256 coefficients, each in [0, q).</param>
    /// <returns>
    /// The largest |r₀| over the coefficients, where <see cref="Decompose" /> splits each into (r₁, r₀).
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="r" /> holds fewer than 256 coefficients.
    /// </exception>
    internal static int LowBitsNorm(int gamma2, ReadOnlySpan<int> r) =>
        LowBitsNorm(KernelKind.Auto, gamma2, r);

    /// <summary>
    /// Returns the largest magnitude of a polynomial's low parts under the γ₂ decomposition, with the specified kernel.
    /// </summary>
    /// <param name="kernel">
    /// The kernel; <see cref="KernelKind.Auto" /> for the one dispatch selects. Any other kind must be one the
    /// processor supports.
    /// </param>
    /// <param name="gamma2">The parameter γ₂: (q − 1) / 32 or (q − 1) / 88.</param>
    /// <param name="r">The 256 coefficients, each in [0, q).</param>
    /// <returns>
    /// The largest |r₀| over the coefficients, where <see cref="Decompose" /> splits each into (r₁, r₀).
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="r" /> holds fewer than 256 coefficients.
    /// </exception>
    /// <remarks>
    /// The scan has no early exit, so the time reveals only that the check happened, not which coefficient drove the
    /// bound. Every kernel returns the same value.
    /// </remarks>
    internal static int LowBitsNorm(KernelKind kernel, int gamma2, ReadOnlySpan<int> r)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(r.Length, N, nameof(r));

        if (Resolve(kernel) == KernelKind.Avx2)
            return Vector256Kernel.LowBitsNorm(gamma2, ref MemoryMarshal.GetReference(r));

        int maximum = 0;
        for (int i = 0; i < N; i++)
        {
            Decompose(gamma2, r[i], out _, out int r0);

            // |r₀| through the sign mask rather than Math.Abs, which branches on the sign of a secret-derived value.
            int sign = r0 >> 31;
            maximum = Math.Max(maximum, (r0 ^ sign) - sign);
        }

        return maximum;
    }

    /// <summary>
    /// Computes the hints of one polynomial of the signature, h = MakeHint(−ct₀, w − cs₂ + ct₀) coefficient by
    /// coefficient (FIPS 204 Algorithm 7, line 26), with the kernel dispatch selects.
    /// </summary>
    /// <param name="gamma2">The parameter γ₂: (q − 1) / 32 or (q − 1) / 88.</param>
    /// <param name="ct0">The 256 coefficients of ct₀, each in [0, q).</param>
    /// <param name="wMinusCs2">The 256 coefficients of w − cs₂, each in [0, q).</param>
    /// <param name="hints">The span receiving the 256 hints, each 0 or 1.</param>
    /// <returns>The number of hints that are 1.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="ct0" />, <paramref name="wMinusCs2" /> or <paramref name="hints" /> holds fewer than 256
    /// coefficients.
    /// </exception>
    internal static int MakeHints(int gamma2, ReadOnlySpan<int> ct0, ReadOnlySpan<int> wMinusCs2, Span<int> hints) =>
        MakeHints(KernelKind.Auto, gamma2, ct0, wMinusCs2, hints);

    /// <summary>
    /// Computes the hints of one polynomial of the signature, h = MakeHint(−ct₀, w − cs₂ + ct₀) coefficient by
    /// coefficient (FIPS 204 Algorithm 7, line 26), with the specified kernel.
    /// </summary>
    /// <param name="kernel">
    /// The kernel; <see cref="KernelKind.Auto" /> for the one dispatch selects. Any other kind must be one the
    /// processor supports.
    /// </param>
    /// <param name="gamma2">The parameter γ₂: (q − 1) / 32 or (q − 1) / 88.</param>
    /// <param name="ct0">The 256 coefficients of ct₀, each in [0, q).</param>
    /// <param name="wMinusCs2">The 256 coefficients of w − cs₂, each in [0, q).</param>
    /// <param name="hints">The span receiving the 256 hints, each 0 or 1.</param>
    /// <returns>The number of hints that are 1.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="ct0" />, <paramref name="wMinusCs2" /> or <paramref name="hints" /> holds fewer than 256
    /// coefficients.
    /// </exception>
    /// <remarks>
    /// Every coefficient is computed and counted, whatever the hints hold. Every kernel produces the same hints.
    /// </remarks>
    internal static int MakeHints(KernelKind kernel, int gamma2, ReadOnlySpan<int> ct0, ReadOnlySpan<int> wMinusCs2, Span<int> hints)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(ct0.Length, N, nameof(ct0));
        ArgumentOutOfRangeException.ThrowIfLessThan(wMinusCs2.Length, N, nameof(wMinusCs2));
        ArgumentOutOfRangeException.ThrowIfLessThan(hints.Length, N, nameof(hints));

        if (Resolve(kernel) == KernelKind.Avx2)
            return Vector256Kernel.MakeHints(gamma2, ref MemoryMarshal.GetReference(ct0), ref MemoryMarshal.GetReference(wMinusCs2), ref MemoryMarshal.GetReference(hints));

        int weight = 0;
        for (int i = 0; i < N; i++)
        {
            int negated = Canonicalize(-ct0[i]);
            int basis = Canonicalize(wMinusCs2[i] + ct0[i] - Q);
            int hint = MakeHint(gamma2, negated, basis);
            hints[i] = hint;
            weight += hint;
        }

        return weight;
    }

    /// <summary>
    /// Computes the infinity norm of a polynomial, the maximum absolute value of the centered representatives, with the
    /// kernel dispatch selects.
    /// </summary>
    /// <param name="poly">The 256 coefficients in [0, q).</param>
    /// <returns>The infinity norm.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="poly" /> holds fewer than 256 coefficients.
    /// </exception>
    internal static int InfinityNorm(ReadOnlySpan<int> poly) =>
        InfinityNorm(KernelKind.Auto, poly);

    /// <summary>
    /// Computes the infinity norm of a polynomial, the maximum absolute value of the centered representatives, with the
    /// specified kernel.
    /// </summary>
    /// <param name="kernel">
    /// The kernel; <see cref="KernelKind.Auto" /> for the one dispatch selects. Any other kind must be one the
    /// processor supports.
    /// </param>
    /// <param name="poly">The 256 coefficients in [0, q).</param>
    /// <returns>The infinity norm.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="poly" /> holds fewer than 256 coefficients.
    /// </exception>
    /// <remarks>
    /// The scan has no early exit, so the time reveals only that a norm check happened — which restart iteration of the
    /// signing loop runs is public by design — and not which coefficient drove the bound. Every kernel returns the same
    /// value.
    /// </remarks>
    internal static int InfinityNorm(KernelKind kernel, ReadOnlySpan<int> poly)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(poly.Length, N, nameof(poly));

        if (Resolve(kernel) == KernelKind.Avx2)
            return Vector256Kernel.InfinityNorm(ref MemoryMarshal.GetReference(poly));

        int maximum = 0;
        for (int i = 0; i < N; i++)
        {
            int centered = poly[i];

            // centered > (q−1)/2 ? centered = q − centered. Folded through a sign-bit mask so the centering of each
            // secret coefficient does not branch; Math.Max lowers to a branch-free conditional move.
            int mask = (((Q - 1) / 2) - centered) >> 31; // -1 when centered > (q−1)/2; otherwise 0
            centered -= ((2 * centered) - Q) & mask;

            maximum = Math.Max(maximum, centered);
        }

        return maximum;
    }
}
