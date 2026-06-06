// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyMath.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;

namespace Bodu.Financial;

/// <summary>
/// Shared, currency-agnostic arithmetic kernel behind the monetary value types. Every routine is expressed purely in
/// terms of a <see cref="decimal" /> amount and a minor-unit scale (or its pre-computed <c>10^scale</c> factor) so that
/// the runtime-tagged <see cref="Money" /> and the type-tagged <see cref="Money{TCurrency}" /> share one implementation
/// of rounding, minor-unit scaling, and penny-accurate allocation rather than maintaining parallel copies.
/// </summary>
internal static class MoneyMath
{
    /// <summary>
    /// The maximum minor-unit precision a currency may declare, matching the <see cref="decimal" /> scale ceiling.
    /// </summary>
    internal const int MaxMinorUnits = 28;

    /// <summary>
    /// The largest share count allocated on the stack before falling back to a heap buffer.
    /// </summary>
    internal const int StackAllocShareThreshold = 64;

    /// <summary>
    /// Pre-computed <c>10^n</c> factors for <c>n</c> in the inclusive range <c>[0, 28]</c>, indexed by minor units.
    /// </summary>
    private static readonly decimal[] s_minorUnitFactors = BuildMinorUnitFactors();

    /// <summary>
    /// Returns the scale factor that converts between a currency's major and minor unit.
    /// </summary>
    /// <param name="minorUnits">The currency's minor-unit precision, in the inclusive range <c>[0, 28]</c>.</param>
    /// <returns><c>10 ^ <paramref name="minorUnits" /></c> as a <see cref="decimal" />.</returns>
    internal static decimal MinorUnitFactor(int minorUnits) =>
        s_minorUnitFactors[minorUnits];

    /// <summary>
    /// Rounds <paramref name="amount" /> to <paramref name="minorUnits" /> fractional digits using
    /// <paramref name="rounding" />. The single rounding entry point both monetary forms route through.
    /// </summary>
    /// <param name="amount">The raw amount to round.</param>
    /// <param name="minorUnits">The number of fractional digits to round to.</param>
    /// <param name="rounding">The midpoint-rounding rule.</param>
    /// <returns>The rounded amount.</returns>
    internal static decimal Round(decimal amount, int minorUnits, MidpointRounding rounding) =>
        decimal.Round(amount, minorUnits, rounding);

    /// <summary>
    /// Distributes <paramref name="amount" /> as evenly as possible across <paramref name="shares" />.<see
    /// cref="System.Span{T}.Length" /> slots so the per-slot amounts sum exactly to <paramref name="amount" />.
    /// </summary>
    /// <param name="amount">The amount to distribute, already at the currency's minor-unit precision.</param>
    /// <param name="minorUnits">The currency's minor-unit precision, in the inclusive range <c>[0, 28]</c>.</param>
    /// <param name="shares">The destination buffer; each element receives one share in major units.</param>
    /// <remarks>
    /// The residual minor units are handed out one per slot from the start of the buffer, preserving the sign of
    /// <paramref name="amount" />. Minor-unit counts are evaluated with <see cref="BigInteger" /> so large amounts
    /// allocate exactly rather than overflowing a fixed-width integer.
    /// </remarks>
    internal static void AllocateEvenly(decimal amount, int minorUnits, Span<decimal> shares)
    {
        var parts = shares.Length;
        BigInteger minorTotal = ToMinorUnits(amount, minorUnits);
        BigInteger basePer = minorTotal / parts;
        BigInteger residual = minorTotal - (basePer * parts);
        int sign = residual.Sign >= 0 ? 1 : -1;
        var residualMagnitude = (int)BigInteger.Abs(residual);

        for (var i = 0; i < parts; i++)
        {
            BigInteger share = basePer + (i < residualMagnitude ? sign : 0);
            shares[i] = FromMinorUnits(share, minorUnits);
        }
    }

    /// <summary>
    /// Distributes <paramref name="amount" /> across the supplied non-negative <paramref name="ratios" /> in proportion
    /// to their magnitudes, with the resulting shares summing exactly to <paramref name="amount" />.
    /// </summary>
    /// <param name="amount">The amount to distribute, already at the currency's minor-unit precision.</param>
    /// <param name="minorUnits">The currency's minor-unit precision, in the inclusive range <c>[0, 28]</c>.</param>
    /// <param name="ratios">The validated, non-negative weights; at least one must be strictly positive.</param>
    /// <returns>
    /// The per-ratio amounts in major units. Residual minor units are distributed by the largest-remainder
    /// (Hamilton) method — each slot receives one extra unit in descending order of its fractional remainder, ties
    /// broken by stable input order. Zero-ratio slots never receive residual.
    /// </returns>
    /// <remarks>
    /// The proportional split retains the established <see cref="decimal" /> remainder arithmetic so its results are
    /// unchanged, while the integer share counts are held in <see cref="BigInteger" /> so a large minor-unit total no
    /// longer overflows a fixed-width integer.
    /// </remarks>
    internal static decimal[] AllocateByRatios(decimal amount, int minorUnits, ReadOnlySpan<decimal> ratios)
    {
        var totalWeight = 0m;
        for (var i = 0; i < ratios.Length; i++)
            totalWeight += ratios[i];

        BigInteger minorTotalSigned = ToMinorUnits(amount, minorUnits);
        int sign = minorTotalSigned.Sign >= 0 ? 1 : -1;
        BigInteger minorTotal = BigInteger.Abs(minorTotalSigned);
        var minorTotalDecimal = (decimal)minorTotal;

        // Compute floored shares over absolute minor units; track each slot's fractional remainder so the residual
        // can go to the slot with the largest remainder (Hamilton / largest-remainder method).
        var shares = new BigInteger[ratios.Length];
        var remainders = new decimal[ratios.Length];
        BigInteger allocated = BigInteger.Zero;
        for (var i = 0; i < ratios.Length; i++)
        {
            var exact = minorTotalDecimal * ratios[i] / totalWeight;
            var floored = decimal.Truncate(exact);
            shares[i] = (BigInteger)floored;
            remainders[i] = exact - floored;
            allocated += shares[i];
        }

        var residual = (int)(minorTotal - allocated);
        if (residual > 0)
        {
            // Sort indices by (descending remainder, ascending index) so ties fall back to stable input order.
            var order = new int[ratios.Length];
            for (var i = 0; i < ratios.Length; i++)
                order[i] = i;

            var remaindersLocal = remainders;
            Array.Sort(order, (a, b) =>
            {
                var cmp = remaindersLocal[b].CompareTo(remaindersLocal[a]);
                return cmp != 0 ? cmp : a.CompareTo(b);
            });

            var distributed = 0;
            for (var k = 0; k < order.Length && distributed < residual; k++)
            {
                var idx = order[k];
                if (ratios[idx] <= 0m)
                    continue;     // never give residual to a zero-ratio slot

                shares[idx]++;
                distributed++;
            }
        }

        var result = new decimal[ratios.Length];
        for (var i = 0; i < ratios.Length; i++)
            result[i] = FromMinorUnits(sign < 0 ? -shares[i] : shares[i], minorUnits);

        return result;
    }

    /// <summary>
    /// Converts an amount already at minor-unit precision to its exact integer minor-unit count, scaling through
    /// <see cref="BigInteger" /> so the result never overflows.
    /// </summary>
    /// <param name="amount">The amount, already rounded to <paramref name="minorUnits" /> fractional digits.</param>
    /// <param name="minorUnits">The currency's minor-unit precision.</param>
    /// <returns>The amount expressed as a signed integer count of minor units.</returns>
    internal static BigInteger ToMinorUnits(decimal amount, int minorUnits)
    {
        Span<int> bits = stackalloc int[4];
        decimal.GetBits(amount, bits);

        var scale = (bits[3] >> 16) & 0xFF;
        var negative = bits[3] < 0;

        BigInteger significand = ((BigInteger)(uint)bits[2] << 64) | ((BigInteger)(uint)bits[1] << 32) | (uint)bits[0];

        // amount = significand * 10^-scale; the minor-unit count is significand * 10^(minorUnits - scale). A
        // value at minor-unit precision always has scale <= minorUnits, so the exponent is non-negative; the
        // defensive divide handles any value carrying spurious trailing-zero scale.
        BigInteger minor = scale <= minorUnits
            ? significand * BigInteger.Pow(10, minorUnits - scale)
            : significand / BigInteger.Pow(10, scale - minorUnits);

        return negative ? -minor : minor;
    }

    /// <summary>
    /// Converts a signed integer minor-unit count back to a major-unit <see cref="decimal" /> at
    /// <paramref name="minorUnits" /> precision.
    /// </summary>
    /// <param name="minor">The signed minor-unit count.</param>
    /// <param name="minorUnits">The currency's minor-unit precision.</param>
    /// <returns>The amount in major units.</returns>
    internal static decimal FromMinorUnits(BigInteger minor, int minorUnits)
    {
        BigInteger quotient = BigInteger.DivRem(minor, BigInteger.Pow(10, minorUnits), out BigInteger remainder);
        return (decimal)quotient + ((decimal)remainder / MinorUnitFactor(minorUnits));
    }

    /// <summary>
    /// Builds the <c>10^n</c> factor table for <c>n</c> in <c>[0, 28]</c>.
    /// </summary>
    /// <returns>The populated factor table indexed by minor units.</returns>
    private static decimal[] BuildMinorUnitFactors()
    {
        var factors = new decimal[MaxMinorUnits + 1];
        var factor = 1m;
        for (var i = 0; i <= MaxMinorUnits; i++)
        {
            factors[i] = factor;
            if (i < MaxMinorUnits)
                factor *= 10m;
        }

        return factors;
    }
}
