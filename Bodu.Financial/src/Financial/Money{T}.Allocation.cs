// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Money{T}.Allocation.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

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
    /// <remarks>
    /// For example, <c>new Money&lt;USD&gt;(0.10m).Allocate(3)</c> returns <c>[0.04, 0.03, 0.03]</c>, and
    /// <c>new Money&lt;USD&gt;(-10m).Allocate(3)</c> returns <c>[-3.34, -3.33, -3.33]</c>. When the amount has fewer
    /// minor units than <paramref name="parts" />, trailing shares are zero (for example,
    /// <c>new Money&lt;USD&gt;(0.02m).Allocate(5)</c> returns <c>[0.01, 0.01, 0, 0, 0]</c>).
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// using Bodu.Financial;
    /// using Bodu.Financial.Currencies;
    ///
    /// // Split a bill three ways without losing a cent.
    /// Money<USD>[] shares = new Money<USD>(10m).Allocate(3);   // [3.34, 3.33, 3.33]
    ///
    /// Money<USD> total = shares[0] + shares[1] + shares[2];    // 10.00 USD (exact)
    ///]]>
    /// </code>
    /// </example>
    public Money<TCurrency>[] Allocate(int parts)
    {
        ThrowHelper.ThrowIfLessThanOrEqual(parts, 0);

        int minorUnits = CurrencyMetadata<TCurrency>.Value.MinorUnits;
        Span<decimal> shares = parts <= MoneyMath.StackAllocShareThreshold ? stackalloc decimal[parts] : new decimal[parts];
        MoneyMath.AllocateEvenly(_amount, minorUnits, shares);

        var result = new Money<TCurrency>[parts];
        for (int i = 0; i < parts; i++)
            result[i] = Money<TCurrency>.FromNormalizedAmount(shares[i]);

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
    public Money<TCurrency>[] Allocate(ReadOnlySpan<decimal> ratios)
    {
        FinancialThrowHelper.ThrowIfAllocationRatiosInvalid(ratios);

        int minorUnits = CurrencyMetadata<TCurrency>.Value.MinorUnits;
        decimal[] shares = MoneyMath.AllocateByRatios(_amount, minorUnits, ratios);

        var result = new Money<TCurrency>[shares.Length];
        for (int i = 0; i < shares.Length; i++)
            result[i] = Money<TCurrency>.FromNormalizedAmount(shares[i]);

        return result;
    }
}
