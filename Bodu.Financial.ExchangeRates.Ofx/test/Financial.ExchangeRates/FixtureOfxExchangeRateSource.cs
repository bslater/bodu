// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FixtureOfxExchangeRateSource.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// An <see cref="IPairRateSource{TSeries}" /> that parses embedded JSON fixtures instead of issuing network
/// requests, mapping currency pairs to fixture files and recording how many pairs it served.
/// </summary>
internal sealed class FixtureOfxExchangeRateSource
    : IPairRateSource<OfxSeriesInfo>
{
    /// <summary>The provider options used while parsing fixtures.</summary>
    private readonly OfxExchangeRateOptions _options;

    /// <summary>The map from <c>FROM/TO</c> pair key to fixture file name.</summary>
    private readonly IReadOnlyDictionary<string, string> _fixtureByPair;

    /// <summary>
    /// Initializes a new instance of the <see cref="FixtureOfxExchangeRateSource" /> class.
    /// </summary>
    /// <param name="options">The provider options used while parsing fixtures.</param>
    /// <param name="fixtureByPair">
    /// An optional pair-to-fixture map; defaults to mapping <c>AUD/USD</c> to the sample fixture.
    /// </param>
    public FixtureOfxExchangeRateSource(
        OfxExchangeRateOptions options,
        IReadOnlyDictionary<string, string>? fixtureByPair = null)
    {
        _options = options;
        _fixtureByPair = fixtureByPair ?? new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["AUD/USD"] = OfxFixtures.AudUsd,
        };
    }

    /// <summary>
    /// Gets the number of pairs this source has served.
    /// </summary>
    /// <value>The pair request count.</value>
    public int GetPairCallCount { get; private set; }

    /// <inheritdoc />
    public ValueTask<PairRateData<OfxSeriesInfo>> GetPairAsync(CurrencyPairRequest request, CancellationToken cancellationToken = default)
    {
        GetPairCallCount++;

        string key = $"{request.Pair.From}/{request.Pair.To}";
        if (_fixtureByPair.TryGetValue(key, out string? fixture))
        {
            byte[] json = OfxFixtures.ReadBytes(fixture);
            return ValueTask.FromResult(OfxSpotRateHistoryResponseParser.Parse(json, request, _options));
        }

        // Unknown pair: behave like one with no published data so inverse-fallback paths can be exercised.
        return ValueTask.FromResult(
            new PairRateData<OfxSeriesInfo>(request.Pair, Array.Empty<RateObservation>(), new OfxSeriesInfo(request.Pair, request.Pair.To.ToString())));
    }
}
