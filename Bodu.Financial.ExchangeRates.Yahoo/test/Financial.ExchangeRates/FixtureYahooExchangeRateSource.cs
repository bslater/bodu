// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FixtureYahooExchangeRateSource.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// An <see cref="IExchangeRatePairSource{TSeries}" /> that parses embedded JSON fixtures instead of issuing network
/// requests, mapping currency pairs to fixture files and recording how many pairs it served.
/// </summary>
internal sealed class FixtureYahooExchangeRateSource
    : IExchangeRatePairSource<YahooSeriesInfo>
{
    /// <summary>The provider options used while parsing fixtures.</summary>
    private readonly YahooExchangeRateOptions _options;

    /// <summary>The map from <c>FROM/TO</c> pair key to fixture file name.</summary>
    private readonly IReadOnlyDictionary<string, string> _fixtureByPair;

    /// <summary>
    /// Initializes a new instance of the <see cref="FixtureYahooExchangeRateSource" /> class.
    /// </summary>
    /// <param name="options">The provider options used while parsing fixtures.</param>
    /// <param name="fixtureByPair">
    /// An optional pair-to-fixture map; defaults to mapping <c>AUD/USD</c> to the sample fixture.
    /// </param>
    public FixtureYahooExchangeRateSource(
        YahooExchangeRateOptions options,
        IReadOnlyDictionary<string, string>? fixtureByPair = null)
    {
        _options = options;
        _fixtureByPair = fixtureByPair ?? new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["AUD/USD"] = YahooFixtures.AudUsd,
        };
    }

    /// <summary>
    /// Gets the number of pairs this source has served.
    /// </summary>
    /// <value>The pair request count.</value>
    public int GetPairCallCount { get; private set; }

    /// <inheritdoc />
    public ValueTask<PairRateData<YahooSeriesInfo>> GetPairAsync(ExchangeRatePairRequest request, CancellationToken cancellationToken = default)
    {
        GetPairCallCount++;

        string symbol = _options.BuildSymbol(request.Pair.From.ToString(), request.Pair.To.ToString());

        string key = $"{request.Pair.From}/{request.Pair.To}";
        if (_fixtureByPair.TryGetValue(key, out string? fixture))
        {
            byte[] json = YahooFixtures.ReadBytes(fixture);
            return ValueTask.FromResult(YahooChartResponseParser.Parse(json, request, symbol, _options));
        }

        // Unknown pair: behave like one with no published data so inverse-fallback paths can be exercised.
        return ValueTask.FromResult(
            new PairRateData<YahooSeriesInfo>(request.Pair, Array.Empty<ExchangeRateObservation>(), new YahooSeriesInfo(request.Pair, symbol, request.Pair.To.ToString())));
    }
}
