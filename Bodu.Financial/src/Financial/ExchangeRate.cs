// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;
using System.Text.Json.Serialization;
using Bodu.Financial.Serialization;

namespace Bodu.Financial;

/// <summary>
/// Represents a single dated foreign-exchange rate observation produced by a named provider.
/// </summary>
/// <remarks>
/// <para>
/// An <see cref="ExchangeRate" /> is an immutable value object intended to be passed back from a provider together with
/// resolution metadata in an <see cref="ExchangeRateLookupResult" />. It carries enough context — direction, date,
/// provider name, and inversion flag — for downstream auditability (for example, tax and accounting reports) without
/// requiring the caller to reach back into the provider.
/// </para>
/// </remarks>
[DebuggerDisplay("{FromIsoCode,nq}->{ToIsoCode,nq} @ {Date,nq} = {Rate} ({Provider,nq})")]
[JsonConverter(typeof(ExchangeRateJsonConverter))]
public readonly record struct ExchangeRate
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExchangeRate" /> class.
    /// </summary>
    /// <param name="fromIsoCode">The source-currency ISO-style code.</param>
    /// <param name="toIsoCode">The destination-currency ISO-style code.</param>
    /// <param name="date">The calendar date on which the rate was observed.</param>
    /// <param name="rate">The multiplier that converts a source-currency amount to the destination currency.</param>
    /// <param name="provider">The non-empty identifier of the publishing source.</param>
    /// <param name="isInverted">
    /// <see langword="true" /> when the rate was derived from the reverse pair; otherwise <see langword="false" />.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="fromIsoCode" />, <paramref name="toIsoCode" />, or <paramref name="provider" /> is
    /// <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="fromIsoCode" /> or <paramref name="toIsoCode" /> is not a three-character uppercase
    /// ASCII code, or if <paramref name="provider" /> is empty or white-space.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="rate" /> is zero or negative.
    /// </exception>
    public ExchangeRate(
        string fromIsoCode,
        string toIsoCode,
        DateOnly date,
        decimal rate,
        string provider,
        bool isInverted = false)
    {
        FinancialThrowHelper.ThrowIfNotValidIsoCode(fromIsoCode);
        FinancialThrowHelper.ThrowIfNotValidIsoCode(toIsoCode);
        FinancialThrowHelper.ThrowIfNullOrWhiteSpaceProvider(provider);
        ThrowHelper.ThrowIfZeroOrNegative(rate);

        FromIsoCode = fromIsoCode;
        ToIsoCode = toIsoCode;
        Date = date;
        Rate = rate;
        Provider = provider;
        IsInverted = isInverted;
    }

    /// <summary>
    /// Gets the source-currency ISO-style code.
    /// </summary>
    /// <returns>The three-character uppercase ASCII source-currency code.</returns>
    public string FromIsoCode { get; }

    /// <summary>
    /// Gets the destination-currency ISO-style code.
    /// </summary>
    /// <returns>The three-character uppercase ASCII destination-currency code.</returns>
    public string ToIsoCode { get; }

    /// <summary>
    /// Gets the calendar date on which the rate was observed.
    /// </summary>
    /// <returns>The observation date.</returns>
    public DateOnly Date { get; }

    /// <summary>
    /// Gets the multiplier that converts an amount in <see cref="FromIsoCode" /> to <see cref="ToIsoCode" />.
    /// </summary>
    /// <returns>A strictly positive multiplier.</returns>
    public decimal Rate { get; }

    /// <summary>
    /// Gets the non-empty identifier of the publishing source.
    /// </summary>
    /// <returns>The provider identifier.</returns>
    public string Provider { get; }

    /// <summary>
    /// Gets a value indicating whether this rate was derived from the reverse pair.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when <see cref="Rate" /> is the reciprocal of an originally published reverse-direction
    /// rate; otherwise <see langword="false" />.
    /// </returns>
    public bool IsInverted { get; }

    /// <summary>
    /// Converts <paramref name="amount" /> from the source currency to the destination currency by multiplying by
    /// <see cref="Rate" />.
    /// </summary>
    /// <param name="amount">The amount in <see cref="FromIsoCode" /> to convert.</param>
    /// <returns>The converted amount in <see cref="ToIsoCode" />, unrounded.</returns>
    /// <remarks>
    /// Rounding is intentionally deferred to the money boundary so the rate object stays decoupled from the destination
    /// currency's minor-unit precision. Use
    /// <see cref="MoneyOfTCurrencyExchangeRateExtensions.ConvertTo{TSource, TTarget}(Money{TSource}, IDatedExchangeRateProvider, DateOnly, ExchangeRateLookupOptions?, MidpointRounding)" />
    /// ,
    /// <see cref="MoneyExchangeRateExtensions.ConvertTo(Money, IDatedExchangeRateProvider, string, DateOnly, ExchangeRateLookupOptions?, MidpointRounding)" />
    /// , or <see cref="Money{TCurrency}.Convert{TTarget}(decimal, MidpointRounding)" /> to apply rounding at the
    /// destination precision.
    /// </remarks>
    public decimal Convert(decimal amount) => amount * Rate;
}
