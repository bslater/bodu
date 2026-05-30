// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyValue.Allocation.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

public readonly partial struct MoneyValue
{
    /// <summary>
    /// Distributes this amount as evenly as possible across <paramref name="parts" /> shares, summing exactly to
    /// the original.
    /// </summary>
    /// <param name="parts">The number of shares to allocate.</param>
    /// <returns>The per-share allocation.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="parts" /> is less than or equal to zero.</exception>
    public MoneyValue[] Allocate(int parts)
    {
        ThrowHelper.ThrowIfLessThanOrEqual(parts, 0);

        decimal factor = MinorUnitFactor();
        long minorTotal = decimal.ToInt64(_amount * factor);
        long basePer = minorTotal / parts;
        long residual = minorTotal - (basePer * parts);
        long sign = residual >= 0 ? 1 : -1;
        long residualMagnitude = Math.Abs(residual);

        string iso = IsoCode;
        MoneyValue[] result = new MoneyValue[parts];
        for (int i = 0; i < parts; i++)
        {
            long share = basePer + (i < residualMagnitude ? sign : 0);
            result[i] = FromNormalized(share / factor, iso);
        }

        return result;
    }

    /// <summary>
    /// Distributes this amount across the supplied non-negative ratios in proportion to their magnitudes.
    /// </summary>
    /// <param name="ratios">The non-negative weights.</param>
    /// <returns>The per-ratio allocation.</returns>
    /// <exception cref="ArgumentException">The ratios are empty, contain a negative value, or sum to zero.</exception>
    public MoneyValue[] Allocate(ReadOnlySpan<decimal> ratios)
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

        decimal factor = MinorUnitFactor();
        long minorTotal = decimal.ToInt64(_amount * factor);

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

        string iso = IsoCode;
        MoneyValue[] result = new MoneyValue[ratios.Length];
        for (int i = 0; i < ratios.Length; i++)
            result[i] = FromNormalized(shares[i] / factor, iso);

        return result;
    }

    /// <summary>
    /// Computes the multiplier that scales between the major and minor unit of this currency.
    /// </summary>
    /// <returns><c>10 ^ MinorUnits</c>.</returns>
    private decimal MinorUnitFactor()
    {
        decimal factor = 1m;
        int minorUnits = MinorUnits;
        for (int i = 0; i < minorUnits; i++)
            factor *= 10m;
        return factor;
    }
}
