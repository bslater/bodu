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
    /// Verifies that the constructor sets the IMF report base address, an unbounded history depth, the report-path and
    /// report-type defaults, and seeds the currency-name map.
    /// </summary>
    [TestMethod]
    public void Ctor_ShouldSetImfDefaults()
    {
        ImfRateProviderOptions options = new();

        Assert.AreEqual(new Uri("https://www.imf.org/external/np/fin/data/"), options.BaseAddress);
        Assert.AreEqual(RateHistoryAvailability.Unbounded, options.HistoryAvailability);
        Assert.AreEqual("rms_mth.aspx", options.ReportPath);
        Assert.AreEqual("REP", options.ReportType);
        Assert.IsTrue(options.EnableDiskCache);
        Assert.AreEqual(TimeSpan.FromHours(12), options.RefreshInterval);
        Assert.IsTrue(options.CurrencyNames.ContainsKey("Japanese yen"));
        Assert.AreEqual("JPY", options.CurrencyNames["Japanese yen"]);
    }
}
