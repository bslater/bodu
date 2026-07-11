// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FixtureFixerRateSource.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// An <see cref="IPairRateSource{TSeries}" /> that parses embedded JSON fixtures instead of issuing network requests,
/// mapping currency pairs to fixture files and recording how many pairs it served.
/// </summary>
internal sealed class FixtureFixerRateSource
    : IPairRateSource<FixerSeriesInfo>
{
    /// <summary>The provider options used while parsing fixtures.</summary>
    private readonly FixerRateProviderOptions _options;

    /// <summary>The map from <c>FROM/TO</c> pair key to fixture file name.</summary>
    private readonly IReadOnlyDictionary<string, string> _fixtureByPair;

    /// <summary>
    /// Initializes a new instance of the <see cref="FixtureFixerRateSource" /> class.
    /// </summary>
    /// <param name="options">The provider options used while parsing fixtures.</param>
    /// <param name="fixtureByPair">
    /// An optional pair-to-fixture map; defaults to mapping <c>EUR/USD</c> to the sample time-series fixture.
    /// </param>
    public FixtureFixerRateSource(
        FixerRateProviderOptions options,
        IReadOnlyDictionary<string, string>? fixtureByPair = null)
    {
        _options = options;
        _fixtureByPair = fixtureByPair ?? new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["EUR/USD"] = FixerFixtures.EurUsdTimeSeries,
        };
    }

    /// <summary>
    /// Gets the number of pairs this source has served.
    /// </summary>
    /// <value>The pair request count.</value>
    public int GetPairCallCount { get; private set; }

    /// <inheritdoc />
    public ValueTask<PairRateData<FixerSeriesInfo>> GetPairAsync(CurrencyPairRequest request, CancellationToken cancellationToken = default)
    {
        GetPairCallCount++;

        string quoteSymbol = _options.MapSymbol(request.Pair.To.ToString());

        string key = $"{request.Pair.From}/{request.Pair.To}";
        if (_fixtureByPair.TryGetValue(key, out string? fixture))
        {
            byte[] json = FixerFixtures.ReadBytes(fixture);
            return ValueTask.FromResult(FixerResponseParser.Parse(json, request, quoteSymbol, _options));
        }

        // Unknown pair: behave like one with no published data so inverse-fallback paths can be exercised.
        return ValueTask.FromResult(
            new PairRateData<FixerSeriesInfo>(
                request.Pair,
                Array.Empty<RateObservation>(),
                new FixerSeriesInfo(request.Pair, request.Pair.From.ToString(), request.Pair.To.ToString())));
    }
}
