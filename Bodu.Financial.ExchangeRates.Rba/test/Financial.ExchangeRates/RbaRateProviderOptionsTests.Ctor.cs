// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaRateProviderOptionsTests.Ctor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class RbaRateProviderOptionsTests
{
    /// <summary>
    /// Verifies that the constructor gives the shared members the RBA's defaults: the historical statistics base
    /// address, the Bodu user agent and the <c>SDR</c> to <c>XDR</c> alias, with the base's timeout, response-size cap,
    /// seven-day look-back, refresh interval, and unbounded history availability, which leaves the advertised depth to
    /// the era catalog; the on-disk cache stays off.
    /// </summary>
    [TestMethod]
    public void Ctor_ShouldSetTheRbaDefaultsOnTheSharedMembers()
    {
        RbaRateProviderOptions rba = new();
        WebRateProviderOptions options = SharedOptions.Of(rba);

        Assert.AreEqual(new Uri("https://www.rba.gov.au/statistics/tables/xls-hist/"), options.BaseAddress);
        Assert.AreEqual("Bodu.Financial.ExchangeRates.Rba", options.UserAgent);
        Assert.AreEqual(TimeSpan.FromSeconds(30), options.HttpTimeout);
        Assert.AreEqual(RateProviderHttpClientFactory.DefaultMaxResponseContentBufferSize, options.MaxResponseContentBufferSize);
        Assert.AreEqual(TimeSpan.FromDays(7), options.DefaultLookback);
        Assert.AreEqual(TimeSpan.FromHours(12), options.RefreshInterval);
        Assert.AreEqual(RateHistoryAvailability.Unbounded, options.HistoryAvailability);
        Assert.IsFalse(options.AllowSynchronousNetworkAccess);
        Assert.HasCount(1, options.CurrencyAliases);
        Assert.AreEqual("XDR", options.CurrencyAliases["SDR"]);
        Assert.IsFalse(rba.EnableDiskCache);
    }
}
