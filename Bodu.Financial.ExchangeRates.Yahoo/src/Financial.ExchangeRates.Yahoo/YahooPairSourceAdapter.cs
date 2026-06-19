// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YahooPairSourceAdapter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Yahoo;

/// <summary>
/// Adapts a Yahoo-specific <see cref="IYahooExchangeRateChartSource" /> to the generic
/// <see cref="IExchangeRatePairSource{TSeries}" /> the <see cref="PairWebExchangeRateProvider{TSeries}" /> base
/// consumes, building the ticker from the pair and projecting the parsed chart into a
/// <see cref="PairRateData{TSeries}" />.
/// </summary>
internal sealed class YahooPairSourceAdapter
    : IExchangeRatePairSource<YahooSeriesInfo>
{
    /// <summary>The underlying chart source that fetches and parses Yahoo Finance charts.</summary>
    private readonly IYahooExchangeRateChartSource _source;

    /// <summary>The provider options supplying the ticker template and currency aliases.</summary>
    private readonly YahooExchangeRateOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="YahooPairSourceAdapter" /> class.
    /// </summary>
    /// <param name="source">The chart source to adapt.</param>
    /// <param name="options">The provider options used to build the ticker.</param>
    internal YahooPairSourceAdapter(IYahooExchangeRateChartSource source, YahooExchangeRateOptions options)
    {
        ThrowHelper.ThrowIfNull(source);
        ThrowHelper.ThrowIfNull(options);

        _source = source;
        _options = options;
    }

    /// <inheritdoc />
    public async ValueTask<PairRateData<YahooSeriesInfo>> GetPairAsync(ExchangeRatePairRequest request, CancellationToken cancellationToken = default)
    {
        string symbol = _options.BuildSymbol(request.Pair.From.ToString(), request.Pair.To.ToString());
        YahooChartRequest chartRequest = new(request.Pair, symbol, request.StartDate, request.EndDate);

        YahooExchangeRateChart chart = await _source.GetChartAsync(chartRequest, cancellationToken).ConfigureAwait(false);

        return new PairRateData<YahooSeriesInfo>(chart.Pair, chart.Observations, chart.GetSeriesInfo());
    }
}
