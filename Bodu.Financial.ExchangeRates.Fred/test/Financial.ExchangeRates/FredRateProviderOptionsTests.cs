// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FredRateProviderOptionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies the configuration surface and validation of <see cref="FredRateProviderOptions" />.
/// </summary>
[TestClass]
public partial class FredRateProviderOptionsTests
{
    /// <summary>
    /// Verifies that the constructor sets the FRED base address, an unbounded history window, and the observations path.
    /// </summary>
    [TestMethod]
    public void Ctor_ShouldSetFredDefaults()
    {
        FredRateProviderOptions options = new();

        Assert.AreEqual(new Uri("https://api.stlouisfed.org/fred/"), options.BaseAddress);
        Assert.AreEqual(RateHistoryAvailability.Unbounded, options.HistoryAvailability);
        Assert.AreEqual("series/observations", options.ObservationsPath);
    }

    /// <summary>
    /// Verifies that the constructor seeds the series map with the built-in EUR/USD mapping.
    /// </summary>
    [TestMethod]
    public void Ctor_ShouldSeedSeriesMap()
    {
        FredRateProviderOptions options = new();

        Assert.IsNotNull(options.SeriesMap);
        Assert.IsTrue(options.SeriesMap.ContainsKey("EUR/USD"));
        Assert.AreEqual("DEXUSEU", options.SeriesMap["EUR/USD"]);
    }
}
