// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateHostRateProviderOptionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies the configuration surface and validation of <see cref="ExchangeRateHostRateProviderOptions" />.
/// </summary>
[TestClass]
public partial class ExchangeRateHostRateProviderOptionsTests
{
    /// <summary>
    /// Verifies that the constructor sets the exchangerate.host base address and the January 1999 history floor.
    /// </summary>
    [TestMethod]
    public void Ctor_ShouldSetExchangeRateHostDefaults()
    {
        ExchangeRateHostRateProviderOptions options = new();

        Assert.AreEqual(new Uri("https://api.exchangerate.host/"), options.BaseAddress);
        Assert.AreEqual(RateHistoryAvailability.Since(new DateOnly(1999, 1, 1)), options.HistoryAvailability);
    }

    /// <summary>
    /// Verifies that the constructor sets the default time-series and historical endpoint paths.
    /// </summary>
    [TestMethod]
    public void Ctor_ShouldSetEndpointPathDefaults()
    {
        ExchangeRateHostRateProviderOptions options = new();

        Assert.AreEqual("timeseries", options.TimeSeriesPath);
        Assert.AreEqual("historical", options.HistoricalPath);
    }
}
