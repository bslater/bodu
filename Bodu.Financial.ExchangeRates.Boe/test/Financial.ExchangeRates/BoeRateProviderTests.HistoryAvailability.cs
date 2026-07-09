// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoeRateProviderTests.HistoryAvailability.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class BoeRateProviderTests
{
    /// <summary>
    /// Verifies that by default the provider advertises a fixed floor at the inception of the Bank of England's daily
    /// spot series, 2 January 1975.
    /// </summary>
    [TestMethod]
    public void HistoryAvailability_WhenDefaultOptions_ShouldReportSinceDailySpotSeriesEpoch()
    {
        (BoeRateProvider provider, _) = Create();

        Assert.AreEqual(RateHistoryAvailabilityKind.Since, provider.HistoryAvailability.Kind);
        Assert.AreEqual(new DateOnly(1975, 1, 2), provider.HistoryAvailability.EarliestDate);
    }

    /// <summary>
    /// Verifies that the provider forwards a customized options availability, so a catalogue restricted to
    /// later-inception series can narrow the advertised floor.
    /// </summary>
    [TestMethod]
    public void HistoryAvailability_WhenCustomizedInOptions_ShouldForwardOptionsValue()
    {
        BoeRateProviderOptions options = new()
        {
            HistoryAvailability = RateHistoryAvailability.Since(new DateOnly(1999, 1, 4)),
            EnableDiskCache = false,
        };
        BoeRateProvider provider = new(new FixtureBoeRateTableSource(options), options);

        Assert.AreEqual(new DateOnly(1999, 1, 4), provider.HistoryAvailability.EarliestDate);
    }
}
