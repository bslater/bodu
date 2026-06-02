// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Money.Allocation.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

public readonly partial struct Money
{
    /// <summary>
    /// Distributes this amount as evenly as possible across <paramref name="parts" /> shares, summing exactly to the
    /// original.
    /// </summary>
    /// <param name="parts">The number of shares to allocate.</param>
    /// <returns>The per-share allocation.</returns>
    /// <exception cref="InvalidOperationException">This value is a default-initialised, currency-less <see cref="Money" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="parts" /> is less than or equal to zero.
    /// </exception>
    /// <exception cref="OverflowException">
    /// The scaled minor-unit count exceeds the range of a 64-bit signed integer.
    /// </exception>
    public Money[] Allocate(int parts)
    {
        EnsureHasCurrency();
        ThrowHelper.ThrowIfLessThanOrEqual(parts, 0);

        var factor = MinorUnitFactor();
        var minorTotal = decimal.ToInt64(_amount * factor);
        var basePer = minorTotal / parts;
        var residual = minorTotal - (basePer * parts);
        long sign = residual >= 0 ? 1 : -1;
        var residualMagnitude = Math.Abs(residual);

        var result = new Money[parts];
        for (var i = 0; i < parts; i++)
        {
            var share = basePer + (i < residualMagnitude ? sign : 0);
            result[i] = WithAmount(share / factor);
        }

        return result;
    }

    /// <summary>
    /// Distributes this amount across the supplied non-negative ratios in proportion to their magnitudes.
    /// </summary>
    /// <param name="ratios">The non-negative weights.</param>
    /// <returns>
    /// The per-ratio allocation, whose sum equals this instance. Residual minor units are distributed by the
    /// <i>largest-remainder method</i> — each slot receives one extra unit in descending order of its fractional
    /// remainder, with ties broken by stable input order. Zero-ratio slots never receive residual.
    /// </returns>
    /// <exception cref="InvalidOperationException">This value is a default-initialised, currency-less <see cref="Money" />.</exception>
    /// <exception cref="ArgumentException">The ratios are empty, contain a negative value, or sum to zero.</exception>
    /// <exception cref="OverflowException">
    /// The scaled minor-unit count exceeds the range of a 64-bit signed integer.
    /// </exception>
    public Money[] Allocate(ReadOnlySpan<decimal> ratios)
    {
        EnsureHasCurrency();
        FinancialThrowHelper.ThrowIfAllocationRatiosInvalid(ratios);

        var totalWeight = 0m;
        for (var i = 0; i < ratios.Length; i++)
            totalWeight += ratios[i];

        var factor = MinorUnitFactor();
        var minorTotalSigned = decimal.ToInt64(_amount * factor);
        long sign = minorTotalSigned >= 0 ? 1 : -1;
        var minorTotal = Math.Abs(minorTotalSigned);

        // Compute floored shares over absolute minor units; track each slot's fractional remainder so the
        // residual can go to the slot with the largest remainder (Hamilton / largest-remainder method).
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

        var result = new Money[ratios.Length];
        for (var i = 0; i < ratios.Length; i++)
            result[i] = WithAmount(sign * shares[i] / factor);

        return result;
    }

    /// <summary>
    /// Computes the multiplier that scales between the major and minor unit of this currency.
    /// </summary>
    /// <returns><c>10 ^ MinorUnits</c>.</returns>
    private decimal MinorUnitFactor()
    {
        var factor = 1m;
        var minorUnits = MinorUnits;
        for (var i = 0; i < minorUnits; i++)
            factor *= 10m;
        return factor;
    }
}
