// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Money.Helpers.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

/// <summary>
/// Provides type-inferring factory methods for <see cref="Money{TCurrency}" />.
/// </summary>
/// <remarks>
/// The methods on this class mirror the constructor and <see cref="Money{TCurrency}.Zero" /> property declared on
/// <see cref="Money{TCurrency}" /> but accept the currency type-parameter only, letting consumers write
/// <c>Money.Of&lt;USD&gt;(19.99m)</c> in place of <c>new Money&lt;USD&gt;(19.99m)</c>.
/// </remarks>
public static class Money
{
    /// <summary>
    /// Creates a <see cref="Money{TCurrency}" /> from the supplied amount, rounding to the currency's minor-unit
    /// precision using banker's rounding.
    /// </summary>
    /// <typeparam name="TCurrency">The currency type identifier.</typeparam>
    /// <param name="amount">The monetary amount in the major unit of <typeparamref name="TCurrency" />.</param>
    /// <returns>The constructed monetary value.</returns>
    public static Money<TCurrency> Of<TCurrency>(decimal amount)
        where TCurrency : ICurrency =>
        new(amount);

    /// <summary>
    /// Creates a <see cref="Money{TCurrency}" /> from the supplied amount and rounding rule.
    /// </summary>
    /// <typeparam name="TCurrency">The currency type identifier.</typeparam>
    /// <param name="amount">The monetary amount.</param>
    /// <param name="rounding">The midpoint-rounding rule applied when normalizing to the minor-unit precision.</param>
    /// <returns>The constructed monetary value.</returns>
    public static Money<TCurrency> Of<TCurrency>(decimal amount, MidpointRounding rounding)
        where TCurrency : ICurrency =>
        new(amount, rounding);

    /// <summary>
    /// Returns the zero value of <typeparamref name="TCurrency" />.
    /// </summary>
    /// <typeparam name="TCurrency">The currency type identifier.</typeparam>
    /// <returns>The zero monetary amount.</returns>
    public static Money<TCurrency> Zero<TCurrency>()
        where TCurrency : ICurrency =>
        default;
}
