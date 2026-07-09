// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FixedDatedRateProvider.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Frozen;
using System.Globalization;
using System.Runtime.CompilerServices;
using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Provides an immutable <see cref="IDatedRateProvider" /> facade over an <see cref="RateBook" />, applying an explicit
/// provider-priority list to disambiguate pairs that carry observations from more than one publishing source.
/// </summary>
/// <remarks>
/// <para>
/// Use this provider as the read-side hand-off from an <see cref="RateTableBuilder" /> built during data ingest. The
/// <see cref="RateTableBuilder.ToBook" /> method materialises the multi-provider book, and this provider then selects
/// rates from it using either the single available provider per pair, or the supplied priority list.
/// </para>
/// <para>
/// Lookups walk the provider priority once per pair, perform a <see cref="FrozenDictionary{TKey, TValue}" /> probe for
/// each candidate (pair, provider), then an <see cref="Array.BinarySearch{T}(T[], T)" /> over the series' date array.
/// Successful and failing lookups both allocate no managed memory beyond the <see cref="RateLookupResult" /> on
/// success.
/// </para>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using Bodu.Financial;
///
/// // Construct directly from a handful of observations (one provider per pair).
/// var provider = new FixedDatedRateProvider(new[]
/// {
///     new ExchangeRate(CurrencyCode.USD, CurrencyCode.EUR, new DateOnly(2024, 3, 1), 0.92m, "ECB"),
///     new ExchangeRate(CurrencyCode.USD, CurrencyCode.EUR, new DateOnly(2024, 3, 4), 0.93m, "ECB"),
/// });
///
/// // Resolve the nearest rate within three days of a weekend date.
/// RateLookupResult result = provider.GetRate(
///     "USD", "EUR", new DateOnly(2024, 3, 2), RateLookupOptions.PreviousWithin(3));
/// decimal rate = result.Rate.Rate;   // 0.92 (resolved from 2024-03-01)
///]]>
/// </code>
/// </example>
/// </remarks>
public sealed class FixedDatedRateProvider
    : IDatedRateProvider, IHistoryAwareRateProvider
{
    /// <summary>The label used as the provider name on synthetic same-currency identity results. Exposed publicly so audit consumers can filter by it without depending on a magic-string literal.</summary>
    public const string IdentityProviderName = "Identity";

    /// <summary>The ordered set of providers consulted for each pair, in priority order.</summary>
    private readonly string[] _providerPriority;

    /// <summary>
    /// Initializes a new instance of the <see cref="FixedDatedRateProvider" /> class from an immutable
    /// <see cref="RateBook" />. The book must contain at most one provider per currency pair.
    /// </summary>
    /// <param name="book">The immutable book to wrap.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="book" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="book" /> contains two providers for the same pair; supply a provider-priority list to
    /// disambiguate.
    /// </exception>
    public FixedDatedRateProvider(RateBook book)
    {
        ThrowHelper.ThrowIfNull(book);

        _providerPriority = BuildSingleProviderList(book);
        Book = book;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FixedDatedRateProvider" /> class from an immutable
    /// <see cref="RateBook" /> and an explicit provider-priority list applied per pair.
    /// </summary>
    /// <param name="book">The immutable book to wrap.</param>
    /// <param name="providerPriority">
    /// The ordered set of providers consulted for every pair. The first provider in this list that has a matching
    /// series for the pair wins; providers absent from the list are unreachable through this provider.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="book" /> or <paramref name="providerPriority" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="providerPriority" /> is empty or contains a null/whitespace entry.
    /// </exception>
    public FixedDatedRateProvider(RateBook book, IEnumerable<string> providerPriority)
    {
        ThrowHelper.ThrowIfNull(book);
        ThrowHelper.ThrowIfNull(providerPriority);

        string[] snapshot = [.. providerPriority];
        if (snapshot.Length == 0)
            throw new ArgumentException(FinancialResourceStrings.Arg_Invalid_ProviderPriorityEmpty, nameof(providerPriority));

        for (int i = 0; i < snapshot.Length; i++)
            ThrowHelper.ThrowIfNullOrWhiteSpace(snapshot[i], nameof(providerPriority));

        Book = book;
        _providerPriority = snapshot;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FixedDatedRateProvider" /> class by materialising the supplied
    /// observations into an <see cref="RateBook" /> first.
    /// </summary>
    /// <param name="rates">The exchange-rate observations to store, in any order.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="rates" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="rates" /> contains rates for the same currency pair from differing providers, or if
    /// two rates share the same pair and date.
    /// </exception>
    public FixedDatedRateProvider(IEnumerable<ExchangeRate> rates)
    {
        ThrowHelper.ThrowIfNull(rates);

        Book = BuildBook(rates);
        _providerPriority = BuildSingleProviderList(Book);
    }

    /// <summary>
    /// Gets the history depth this provider advertises, derived from the wrapped book's contents.
    /// </summary>
    /// <value>
    /// <see cref="RateHistoryAvailability.Since(DateOnly)" /> anchored at the earliest observation date across every
    /// series in the book, or <see cref="RateHistoryAvailability.Unbounded" /> when the book is empty (an empty book
    /// has no floor to declare, and every lookup misses regardless).
    /// </value>
    public RateHistoryAvailability HistoryAvailability =>
        TryGetEarliestDateInBook(out DateOnly earliest)
            ? RateHistoryAvailability.Since(earliest)
            : RateHistoryAvailability.Unbounded;

    /// <summary>
    /// Gets the immutable book backing every lookup this provider performs.
    /// </summary>
    /// <value>The wrapped <see cref="RateBook" />; the same instance for the provider's lifetime.</value>
    /// <remarks>
    /// The book is immutable and safe to share or query concurrently. It carries the raw multi-provider series only —
    /// the provider-priority policy this provider applies per pair is not part of the book, so rewrapping the book
    /// (directly or via <see cref="RateBook.ToBuilder" />) does not carry the policy across.
    /// </remarks>
    public RateBook Book { get; }

    /// <inheritdoc />
    public RateLookupResult GetRate(
        string fromIsoCode,
        string toIsoCode,
        DateOnly date,
        RateLookupOptions? options = null)
    {
        return TryGetRate(fromIsoCode, toIsoCode, date, options, out RateLookupResult result)
            ? result
            : throw new KeyNotFoundException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    FinancialResourceStrings.IO_KeyNotFound_DatedExchangeRate,
                    fromIsoCode,
                    toIsoCode,
                    date,
                    (options ?? RateLookupOptions.Exact).DateResolution,
                    (options ?? RateLookupOptions.Exact).ToleranceDays));
    }

    /// <inheritdoc />
    public RateLookupResult GetRate(
        string fromIsoCode,
        string toIsoCode,
        RateLookupOptions? options = null)
    {
        FinancialThrowHelper.ThrowIfNotValidIsoCode(fromIsoCode);
        FinancialThrowHelper.ThrowIfNotValidIsoCode(toIsoCode);
        options ??= RateLookupOptions.Exact;
        options.Validate();

        if (options.AllowSameCurrencyIdentityRate && string.Equals(fromIsoCode, toIsoCode, StringComparison.Ordinal))
        {
            CurrencyCode identityCode = CurrencyInfo.ParseCurrencyCode(fromIsoCode);
            DateOnly identityDate = LatestDateInBook();
            ExchangeRate identity = new(identityCode, identityCode, identityDate, 1m, IdentityProviderName);
            return new RateLookupResult(identity, identityDate, options.DateResolution, 0, RateProvenance.Live(identity.Provider));
        }

        CurrencyPair directPair = new(CurrencyInfo.ParseCurrencyCode(fromIsoCode), CurrencyInfo.ParseCurrencyCode(toIsoCode));

        if (TryGetLatestRate(directPair, options, isInverted: false, out RateLookupResult result))
            return result;

        if (options.AllowInverse && TryGetLatestRate(directPair.Inverse(), options, isInverted: true, out result))
            return result;

        throw new KeyNotFoundException(
            string.Format(
                CultureInfo.CurrentCulture,
                FinancialResourceStrings.IO_KeyNotFound_DatedExchangeRate,
                fromIsoCode,
                toIsoCode,
                "(latest)",
                options.DateResolution,
                options.ToleranceDays));
    }

    /// <inheritdoc />
    public ValueTask<RateLookupResult> GetRateAsync(
        string fromIsoCode,
        string toIsoCode,
        RateLookupOptions? options = null,
        CancellationToken cancellationToken = default) =>
        new(GetRate(fromIsoCode, toIsoCode, options));

    /// <inheritdoc />
    public ValueTask<RateLookupResult> GetRateAsync(
        string fromIsoCode,
        string toIsoCode,
        DateOnly date,
        RateLookupOptions? options = null,
        CancellationToken cancellationToken = default) =>
        new(GetRate(fromIsoCode, toIsoCode, date, options));

    /// <inheritdoc />
    public bool TryGetRate(
        string fromIsoCode,
        string toIsoCode,
        DateOnly date,
        RateLookupOptions? options,
        out RateLookupResult result)
    {
        FinancialThrowHelper.ThrowIfNotValidIsoCode(fromIsoCode);
        FinancialThrowHelper.ThrowIfNotValidIsoCode(toIsoCode);
        options ??= RateLookupOptions.Exact;
        options.Validate();

        if (options.AllowSameCurrencyIdentityRate &&
            string.Equals(fromIsoCode, toIsoCode, StringComparison.Ordinal))
        {
            CurrencyCode identityCode = CurrencyInfo.ParseCurrencyCode(fromIsoCode);
            ExchangeRate identity = new(identityCode, identityCode, date, 1m, IdentityProviderName);
            result = new RateLookupResult(identity, date, options.DateResolution, 0, RateProvenance.Live(identity.Provider));
            return true;
        }

        CurrencyPair directPair = new(CurrencyInfo.ParseCurrencyCode(fromIsoCode), CurrencyInfo.ParseCurrencyCode(toIsoCode));

        if (TryGetDirectRate(directPair, date, options, isInverted: false, out result))
            return true;

        if (options.AllowInverse)
        {
            CurrencyPair inversePair = directPair.Inverse();

            if (TryGetDirectRate(inversePair, date, options, isInverted: true, out result))
                return true;
        }

        result = default;
        return false;
    }

    /// <inheritdoc />
    public RateRangeResult GetRates(
        string fromIsoCode,
        string toIsoCode,
        DateOnly startDate,
        DateOnly endDate)
    {
        FinancialThrowHelper.ThrowIfNotValidIsoCode(fromIsoCode);
        FinancialThrowHelper.ThrowIfNotValidIsoCode(toIsoCode);
        if (endDate < startDate)
            throw new ArgumentException(FinancialResourceStrings.Arg_Invalid_ExchangeRateRangeInverted, nameof(endDate));

        CurrencyPair pair = new(CurrencyInfo.ParseCurrencyCode(fromIsoCode), CurrencyInfo.ParseCurrencyCode(toIsoCode));
        List<ExchangeRate> result = new();

        // Prefer the directly quoted series; fall back to the reverse pair, reporting each observation inverted.
        if (!TryCollectRange(pair, startDate, endDate, isInverted: false, result))
            _ = TryCollectRange(pair.Inverse(), startDate, endDate, isInverted: true, result);

        result.Sort(static (left, right) => left.Date.CompareTo(right.Date));
        return new RateRangeResult(fromIsoCode, toIsoCode, startDate, endDate, result);
    }

    /// <inheritdoc />
    public ValueTask<RateRangeResult> GetRatesAsync(
        string fromIsoCode,
        string toIsoCode,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default) =>
        new(GetRates(fromIsoCode, toIsoCode, startDate, endDate));

    /// <summary>
    /// Materialises the supplied observations into a multi-provider <see cref="RateBook" />, validating that each pair
    /// carries observations from a single provider only.
    /// </summary>
    /// <param name="rates">The observations to group.</param>
    /// <returns>A new <see cref="RateBook" /> containing one series per pair.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown if observations for the same pair carry differing provider identifiers, or if duplicate dates are
    /// detected while building any series.
    /// </exception>
    private static RateBook BuildBook(IEnumerable<ExchangeRate> rates)
    {
        Dictionary<CurrencyPair, (string Provider, List<(DateOnly Date, decimal Rate)> Entries)> grouped = new();

        foreach (ExchangeRate observation in rates)
        {
            CurrencyPair pair = observation.Pair;

            if (!grouped.TryGetValue(pair, out (string Provider, List<(DateOnly, decimal)> Entries) bucket))
            {
                bucket = (observation.Provider, new List<(DateOnly, decimal)>());
                grouped[pair] = bucket;
            }
            else if (!string.Equals(bucket.Provider, observation.Provider, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        FinancialResourceStrings.Arg_Invalid_RateSeriesProviderConflict,
                        pair.From.ToString(),
                        pair.To.ToString(),
                        bucket.Provider,
                        observation.Provider),
                    nameof(rates));
            }

            bucket.Entries.Add((observation.Date, observation.Rate));
        }

        List<RateSeries> materialised = new(grouped.Count);
        foreach (KeyValuePair<CurrencyPair, (string Provider, List<(DateOnly Date, decimal Rate)> Entries)> entry in grouped)
        {
            materialised.Add(new RateSeries(entry.Key, entry.Value.Provider, entry.Value.Entries));
        }

        return new RateBook(materialised);
    }

    /// <summary>
    /// Builds the one-provider-per-pair priority list used by the book-only constructor and the legacy convenience
    /// constructor. Detects pairs that carry more than one provider and throws to surface the ambiguity.
    /// </summary>
    /// <param name="book">The book to inspect.</param>
    /// <returns>An array containing every distinct provider that appears in the book exactly once per pair.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown if the book contains two series for the same pair from different providers.
    /// </exception>
    private static string[] BuildSingleProviderList(RateBook book)
    {
        Dictionary<CurrencyPair, string> pairProvider = new();
        HashSet<string> providers = new(StringComparer.Ordinal);

        foreach (RateSeriesKey key in book.Keys)
        {
            if (pairProvider.TryGetValue(key.Pair, out string? existing) &&
                !string.Equals(existing, key.Provider, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        FinancialResourceStrings.Arg_Invalid_RateSeriesProviderConflict,
                        key.Pair.From.ToString(),
                        key.Pair.To.ToString(),
                        existing,
                        key.Provider),
                    nameof(book));
            }

            pairProvider[key.Pair] = key.Provider;
            providers.Add(key.Provider);
        }

        return [.. providers];
    }

    /// <summary>
    /// Attempts to resolve a rate by walking the provider priority for the supplied <paramref name="pair" />.
    /// </summary>
    /// <param name="pair">The pair to probe, possibly the inverse of the user's requested pair.</param>
    /// <param name="requestedDate">The original requested date.</param>
    /// <param name="options">The lookup options to apply.</param>
    /// <param name="isInverted">
    /// <see langword="true" /> if <paramref name="pair" /> is the inverse of the originally requested pair, in which
    /// case the returned rate is inverted before being reported back to the caller.
    /// </param>
    /// <param name="result">When this method returns <see langword="true" />, the resolved lookup result.</param>
    /// <returns><see langword="true" /> if a rate was resolved; otherwise <see langword="false" />.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryGetDirectRate(
        CurrencyPair pair,
        DateOnly requestedDate,
        RateLookupOptions options,
        bool isInverted,
        out RateLookupResult result)
    {
        string[] priority = _providerPriority;
        for (int i = 0; i < priority.Length; i++)
        {
            if (!Book.TryGetSeries(pair, priority[i], out RateSeries? series) || series is null)
                continue;

            if (!series.TryGetRate(requestedDate, options, out DateOnly resolvedDate, out decimal rawRate))
                continue;

            CurrencyCode reportedFrom = isInverted ? pair.To : pair.From;
            CurrencyCode reportedTo = isInverted ? pair.From : pair.To;

            // Pass the originally observed rate so an inverted conversion divides by it rather than multiplying by a
            // pre-rounded reciprocal; the reported Rate is still the From->To multiplier. The series' fetch instant is
            // stamped onto the served rate as provenance.
            var rate = ExchangeRate.FromObservedRate(reportedFrom, reportedTo, resolvedDate, rawRate, series.Provider, isInverted, series.FetchedAtUtc);

            int offsetDays = Math.Abs(resolvedDate.DayNumber - requestedDate.DayNumber);
            result = new RateLookupResult(rate, requestedDate, options.DateResolution, offsetDays, RateProvenance.Live(rate.Provider));
            return true;
        }

        result = default;
        return false;
    }

    /// <summary>
    /// Attempts to resolve the most recent observation for the supplied <paramref name="pair" /> by walking the
    /// provider priority and selecting the latest dated observation of the first matching series.
    /// </summary>
    /// <param name="pair">The pair to probe, possibly the inverse of the user's requested pair.</param>
    /// <param name="options">The lookup options to apply.</param>
    /// <param name="isInverted">
    /// <see langword="true" /> if <paramref name="pair" /> is the inverse of the originally requested pair, in which
    /// case the returned rate is inverted before being reported back to the caller.
    /// </param>
    /// <param name="result">When this method returns <see langword="true" />, the resolved lookup result.</param>
    /// <returns><see langword="true" /> if a rate was resolved; otherwise <see langword="false" />.</returns>
    private bool TryGetLatestRate(
        CurrencyPair pair,
        RateLookupOptions options,
        bool isInverted,
        out RateLookupResult result)
    {
        string[] priority = _providerPriority;
        for (int i = 0; i < priority.Length; i++)
        {
            if (!Book.TryGetSeries(pair, priority[i], out RateSeries? series) || series is null || series.Count == 0)
                continue;

            // Observations are stored in ascending date order, so the last is the most recent.
            RateObservation latest = series.GetObservations().Last();

            CurrencyCode reportedFrom = isInverted ? pair.To : pair.From;
            CurrencyCode reportedTo = isInverted ? pair.From : pair.To;

            // Stamp the series' fetch instant onto the served rate as provenance.
            var rate = ExchangeRate.FromObservedRate(reportedFrom, reportedTo, latest.Date, latest.Rate, series.Provider, isInverted, series.FetchedAtUtc);

            result = new RateLookupResult(rate, latest.Date, options.DateResolution, 0, RateProvenance.Live(rate.Provider));
            return true;
        }

        result = default;
        return false;
    }

    /// <summary>
    /// Collects the in-window observations for the supplied <paramref name="pair" /> from the first series that matches
    /// it under the provider priority, appending one <see cref="ExchangeRate" /> per observation.
    /// </summary>
    /// <param name="pair">The pair to probe, possibly the inverse of the user's requested pair.</param>
    /// <param name="startDate">The inclusive start of the range.</param>
    /// <param name="endDate">The inclusive end of the range.</param>
    /// <param name="isInverted">
    /// <see langword="true" /> if <paramref name="pair" /> is the inverse of the requested pair, in which case each
    /// observation is reported inverted.
    /// </param>
    /// <param name="result">The accumulator the matching observations are appended to.</param>
    /// <returns>
    /// <see langword="true" /> if a series matched the pair (even when no observation falls in the window); otherwise
    /// <see langword="false" />.
    /// </returns>
    private bool TryCollectRange(
        CurrencyPair pair,
        DateOnly startDate,
        DateOnly endDate,
        bool isInverted,
        List<ExchangeRate> result)
    {
        string[] priority = _providerPriority;
        for (int i = 0; i < priority.Length; i++)
        {
            if (!Book.TryGetSeries(pair, priority[i], out RateSeries? series) || series is null)
                continue;

            CurrencyCode reportedFrom = isInverted ? pair.To : pair.From;
            CurrencyCode reportedTo = isInverted ? pair.From : pair.To;

            foreach (RateObservation observation in series.GetObservations())
            {
                if (observation.Date < startDate || observation.Date > endDate)
                    continue;

                // Stamp the resolving series' fetch instant onto each materialized observation.
                result.Add(ExchangeRate.FromObservedRate(reportedFrom, reportedTo, observation.Date, observation.Rate, series.Provider, isInverted, series.FetchedAtUtc));
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// Returns the latest observation date present across every series in the book, or <see cref="DateOnly.MinValue" />
    /// when the book is empty. Used to stamp a same-currency identity result on the undated surface.
    /// </summary>
    /// <returns>The most recent observation date in the book, or <see cref="DateOnly.MinValue" /> when empty.</returns>
    private DateOnly LatestDateInBook()
    {
        DateOnly max = DateOnly.MinValue;
        bool any = false;

        foreach (RateSeriesKey key in Book.Keys)
        {
            if (!Book.TryGetSeries(key.Pair, key.Provider, out RateSeries? series) || series is null || series.Count == 0)
                continue;

            DateOnly last = series.GetObservations().Last().Date;
            if (!any || last > max)
            {
                max = last;
                any = true;
            }
        }

        return max;
    }

    /// <summary>
    /// Attempts to find the earliest observation date present across every series in the book. Series observations are
    /// stored in ascending date order, so each series contributes its first observation. Used to derive the advertised
    /// <see cref="HistoryAvailability" />.
    /// </summary>
    /// <param name="earliest">
    /// When this method returns <see langword="true" />, the earliest observation date in the book; otherwise
    /// <see cref="DateOnly.MinValue" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when the book contains at least one observation; otherwise <see langword="false" />.
    /// </returns>
    private bool TryGetEarliestDateInBook(out DateOnly earliest)
    {
        DateOnly min = DateOnly.MinValue;
        bool any = false;

        foreach (RateSeriesKey key in Book.Keys)
        {
            if (!Book.TryGetSeries(key.Pair, key.Provider, out RateSeries? series) || series is null || series.Count == 0)
                continue;

            DateOnly first = series.GetObservations().First().Date;
            if (!any || first < min)
            {
                min = first;
                any = true;
            }
        }

        earliest = any ? min : DateOnly.MinValue;
        return any;
    }
}
