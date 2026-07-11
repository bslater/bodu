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
    /// Verifies that the constructor sets the IMF base address, an unbounded history depth, the CompactData and
    /// dataflow defaults, and seeds the series map with the default USD/GBP series.
    /// </summary>
    [TestMethod]
    public void Ctor_ShouldSetImfDefaults()
    {
        ImfRateProviderOptions options = new();

        Assert.AreEqual(new Uri("https://dataservices.imf.org/REST/SDMX_JSON.svc/"), options.BaseAddress);
        Assert.AreEqual(RateHistoryAvailability.Unbounded, options.HistoryAvailability);
        Assert.AreEqual("CompactData", options.CompactDataPath);
        Assert.AreEqual("IFS", options.Dataflow);
        Assert.IsTrue(options.SeriesMap.ContainsKey("USD/GBP"));
        Assert.AreEqual("M.GB.ENDE_XDC_USD_RATE", options.SeriesMap["USD/GBP"]);
    }
}
