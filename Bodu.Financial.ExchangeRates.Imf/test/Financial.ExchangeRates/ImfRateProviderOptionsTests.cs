// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ImfRateProviderOptionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies the configuration surface and validation of <see cref="ImfRateProviderOptions" />.
/// </summary>
[TestClass]
public partial class ImfRateProviderOptionsTests
{
    /// <summary>
    /// Verifies that the constructor sets the IMF SDMX Data API base address, an unbounded history depth, the data-path,
    /// dataflow, and version defaults, and seeds the series map with the default daily USD/GBP series.
    /// </summary>
    [TestMethod]
    public void Ctor_ShouldSetImfDefaults()
    {
        ImfRateProviderOptions options = new();

        Assert.AreEqual(new Uri("https://api.imf.org/external/sdmx/3.0/"), options.BaseAddress);
        Assert.AreEqual(RateHistoryAvailability.Unbounded, options.HistoryAvailability);
        Assert.AreEqual("data/dataflow", options.DataPath);
        Assert.AreEqual("IMF.STA/ER", options.Dataflow);
        Assert.AreEqual("+", options.DataVersion);
        Assert.IsTrue(options.SeriesMap.ContainsKey("USD/GBP"));
        Assert.AreEqual("D.GB.ENDE_XDC_USD_RATE", options.SeriesMap["USD/GBP"]);
    }
}
