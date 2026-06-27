// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRatePair.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;
using System.Text.Json.Serialization;
using Bodu.Financial.Currencies;
using Bodu.Financial.Serialization;

namespace Bodu.Financial;

/// <summary>
/// Represents an ordered pair of currencies that identifies the direction of an exchange-rate quotation.
/// </summary>
/// <remarks>
/// <para>
/// A pair is a strongly typed key intended for use in dictionaries and lookup tables. Compared to a raw tuple of codes,
/// it centralises validation and makes the directional meaning of each currency obvious at the call site.
/// </para>
/// </remarks>
[DebuggerDisplay("{From,nq}/{To,nq}")]
[JsonConverter(typeof(ExchangeRatePairJsonConverter))]
public readonly record struct ExchangeRatePair
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExchangeRatePair" /> class. Initializes a new instance of the
    /// <see cref="ExchangeRatePair" /> struct.
    /// </summary>
    /// <param name="from">The source currency.</param>
    /// <param name="to">The destination currency.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="from" /> or <paramref name="to" /> is <see cref="CurrencyCode.None" /> or is not a
    /// defined <see cref="CurrencyCode" /> member.
    /// </exception>
    public ExchangeRatePair(CurrencyCode from, CurrencyCode to)
    {
        FinancialThrowHelper.ThrowIfNotDefinedCurrencyCode(from);
        FinancialThrowHelper.ThrowIfNotDefinedCurrencyCode(to);

        From = from;
        To = to;
    }

    /// <summary>
    /// Gets the source currency.
    /// </summary>
    /// <value>The currency an amount is converted from.</value>
    public CurrencyCode From { get; }

    /// <summary>
    /// Gets the destination currency.
    /// </summary>
    /// <value>The currency an amount is converted to.</value>
    public CurrencyCode To { get; }

    /// <summary>
    /// Gets a value indicating whether this instance carries two real currencies and can therefore be used safely as a
    /// directional key.
    /// </summary>
    /// <value>
    /// <see langword="true" /> when both <see cref="From" /> and <see cref="To" /> are not
    /// <see cref="CurrencyCode.None" />; otherwise <see langword="false" />.
    /// </value>
    /// <remarks>
    /// Because <see cref="ExchangeRatePair" /> is a value type, <see langword="default" /><c>(ExchangeRatePair)</c>
    /// bypasses the validating constructor and leaves both currencies <see cref="CurrencyCode.None" />. Public
    /// boundaries that accept an <see cref="ExchangeRatePair" /> should reject any instance whose
    /// <see cref="IsValid" /> property is <see langword="false" />.
    /// </remarks>
    public bool IsValid => From != CurrencyCode.None && To != CurrencyCode.None;

    /// <summary>
    /// Returns the inverse pair with <see cref="From" /> and <see cref="To" /> swapped.
    /// </summary>
    /// <returns>A new <see cref="ExchangeRatePair" /> describing the reverse-direction quotation.</returns>
    public ExchangeRatePair Inverse() => new(To, From);
}
