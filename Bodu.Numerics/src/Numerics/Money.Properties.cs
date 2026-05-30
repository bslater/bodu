// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Money.Properties.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

public readonly partial struct Money<TCurrency>
{
    /// <summary>
    /// Gets a <see cref="Money{TCurrency}" /> representing zero of <typeparamref name="TCurrency" />.
    /// </summary>
    /// <returns>The zero monetary amount.</returns>
    public static Money<TCurrency> Zero =>
        default;

    /// <summary>
    /// Gets the ISO 4217 alphabetic code of <typeparamref name="TCurrency" />.
    /// </summary>
    /// <returns>The currency code, such as <c>"USD"</c>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when <typeparamref name="TCurrency" /> reports invalid metadata.</exception>
    public static string IsoCode =>
        CurrencyMetadata<TCurrency>.Value.IsoCode;

    /// <summary>
    /// Gets the minor-unit precision of <typeparamref name="TCurrency" />.
    /// </summary>
    /// <returns>The non-negative number of fractional digits the currency uses.</returns>
    /// <exception cref="InvalidOperationException">Thrown when <typeparamref name="TCurrency" /> reports invalid metadata.</exception>
    public static int MinorUnits =>
        CurrencyMetadata<TCurrency>.Value.MinorUnits;

    /// <summary>
    /// Gets the rounded monetary amount in the major unit of <typeparamref name="TCurrency" />.
    /// </summary>
    /// <returns>The amount stored by this instance, rounded to <see cref="MinorUnits" /> decimal places.</returns>
    public decimal Amount =>
        _amount;

    /// <summary>
    /// Gets a value indicating whether this instance represents zero.
    /// </summary>
    /// <returns><see langword="true" /> when <see cref="Amount" /> is zero; otherwise <see langword="false" />.</returns>
    public bool IsZero =>
        _amount == 0m;

    /// <summary>
    /// Gets a value indicating whether this instance is strictly greater than zero.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when <see cref="Amount" /> is positive; <see langword="false" /> when it is zero
    /// or negative.
    /// </returns>
    public bool IsPositive =>
        _amount > 0m;

    /// <summary>
    /// Gets a value indicating whether this instance is strictly less than zero.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when <see cref="Amount" /> is negative; <see langword="false" /> when it is zero
    /// or positive.
    /// </returns>
    public bool IsNegative =>
        _amount < 0m;

    /// <summary>
    /// Gets the sign of this instance.
    /// </summary>
    /// <returns>
    /// <c>-1</c> when the amount is negative, <c>0</c> when the amount is zero, and <c>1</c> when the amount is positive.
    /// </returns>
    public int Sign =>
        Math.Sign(_amount);

    /// <summary>
    /// Gets the absolute value of this instance.
    /// </summary>
    /// <returns>A <see cref="Money{TCurrency}" /> whose amount is <c>|Amount|</c>.</returns>
    public Money<TCurrency> Abs =>
        Money<TCurrency>.FromNormalizedAmount(Math.Abs(_amount));
}
