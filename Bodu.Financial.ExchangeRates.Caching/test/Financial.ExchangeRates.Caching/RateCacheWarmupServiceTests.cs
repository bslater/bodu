// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateCacheWarmupServiceTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Verifies the startup warm-up hosted service end to end over a real service provider: the registered caching
/// providers are warmed over the configured window, keyed aggregation children are warmed by name, failures never
/// escape the run, and cancellation aborts it quietly.
/// </summary>
[TestClass]
public sealed class RateCacheWarmupServiceTests
{
    /// <summary>The fixed window warmed by the tests.</summary>
    private static readonly DateOnly WindowStart = new(2023, 1, 3);

    /// <summary>The fixed window end warmed by the tests.</summary>
    private static readonly DateOnly WindowEnd = new(2023, 1, 4);

    /// <summary>
    /// Verifies that starting the host warms every registered caching provider, so a later range lookup of the window
    /// is a cache hit with no further inner fetch.
    /// </summary>
    [TestMethod]
    public async Task StartAsync_WhenProvidersRegistered_ShouldWarmConfiguredWindow()
    {
        var inner = new CountingDatedRateProvider(new[]
        {
            new ExchangeRate(CurrencyCode.AUD, CurrencyCode.USD, WindowStart, 0.5m, "Warm"),
        });
        ServiceProvider provider = BuildProvider(inner, warmup =>
        {
            warmup.Pairs.Add("AUD/USD");
            warmup.StartDate = WindowStart;
            warmup.EndDate = WindowEnd;
        });

        await RunWarmupAsync(provider);

        Assert.AreEqual(1, inner.GetRatesAsyncCallCount, "the warm-up must fetch the window once");
        RateRangeResult hit = provider.GetRequiredService<CachingRateProvider>().GetRates("AUD", "USD", WindowStart, WindowEnd);
        Assert.AreEqual(1, inner.GetRatesAsyncCallCount, "the warmed window must serve from the cache");
        Assert.HasCount(1, hit.Rates);
    }

    /// <summary>
    /// Verifies that a keyed aggregation child named in the options is warmed alongside the direct registrations, and
    /// an unknown name is skipped without failing the run.
    /// </summary>
    [TestMethod]
    public async Task StartAsync_WhenKeyedProviderNamed_ShouldWarmKeyedChild()
    {
        var inner = new CountingDatedRateProvider(new[]
        {
            new ExchangeRate(CurrencyCode.AUD, CurrencyCode.USD, WindowStart, 0.5m, "RBA"),
        });
        var services = new ServiceCollection();
        services.AddFinancialService()
            .AddAggregatedRateProvider(
                aggregate => aggregate.AddCachedChild("RBA", _ => inner),
                configureCache: options => options.CacheDirectory = NewDirectory())
            .AddRateCacheWarmup(configure: warmup =>
            {
                warmup.Pairs.Add("AUD/USD");
                warmup.StartDate = WindowStart;
                warmup.EndDate = WindowEnd;
                warmup.Providers.Add("RBA");
                warmup.Providers.Add("Unknown");
            });
        await using ServiceProvider provider = services.BuildServiceProvider();

        await RunWarmupAsync(provider);

        Assert.AreEqual(1, inner.GetRatesAsyncCallCount, "the keyed child must be warmed once; the unknown name is skipped");
    }

    /// <summary>
    /// Verifies that a pair whose currency code is unknown to the registry is swallowed by the warm-up, which
    /// completes cleanly and still warms nothing rather than faulting the host.
    /// </summary>
    [TestMethod]
    public async Task StartAsync_WhenPairUnresolvable_ShouldCompleteQuietly()
    {
        var inner = new CountingDatedRateProvider(Array.Empty<ExchangeRate>());
        ServiceProvider provider = BuildProvider(inner, warmup =>
        {
            warmup.Pairs.Add("QQQ/QQZ");
            warmup.StartDate = WindowStart;
            warmup.EndDate = WindowEnd;
        });

        await RunWarmupAsync(provider);
    }

    /// <summary>
    /// Verifies that a token cancelled before the run starts aborts the warm-up quietly without touching the inner
    /// provider.
    /// </summary>
    [TestMethod]
    public async Task StartAsync_WhenTokenAlreadyCancelled_ShouldAbortQuietly()
    {
        var inner = new CountingDatedRateProvider(new[]
        {
            new ExchangeRate(CurrencyCode.AUD, CurrencyCode.USD, WindowStart, 0.5m, "Warm"),
        });
        ServiceProvider provider = BuildProvider(inner, warmup =>
        {
            warmup.Pairs.Add("AUD/USD");
            warmup.StartDate = WindowStart;
            warmup.EndDate = WindowEnd;
        });
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        IHostedService hosted = provider.GetRequiredService<IHostedService>();
        await hosted.StartAsync(cts.Token);
        if (hosted is BackgroundService background && background.ExecuteTask is { } run)
            await run;

        Assert.AreEqual(0, inner.GetRatesAsyncCallCount, "a pre-cancelled run must not fetch anything");
    }

    /// <summary>
    /// Builds a service provider with one cached provider over the supplied inner source and the warm-up registered.
    /// </summary>
    /// <param name="inner">The counting inner source instance the cached provider wraps.</param>
    /// <param name="configureWarmup">The warm-up options callback.</param>
    /// <returns>The built service provider.</returns>
    private static ServiceProvider BuildProvider(CountingDatedRateProvider inner, Action<RateCacheWarmupOptions> configureWarmup)
    {
        var services = new ServiceCollection();
        services.AddSingleton(inner);
        services.AddFinancialService()
            .AddCachedRateProvider<CountingDatedRateProvider>("Warm", configure: options => options.CacheDirectory = NewDirectory())
            .AddRateCacheWarmup(configure: configureWarmup);
        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Starts the registered hosted warm-up and awaits its background run to completion.
    /// </summary>
    /// <param name="provider">The built service provider.</param>
    /// <returns>A task that completes when the warm-up run has finished.</returns>
    private static async Task RunWarmupAsync(ServiceProvider provider)
    {
        IHostedService hosted = provider.GetRequiredService<IHostedService>();
        await hosted.StartAsync(CancellationToken.None);
        if (hosted is BackgroundService background && background.ExecuteTask is { } run)
            await run;
    }

    /// <summary>
    /// Builds a unique temporary cache directory for one test.
    /// </summary>
    /// <returns>The directory path.</returns>
    private static string NewDirectory() =>
        Path.Combine(Path.GetTempPath(), "bodu-warmup-tests", Guid.NewGuid().ToString("N"));
}
