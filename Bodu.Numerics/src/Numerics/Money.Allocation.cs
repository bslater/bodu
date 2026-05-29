// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Money.Allocation.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

public readonly partial struct Money<TCurrency>
{
    /// <summary>
    /// Distributes this amount as evenly as possible across <paramref name="parts" /> shares, in a way that the
    /// shares sum exactly to the original amount.
    /// </summary>
    /// <param name="parts">The number of shares to allocate. Must be greater than zero.</param>
    /// <returns>
    /// An array of <paramref name="parts" /> <see cref="Money{TCurrency}" /> values whose sum equals this instance.
    /// Any residual minor units are distributed one per share from the start of the array, preserving the sign of
    /// the original amount.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="parts" /> is less than or equal to zero.</exception>
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

        long minorTotal = ToMinorUnits(_amount);
        long basePer = minorTotal / parts;
        long residual = minorTotal - (basePer * parts);
        long sign = residual >= 0 ? 1 : -1;
        long residualMagnitude = Math.Abs(residual);

        decimal factor = MinorUnitFactor();
        Money<TCurrency>[] result = new Money<TCurrency>[parts];
        for (int i = 0; i < parts; i++)
        {
            long share = basePer + (i < residualMagnitude ? sign : 0);
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
    /// An array of <see cref="Money{TCurrency}" /> values whose length equals <paramref name="ratios" />.<see cref="ReadOnlySpan{T}.Length" />
    /// and whose sum equals this instance. Any residual minor units are distributed across the positive-ratio
    /// shares from the start of the array.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="ratios" /> is empty, contains a negative element, or sums to zero.</exception>
    /// <exception cref="OverflowException">
    /// Thrown when the scaled minor-unit count exceeds the range of a 64-bit signed integer.
    /// </exception>
    public Money<TCurrency>[] Allocate(ReadOnlySpan<decimal> ratios)
    {
        if (ratios.IsEmpty)
            throw new ArgumentException("At least one ratio must be supplied.", nameof(ratios));

        decimal totalWeight = 0m;
        for (int i = 0; i < ratios.Length; i++)
        {
            if (ratios[i] < 0m)
                throw new ArgumentException("Ratios must not be negative.", nameof(ratios));
            totalWeight += ratios[i];
        }

        if (totalWeight == 0m)
            throw new ArgumentException("At least one ratio must be strictly positive.", nameof(ratios));

        long minorTotal = ToMinorUnits(_amount);
        decimal factor = MinorUnitFactor();

        long[] shares = new long[ratios.Length];
        long allocated = 0;
        for (int i = 0; i < ratios.Length; i++)
        {
            decimal exactShare = minorTotal * ratios[i] / totalWeight;
            long flooredShare = (long)decimal.Truncate(exactShare);
            shares[i] = flooredShare;
            allocated += flooredShare;
        }

        long residual = minorTotal - allocated;
        long sign = residual >= 0 ? 1 : -1;
        long residualMagnitude = Math.Abs(residual);

        if (residualMagnitude > 0)
        {
            int cursor = 0;
            long distributed = 0;
            while (distributed < residualMagnitude)
            {
                if (ratios[cursor] > 0m)
                {
                    shares[cursor] += sign;
                    distributed++;
                }

                cursor = (cursor + 1) % ratios.Length;
            }
        }

        Money<TCurrency>[] result = new Money<TCurrency>[ratios.Length];
        for (int i = 0; i < ratios.Length; i++)
            result[i] = Money<TCurrency>.FromNormalizedAmount(shares[i] / factor);

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
        decimal.ToInt64(amount * MinorUnitFactor());

    /// <summary>
    /// Returns the scale factor that converts between the major unit and the minor unit of
    /// <typeparamref name="TCurrency" />.
    /// </summary>
    /// <returns><c>10^TCurrency.MinorUnits</c> as a <see cref="decimal" />.</returns>
    private static decimal MinorUnitFactor()
    {
        decimal factor = 1m;
        int minorUnits = TCurrency.MinorUnits;
        for (int i = 0; i < minorUnits; i++)
            factor *= 10m;

        return factor;
    }
}
