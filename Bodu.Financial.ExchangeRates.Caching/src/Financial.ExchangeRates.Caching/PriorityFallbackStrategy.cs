// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PriorityFallbackStrategy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// An <see cref="IExchangeRateAggregationStrategy" /> that returns the first successful result from the ordered
/// candidates, giving deterministic, auditable fallback.
/// </summary>
/// <remarks>
/// On every lookup the candidates are consulted in order and the first to satisfy the request wins, so a preferred
/// provider's fallback-date hit beats a lower-priority provider's exact-date hit. This is the default strategy and the
/// successor to the former <c>CompositeDatedExchangeRateProvider</c>.
/// </remarks>
public sealed class PriorityFallbackStrategy
    : IExchangeRateAggregationStrategy
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PriorityFallbackStrategy" /> class.
    /// </summary>
    private PriorityFallbackStrategy()
    {
    }

    /// <summary>
    /// Gets the shared stateless instance of the strategy.
    /// </summary>
    /// <returns>The singleton <see cref="PriorityFallbackStrategy" />.</returns>
    public static PriorityFallbackStrategy Instance { get; } = new();

    /// <inheritdoc />
    public bool TryAggregate(
        string fromIsoCode,
        string toIsoCode,
        DateOnly date,
        ExchangeRateLookupOptions options,
        IReadOnlyList<NamedDatedExchangeRateProvider> candidates,
        out ExchangeRateLookupResult result)
    {
        ThrowHelper.ThrowIfNull(candidates);

        for (var i = 0; i < candidates.Count; i++)
        {
            if (candidates[i].Provider.TryGetRate(fromIsoCode, toIsoCode, date, options, out result))
                return true;
        }

        result = default;
        return false;
    }

    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<ExchangeRateLookupResult>> AggregateRangeAsync(
        string fromIsoCode,
        string toIsoCode,
        DateOnly startDate,
        DateOnly endDate,
        IReadOnlyList<NamedDatedExchangeRateProvider> candidates,
        CancellationToken cancellationToken)
    {
        ThrowHelper.ThrowIfNull(candidates);

        for (var i = 0; i < candidates.Count; i++)
        {
            IReadOnlyList<ExchangeRateLookupResult> rates =
                [.. await candidates[i].Provider.GetRatesAsync(fromIsoCode, toIsoCode, startDate, endDate, cancellationToken).ConfigureAwait(false)];

            if (rates.Count > 0)
                return rates;
        }

        return Array.Empty<ExchangeRateLookupResult>();
    }
}
