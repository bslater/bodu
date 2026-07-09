// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaRateProviderTests.TimeProvider.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class RbaRateProviderTests
{
    /// <summary>
    /// Verifies that the undated lookup resolves the current instant from the injected <see cref="TimeProvider" />, so
    /// the most-recent rate is taken as of the provider's clock rather than the wall clock.
    /// </summary>
    [TestMethod]
    public async Task GetRate_WhenUndatedAndTimeProviderInjected_ShouldResolveAgainstInjectedClock()
    {
        RbaRateProviderOptions options = new() { AllowSynchronousNetworkAccess = false, EnableDiskCache = false };
        FixtureRbaRateTableSource source = new(options);
        MutableTimeProvider timeProvider = new(new DateTimeOffset(2023, 1, 3, 0, 0, 0, TimeSpan.Zero));
        RbaRateProvider provider = new(source, options, logger: null, timeProvider);
        await provider.LoadRangeAsync(new DateOnly(2023, 1, 1), new DateOnly(2026, 12, 31));

        decimal rate = provider.GetRate("AUD", "USD").Rate.Rate;

        Assert.AreEqual(0.6828m, rate);
    }

    /// <summary>
    /// Verifies that a served rate is stamped with the load instant captured from the injected
    /// <see cref="TimeProvider" /> at the moment the era was downloaded.
    /// </summary>
    [TestMethod]
    public async Task GetRate_WhenEraLoaded_ShouldStampServedRateWithFetchInstant()
    {
        DateTimeOffset fetchedAt = new(2023, 1, 3, 10, 0, 0, TimeSpan.Zero);
        RbaRateProviderOptions options = new() { AllowSynchronousNetworkAccess = false, EnableDiskCache = false };
        FixtureRbaRateTableSource source = new(options);
        MutableTimeProvider timeProvider = new(fetchedAt);
        RbaRateProvider provider = new(source, options, logger: null, timeProvider);
        await provider.LoadRangeAsync(new DateOnly(2023, 1, 1), new DateOnly(2026, 12, 31));

        RateLookupResult result = provider.GetRate("AUD", "USD", new DateOnly(2023, 1, 3));

        Assert.AreEqual(fetchedAt, result.Rate.FetchedAtUtc);
    }
}
