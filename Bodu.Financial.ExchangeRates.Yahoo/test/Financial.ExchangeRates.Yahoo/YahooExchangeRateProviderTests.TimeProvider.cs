// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YahooExchangeRateProviderTests.TimeProvider.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Yahoo;

public partial class YahooExchangeRateProviderTests
{
    /// <summary>
    /// Verifies that the undated lookup resolves the current instant from the injected <see cref="TimeProvider" />, so
    /// the most-recent rate is taken as of the provider's clock rather than the wall clock.
    /// </summary>
    [TestMethod]
    public async Task GetRate_WhenUndatedAndTimeProviderInjected_ShouldResolveAgainstInjectedClock()
    {
        YahooExchangeRateOptions options = new() { AllowSynchronousNetworkAccess = false };
        FixtureYahooExchangeRateChartSource source = new(options);
        MutableTimeProvider timeProvider = new(new DateTimeOffset(2023, 1, 6, 0, 0, 0, TimeSpan.Zero));
        YahooExchangeRateProvider provider = new(source, options, logger: null, timeProvider);
        await provider.LoadPairAsync("AUD", "USD", new DateOnly(2023, 1, 1), new DateOnly(2023, 1, 31));

        var rate = provider.GetRate("AUD", "USD");

        Assert.AreEqual(0.6855m, rate);
    }
}
