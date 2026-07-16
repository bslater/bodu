// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLDsaEngine.Rounding.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

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
    /// <param name="gamma2">The parameter γ₂.</param>
    /// <param name="r">The coefficient in [0, q).</param>
    /// <param name="r1">Receives the high part in [0, (q − 1)/α).</param>
    /// <param name="r0">Receives the centered low part.</param>
    private static void Decompose(int gamma2, int r, out int r1, out int r0)
    {
        int alpha = 2 * gamma2;

        r0 = r % alpha;

        // r0 > gamma2 ? r0 -= alpha. The divisor alpha is a public parameter; only the two comparisons below carried
        // secret-dependent control flow, so both are folded through sign-bit masks to keep the decomposition of the
        // secret signing-loop coefficients branch-free.
        int highMask = (gamma2 - r0) >> 31; // -1 when r0 > gamma2; otherwise 0
        r0 -= alpha & highMask;

        int hi = r - r0;
        r1 = hi / alpha;

        // Wrap-around case r − r0 = q − 1 maps onto r1 = 0, r0-- without branching.
        int diff = hi - (Q - 1);
        int wrapMask = ~((diff | -diff) >> 31); // -1 when hi == q − 1; otherwise 0
        r1 &= ~wrapMask;
        r0 += wrapMask;
    }

    /// <summary>
    /// Returns the high part of a coefficient under the γ₂ decomposition (FIPS 204 Algorithm 37).
    /// </summary>
    /// <param name="gamma2">The parameter γ₂.</param>
    /// <param name="r">The coefficient in [0, q).</param>
    /// <returns>The high part r₁.</returns>
    private static int HighBits(int gamma2, int r)
    {
        Decompose(gamma2, r, out int r1, out _);

        return r1;
    }

    /// <summary>
    /// Computes the hint bit indicating whether adding <paramref name="z" /> to <paramref name="r" /> changes the high
    /// part (FIPS 204 Algorithm 39).
    /// </summary>
    /// <param name="gamma2">The parameter γ₂.</param>
    /// <param name="z">The perturbation coefficient in [0, q).</param>
    /// <param name="r">The base coefficient in [0, q).</param>
    /// <returns>1 when the high parts differ; otherwise, 0.</returns>
    private static int MakeHint(int gamma2, int z, int r)
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
    /// <param name="gamma2">The parameter γ₂.</param>
    /// <param name="hint">The hint bit (0 or 1).</param>
    /// <param name="r">The coefficient in [0, q).</param>
    /// <returns>The corrected high part.</returns>
    private static int UseHint(int gamma2, int hint, int r)
    {
        int m = (Q - 1) / (2 * gamma2);
        Decompose(gamma2, r, out int r1, out int r0);

        if (hint == 0)
            return r1;

        return r0 > 0 ? (r1 + 1) % m : (r1 - 1 + m) % m;
    }

    /// <summary>
    /// Computes the infinity norm of a polynomial: the maximum absolute value of the centered representatives.
    /// </summary>
    /// <param name="poly">The 256 coefficients in [0, q).</param>
    /// <returns>The infinity norm.</returns>
    /// <remarks>
    /// The scan has no early exit, so the time reveals only that a norm check happened — which restart iteration of the
    /// signing loop runs is public by design — and not which coefficient drove the bound.
    /// </remarks>
    private static int InfinityNorm(ReadOnlySpan<int> poly)
    {
        int maximum = 0;
        for (int i = 0; i < N; i++)
        {
            int centered = poly[i];

            // centered > (q−1)/2 ? centered = q − centered. Folded through a sign-bit mask so the centering of each
            // secret coefficient does not branch; Math.Max lowers to a branch-free conditional move.
            int mask = ((Q - 1) / 2 - centered) >> 31; // -1 when centered > (q−1)/2; otherwise 0
            centered -= ((2 * centered) - Q) & mask;

            maximum = Math.Max(maximum, centered);
        }

        return maximum;
    }
}
