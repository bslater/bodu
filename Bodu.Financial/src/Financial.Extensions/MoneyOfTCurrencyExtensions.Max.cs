// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyOfTCurrencyExtensions.Max.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.Extensions;

public static partial class MoneyOfTCurrencyExtensions
{
    /// <summary>
    /// Returns the larger of two monetary values in the same currency.
    /// </summary>
    /// <typeparam name="TCurrency">The currency tag identifying the monetary type.</typeparam>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns>The greater of <paramref name="left" /> and <paramref name="right" />.</returns>
    public static Money<TCurrency> Max<TCurrency>(Money<TCurrency> left, Money<TCurrency> right)
        where TCurrency : ICurrency =>
        left.Amount >= right.Amount ? left : right;
}
