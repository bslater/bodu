// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Bodu.Financial.Currencies;
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
[DebuggerDisplay("{From,nq}->{To,nq} @ {Date,nq} = {Rate} ({Provider,nq})")]
[JsonConverter(typeof(ExchangeRateJsonConverter))]
public readonly record struct ExchangeRate
{
    /// <summary>The underlying observed rate used for precise conversion. For a non-inverted rate this equals <see cref="Rate" />; for an inverted rate it is the original reverse-pair rate, so conversion divides by it rather than multiplying by a pre-rounded reciprocal.</summary>
    private readonly decimal _observedRate;

    /// <summary>The UTC instant at which the upstream data backing this rate was originally fetched, or <see langword="null" /> when the fetch instant is not tracked. Carried as provenance metadata only and excluded from equality.</summary>
    private readonly DateTimeOffset? _fetchedAtUtc;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExchangeRate" /> struct.
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
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="provider" /> is <see langword="null" />.</exception>
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
    /// Initializes a new instance of the <see cref="ExchangeRate" /> struct from two ISO 4217 alphabetic codes.
    /// </summary>
    /// <param name="fromIsoCode">The source-currency ISO 4217 alphabetic code.</param>
    /// <param name="toIsoCode">The destination-currency ISO 4217 alphabetic code.</param>
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
    /// Thrown if <paramref name="fromIsoCode" />, <paramref name="toIsoCode" />, or <paramref name="provider" /> is
    /// <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="fromIsoCode" /> or <paramref name="toIsoCode" /> is not a known currency, or if
    /// <paramref name="provider" /> is empty or white-space.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="rate" /> is zero or negative.</exception>
    /// <remarks>
    /// Transitional string entry point retained while the exchange-rate surface migrates to <see cref="CurrencyCode" />;
    /// prefer the <see cref="CurrencyCode" /> constructor.
    /// </remarks>
    public ExchangeRate(
        string fromIsoCode,
        string toIsoCode,
        DateOnly date,
        decimal rate,
        string provider,
        bool isInverted = false,
        DateTimeOffset? fetchedAtUtc = null)
        : this(Parse(fromIsoCode), Parse(toIsoCode), date, rate, provider, isInverted, fetchedAtUtc)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExchangeRate" /> struct from fully resolved field values, including
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
        FinancialThrowHelper.ThrowIfNullOrWhiteSpaceProvider(provider);
        ThrowHelper.ThrowIfZeroOrNegative(rate);

        From = from;
        To = to;
        Date = date;
        Rate = rate;
        Provider = provider;
        IsInverted = isInverted;
        _observedRate = observedRate;
        _fetchedAtUtc = fetchedAtUtc;
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
    /// Transitional string overload of <see cref="FromObservedRate(CurrencyCode, CurrencyCode, DateOnly, decimal, string, bool, DateTimeOffset?)" />.
    /// </summary>
    /// <param name="fromIsoCode">The reported source-currency ISO code.</param>
    /// <param name="toIsoCode">The reported destination-currency ISO code.</param>
    /// <param name="date">The calendar date on which the rate was observed.</param>
    /// <param name="observedRate">The originally observed rate.</param>
    /// <param name="provider">The non-empty identifier of the publishing source.</param>
    /// <param name="isInverted"><see langword="true" /> when <paramref name="observedRate" /> is the reverse-pair rate.</param>
    /// <param name="fetchedAtUtc">The upstream fetch instant, or <see langword="null" /> when not tracked.</param>
    /// <returns>The constructed exchange rate.</returns>
    internal static ExchangeRate FromObservedRate(
        string fromIsoCode,
        string toIsoCode,
        DateOnly date,
        decimal observedRate,
        string provider,
        bool isInverted,
        DateTimeOffset? fetchedAtUtc = null) =>
        FromObservedRate(Parse(fromIsoCode), Parse(toIsoCode), date, observedRate, provider, isInverted, fetchedAtUtc);

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
    /// Transitional string overload of <see cref="FromComponents(CurrencyCode, CurrencyCode, DateOnly, decimal, decimal, string, bool, DateTimeOffset?)" />.
    /// </summary>
    /// <param name="fromIsoCode">The source-currency ISO code.</param>
    /// <param name="toIsoCode">The destination-currency ISO code.</param>
    /// <param name="date">The calendar date on which the rate was observed.</param>
    /// <param name="rate">The reported source-to-destination multiplier.</param>
    /// <param name="observedRate">The underlying observed rate used for precise conversion.</param>
    /// <param name="provider">The non-empty identifier of the publishing source.</param>
    /// <param name="isInverted"><see langword="true" /> when derived from the reverse pair.</param>
    /// <param name="fetchedAtUtc">The upstream fetch instant, or <see langword="null" /> when not tracked.</param>
    /// <returns>The constructed exchange rate.</returns>
    internal static ExchangeRate FromComponents(
        string fromIsoCode,
        string toIsoCode,
        DateOnly date,
        decimal rate,
        decimal observedRate,
        string provider,
        bool isInverted,
        DateTimeOffset? fetchedAtUtc = null) =>
        FromComponents(Parse(fromIsoCode), Parse(toIsoCode), date, rate, observedRate, provider, isInverted, fetchedAtUtc);

    /// <summary>
    /// Gets the source currency.
    /// </summary>
    /// <returns>The currency an amount is converted from.</returns>
    public CurrencyCode From { get; }

    /// <summary>
    /// Gets the destination currency.
    /// </summary>
    /// <returns>The currency an amount is converted to.</returns>
    public CurrencyCode To { get; }

    /// <summary>
    /// Gets the source-currency ISO 4217 alphabetic code.
    /// </summary>
    /// <returns>The source currency's code, or an empty string when <see cref="From" /> is <see cref="CurrencyCode.None" />.</returns>
    public string FromIsoCode => From == CurrencyCode.None ? string.Empty : From.ToString();

    /// <summary>
    /// Gets the destination-currency ISO 4217 alphabetic code.
    /// </summary>
    /// <returns>The destination currency's code, or an empty string when <see cref="To" /> is <see cref="CurrencyCode.None" />.</returns>
    public string ToIsoCode => To == CurrencyCode.None ? string.Empty : To.ToString();

    /// <summary>
    /// Gets the directional currency pair this rate quotes.
    /// </summary>
    /// <returns>An <see cref="ExchangeRatePair" /> of <see cref="From" /> and <see cref="To" />.</returns>
    public ExchangeRatePair Pair => new(From, To);

    /// <summary>
    /// Gets the calendar date on which the rate was observed.
    /// </summary>
    /// <returns>The observation date.</returns>
    public DateOnly Date { get; }

    /// <summary>
    /// Gets the multiplier that converts an amount in <see cref="From" /> to <see cref="To" />.
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
    /// Gets the UTC instant at which the upstream data backing this rate was originally fetched, or
    /// <see langword="null" /> when not tracked.
    /// </summary>
    /// <returns>The fetch instant when known; otherwise <see langword="null" />.</returns>
    /// <remarks>
    /// The value is provenance metadata describing when the load that produced this rate downloaded its source data. It
    /// is excluded from <see cref="Equals(ExchangeRate)" /> and <see cref="GetHashCode" />, so two rates that differ
    /// only in their fetch instant still compare equal.
    /// </remarks>
    public DateTimeOffset? FetchedAtUtc => _fetchedAtUtc;

    /// <summary>
    /// Gets the underlying observed rate used for precise conversion. Equals <see cref="Rate" /> for a non-inverted
    /// rate; for an inverted rate it is the original reverse-pair rate.
    /// </summary>
    /// <returns>The observed rate.</returns>
    internal decimal ObservedRate => _observedRate;

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
        IsInverted ? amount / _observedRate : amount * _observedRate;

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
        new(From, To, Date, Rate, _observedRate, Provider, IsInverted, fetchedAtUtc);

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

    /// <summary>
    /// Resolves an ISO 4217 alphabetic code to a <see cref="CurrencyCode" />, reporting the originating constructor
    /// parameter on failure.
    /// </summary>
    /// <param name="isoCode">The ISO 4217 alphabetic code to resolve.</param>
    /// <param name="paramName">The originating parameter name; inferred from the call site.</param>
    /// <returns>The resolved <see cref="CurrencyCode" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="isoCode" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="isoCode" /> is not a known currency.</exception>
    private static CurrencyCode Parse(string isoCode, [CallerArgumentExpression(nameof(isoCode))] string? paramName = null)
    {
        ThrowHelper.ThrowIfNull(isoCode, paramName);

        if (!CurrencyInfo.TryGetCurrencyCode(isoCode, out CurrencyCode code))
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, FinancialResourceStrings.Arg_Invalid_UnknownCurrencyRejected, isoCode),
                paramName);

        return code;
    }
}
