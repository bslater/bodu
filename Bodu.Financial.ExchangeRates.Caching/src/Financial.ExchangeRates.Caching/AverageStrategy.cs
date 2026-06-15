// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AverageStrategy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// An <see cref="IExchangeRateAggregationStrategy" /> that returns the arithmetic mean of every candidate that can
/// resolve the request, tagged with a synthetic provider label.
/// </summary>
/// <remarks>
/// <para>
/// The mean is computed in <see cref="decimal" /> over the forward rates each candidate returns (each candidate has
/// already resolved its own inverse, so the synthesized rate is never itself inverted) and is not pre-rounded; rounding
/// is deferred to the money boundary. The synthesized observation is dated at the requested date, and its reported
/// offset is the largest offset among the contributing candidates.
/// </para>
/// <para>
/// Averaging is most meaningful with <see cref="ExchangeRateLookupOptions.Exact" />, where every contributor shares the
/// requested date; under fallback resolutions the contributors may have resolved different dates. The range overload
/// averages only the dates present in <em>every</em> contributing candidate (an inner join by date).
/// </para>
/// </remarks>
public sealed class AverageStrategy
    : IExchangeRateAggregationStrategy
{
    /// <summary>
    /// The default provider label applied to a synthesized average rate.
    /// </summary>
    public const string DefaultProviderLabel = "Average";

    /// <summary>
    /// The label tagged onto each synthesized average rate.
    /// </summary>
    private readonly string _providerLabel;

    /// <summary>
    /// Initializes a new instance of the <see cref="AverageStrategy" /> class.
    /// </summary>
    /// <param name="providerLabel">The provider label applied to each synthesized average rate.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="providerLabel" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="providerLabel" /> is empty or white space.
    /// </exception>
    public AverageStrategy(string providerLabel = DefaultProviderLabel)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(providerLabel);

        _providerLabel = providerLabel;
    }

    /// <summary>
    /// Gets the provider label applied to each synthesized average rate.
    /// </summary>
    /// <returns>The synthetic provider label.</returns>
    public string ProviderLabel => _providerLabel;

    /// <inheritdoc />
    public bool TryAggregate(
        string fromIsoCode,
        string toIsoCode,
        DateOnly date,
        ExchangeRateLookupOptions options,
        IReadOnlyList<NamedDatedExchangeRateProvider> candidates,
        out ExchangeRateLookupResult result)
    {
        ThrowHelper.ThrowIfNull(options);
        ThrowHelper.ThrowIfNull(candidates);

        var sum = 0m;
        var count = 0;
        var maxOffset = 0;

        for (var i = 0; i < candidates.Count; i++)
        {
            if (candidates[i].Provider.TryGetRate(fromIsoCode, toIsoCode, date, options, out ExchangeRateLookupResult candidate))
            {
                sum += candidate.Rate.Rate;
                count++;

                if (candidate.OffsetDays > maxOffset)
                    maxOffset = candidate.OffsetDays;
            }
        }

        if (count == 0)
        {
            result = default;
            return false;
        }

        ExchangeRate rate = new(fromIsoCode, toIsoCode, date, sum / count, _providerLabel);
        result = new ExchangeRateLookupResult(rate, date, options.DateResolution, maxOffset, ExchangeRateProvenance.Live(rate.Provider));
        return true;
    }

    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<ExchangeRate>> AggregateRangeAsync(
        string fromIsoCode,
        string toIsoCode,
        DateOnly startDate,
        DateOnly endDate,
        IReadOnlyList<NamedDatedExchangeRateProvider> candidates,
        CancellationToken cancellationToken)
    {
        ThrowHelper.ThrowIfNull(candidates);

        if (candidates.Count == 0)
            return Array.Empty<ExchangeRate>();

        // Gather each candidate's observations keyed by date; the last observation wins for a duplicated date.
        List<Dictionary<DateOnly, decimal>> perCandidate = new(candidates.Count);
        foreach (NamedDatedExchangeRateProvider candidate in candidates)
        {
            IReadOnlyList<ExchangeRate> rates =
                await candidate.Provider.GetRatesAsync(fromIsoCode, toIsoCode, startDate, endDate, cancellationToken).ConfigureAwait(false);

            Dictionary<DateOnly, decimal> byDate = new(rates.Count);
            foreach (ExchangeRate rate in rates)
                byDate[rate.Date] = rate.Rate;

            // A missing or empty candidate makes the inner join empty; return early.
            if (byDate.Count == 0)
                return Array.Empty<ExchangeRate>();

            perCandidate.Add(byDate);
        }

        // Drive the join from the smallest candidate so the membership probes are minimized.
        Dictionary<DateOnly, decimal> smallest = perCandidate[0];
        for (var i = 1; i < perCandidate.Count; i++)
        {
            if (perCandidate[i].Count < smallest.Count)
                smallest = perCandidate[i];
        }

        List<ExchangeRate> result = new(smallest.Count);
        foreach (DateOnly date in smallest.Keys)
        {
            var sum = 0m;
            var present = true;

            foreach (Dictionary<DateOnly, decimal> byDate in perCandidate)
            {
                if (!byDate.TryGetValue(date, out var rate))
                {
                    present = false;
                    break;
                }

                sum += rate;
            }

            if (present)
                result.Add(new ExchangeRate(fromIsoCode, toIsoCode, date, sum / perCandidate.Count, _providerLabel));
        }

        result.Sort(static (left, right) => left.Date.CompareTo(right.Date));
        return result;
    }
}
