// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Money{T}.Properties.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

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
    /// <exception cref="InvalidOperationException">
    /// Thrown when <typeparamref name="TCurrency" /> reports invalid metadata.
    /// </exception>
    public static string IsoCode =>
        CurrencyMetadata<TCurrency>.Value.IsoCode;

    /// <summary>
    /// Gets the minor-unit precision of <typeparamref name="TCurrency" />.
    /// </summary>
    /// <returns>The non-negative number of fractional digits the currency uses.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <typeparamref name="TCurrency" /> reports invalid metadata.
    /// </exception>
    public static int MinorUnits =>
        CurrencyMetadata<TCurrency>.Value.MinorUnits;

    /// <summary>
    /// Gets the rounded monetary amount in the major unit of <typeparamref name="TCurrency" />.
    /// </summary>
    /// <returns>The amount stored by this instance, rounded to <see cref="MinorUnits" /> decimal places.</returns>
    public decimal Amount =>
        _amount;
}
