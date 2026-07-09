// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;
using System.Text.Json.Serialization;
using Bodu.Financial.Currencies;
using Bodu.Financial.Serialization;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Represents a single dated foreign-exchange rate observation produced by a named provider.
/// </summary>
/// <remarks>
/// <para>
/// An <see cref="ExchangeRate" /> is an immutable value object intended to be passed back from a provider together with
/// resolution metadata in an <see cref="RateLookupResult" />. It carries enough context — direction, date, provider
/// name, and inversion flag — for downstream auditability (for example, tax and accounting reports) without requiring
/// the caller to reach back into the provider.
/// </para>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using Bodu.Financial;
///
/// // A single USD -> EUR observation published by "ECB" on a given day.
/// var rate = new ExchangeRate(
///     CurrencyCode.USD, CurrencyCode.EUR, new DateOnly(2024, 3, 1), 0.92m, "ECB");
///
/// // Apply it; rounding is deferred to the money boundary.
/// decimal euros = rate.Convert(100m);   // 92.00
///]]>
/// </code>
/// </example>
/// </remarks>
[DebuggerDisplay("{From,nq}->{To,nq} @ {Date,nq} = {Rate} ({Provider,nq})")]
[JsonConverter(typeof(ExchangeRateJsonConverter))]
public readonly record struct ExchangeRate
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExchangeRate" /> class.
    /// </summary>
    /// <param name="from">The source currency.</param>
    /// <param name="to">The destination currency.</param>
    /// <param name="date">The calendar date on which the rate was observed.</param>
    /// <param name="rate">The multiplier that converts a source-currency amount to the destination currency.</param>
    /// <param name="provider">The non-empty identifier of the publishing source.</param>
    /// <param name="isInverted">
    /// <see langword="true" /> when the rate was derived from the reverse pair; otherwise <see langword="false" />.
    /// </param>
    /// <param name="fetchedAtUtc">
    /// The UTC instant at which the upstream data backing this rate was originally fetched, or <see langword="null" />
    /// when not tracked.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="provider" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="provider" /> is empty or white-space.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="from" /> or <paramref name="to" /> is not a defined currency, or if
    /// <paramref name="rate" /> is zero or negative.
    /// </exception>
    public ExchangeRate(
        CurrencyCode from,
        CurrencyCode to,
        DateOnly date,
        decimal rate,
        string provider,
        bool isInverted = false,
        DateTimeOffset? fetchedAtUtc = null)
        : this(from, to, date, rate, isInverted ? 1m / rate : rate, provider, isInverted, fetchedAtUtc)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExchangeRate" /> class from fully resolved field values, including
    /// the underlying observed rate.
    /// </summary>
    /// <param name="from">The source currency.</param>
    /// <param name="to">The destination currency.</param>
    /// <param name="date">The calendar date on which the rate was observed.</param>
    /// <param name="rate">The multiplier that converts a source-currency amount to the destination currency.</param>
    /// <param name="observedRate">The underlying observed rate used for precise conversion.</param>
    /// <param name="provider">The non-empty identifier of the publishing source.</param>
    /// <param name="isInverted"><see langword="true" /> when derived from the reverse pair.</param>
    /// <param name="fetchedAtUtc">
    /// The UTC instant at which the upstream data backing this rate was originally fetched, or <see langword="null" />
    /// when not tracked.
    /// </param>
    private ExchangeRate(
        CurrencyCode from,
        CurrencyCode to,
        DateOnly date,
        decimal rate,
        decimal observedRate,
        string provider,
        bool isInverted,
        DateTimeOffset? fetchedAtUtc)
    {
        FinancialThrowHelper.ThrowIfNotDefinedCurrencyCode(from);
        FinancialThrowHelper.ThrowIfNotDefinedCurrencyCode(to);
        ThrowHelper.ThrowIfNullOrWhiteSpace(provider);
        ThrowHelper.ThrowIfZeroOrNegative(rate);

        From = from;
        To = to;
        Date = date;
        Rate = rate;
        Provider = provider;
        IsInverted = isInverted;
        ObservedRate = observedRate;
        FetchedAtUtc = fetchedAtUtc;
    }

    /// <summary>
    /// Creates an <see cref="ExchangeRate" /> from an originally observed rate, deriving the public <see cref="Rate" />
    /// multiplier from it. When <paramref name="isInverted" /> is <see langword="true" />,
    /// <paramref name="observedRate" /> is the reverse-pair rate and conversion divides by it, avoiding the precision
    /// loss of multiplying by a pre-rounded reciprocal.
    /// </summary>
    /// <param name="from">The reported source currency.</param>
    /// <param name="to">The reported destination currency.</param>
    /// <param name="date">The calendar date on which the rate was observed.</param>
    /// <param name="observedRate">The originally observed rate.</param>
    /// <param name="provider">The non-empty identifier of the publishing source.</param>
    /// <param name="isInverted">
    /// <see langword="true" /> when <paramref name="observedRate" /> is the reverse-pair rate.
    /// </param>
    /// <param name="fetchedAtUtc">
    /// The UTC instant at which the upstream data backing this rate was originally fetched, or <see langword="null" />
    /// when not tracked.
    /// </param>
    /// <returns>The constructed exchange rate.</returns>
    internal static ExchangeRate FromObservedRate(
        CurrencyCode from,
        CurrencyCode to,
        DateOnly date,
        decimal observedRate,
        string provider,
        bool isInverted,
        DateTimeOffset? fetchedAtUtc = null)
    {
        ThrowHelper.ThrowIfZeroOrNegative(observedRate);

        decimal rate = isInverted ? 1m / observedRate : observedRate;
        return new ExchangeRate(from, to, date, rate, observedRate, provider, isInverted, fetchedAtUtc);
    }

    /// <summary>
    /// Creates an <see cref="ExchangeRate" /> from independently supplied reported multiplier and underlying observed
    /// rate, preserving both exactly. Used when rehydrating a serialized rate so neither value is recomputed (and thus
    /// re-rounded) from the other.
    /// </summary>
    /// <param name="from">The source currency.</param>
    /// <param name="to">The destination currency.</param>
    /// <param name="date">The calendar date on which the rate was observed.</param>
    /// <param name="rate">The reported source-to-destination multiplier.</param>
    /// <param name="observedRate">The underlying observed rate used for precise conversion.</param>
    /// <param name="provider">The non-empty identifier of the publishing source.</param>
    /// <param name="isInverted"><see langword="true" /> when derived from the reverse pair.</param>
    /// <param name="fetchedAtUtc">
    /// The UTC instant at which the upstream data backing this rate was originally fetched, or <see langword="null" />
    /// when not tracked.
    /// </param>
    /// <returns>The constructed exchange rate.</returns>
    internal static ExchangeRate FromComponents(
        CurrencyCode from,
        CurrencyCode to,
        DateOnly date,
        decimal rate,
        decimal observedRate,
        string provider,
        bool isInverted,
        DateTimeOffset? fetchedAtUtc = null) =>
        new(from, to, date, rate, observedRate, provider, isInverted, fetchedAtUtc);

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
    /// Gets the directional currency pair this rate quotes.
    /// </summary>
    /// <value>An <see cref="CurrencyPair" /> of <see cref="From" /> and <see cref="To" />.</value>
    public CurrencyPair Pair => new(From, To);

    /// <summary>
    /// Gets the calendar date on which the rate was observed.
    /// </summary>
    /// <value>The observation date.</value>
    public DateOnly Date { get; }

    /// <summary>
    /// Gets the multiplier that converts an amount in <see cref="From" /> to <see cref="To" />.
    /// </summary>
    /// <value>A strictly positive multiplier.</value>
    public decimal Rate { get; }

    /// <summary>
    /// Gets the non-empty identifier of the publishing source.
    /// </summary>
    /// <value>The provider identifier.</value>
    public string Provider { get; }

    /// <summary>
    /// Gets a value indicating whether this rate was derived from the reverse pair.
    /// </summary>
    /// <value>
    /// <see langword="true" /> when <see cref="Rate" /> is the reciprocal of an originally published reverse-direction
    /// rate; otherwise <see langword="false" />.
    /// </value>
    public bool IsInverted { get; }

    /// <summary>
    /// Gets the UTC instant at which the upstream data backing this rate was originally fetched, or
    /// <see langword="null" /> when not tracked.
    /// </summary>
    /// <value>The fetch instant when known; otherwise <see langword="null" />.</value>
    /// <remarks>
    /// The value is provenance metadata describing when the load that produced this rate downloaded its source data. It
    /// is excluded from <see cref="Equals(ExchangeRate)" /> and <see cref="GetHashCode" />, so two rates that differ
    /// only in their fetch instant still compare equal.
    /// </remarks>
    public DateTimeOffset? FetchedAtUtc { get; }

    /// <summary>
    /// Gets the underlying observed rate used for precise conversion. Equals <see cref="Rate" /> for a non-inverted
    /// rate; for an inverted rate it is the original reverse-pair rate.
    /// </summary>
    /// <value>The observed rate.</value>
    internal decimal ObservedRate { get; }

    /// <summary>
    /// Converts <paramref name="amount" /> from the source currency to the destination currency.
    /// </summary>
    /// <param name="amount">The amount in <see cref="From" /> to convert.</param>
    /// <returns>The converted amount in <see cref="To" />, unrounded.</returns>
    /// <remarks>
    /// A non-inverted rate multiplies by <see cref="Rate" />; an inverted rate divides by the original reverse-pair
    /// rate rather than multiplying by a pre-rounded reciprocal, avoiding a double-rounding step. Rounding is
    /// intentionally deferred to the money boundary so the rate object stays decoupled from the destination currency's
    /// minor-unit precision.
    /// </remarks>
    public decimal Convert(decimal amount) =>
        IsInverted ? amount / ObservedRate : amount * ObservedRate;

    /// <summary>
    /// Returns a copy of this rate with the specified upstream fetch instant, preserving all other values.
    /// </summary>
    /// <param name="fetchedAtUtc">
    /// The UTC instant the upstream data backing the returned rate was originally fetched, or <see langword="null" />
    /// when not tracked.
    /// </param>
    /// <returns>
    /// A new <see cref="ExchangeRate" /> identical to this one except for its <see cref="FetchedAtUtc" /> value.
    /// </returns>
    /// <remarks>
    /// The internal observed rate is carried over exactly, so an inverted rate's precise reverse-pair value survives
    /// the copy. Because <see cref="FetchedAtUtc" /> is excluded from equality, the returned rate compares equal to
    /// this one.
    /// </remarks>
    public ExchangeRate WithFetchedAtUtc(DateTimeOffset? fetchedAtUtc) =>
        new(From, To, Date, Rate, ObservedRate, Provider, IsInverted, fetchedAtUtc);

    /// <summary>
    /// Determines whether this rate equals <paramref name="other" /> by its public fields. The internal observed rate
    /// and the <see cref="FetchedAtUtc" /> fetch instant are excluded — both are provenance metadata — so two rates
    /// that report the same direction, date, multiplier, provider, and inversion compare equal regardless of how each
    /// was constructed or when its source data was fetched.
    /// </summary>
    /// <param name="other">The rate to compare with.</param>
    /// <returns><see langword="true" /> when the public fields match; otherwise <see langword="false" />.</returns>
    public bool Equals(ExchangeRate other) =>
        From == other.From
        && To == other.To
        && Date == other.Date
        && Rate == other.Rate
        && Provider == other.Provider
        && IsInverted == other.IsInverted;

    /// <summary>
    /// Returns a hash code over the public fields, consistent with <see cref="Equals(ExchangeRate)" />.
    /// </summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode() =>
        HashCode.Combine(From, To, Date, Rate, Provider, IsInverted);
}
