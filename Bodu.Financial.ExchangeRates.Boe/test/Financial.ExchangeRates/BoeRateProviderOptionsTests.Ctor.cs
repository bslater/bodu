// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoeRateProviderOptionsTests.Ctor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class BoeRateProviderOptionsTests
{
    /// <summary>
    /// Verifies that the constructor gives the shared members the Bank of England's defaults: the IADB base address,
    /// the Bodu user agent, a ten-day look-back and a history floor at 2 January 1975, with the base's timeout,
    /// response-size cap, refresh interval and empty alias map.
    /// </summary>
    [TestMethod]
    public void Ctor_ShouldSetTheBankOfEnglandDefaultsOnTheSharedMembers()
    {
        WebRateProviderOptions options = SharedOptions.Of(new BoeRateProviderOptions());

        Assert.AreEqual(new Uri("https://www.bankofengland.co.uk/boeapps/database/"), options.BaseAddress);
        Assert.AreEqual("Bodu.Financial.ExchangeRates.Boe", options.UserAgent);
        Assert.AreEqual(TimeSpan.FromSeconds(30), options.HttpTimeout);
        Assert.AreEqual(RateProviderHttpClientFactory.DefaultMaxResponseContentBufferSize, options.MaxResponseContentBufferSize);
        Assert.AreEqual(TimeSpan.FromDays(10), options.DefaultLookback);
        Assert.AreEqual(TimeSpan.FromHours(12), options.RefreshInterval);
        Assert.AreEqual(RateHistoryAvailability.Since(new DateOnly(1975, 1, 2)), options.HistoryAvailability);
        Assert.IsFalse(options.AllowSynchronousNetworkAccess);
        Assert.IsEmpty(options.CurrencyAliases);
    }
}
