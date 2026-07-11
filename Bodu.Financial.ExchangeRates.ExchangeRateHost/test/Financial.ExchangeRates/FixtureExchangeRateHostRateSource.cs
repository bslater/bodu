// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FixtureExchangeRateHostRateSource.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// An <see cref="IPairRateSource{TSeries}" /> that parses embedded JSON fixtures instead of issuing network requests,
/// mapping currency pairs to fixture files and recording how many pairs it served.
/// </summary>
internal sealed class FixtureExchangeRateHostRateSource
    : IPairRateSource<ExchangeRateHostSeriesInfo>
{
    /// <summary>The provider options used while parsing fixtures.</summary>
    private readonly ExchangeRateHostRateProviderOptions _options;

    /// <summary>The map from <c>FROM/TO</c> pair key to fixture file name.</summary>
    private readonly IReadOnlyDictionary<string, string> _fixtureByPair;

    /// <summary>
    /// Initializes a new instance of the <see cref="FixtureExchangeRateHostRateSource" /> class.
    /// </summary>
    /// <param name="options">The provider options used while parsing fixtures.</param>
    /// <param name="fixtureByPair">
    /// An optional pair-to-fixture map; defaults to mapping <c>EUR/USD</c> to the sample time-series fixture.
    /// </param>
    public FixtureExchangeRateHostRateSource(
        ExchangeRateHostRateProviderOptions options,
        IReadOnlyDictionary<string, string>? fixtureByPair = null)
    {
        _options = options;
        _fixtureByPair = fixtureByPair ?? new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["EUR/USD"] = ExchangeRateHostFixtures.EurUsdTimeSeries,
        };
    }

    /// <summary>
    /// Gets the number of pairs this source has served.
    /// </summary>
    /// <value>The pair request count.</value>
    public int GetPairCallCount { get; private set; }

    /// <inheritdoc />
    public ValueTask<PairRateData<ExchangeRateHostSeriesInfo>> GetPairAsync(CurrencyPairRequest request, CancellationToken cancellationToken = default)
    {
        GetPairCallCount++;

        string sourceSymbol = _options.MapSymbol(request.Pair.From.ToString());
        string quoteSymbol = _options.MapSymbol(request.Pair.To.ToString());

        string key = $"{request.Pair.From}/{request.Pair.To}";
        if (_fixtureByPair.TryGetValue(key, out string? fixture))
        {
            byte[] json = ExchangeRateHostFixtures.ReadBytes(fixture);
            return ValueTask.FromResult(ExchangeRateHostResponseParser.Parse(json, request, sourceSymbol, quoteSymbol, _options));
        }

        // Unknown pair: behave like one with no published data so inverse-fallback paths can be exercised.
        return ValueTask.FromResult(
            new PairRateData<ExchangeRateHostSeriesInfo>(
                request.Pair,
                Array.Empty<RateObservation>(),
                new ExchangeRateHostSeriesInfo(request.Pair, request.Pair.From.ToString(), request.Pair.To.ToString())));
    }
}
