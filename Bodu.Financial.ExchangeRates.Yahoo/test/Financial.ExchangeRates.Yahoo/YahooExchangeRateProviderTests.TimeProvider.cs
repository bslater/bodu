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

    /// <summary>
    /// Verifies that a served rate is stamped with the load instant captured from the injected
    /// <see cref="TimeProvider" /> at the moment the chart was fetched.
    /// </summary>
    [TestMethod]
    public async Task GetRate_WhenPairLoaded_ShouldStampServedRateWithFetchInstant()
    {
        DateTimeOffset fetchedAt = new(2023, 1, 6, 12, 30, 0, TimeSpan.Zero);
        YahooExchangeRateOptions options = new() { AllowSynchronousNetworkAccess = false };
        FixtureYahooExchangeRateChartSource source = new(options);
        MutableTimeProvider timeProvider = new(fetchedAt);
        YahooExchangeRateProvider provider = new(source, options, logger: null, timeProvider);
        await provider.LoadPairAsync("AUD", "USD", new DateOnly(2023, 1, 1), new DateOnly(2023, 1, 31));

        ExchangeRateLookupResult result = provider.GetRate("AUD", "USD", new DateOnly(2023, 1, 3));

        Assert.AreEqual(fetchedAt, result.Rate.FetchedAtUtc);
    }
}
