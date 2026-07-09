// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HistoryAvailabilityGuard.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Provides the shared date arithmetic the caching and aggregating decorators use when honouring a source's advertised
/// <see cref="ExchangeRateHistoryAvailability" />.
/// </summary>
internal static class HistoryAvailabilityGuard
{
    /// <summary>
    /// Resolves the latest date a single-date lookup could resolve to under the supplied rules: the requested date for
    /// exact and backward-resolving rules, or the requested date plus the tolerance for forward-capable rules.
    /// </summary>
    /// <param name="date">The requested date.</param>
    /// <param name="options">The lookup rules to apply; <see langword="null" /> selects the exact-match rules.</param>
    /// <returns>The latest reachable date, saturating at <see cref="DateOnly.MaxValue" />.</returns>
    /// <remarks>
    /// Forward-resolving rules (<see cref="ExchangeRateDateResolution.NextOnOrAfter" /> and the nearest family) can
    /// reach up to <see cref="ExchangeRateLookupOptions.ToleranceDays" /> past the requested date, so availability
    /// guards must test this reachable maximum rather than the requested date itself — a request just outside an
    /// advertised floor whose tolerance reaches back inside it can still be served.
    /// </remarks>
    internal static DateOnly LatestReachableDate(DateOnly date, ExchangeRateLookupOptions? options)
    {
        ExchangeRateLookupOptions effective = options ?? ExchangeRateLookupOptions.Exact;
        if (effective.DateResolution is ExchangeRateDateResolution.Exact or ExchangeRateDateResolution.PreviousOnOrBefore)
            return date;

        // Widen in day-number space so an extreme tolerance saturates at DateOnly.MaxValue instead of overflowing.
        long reachableDay = (long)date.DayNumber + effective.ToleranceDays;
        return reachableDay >= DateOnly.MaxValue.DayNumber ? DateOnly.MaxValue : DateOnly.FromDayNumber((int)reachableDay);
    }
}
