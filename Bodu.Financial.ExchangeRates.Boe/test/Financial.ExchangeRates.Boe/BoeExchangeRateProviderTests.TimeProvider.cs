// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoeExchangeRateProviderTests.TimeProvider.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Boe;

public partial class BoeExchangeRateProviderTests
{
    /// <summary>
    /// Verifies that the undated lookup resolves the current instant from the injected <see cref="TimeProvider" />, so
    /// the most-recent rate is taken as of the provider's clock rather than the wall clock.
    /// </summary>
    [TestMethod]
    public async Task GetRate_WhenUndatedAndTimeProviderInjected_ShouldResolveAgainstInjectedClock()
    {
        BoeExchangeRateOptions options = new() { AllowSynchronousNetworkAccess = false, EnableDiskCache = false };
        FixtureBoeExchangeRateTableSource source = new(options);
        MutableTimeProvider timeProvider = new(new DateTimeOffset(2023, 1, 3, 0, 0, 0, TimeSpan.Zero));
        BoeExchangeRateProvider provider = new(source, options, logger: null, timeProvider);
        await provider.LoadRangeAsync(new DateOnly(2023, 1, 1), new DateOnly(2023, 12, 31));

        var rate = provider.GetRate("GBP", "USD").Rate.Rate;

        Assert.AreEqual(1.2065m, rate);
    }

    /// <summary>
    /// Verifies that a served rate is stamped with the load instant captured from the injected
    /// <see cref="TimeProvider" /> at the moment the range was downloaded.
    /// </summary>
    [TestMethod]
    public async Task GetRate_WhenRangeLoaded_ShouldStampServedRateWithFetchInstant()
    {
        DateTimeOffset fetchedAt = new(2023, 1, 3, 11, 0, 0, TimeSpan.Zero);
        BoeExchangeRateOptions options = new() { AllowSynchronousNetworkAccess = false, EnableDiskCache = false };
        FixtureBoeExchangeRateTableSource source = new(options);
        MutableTimeProvider timeProvider = new(fetchedAt);
        BoeExchangeRateProvider provider = new(source, options, logger: null, timeProvider);
        await provider.LoadRangeAsync(new DateOnly(2023, 1, 1), new DateOnly(2023, 12, 31));

        ExchangeRateLookupResult result = provider.GetRate("GBP", "USD", new DateOnly(2023, 1, 3));

        Assert.AreEqual(fetchedAt, result.Rate.FetchedAtUtc);
    }
}
