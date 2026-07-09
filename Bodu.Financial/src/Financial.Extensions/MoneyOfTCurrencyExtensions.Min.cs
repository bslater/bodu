// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyOfTCurrencyExtensions.Min.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial.Extensions;

public static partial class MoneyOfTCurrencyExtensions
{
    /// <summary>
    /// Returns the smaller of two monetary values in the same currency.
    /// </summary>
    /// <typeparam name="TCurrency">The currency tag identifying the monetary type.</typeparam>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns>The lesser of <paramref name="left" /> and <paramref name="right" />.</returns>
    public static Money<TCurrency> Min<TCurrency>(Money<TCurrency> left, Money<TCurrency> right)
        where TCurrency : ICurrency =>
        left.Amount <= right.Amount ? left : right;
}
