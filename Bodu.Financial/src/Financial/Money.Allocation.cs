// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Money.Allocation.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

public readonly partial struct Money<TCurrency>
{
    /// <summary>
    /// Distributes this amount as evenly as possible across <paramref name="parts" /> shares, in a way that the shares
    /// sum exactly to the original amount.
    /// </summary>
    /// <param name="parts">The number of shares to allocate. Must be greater than zero.</param>
    /// <returns>
    /// An array of <paramref name="parts" /> <see cref="Money{TCurrency}" /> values whose sum equals this instance. Any
    /// residual minor units are distributed one per share from the start of the array, preserving the sign of the
    /// original amount.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="parts" /> is less than or equal to zero.
    /// </exception>
    /// <exception cref="OverflowException">
    /// Thrown when the scaled minor-unit count exceeds the range of a 64-bit signed integer.
    /// </exception>
    /// <remarks>
    /// For example, <c>new Money&lt;USD&gt;(0.10m).Allocate(3)</c> returns <c>[0.04, 0.03, 0.03]</c>, and
    /// <c>new Money&lt;USD&gt;(-10m).Allocate(3)</c> returns <c>[-3.34, -3.33, -3.33]</c>. When the amount has fewer
    /// minor units than <paramref name="parts" />, trailing shares are zero (for example,
    /// <c>new Money&lt;USD&gt;(0.02m).Allocate(5)</c> returns <c>[0.01, 0.01, 0, 0, 0]</c>).
    /// </remarks>
    public Money<TCurrency>[] Allocate(int parts)
    {
        ThrowHelper.ThrowIfLessThanOrEqual(parts, 0);

        var minorTotal = ToMinorUnits(_amount);
        var basePer = minorTotal / parts;
        var residual = minorTotal - (basePer * parts);
        long sign = residual >= 0 ? 1 : -1;
        var residualMagnitude = Math.Abs(residual);

        var factor = MinorUnitFactor();
        var result = new Money<TCurrency>[parts];
        for (var i = 0; i < parts; i++)
        {
            var share = basePer + (i < residualMagnitude ? sign : 0);
            result[i] = Money<TCurrency>.FromNormalizedAmount(share / factor);
        }

        return result;
    }

    /// <summary>
    /// Distributes this amount across the supplied non-negative <paramref name="ratios" /> in proportion to their
    /// magnitudes, ensuring the resulting shares sum exactly to the original amount.
    /// </summary>
    /// <param name="ratios">
    /// The non-negative weight applied to each share. At least one weight must be strictly positive; zero weights
    /// produce a zero share.
    /// </param>
    /// <returns>
    /// An array of <see cref="Money{TCurrency}" /> values whose length equals <paramref name="ratios" />.
    /// <see cref="ReadOnlySpan{T}.Length" /> and whose sum equals this instance. Residual minor units are distributed
    /// by the <i>largest-remainder method</i> — each slot receives one extra unit in descending order of its fractional
    /// remainder, with ties broken by stable input order. Zero-ratio slots never receive residual.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="ratios" /> is empty, contains a negative element, or sums to zero.
    /// </exception>
    /// <exception cref="OverflowException">
    /// Thrown when the scaled minor-unit count exceeds the range of a 64-bit signed integer.
    /// </exception>
    public Money<TCurrency>[] Allocate(ReadOnlySpan<decimal> ratios)
    {
        FinancialThrowHelper.ThrowIfAllocationRatiosInvalid(ratios);

        var totalWeight = 0m;
        for (var i = 0; i < ratios.Length; i++)
            totalWeight += ratios[i];

        var minorTotalSigned = ToMinorUnits(_amount);
        long sign = minorTotalSigned >= 0 ? 1 : -1;
        var minorTotal = Math.Abs(minorTotalSigned);
        var factor = MinorUnitFactor();

        // Compute floored shares over absolute minor units; track each slot's fractional remainder so the
        // residual can go to the slot with the largest remainder (Hamilton method).
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

            // Local capture to allow Array.Sort with span.
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

        var result = new Money<TCurrency>[ratios.Length];
        for (var i = 0; i < ratios.Length; i++)
            result[i] = Money<TCurrency>.FromNormalizedAmount(sign * shares[i] / factor);

        return result;
    }

    /// <summary>
    /// Converts the rounded major-unit amount to an exact minor-unit count.
    /// </summary>
    /// <param name="amount">The amount, already rounded to <c>TCurrency.MinorUnits</c>.</param>
    /// <returns>The amount expressed as an integer count of minor units.</returns>
    /// <exception cref="OverflowException">
    /// Thrown when the scaled minor-unit count exceeds the range of a 64-bit signed integer.
    /// </exception>
    private static long ToMinorUnits(decimal amount) =>
        decimal.ToInt64(amount * CurrencyMetadata<TCurrency>.Value.MinorUnitFactor);

    /// <summary>
    /// Returns the scale factor that converts between the major unit and the minor unit of
    /// <typeparamref name="TCurrency" />, as cached by the validated metadata descriptor.
    /// </summary>
    /// <returns><c>10^MinorUnits</c> as a <see cref="decimal" />.</returns>
    private static decimal MinorUnitFactor() =>
        CurrencyMetadata<TCurrency>.Value.MinorUnitFactor;
}
