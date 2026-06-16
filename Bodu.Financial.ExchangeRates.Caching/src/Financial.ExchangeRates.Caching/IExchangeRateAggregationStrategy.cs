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
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // A custom strategy that prefers the last candidate able to resolve (reverse priority).
/// public sealed class LastAvailableStrategy : IExchangeRateAggregationStrategy
/// {
///     public bool TryAggregate(string fromIsoCode, string toIsoCode, DateOnly date,
///         ExchangeRateLookupOptions options, IReadOnlyList<NamedDatedExchangeRateProvider> candidates,
///         out ExchangeRateLookupResult result)
///     {
///         for (int i = candidates.Count - 1; i >= 0; i--)
///         {
///             if (candidates[i].Provider.TryGetRate(fromIsoCode, toIsoCode, date, options, out result))
///                 return true;
///         }
///
///         result = default;
///         return false;
///     }
///
///     public async ValueTask<IReadOnlyList<ExchangeRate>> AggregateRangeAsync(string fromIsoCode, string toIsoCode,
///         DateOnly startDate, DateOnly endDate, IReadOnlyList<NamedDatedExchangeRateProvider> candidates,
///         CancellationToken cancellationToken)
///     {
///         for (int i = candidates.Count - 1; i >= 0; i--)
///         {
///             IReadOnlyList<ExchangeRate> rates = [.. await candidates[i].Provider
///                 .GetRatesAsync(fromIsoCode, toIsoCode, startDate, endDate, cancellationToken)];
///             if (rates.Count > 0)
///                 return rates;
///         }
///
///         return Array.Empty<ExchangeRate>();
///     }
/// }
///]]>
/// </code>
/// </example>
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

    /// <summary>
    /// Combines the candidates' rates over the inclusive date range <paramref name="startDate" /> to
    /// <paramref name="endDate" /> synchronously.
    /// </summary>
    /// <param name="fromIsoCode">The source-currency ISO code.</param>
    /// <param name="toIsoCode">The destination-currency ISO code.</param>
    /// <param name="startDate">The inclusive start of the range.</param>
    /// <param name="endDate">The inclusive end of the range.</param>
    /// <param name="candidates">The ordered candidate providers to combine.</param>
    /// <returns>The combined rates ordered by date, or an empty list when none are available.</returns>
    /// <remarks>
    /// This member lets the aggregator's synchronous range surface stay synchronous rather than block on
    /// <see cref="AggregateRangeAsync" />. The default implementation blocks on the asynchronous overload as a
    /// compatibility fallback for strategies that supply only the asynchronous combination; the built-in strategies
    /// override it with a genuinely synchronous implementation. Override it whenever the candidates expose a
    /// synchronous range surface.
    /// </remarks>
    IReadOnlyList<ExchangeRate> AggregateRange(
        string fromIsoCode,
        string toIsoCode,
        DateOnly startDate,
        DateOnly endDate,
        IReadOnlyList<NamedDatedExchangeRateProvider> candidates)
    {
        // Compatibility fallback: a strategy that implements only the asynchronous combination still works on the
        // synchronous surface by blocking here. The shipped strategies override this so no block occurs in the common
        // case.
#pragma warning disable VSTHRD002 // Intentional compatibility fallback for async-only strategy implementations.
        return AggregateRangeAsync(fromIsoCode, toIsoCode, startDate, endDate, candidates, CancellationToken.None)
            .AsTask().GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
    }
}
