// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaRateProviderTests.HistoryAvailability.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class RbaRateProviderTests
{
    /// <summary>
    /// Verifies that with the default era catalogue the provider advertises a fixed floor at the start of the first
    /// historical workbook era, 1 January 1983.
    /// </summary>
    [TestMethod]
    public void HistoryAvailability_WhenDefaultEras_ShouldReportSinceFirstEraStart()
    {
        (RbaRateProvider provider, _) = Create();

        Assert.AreEqual(RateHistoryAvailabilityKind.Since, provider.HistoryAvailability.Kind);
        Assert.AreEqual(new DateOnly(1983, 1, 1), provider.HistoryAvailability.EarliestDate);
    }

    /// <summary>
    /// Verifies that a custom era catalogue narrows the advertised floor to its earliest era start, regardless of
    /// catalogue order.
    /// </summary>
    [TestMethod]
    public void HistoryAvailability_WhenCustomEras_ShouldReportEarliestEraStart()
    {
        RbaRateProviderOptions options = new()
        {
            Eras =
            [
                new RbaEra("2023-current", new DateOnly(2023, 1, 1), null),
                new RbaEra("2018-2022", new DateOnly(2018, 1, 1), new DateOnly(2022, 12, 31)),
            ],
            EnableDiskCache = false,
        };
        RbaRateProvider provider = new(new FixtureRbaRateTableSource(options), options);

        Assert.AreEqual(RateHistoryAvailabilityKind.Since, provider.HistoryAvailability.Kind);
        Assert.AreEqual(new DateOnly(2018, 1, 1), provider.HistoryAvailability.EarliestDate);
    }
}
