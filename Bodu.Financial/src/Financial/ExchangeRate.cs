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
    /// The underlying observed rate used for precise conversion. For a non-inverted rate this equals <see cref="Rate" />;
    /// for an inverted rate it is the original reverse-pair rate, so conversion divides by it rather than multiplying by
    /// a pre-rounded reciprocal.
    /// </summary>
    private readonly decimal _observedRate;

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
        : this(fromIsoCode, toIsoCode, date, rate, isInverted ? 1m / rate : rate, provider, isInverted)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExchangeRate" /> struct from fully resolved field values, including
    /// the underlying observed rate.
    /// </summary>
    /// <param name="fromIsoCode">The source-currency ISO-style code.</param>
    /// <param name="toIsoCode">The destination-currency ISO-style code.</param>
    /// <param name="date">The calendar date on which the rate was observed.</param>
    /// <param name="rate">The multiplier that converts a source-currency amount to the destination currency.</param>
    /// <param name="observedRate">The underlying observed rate used for precise conversion.</param>
    /// <param name="provider">The non-empty identifier of the publishing source.</param>
    /// <param name="isInverted"><see langword="true" /> when derived from the reverse pair.</param>
    private ExchangeRate(
        string fromIsoCode,
        string toIsoCode,
        DateOnly date,
        decimal rate,
        decimal observedRate,
        string provider,
        bool isInverted)
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
        _observedRate = observedRate;
    }

    /// <summary>
    /// Creates an <see cref="ExchangeRate" /> from an originally observed rate, deriving the public <see cref="Rate" />
    /// multiplier from it. When <paramref name="isInverted" /> is <see langword="true" />, <paramref name="observedRate" />
    /// is the reverse-pair rate and conversion divides by it, avoiding the precision loss of multiplying by a pre-rounded
    /// reciprocal.
    /// </summary>
    /// <param name="fromIsoCode">The reported source-currency ISO-style code.</param>
    /// <param name="toIsoCode">The reported destination-currency ISO-style code.</param>
    /// <param name="date">The calendar date on which the rate was observed.</param>
    /// <param name="observedRate">The originally observed rate.</param>
    /// <param name="provider">The non-empty identifier of the publishing source.</param>
    /// <param name="isInverted"><see langword="true" /> when <paramref name="observedRate" /> is the reverse-pair rate.</param>
    /// <returns>The constructed exchange rate.</returns>
    internal static ExchangeRate FromObservedRate(
        string fromIsoCode,
        string toIsoCode,
        DateOnly date,
        decimal observedRate,
        string provider,
        bool isInverted)
    {
        ThrowHelper.ThrowIfZeroOrNegative(observedRate);

        var rate = isInverted ? 1m / observedRate : observedRate;
        return new ExchangeRate(fromIsoCode, toIsoCode, date, rate, observedRate, provider, isInverted);
    }

    /// <summary>
    /// Creates an <see cref="ExchangeRate" /> from independently supplied reported multiplier and underlying observed
    /// rate, preserving both exactly. Used when rehydrating a serialized rate so neither value is recomputed (and thus
    /// re-rounded) from the other.
    /// </summary>
    /// <param name="fromIsoCode">The source-currency ISO-style code.</param>
    /// <param name="toIsoCode">The destination-currency ISO-style code.</param>
    /// <param name="date">The calendar date on which the rate was observed.</param>
    /// <param name="rate">The reported source-to-destination multiplier.</param>
    /// <param name="observedRate">The underlying observed rate used for precise conversion.</param>
    /// <param name="provider">The non-empty identifier of the publishing source.</param>
    /// <param name="isInverted"><see langword="true" /> when derived from the reverse pair.</param>
    /// <returns>The constructed exchange rate.</returns>
    internal static ExchangeRate FromComponents(
        string fromIsoCode,
        string toIsoCode,
        DateOnly date,
        decimal rate,
        decimal observedRate,
        string provider,
        bool isInverted) =>
        new(fromIsoCode, toIsoCode, date, rate, observedRate, provider, isInverted);

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
    /// Gets the underlying observed rate used for precise conversion. Equals <see cref="Rate" /> for a non-inverted
    /// rate; for an inverted rate it is the original reverse-pair rate.
    /// </summary>
    /// <returns>The observed rate.</returns>
    internal decimal ObservedRate => _observedRate;

    /// <summary>
    /// Converts <paramref name="amount" /> from the source currency to the destination currency.
    /// </summary>
    /// <param name="amount">The amount in <see cref="FromIsoCode" /> to convert.</param>
    /// <returns>The converted amount in <see cref="ToIsoCode" />, unrounded.</returns>
    /// <remarks>
    /// A non-inverted rate multiplies by <see cref="Rate" />; an inverted rate divides by the original reverse-pair
    /// rate rather than multiplying by a pre-rounded reciprocal, avoiding a double-rounding step. Rounding is
    /// intentionally deferred to the money boundary so the rate object stays decoupled from the destination currency's
    /// minor-unit precision. Use
    /// <see cref="MoneyOfTCurrencyExchangeRateExtensions.ConvertTo{TSource, TTarget}(Money{TSource}, IDatedExchangeRateProvider, DateOnly, ExchangeRateLookupOptions?, MidpointRounding)" />
    /// ,
    /// <see cref="MoneyExchangeRateExtensions.ConvertTo(Money, IDatedExchangeRateProvider, string, DateOnly, ExchangeRateLookupOptions?, MidpointRounding)" />
    /// , or <see cref="Money{TCurrency}.Convert{TTarget}(decimal, MidpointRounding)" /> to apply rounding at the
    /// destination precision.
    /// </remarks>
    public decimal Convert(decimal amount) =>
        IsInverted ? amount / _observedRate : amount * _observedRate;

    /// <summary>
    /// Determines whether this rate equals <paramref name="other" /> by its public fields. The internal observed rate
    /// is excluded so two rates that report the same direction, date, multiplier, provider, and inversion compare equal
    /// regardless of how each was constructed.
    /// </summary>
    /// <param name="other">The rate to compare with.</param>
    /// <returns><see langword="true" /> when the public fields match; otherwise <see langword="false" />.</returns>
    public bool Equals(ExchangeRate other) =>
        FromIsoCode == other.FromIsoCode
        && ToIsoCode == other.ToIsoCode
        && Date == other.Date
        && Rate == other.Rate
        && Provider == other.Provider
        && IsInverted == other.IsInverted;

    /// <summary>
    /// Returns a hash code over the public fields, consistent with <see cref="Equals(ExchangeRate)" />.
    /// </summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode() =>
        HashCode.Combine(FromIsoCode, ToIsoCode, Date, Rate, Provider, IsInverted);
}
