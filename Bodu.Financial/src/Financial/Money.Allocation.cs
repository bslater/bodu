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
    /// <exception cref="InvalidOperationException">
    /// This value is a default-initialised, currency-less <see cref="Money" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="parts" /> is less than or equal to zero.
    /// </exception>
    public Money[] Allocate(int parts)
    {
        EnsureHasCurrency();
        ThrowHelper.ThrowIfLessThanOrEqual(parts, 0);

        Span<decimal> shares = parts <= MoneyMath.StackAllocShareThreshold ? stackalloc decimal[parts] : new decimal[parts];
        MoneyMath.AllocateEvenly(_amount, MinorUnits, shares);

        var result = new Money[parts];
        for (var i = 0; i < parts; i++)
            result[i] = WithAmount(shares[i]);

        return result;
    }

    /// <summary>
    /// Distributes this amount across the supplied non-negative ratios in proportion to their magnitudes.
    /// </summary>
    /// <param name="ratios">The non-negative weights.</param>
    /// <returns>
    /// The per-ratio allocation, whose sum equals this instance. Residual minor units are distributed by the <i>
    /// largest-remainder method</i> — each slot receives one extra unit in descending order of its fractional
    /// remainder, with ties broken by stable input order. Zero-ratio slots never receive residual.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// This value is a default-initialised, currency-less <see cref="Money" />.
    /// </exception>
    /// <exception cref="ArgumentException">The ratios are empty, contain a negative value, or sum to zero.</exception>
    public Money[] Allocate(ReadOnlySpan<decimal> ratios)
    {
        EnsureHasCurrency();
        FinancialThrowHelper.ThrowIfAllocationRatiosInvalid(ratios);

        var shares = MoneyMath.AllocateByRatios(_amount, MinorUnits, ratios);

        var result = new Money[shares.Length];
        for (var i = 0; i < shares.Length; i++)
            result[i] = WithAmount(shares[i]);

        return result;
    }
}
