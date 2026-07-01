// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WebExchangeRateProviderTestDoubles.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

/// <summary>
/// A minimal single-base <see cref="WebExchangeRateProvider" /> that loads its whole catalogue in one shot, modelling a
/// bulk feed (such as RBA, ECB, or Bank of England) so the base warm-up surface can be exercised directly against a
/// provider that is not built on <see cref="PairWebExchangeRateProvider{TSeries}" />.
/// </summary>
internal sealed class TestBulkWebExchangeRateProvider
    : WebExchangeRateProvider
{
    /// <summary>The ISO code of the currency this feed quotes against; a request must involve it.</summary>
    public const string BaseIsoCode = "AUD";

    /// <summary>The catalogue this feed serves, loaded as a single unit.</summary>
    private readonly IReadOnlyList<ExchangeRate> _catalogue;

    /// <summary>Tracks whether the catalogue has been loaded.</summary>
    private bool _loaded;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestBulkWebExchangeRateProvider" /> class.
    /// </summary>
    /// <param name="catalogue">The observations the feed serves once loaded.</param>
    public TestBulkWebExchangeRateProvider(params ExchangeRate[] catalogue)
        : base(ownedHttpClient: null, timeProvider: null)
    {
        _catalogue = catalogue;
    }

    /// <summary>
    /// Gets the number of times the catalogue has been fetched.
    /// </summary>
    /// <value>The fetch count.</value>
    public int LoadCount { get; private set; }

    /// <inheritdoc />
    protected override string ProviderId => "TESTBULK";

    /// <inheritdoc />
    protected override bool AllowSynchronousNetworkAccess => false;

    /// <inheritdoc />
    protected override TimeSpan DefaultLookback => TimeSpan.Zero;

    /// <inheritdoc />
    protected override ValueTask EnsureLoadedAsync(ExchangeRatePair pair, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken)
    {
        lock (SyncRoot)
        {
            if (!_loaded)
            {
                LoadCount++;
                AddObservations(_catalogue);
                _loaded = true;
                RebuildSnapshot();
            }
        }

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    protected override bool IsLoaded(ExchangeRatePair pair, DateOnly startDate, DateOnly endDate)
    {
        lock (SyncRoot)
        {
            return _loaded;
        }
    }

    /// <inheritdoc />
    protected override void ValidateRangeRequest(string fromIsoCode, string toIsoCode, DateOnly startDate, DateOnly endDate)
    {
        if (!string.Equals(fromIsoCode, BaseIsoCode, StringComparison.Ordinal) &&
            !string.Equals(toIsoCode, BaseIsoCode, StringComparison.Ordinal))
        {
            throw new ExchangeRateSeriesNotFoundException($"neither {fromIsoCode} nor {toIsoCode} is {BaseIsoCode}");
        }
    }
}
