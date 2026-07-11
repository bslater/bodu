// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FixtureImfRateSource.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// An <see cref="IPairRateSource{TSeries}" /> that parses embedded JSON fixtures instead of issuing network requests,
/// mapping currency pairs to fixture files and recording how many pairs it served.
/// </summary>
internal sealed class FixtureImfRateSource
    : IPairRateSource<ImfSeriesInfo>
{
    /// <summary>The provider options used while resolving series keys.</summary>
    private readonly ImfRateProviderOptions _options;

    /// <summary>The map from <c>FROM/TO</c> pair key to fixture file name.</summary>
    private readonly IReadOnlyDictionary<string, string> _fixtureByPair;

    /// <summary>
    /// Initializes a new instance of the <see cref="FixtureImfRateSource" /> class.
    /// </summary>
    /// <param name="options">The provider options used while resolving series keys.</param>
    /// <param name="fixtureByPair">
    /// An optional pair-to-fixture map; defaults to mapping <c>USD/GBP</c> to the sample monthly fixture.
    /// </param>
    public FixtureImfRateSource(
        ImfRateProviderOptions options,
        IReadOnlyDictionary<string, string>? fixtureByPair = null)
    {
        _options = options;
        _fixtureByPair = fixtureByPair ?? new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["USD/GBP"] = ImfFixtures.UsdGbp2023,
        };
    }

    /// <summary>
    /// Gets the number of pairs this source has served.
    /// </summary>
    /// <value>The pair request count.</value>
    public int GetPairCallCount { get; private set; }

    /// <inheritdoc />
    public ValueTask<PairRateData<ImfSeriesInfo>> GetPairAsync(CurrencyPairRequest request, CancellationToken cancellationToken = default)
    {
        GetPairCallCount++;

        string key = $"{request.Pair.From}/{request.Pair.To}";
        if (_fixtureByPair.TryGetValue(key, out string? fixture))
        {
            _ = _options.TryGetSeriesKey(request.Pair, out string seriesKey);
            byte[] json = ImfFixtures.ReadBytes(fixture);
            return ValueTask.FromResult(ImfResponseParser.Parse(json, request, seriesKey));
        }

        // Unknown pair: behave like one with no published data so inverse-fallback paths can be exercised.
        return ValueTask.FromResult(
            new PairRateData<ImfSeriesInfo>(
                request.Pair,
                Array.Empty<RateObservation>(),
                new ImfSeriesInfo(request.Pair, string.Empty)));
    }
}
