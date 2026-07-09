// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YahooRateProviderTests.TimeProvider.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class YahooRateProviderTests
{
    /// <summary>
    /// Verifies that the undated lookup resolves the current instant from the injected <see cref="TimeProvider" />, so
    /// the most-recent rate is taken as of the provider's clock rather than the wall clock.
    /// </summary>
    [TestMethod]
    public async Task GetRate_WhenUndatedAndTimeProviderInjected_ShouldResolveAgainstInjectedClock()
    {
        YahooRateProviderOptions options = new() { AllowSynchronousNetworkAccess = false };
        FixtureYahooRateSource source = new(options);
        MutableTimeProvider timeProvider = new(new DateTimeOffset(2023, 1, 6, 0, 0, 0, TimeSpan.Zero));
        YahooRateProvider provider = new(source, options, logger: null, timeProvider);
        await provider.LoadPairAsync("AUD", "USD", new DateOnly(2023, 1, 1), new DateOnly(2023, 1, 31));

        decimal rate = provider.GetRate("AUD", "USD").Rate.Rate;

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
        YahooRateProviderOptions options = new() { AllowSynchronousNetworkAccess = false };
        FixtureYahooRateSource source = new(options);
        MutableTimeProvider timeProvider = new(fetchedAt);
        YahooRateProvider provider = new(source, options, logger: null, timeProvider);
        await provider.LoadPairAsync("AUD", "USD", new DateOnly(2023, 1, 1), new DateOnly(2023, 1, 31));

        RateLookupResult result = provider.GetRate("AUD", "USD", new DateOnly(2023, 1, 3));

        Assert.AreEqual(fetchedAt, result.Rate.FetchedAtUtc);
    }
}
