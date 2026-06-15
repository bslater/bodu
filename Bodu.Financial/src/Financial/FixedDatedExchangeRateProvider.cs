// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FixedDatedExchangeRateProvider.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Frozen;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Bodu.Financial;

/// <summary>
/// Provides an immutable <see cref="IDatedExchangeRateProvider" /> facade over an <see cref="ExchangeRateBook" />,
/// applying an explicit provider-priority list to disambiguate pairs that carry observations from more than one
/// publishing source.
/// </summary>
/// <remarks>
/// <para>
/// Use this provider as the read-side hand-off from an <see cref="ExchangeRateTableBuilder" /> built during data
/// ingest. The <see cref="ExchangeRateTableBuilder.ToBook" /> method materialises the multi-provider book, and this
/// provider then selects rates from it using either the single available provider per pair, or the supplied priority
/// list.
/// </para>
/// <para>
/// Lookups walk the provider priority once per pair, perform a <see cref="FrozenDictionary{TKey, TValue}" /> probe for
/// each candidate (pair, provider), then an <see cref="Array.BinarySearch{T}(T[], T)" /> over the series' date array.
/// Successful and failing lookups both allocate no managed memory beyond the <see cref="ExchangeRateLookupResult" /> on
/// success.
/// </para>
/// </remarks>
public sealed class FixedDatedExchangeRateProvider
    : IDatedExchangeRateProvider
{
    /// <summary>
    /// The label used as the provider name on synthetic same-currency identity results. Exposed publicly so audit
    /// consumers can filter by it without depending on a magic-string literal.
    /// </summary>
    public const string IdentityProviderName = "Identity";

    /// <summary>
    /// The underlying immutable multi-provider book that backs every lookup.
    /// </summary>
    private readonly ExchangeRateBook _book;

    /// <summary>
    /// The ordered set of providers consulted for each pair, in priority order.
    /// </summary>
    private readonly string[] _providerPriority;

    /// <summary>
    /// Initializes a new instance of the <see cref="FixedDatedExchangeRateProvider" /> class from an immutable
    /// <see cref="ExchangeRateBook" />. The book must contain at most one provider per currency pair.
    /// </summary>
    /// <param name="book">The immutable book to wrap.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="book" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="book" /> contains two providers for the same pair; supply a provider-priority list to
    /// disambiguate.
    /// </exception>
    public FixedDatedExchangeRateProvider(ExchangeRateBook book)
    {
        ThrowHelper.ThrowIfNull(book);

        _providerPriority = BuildSingleProviderList(book);
        _book = book;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FixedDatedExchangeRateProvider" /> class from an immutable
    /// <see cref="ExchangeRateBook" /> and an explicit provider-priority list applied per pair.
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
    public FixedDatedExchangeRateProvider(ExchangeRateBook book, IEnumerable<string> providerPriority)
    {
        ThrowHelper.ThrowIfNull(book);
        ThrowHelper.ThrowIfNull(providerPriority);

        string[] snapshot = [.. providerPriority];
        if (snapshot.Length == 0)
            throw new ArgumentException(FinancialResourceStrings.Arg_Invalid_ProviderPriorityEmpty, nameof(providerPriority));

        for (var i = 0; i < snapshot.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(snapshot[i]))
                throw new ArgumentException(FinancialResourceStrings.Arg_Invalid_ProviderNullOrWhiteSpace, nameof(providerPriority));
        }

        _book = book;
        _providerPriority = snapshot;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FixedDatedExchangeRateProvider" /> class by materialising the
    /// supplied observations into an <see cref="ExchangeRateBook" /> first.
    /// </summary>
    /// <param name="rates">The exchange-rate observations to store, in any order.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="rates" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="rates" /> contains rates for the same currency pair from differing providers, or if
    /// two rates share the same pair and date.
    /// </exception>
    public FixedDatedExchangeRateProvider(IEnumerable<ExchangeRate> rates)
    {
        ThrowHelper.ThrowIfNull(rates);

        _book = BuildBook(rates);
        _providerPriority = BuildSingleProviderList(_book);
    }

    /// <inheritdoc />
    public ExchangeRateLookupResult GetRate(
        string fromIsoCode,
        string toIsoCode,
        DateOnly date,
        ExchangeRateLookupOptions? options = null)
    {
        return TryGetRate(fromIsoCode, toIsoCode, date, options, out ExchangeRateLookupResult result)
            ? result
            : throw new KeyNotFoundException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    FinancialResourceStrings.IO_KeyNotFound_DatedExchangeRate,
                    fromIsoCode,
                    toIsoCode,
                    date,
                    (options ?? ExchangeRateLookupOptions.Exact).DateResolution,
                    (options ?? ExchangeRateLookupOptions.Exact).ToleranceDays));
    }

    /// <inheritdoc />
    public ExchangeRateLookupResult GetRate(
        string fromIsoCode,
        string toIsoCode,
        ExchangeRateLookupOptions? options = null)
    {
        FinancialThrowHelper.ThrowIfNotValidIsoCode(fromIsoCode);
        FinancialThrowHelper.ThrowIfNotValidIsoCode(toIsoCode);
        options ??= ExchangeRateLookupOptions.Exact;
        options.Validate();

        if (options.AllowSameCurrencyIdentityRate && string.Equals(fromIsoCode, toIsoCode, StringComparison.Ordinal))
        {
            var identityDate = LatestDateInBook();
            ExchangeRate identity = new(fromIsoCode, toIsoCode, identityDate, 1m, IdentityProviderName);
            return new ExchangeRateLookupResult(identity, identityDate, options.DateResolution, 0);
        }

        ExchangeRatePair directPair = new(fromIsoCode, toIsoCode);

        if (TryGetLatestRate(directPair, options, isInverted: false, out ExchangeRateLookupResult result))
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
    public ValueTask<ExchangeRateLookupResult> GetRateAsync(
        string fromIsoCode,
        string toIsoCode,
        ExchangeRateLookupOptions? options = null,
        CancellationToken cancellationToken = default) =>
        new(GetRate(fromIsoCode, toIsoCode, options));

    /// <inheritdoc />
    public ValueTask<ExchangeRateLookupResult> GetRateAsync(
        string fromIsoCode,
        string toIsoCode,
        DateOnly date,
        ExchangeRateLookupOptions? options = null,
        CancellationToken cancellationToken = default) =>
        new(GetRate(fromIsoCode, toIsoCode, date, options));

    /// <inheritdoc />
    public bool TryGetRate(
        string fromIsoCode,
        string toIsoCode,
        DateOnly date,
        ExchangeRateLookupOptions? options,
        out ExchangeRateLookupResult result)
    {
        FinancialThrowHelper.ThrowIfNotValidIsoCode(fromIsoCode);
        FinancialThrowHelper.ThrowIfNotValidIsoCode(toIsoCode);
        options ??= ExchangeRateLookupOptions.Exact;
        options.Validate();

        if (options.AllowSameCurrencyIdentityRate &&
            string.Equals(fromIsoCode, toIsoCode, StringComparison.Ordinal))
        {
            ExchangeRate identity = new(fromIsoCode, toIsoCode, date, 1m, IdentityProviderName);
            result = new ExchangeRateLookupResult(identity, date, options.DateResolution, 0);
            return true;
        }

        ExchangeRatePair directPair = new(fromIsoCode, toIsoCode);

        if (TryGetDirectRate(directPair, date, options, isInverted: false, out result))
            return true;

        if (options.AllowInverse)
        {
            ExchangeRatePair inversePair = directPair.Inverse();

            if (TryGetDirectRate(inversePair, date, options, isInverted: true, out result))
                return true;
        }

        result = default;
        return false;
    }

    /// <inheritdoc />
    public ExchangeRateRangeResult GetRates(
        string fromIsoCode,
        string toIsoCode,
        DateOnly startDate,
        DateOnly endDate)
    {
        FinancialThrowHelper.ThrowIfNotValidIsoCode(fromIsoCode);
        FinancialThrowHelper.ThrowIfNotValidIsoCode(toIsoCode);
        if (endDate < startDate)
            throw new ArgumentException(FinancialResourceStrings.Arg_Invalid_ExchangeRateRangeInverted, nameof(endDate));

        ExchangeRatePair pair = new(fromIsoCode, toIsoCode);
        List<ExchangeRate> result = new();

        // Prefer the directly quoted series; fall back to the reverse pair, reporting each observation inverted.
        if (!TryCollectRange(pair, startDate, endDate, isInverted: false, result))
            _ = TryCollectRange(pair.Inverse(), startDate, endDate, isInverted: true, result);

        result.Sort(static (left, right) => left.Date.CompareTo(right.Date));
        return new ExchangeRateRangeResult(fromIsoCode, toIsoCode, startDate, endDate, result);
    }

    /// <inheritdoc />
    public ValueTask<ExchangeRateRangeResult> GetRatesAsync(
        string fromIsoCode,
        string toIsoCode,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default) =>
        new(GetRates(fromIsoCode, toIsoCode, startDate, endDate));

    /// <summary>
    /// Materialises the supplied observations into a multi-provider <see cref="ExchangeRateBook" />, validating that
    /// each pair carries observations from a single provider only.
    /// </summary>
    /// <param name="rates">The observations to group.</param>
    /// <returns>A new <see cref="ExchangeRateBook" /> containing one series per pair.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown if observations for the same pair carry differing provider identifiers, or if duplicate dates are
    /// detected while building any series.
    /// </exception>
    private static ExchangeRateBook BuildBook(IEnumerable<ExchangeRate> rates)
    {
        Dictionary<ExchangeRatePair, (string Provider, List<(DateOnly Date, decimal Rate)> Entries)> grouped = new();

        foreach (ExchangeRate observation in rates)
        {
            ExchangeRatePair pair = new(observation.FromIsoCode, observation.ToIsoCode);

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
                        pair.FromIsoCode,
                        pair.ToIsoCode,
                        bucket.Provider,
                        observation.Provider),
                    nameof(rates));
            }

            bucket.Entries.Add((observation.Date, observation.Rate));
        }

        List<ExchangeRateSeries> materialised = new(grouped.Count);
        foreach (KeyValuePair<ExchangeRatePair, (string Provider, List<(DateOnly Date, decimal Rate)> Entries)> entry in grouped)
        {
            materialised.Add(new ExchangeRateSeries(entry.Key, entry.Value.Provider, entry.Value.Entries));
        }

        return new ExchangeRateBook(materialised);
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
    private static string[] BuildSingleProviderList(ExchangeRateBook book)
    {
        Dictionary<ExchangeRatePair, string> pairProvider = new();
        HashSet<string> providers = new(StringComparer.Ordinal);

        foreach (ExchangeRateSeriesKey key in book.Keys)
        {
            if (pairProvider.TryGetValue(key.Pair, out var existing) &&
                !string.Equals(existing, key.Provider, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        FinancialResourceStrings.Arg_Invalid_RateSeriesProviderConflict,
                        key.Pair.FromIsoCode,
                        key.Pair.ToIsoCode,
                        existing,
                        key.Provider),
                    nameof(book));
            }

            pairProvider[key.Pair] = key.Provider;
            providers.Add(key.Provider);
        }

        return providers.ToArray();
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
        ExchangeRatePair pair,
        DateOnly requestedDate,
        ExchangeRateLookupOptions options,
        bool isInverted,
        out ExchangeRateLookupResult result)
    {
        var priority = _providerPriority;
        for (var i = 0; i < priority.Length; i++)
        {
            if (!_book.TryGetSeries(pair, priority[i], out ExchangeRateSeries? series) || series is null)
                continue;

            if (!series.TryGetRate(requestedDate, options, out DateOnly resolvedDate, out var rawRate))
                continue;

            var reportedFrom = isInverted ? pair.ToIsoCode : pair.FromIsoCode;
            var reportedTo = isInverted ? pair.FromIsoCode : pair.ToIsoCode;

            // Pass the originally observed rate so an inverted conversion divides by it rather than multiplying by a
            // pre-rounded reciprocal; the reported Rate is still the From->To multiplier.
            var rate = ExchangeRate.FromObservedRate(reportedFrom, reportedTo, resolvedDate, rawRate, series.Provider, isInverted);

            var offsetDays = Math.Abs(resolvedDate.DayNumber - requestedDate.DayNumber);
            result = new ExchangeRateLookupResult(rate, requestedDate, options.DateResolution, offsetDays);
            return true;
        }

        result = default;
        return false;
    }

    /// <summary>
    /// Attempts to resolve the most recent observation for the supplied <paramref name="pair" /> by walking the provider
    /// priority and selecting the latest dated observation of the first matching series.
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
        ExchangeRatePair pair,
        ExchangeRateLookupOptions options,
        bool isInverted,
        out ExchangeRateLookupResult result)
    {
        var priority = _providerPriority;
        for (var i = 0; i < priority.Length; i++)
        {
            if (!_book.TryGetSeries(pair, priority[i], out ExchangeRateSeries? series) || series is null || series.Count == 0)
                continue;

            // Observations are stored in ascending date order, so the last is the most recent.
            ExchangeRateObservation latest = series.GetObservations().Last();

            var reportedFrom = isInverted ? pair.ToIsoCode : pair.FromIsoCode;
            var reportedTo = isInverted ? pair.FromIsoCode : pair.ToIsoCode;
            var rate = ExchangeRate.FromObservedRate(reportedFrom, reportedTo, latest.Date, latest.Rate, series.Provider, isInverted);

            result = new ExchangeRateLookupResult(rate, latest.Date, options.DateResolution, 0);
            return true;
        }

        result = default;
        return false;
    }

    /// <summary>
    /// Collects the in-window observations for the supplied <paramref name="pair" /> from the first series that matches
    /// it under the provider priority, appending one <see cref="ExchangeRateLookupResult" /> per observation.
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
        ExchangeRatePair pair,
        DateOnly startDate,
        DateOnly endDate,
        bool isInverted,
        List<ExchangeRate> result)
    {
        var priority = _providerPriority;
        for (var i = 0; i < priority.Length; i++)
        {
            if (!_book.TryGetSeries(pair, priority[i], out ExchangeRateSeries? series) || series is null)
                continue;

            var reportedFrom = isInverted ? pair.ToIsoCode : pair.FromIsoCode;
            var reportedTo = isInverted ? pair.FromIsoCode : pair.ToIsoCode;

            foreach (ExchangeRateObservation observation in series.GetObservations())
            {
                if (observation.Date < startDate || observation.Date > endDate)
                    continue;

                result.Add(ExchangeRate.FromObservedRate(reportedFrom, reportedTo, observation.Date, observation.Rate, series.Provider, isInverted));
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
        var any = false;

        foreach (ExchangeRateSeriesKey key in _book.Keys)
        {
            if (!_book.TryGetSeries(key.Pair, key.Provider, out ExchangeRateSeries? series) || series is null || series.Count == 0)
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
}
