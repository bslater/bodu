// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyMath.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

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
    /// <param name="factor">The minor-unit factor <c>10^MinorUnits</c> for the currency.</param>
    /// <param name="shares">The destination buffer; each element receives one share in major units.</param>
    /// <exception cref="OverflowException">
    /// The scaled minor-unit count exceeds the range of a 64-bit signed integer.
    /// </exception>
    /// <remarks>
    /// The residual minor units are handed out one per slot from the start of the buffer, preserving the sign of
    /// <paramref name="amount" />.
    /// </remarks>
    internal static void AllocateEvenly(decimal amount, decimal factor, Span<decimal> shares)
    {
        var parts = shares.Length;
        var minorTotal = decimal.ToInt64(amount * factor);
        var basePer = minorTotal / parts;
        var residual = minorTotal - (basePer * parts);
        long sign = residual >= 0 ? 1 : -1;
        var residualMagnitude = Math.Abs(residual);

        for (var i = 0; i < parts; i++)
        {
            var share = basePer + (i < residualMagnitude ? sign : 0);
            shares[i] = share / factor;
        }
    }

    /// <summary>
    /// Distributes <paramref name="amount" /> across the supplied non-negative <paramref name="ratios" /> in proportion
    /// to their magnitudes, with the resulting shares summing exactly to <paramref name="amount" />.
    /// </summary>
    /// <param name="amount">The amount to distribute, already at the currency's minor-unit precision.</param>
    /// <param name="factor">The minor-unit factor <c>10^MinorUnits</c> for the currency.</param>
    /// <param name="ratios">The validated, non-negative weights; at least one must be strictly positive.</param>
    /// <returns>
    /// The per-ratio amounts in major units. Residual minor units are distributed by the largest-remainder
    /// (Hamilton) method — each slot receives one extra unit in descending order of its fractional remainder, ties
    /// broken by stable input order. Zero-ratio slots never receive residual.
    /// </returns>
    /// <exception cref="OverflowException">
    /// The scaled minor-unit count exceeds the range of a 64-bit signed integer.
    /// </exception>
    internal static decimal[] AllocateByRatios(decimal amount, decimal factor, ReadOnlySpan<decimal> ratios)
    {
        var totalWeight = 0m;
        for (var i = 0; i < ratios.Length; i++)
            totalWeight += ratios[i];

        var minorTotalSigned = decimal.ToInt64(amount * factor);
        long sign = minorTotalSigned >= 0 ? 1 : -1;
        var minorTotal = Math.Abs(minorTotalSigned);

        // Compute floored shares over absolute minor units; track each slot's fractional remainder so the residual
        // can go to the slot with the largest remainder (Hamilton / largest-remainder method).
        var shares = new long[ratios.Length];
        var remainders = new decimal[ratios.Length];
        long allocated = 0;
        for (var i = 0; i < ratios.Length; i++)
        {
            var exact = minorTotal * ratios[i] / totalWeight;
            var floored = decimal.Truncate(exact);
            shares[i] = (long)floored;
            remainders[i] = exact - floored;
            allocated += shares[i];
        }

        var residual = minorTotal - allocated;
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

            long distributed = 0;
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
            result[i] = sign * shares[i] / factor;

        return result;
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
