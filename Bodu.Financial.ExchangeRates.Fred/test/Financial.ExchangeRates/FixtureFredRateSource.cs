// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FixtureFredRateSource.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// An <see cref="IPairRateSource{TSeries}" /> that parses embedded JSON fixtures instead of issuing network requests,
/// mapping currency pairs to fixture files and recording how many pairs it served.
/// </summary>
internal sealed class FixtureFredRateSource
    : IPairRateSource<FredSeriesInfo>
{
    /// <summary>The provider options used while resolving series identifiers.</summary>
    private readonly FredRateProviderOptions _options;

    /// <summary>The map from <c>FROM/TO</c> pair key to fixture file name.</summary>
    private readonly IReadOnlyDictionary<string, string> _fixtureByPair;

    /// <summary>
    /// Initializes a new instance of the <see cref="FixtureFredRateSource" /> class.
    /// </summary>
    /// <param name="options">The provider options used while resolving series identifiers.</param>
    /// <param name="fixtureByPair">
    /// An optional pair-to-fixture map; defaults to mapping <c>EUR/USD</c> to the sample observations fixture.
    /// </param>
    public FixtureFredRateSource(
        FredRateProviderOptions options,
        IReadOnlyDictionary<string, string>? fixtureByPair = null)
    {
        _options = options;
        _fixtureByPair = fixtureByPair ?? new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["EUR/USD"] = FredFixtures.DexUsEu,
        };
    }

    /// <summary>
    /// Gets the number of pairs this source has served.
    /// </summary>
    /// <value>The pair request count.</value>
    public int GetPairCallCount { get; private set; }

    /// <inheritdoc />
    public ValueTask<PairRateData<FredSeriesInfo>> GetPairAsync(CurrencyPairRequest request, CancellationToken cancellationToken = default)
    {
        GetPairCallCount++;

        string key = $"{request.Pair.From}/{request.Pair.To}";
        if (_fixtureByPair.TryGetValue(key, out string? fixture))
        {
            _ = _options.TryGetSeriesId(request.Pair, out string seriesId);
            byte[] json = FredFixtures.ReadBytes(fixture);
            return ValueTask.FromResult(FredResponseParser.Parse(json, request, seriesId));
        }

        // Unknown pair: behave like one with no published data so inverse-fallback paths can be exercised.
        return ValueTask.FromResult(
            new PairRateData<FredSeriesInfo>(
                request.Pair,
                Array.Empty<RateObservation>(),
                new FredSeriesInfo(request.Pair, string.Empty)));
    }
}
