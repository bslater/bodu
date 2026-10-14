// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EcbRateProviderOptionsTests.Ctor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class EcbRateProviderOptionsTests
{
    /// <summary>
    /// Verifies that the constructor gives the shared members the ECB's defaults: the <c>eurofxref</c> base address and
    /// the Bodu user agent, with the base's timeout, response-size cap, seven-day look-back, refresh interval, empty
    /// alias map, and unbounded history availability, which leaves the advertised depth to the feed catalogue.
    /// </summary>
    [TestMethod]
    public void Ctor_ShouldSetTheEcbDefaultsOnTheSharedMembers()
    {
        WebRateProviderOptions options = SharedOptions.Of(new EcbRateProviderOptions());

        Assert.AreEqual(new Uri("https://www.ecb.europa.eu/stats/eurofxref/"), options.BaseAddress);
        Assert.AreEqual("Bodu.Financial.ExchangeRates.Ecb", options.UserAgent);
        Assert.AreEqual(TimeSpan.FromSeconds(30), options.HttpTimeout);
        Assert.AreEqual(RateProviderHttpClientFactory.DefaultMaxResponseContentBufferSize, options.MaxResponseContentBufferSize);
        Assert.AreEqual(TimeSpan.FromDays(7), options.DefaultLookback);
        Assert.AreEqual(TimeSpan.FromHours(12), options.RefreshInterval);
        Assert.AreEqual(RateHistoryAvailability.Unbounded, options.HistoryAvailability);
        Assert.IsFalse(options.AllowSynchronousNetworkAccess);
        Assert.IsEmpty(options.CurrencyAliases);
    }
}
