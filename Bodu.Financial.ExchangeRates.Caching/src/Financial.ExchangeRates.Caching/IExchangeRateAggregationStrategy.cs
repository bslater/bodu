// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IExchangeRateAggregationStrategy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Decides how an <see cref="AggregatingExchangeRateProvider" /> combines the results of an ordered set of candidate
/// providers into a single answer.
/// </summary>
/// <remarks>
/// This is the extensibility seam of the aggregation design: ship strategies such as
/// <see cref="PriorityFallbackStrategy" /> and <see cref="AverageStrategy" /> cover the common cases, and consumers can
/// implement this interface to craft their own (weighted, median, first-non-stale, and so on). The aggregator resolves
/// the candidate set and per-pair strategy, and never passes a <see langword="null" /> options value; same-currency
/// identity is handled by the aggregator before the strategy is consulted.
/// </remarks>
public interface IExchangeRateAggregationStrategy
{
    /// <summary>
    /// Attempts to resolve a single-date rate from the supplied candidates.
    /// </summary>
    /// <param name="fromIsoCode">The source-currency ISO code.</param>
    /// <param name="toIsoCode">The destination-currency ISO code.</param>
    /// <param name="date">The calendar date for which a rate is required.</param>
    /// <param name="options">The lookup rules to apply; never <see langword="null" />.</param>
    /// <param name="candidates">The ordered candidate providers to combine.</param>
    /// <param name="result">When this method returns <see langword="true" />, the resolved result.</param>
    /// <returns>
    /// <see langword="true" /> when the candidates yielded a rate; otherwise <see langword="false" />.
    /// </returns>
    bool TryAggregate(
        string fromIsoCode,
        string toIsoCode,
        DateOnly date,
        ExchangeRateLookupOptions options,
        IReadOnlyList<NamedDatedExchangeRateProvider> candidates,
        out ExchangeRateLookupResult result);

    /// <summary>
    /// Combines the candidates' rates over the inclusive date range <paramref name="startDate" /> to
    /// <paramref name="endDate" />.
    /// </summary>
    /// <param name="fromIsoCode">The source-currency ISO code.</param>
    /// <param name="toIsoCode">The destination-currency ISO code.</param>
    /// <param name="startDate">The inclusive start of the range.</param>
    /// <param name="endDate">The inclusive end of the range.</param>
    /// <param name="candidates">The ordered candidate providers to combine.</param>
    /// <param name="cancellationToken">A token to observe while awaiting the operation.</param>
    /// <returns>The combined rates ordered by date, or an empty list when none are available.</returns>
    ValueTask<IReadOnlyList<ExchangeRate>> AggregateRangeAsync(
        string fromIsoCode,
        string toIsoCode,
        DateOnly startDate,
        DateOnly endDate,
        IReadOnlyList<NamedDatedExchangeRateProvider> candidates,
        CancellationToken cancellationToken);
}
