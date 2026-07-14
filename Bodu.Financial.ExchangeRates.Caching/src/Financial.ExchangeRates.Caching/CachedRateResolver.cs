// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CachedRateResolver.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Resolves a single-date lookup directly against the fresh cached rows of a pair (and, when permitted, its inverse),
/// replicating <see cref="FixedDatedRateProvider" />'s lookup semantics without materializing a provider snapshot.
/// </summary>
/// <remarks>
/// <para>
/// The caching decorator previously answered a cached single-date lookup by copying every cached row into a throwaway
/// <see cref="FixedDatedRateProvider" /> — building a frozen rate book, per-pair series storage, and several interim
/// collections per lookup. This resolver answers the same question over the row lists the decorator already holds: a
/// binary search over the date-sorted rows, the shared <see cref="RateDateSearch" /> candidate selection (the single
/// source of truth for previous/next/nearest and tie semantics), and the same strict tolerance clamp as
/// <c>RateSeriesStorage</c>.
/// </para>
/// <para>
/// Semantics deliberately mirror <see cref="FixedDatedRateProvider.TryGetRate(string, string, DateOnly,
/// RateLookupOptions, out RateLookupResult)" />: the same-currency identity short-circuit, an unconditional preference
/// for the direct pair with the inverse consulted only when the direct rows yield nothing, inverse composition through
/// <see cref="ExchangeRate.FromObservedRate" /> so the served rate divides by the originally observed reverse-pair
/// rate, and a <see cref="RateProvenance.Live(string)" /> provenance the decorator subsequently overwrites with the
/// cache lineage.
/// </para>
/// </remarks>
internal static class CachedRateResolver
{
    /// <summary>The largest row count whose day numbers are staged on the stack; larger sets fall back to a heap array.</summary>
    private const int MaxStackDays = 256;

    /// <summary>
    /// Attempts to resolve a lookup for <paramref name="pair" /> on <paramref name="date" /> from the supplied fresh
    /// cached rows.
    /// </summary>
    /// <param name="pair">The requested currency pair.</param>
    /// <param name="direct">The fresh cached rows of the requested pair, ordered ascending by date.</param>
    /// <param name="inverse">
    /// The fresh cached rows of the inverse pair, ordered ascending by date; empty when inversion is not permitted.
    /// </param>
    /// <param name="date">The requested date.</param>
    /// <param name="options">
    /// The lookup options, or <see langword="null" /> for <see cref="RateLookupOptions.Exact" />.
    /// </param>
    /// <param name="provider">The provider name the served rate is attributed to.</param>
    /// <param name="result">When this method returns <see langword="true" />, the resolved lookup result.</param>
    /// <returns>
    /// <see langword="true" /> when a rate was resolved from the cached rows; otherwise <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="options" /> fails validation.</exception>
    public static bool TryResolve(
        CurrencyPair pair,
        IReadOnlyList<CachedRate> direct,
        IReadOnlyList<CachedRate> inverse,
        DateOnly date,
        RateLookupOptions? options,
        string provider,
        out RateLookupResult result)
    {
        options ??= RateLookupOptions.Exact;
        options.Validate();

        // Same-currency identity, replicating the snapshot provider's short-circuit: a synthetic 1.0 rate attributed
        // to the well-known identity provider, before any pair resolution.
        if (options.AllowSameCurrencyIdentityRate && pair.From == pair.To)
        {
            ExchangeRate identity = new(pair.From, pair.To, date, 1m, FixedDatedRateProvider.IdentityProviderName);
            result = new RateLookupResult(identity, date, options.DateResolution, 0, RateProvenance.Live(identity.Provider));
            return true;
        }

        // The direct pair is preferred unconditionally; the inverse rows are consulted only when the direct rows
        // yield nothing, never as a nearest-across-both.
        if (TryResolveRows(direct, date, options, out DateOnly resolvedDate, out decimal observed, out int offsetDays))
        {
            var rate = ExchangeRate.FromObservedRate(pair.From, pair.To, resolvedDate, observed, provider, isInverted: false);
            result = new RateLookupResult(rate, date, options.DateResolution, offsetDays, RateProvenance.Live(rate.Provider));
            return true;
        }

        if (options.AllowInverse && TryResolveRows(inverse, date, options, out resolvedDate, out observed, out offsetDays))
        {
            // The observed rate is the reverse-pair rate; FromObservedRate reports the requested orientation with the
            // reciprocal multiplier while preserving the observed value for precise conversion.
            var rate = ExchangeRate.FromObservedRate(pair.From, pair.To, resolvedDate, observed, provider, isInverted: true);
            result = new RateLookupResult(rate, date, options.DateResolution, offsetDays, RateProvenance.Live(rate.Provider));
            return true;
        }

        result = default;
        return false;
    }

    /// <summary>
    /// Resolves the requested date against one pair's date-sorted rows: an exact binary-search hit, or — for non-exact
    /// resolutions — the shared candidate selection followed by the strict tolerance clamp.
    /// </summary>
    /// <param name="rows">The fresh cached rows, ordered ascending by date.</param>
    /// <param name="date">The requested date.</param>
    /// <param name="options">The validated lookup options.</param>
    /// <param name="resolvedDate">
    /// When this method returns <see langword="true" />, the resolved observation date.
    /// </param>
    /// <param name="observed">When this method returns <see langword="true" />, the observed rate on that date.</param>
    /// <param name="offsetDays">
    /// When this method returns <see langword="true" />, the absolute distance in days between the requested and
    /// resolved dates.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when an observation satisfied the resolution policy; otherwise <see langword="false" />.
    /// </returns>
    private static bool TryResolveRows(
        IReadOnlyList<CachedRate> rows,
        DateOnly date,
        RateLookupOptions options,
        out DateOnly resolvedDate,
        out decimal observed,
        out int offsetDays)
    {
        resolvedDate = default;
        observed = default;
        offsetDays = 0;

        if (rows.Count == 0)
            return false;

        // Stage the day numbers so the shared RateDateSearch selection operates on the same representation the series
        // storage uses; typical row counts stay on the stack.
        Span<int> days = rows.Count <= MaxStackDays ? stackalloc int[MaxStackDays] : new int[rows.Count];
        days = days[..rows.Count];
        for (int i = 0; i < rows.Count; i++)
            days[i] = rows[i].Date.DayNumber;

        int requested = date.DayNumber;
        int index = ((ReadOnlySpan<int>)days).BinarySearch(requested);
        if (index >= 0)
        {
            resolvedDate = rows[index].Date;
            observed = rows[index].Rate;
            return true;
        }

        // Exact never falls back to a neighbouring observation.
        if (options.DateResolution == RateDateResolution.Exact)
            return false;

        int next = ~index;
        int previous = next - 1;
        if (!RateDateSearch.TrySelectCandidate(days, requested, options.DateResolution, previous, next, out int candidate))
            return false;

        offsetDays = Math.Abs(days[candidate] - requested);
        if (offsetDays > options.ToleranceDays)
            return false;

        resolvedDate = rows[candidate].Date;
        observed = rows[candidate].Rate;
        return true;
    }
}
