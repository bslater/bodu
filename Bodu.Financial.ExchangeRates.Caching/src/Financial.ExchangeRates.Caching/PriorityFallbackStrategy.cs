// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PriorityFallbackStrategy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// An <see cref="IRateAggregationStrategy" /> that returns the first successful result from the ordered candidates,
/// giving deterministic, auditable fallback.
/// </summary>
/// <remarks>
/// On every lookup the candidates are consulted in order and the first to satisfy the request wins, so a preferred
/// provider's fallback-date hit beats a lower-priority provider's exact-date hit. This is the default strategy and the
/// successor to the former <c>CompositeDatedRateProvider</c>.
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using Bodu.Financial.ExchangeRates;
/// using Bodu.Financial.ExchangeRates.Caching;
///
/// // Children in priority order: the first source that can serve a pair wins,
/// // so a pair the primary does not quote falls through to the next source.
/// var aggregate = new AggregatingRateProvider(new[]
/// {
///     new NamedDatedRateProvider("RBA", rbaProvider),
///     new NamedDatedRateProvider("ECB", ecbProvider),
/// });
///
/// // Provenance names the child that answered.
/// RateLookupResult result = aggregate.GetRate("AUD", "USD", new DateOnly(2024, 3, 15));
/// var servedBy = result.Rate.Provider;
///]]>
/// </code>
/// </example>
/// </remarks>
public sealed class PriorityFallbackStrategy
    : IRateAggregationStrategy
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
    /// <value>The singleton <see cref="PriorityFallbackStrategy" />.</value>
    public static PriorityFallbackStrategy Instance { get; } = new();

    /// <inheritdoc />
    public bool TryAggregate(
        string fromIsoCode,
        string toIsoCode,
        DateOnly date,
        RateLookupOptions options,
        IReadOnlyList<NamedDatedRateProvider> candidates,
        out RateLookupResult result)
    {
        ThrowHelper.ThrowIfNull(candidates);

        for (int i = 0; i < candidates.Count; i++)
        {
            if (candidates[i].Provider.TryGetRate(fromIsoCode, toIsoCode, date, options, out result))
                return true;
        }

        result = default;
        return false;
    }

    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<ExchangeRate>> AggregateRangeAsync(
        string fromIsoCode,
        string toIsoCode,
        DateOnly startDate,
        DateOnly endDate,
        IReadOnlyList<NamedDatedRateProvider> candidates,
        CancellationToken cancellationToken)
    {
        ThrowHelper.ThrowIfNull(candidates);

        for (int i = 0; i < candidates.Count; i++)
        {
            IReadOnlyList<ExchangeRate> rates =
                [.. await candidates[i].Provider.GetRatesAsync(fromIsoCode, toIsoCode, startDate, endDate, cancellationToken).ConfigureAwait(false)];

            if (rates.Count > 0)
                return rates;
        }

        return Array.Empty<ExchangeRate>();
    }

    /// <inheritdoc />
    public IReadOnlyList<ExchangeRate> AggregateRange(
        string fromIsoCode,
        string toIsoCode,
        DateOnly startDate,
        DateOnly endDate,
        IReadOnlyList<NamedDatedRateProvider> candidates)
    {
        ThrowHelper.ThrowIfNull(candidates);

        for (int i = 0; i < candidates.Count; i++)
        {
            IReadOnlyList<ExchangeRate> rates =
                [.. candidates[i].Provider.GetRates(fromIsoCode, toIsoCode, startDate, endDate)];

            if (rates.Count > 0)
                return rates;
        }

        return Array.Empty<ExchangeRate>();
    }
}
