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
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="parts" /> is less than or equal to zero.
    /// </exception>
    public Money[] Allocate(int parts)
    {
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
    /// <returns>The per-ratio allocation.</returns>
    /// <exception cref="ArgumentException">The ratios are empty, contain a negative value, or sum to zero.</exception>
    public Money[] Allocate(ReadOnlySpan<decimal> ratios)
    {
        FinancialThrowHelper.ThrowIfAllocationRatiosInvalid(ratios);

        var totalWeight = 0m;
        for (var i = 0; i < ratios.Length; i++)
            totalWeight += ratios[i];

        var factor = MinorUnitFactor();
        var minorTotal = decimal.ToInt64(_amount * factor);

        var shares = new long[ratios.Length];
        long allocated = 0;
        for (var i = 0; i < ratios.Length; i++)
        {
            var exactShare = minorTotal * ratios[i] / totalWeight;
            var flooredShare = (long)decimal.Truncate(exactShare);
            shares[i] = flooredShare;
            allocated += flooredShare;
        }

        var residual = minorTotal - allocated;
        long sign = residual >= 0 ? 1 : -1;
        var residualMagnitude = Math.Abs(residual);

        if (residualMagnitude > 0)
        {
            var cursor = 0;
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

        var result = new Money[ratios.Length];
        for (var i = 0; i < ratios.Length; i++)
            result[i] = WithAmount(shares[i] / factor);

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
