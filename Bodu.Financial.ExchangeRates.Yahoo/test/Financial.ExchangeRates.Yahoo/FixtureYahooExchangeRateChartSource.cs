// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FixtureYahooExchangeRateChartSource.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Yahoo;

/// <summary>
/// An <see cref="IYahooExchangeRateChartSource" /> that parses embedded JSON fixtures instead of issuing network
/// requests, mapping ticker symbols to fixture files and recording how many charts it served.
/// </summary>
internal sealed class FixtureYahooExchangeRateChartSource
    : IYahooExchangeRateChartSource
{
    /// <summary>The provider options used while parsing fixtures.</summary>
    private readonly YahooExchangeRateOptions _options;

    /// <summary>The map from ticker symbol to fixture file name.</summary>
    private readonly IReadOnlyDictionary<string, string> _fixtureBySymbol;

    /// <summary>
    /// Initializes a new instance of the <see cref="FixtureYahooExchangeRateChartSource" /> class.
    /// </summary>
    /// <param name="options">The provider options used while parsing fixtures.</param>
    /// <param name="fixtureBySymbol">
    /// An optional symbol-to-fixture map; defaults to mapping <c>AUDUSD=X</c> to the sample fixture.
    /// </param>
    public FixtureYahooExchangeRateChartSource(
        YahooExchangeRateOptions options,
        IReadOnlyDictionary<string, string>? fixtureBySymbol = null)
    {
        _options = options;
        _fixtureBySymbol = fixtureBySymbol ?? new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["AUDUSD=X"] = YahooFixtures.AudUsd,
        };
    }

    /// <summary>
    /// Gets the number of charts this source has served.
    /// </summary>
    /// <returns>The chart request count.</returns>
    public int GetChartCallCount { get; private set; }

    /// <inheritdoc />
    public ValueTask<YahooExchangeRateChart> GetChartAsync(YahooChartRequest request, CancellationToken cancellationToken = default)
    {
        GetChartCallCount++;

        if (_fixtureBySymbol.TryGetValue(request.Symbol, out string? fixture))
        {
            byte[] json = YahooFixtures.ReadBytes(fixture);
            return ValueTask.FromResult(YahooChartResponseParser.Parse(json, request, _options));
        }

        // Unknown symbol: behave like a pair with no published data so inverse-fallback paths can be exercised.
        return ValueTask.FromResult(
            new YahooExchangeRateChart(request.Pair, request.Symbol, request.Pair.ToIsoCode, Array.Empty<ExchangeRateObservation>()));
    }
}
