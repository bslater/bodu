// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateCacheWarmupServiceTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Bodu.Globalization.Calendar.Caching;

/// <summary>
/// Verifies the startup warm-up hosted service end to end over a real service provider: the caching decorator is
/// warmed over the configured span, a non-caching registration no-ops, failures never escape the run, and
/// cancellation aborts it quietly.
/// </summary>
[TestClass]
public sealed class NotableDateCacheWarmupServiceTests
{
    /// <summary>
    /// Verifies that starting the host warms the configured territories and span, so later in-span resolves are cache
    /// hits with no further computations.
    /// </summary>
    [TestMethod]
    public async Task StartAsync_WhenCachingServiceRegistered_ShouldWarmConfiguredSpan()
    {
        var inner = new CountingNotableDateService();
        ServiceProvider provider = BuildProvider(inner, warmup =>
        {
            warmup.Territories.Add("US");
            warmup.FirstYear = 2026;
            warmup.LastYear = 2027;
        });

        await RunWarmupAsync(provider);
        int computesAfterWarm = inner.ResolveCount;

        INotableDateService service = provider.GetRequiredService<INotableDateService>();
        _ = service.Resolve(new DateOnly(2026, 6, 15), "US");
        _ = service.Resolve(new DateOnly(2027, 1, 1), "US");

        Assert.AreEqual(2, computesAfterWarm, "the warm-up must compute each cold year once");
        Assert.AreEqual(computesAfterWarm, inner.ResolveCount, "later in-span resolves must serve from the cache");
    }

    /// <summary>
    /// Verifies that when the registered notable-date service is not the caching decorator the run logs and no-ops
    /// rather than faulting the host.
    /// </summary>
    [TestMethod]
    public async Task StartAsync_WhenServiceNotCachingDecorator_ShouldNoOpQuietly()
    {
        var inner = new CountingNotableDateService();
        var services = new ServiceCollection();
        services.AddSingleton<INotableDateService>(inner);
        services.AddNotableDateCacheWarmup(configure: warmup => warmup.Territories.Add("US"));
        await using ServiceProvider provider = services.BuildServiceProvider();

        await RunWarmupAsync(provider);

        Assert.AreEqual(0, inner.ResolveCount, "a non-caching registration must not be warmed");
    }

    /// <summary>
    /// Verifies that a territory whose computation fails is skipped while the remaining territories still warm, and
    /// the run completes cleanly.
    /// </summary>
    [TestMethod]
    public async Task StartAsync_WhenOneTerritoryFails_ShouldWarmRemainingTerritories()
    {
        var gated = new GatedNotableDateService(throwOnFirstCall: true);
        gated.Open();
        ServiceProvider provider = BuildProvider(gated, warmup =>
        {
            warmup.Territories.Add("XX");
            warmup.Territories.Add("US");
            warmup.FirstYear = 2026;
            warmup.LastYear = 2026;
        });

        await RunWarmupAsync(provider);

        Assert.AreEqual(2, gated.ResolveCount, "the healthy territory must still have been computed after the failure");
    }

    /// <summary>
    /// Verifies that a token cancelled before the run starts aborts the warm-up quietly without computing anything.
    /// </summary>
    [TestMethod]
    public async Task StartAsync_WhenTokenAlreadyCancelled_ShouldAbortQuietly()
    {
        var inner = new CountingNotableDateService();
        ServiceProvider provider = BuildProvider(inner, warmup => warmup.Territories.Add("US"));
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        IHostedService hosted = provider.GetRequiredService<IHostedService>();
        await hosted.StartAsync(cts.Token);
        if (hosted is BackgroundService background && background.ExecuteTask is { } run)
            await run;

        Assert.AreEqual(0, inner.ResolveCount, "a pre-cancelled run must not compute anything");
    }

    /// <summary>
    /// Builds a service provider with the caching decorator over the supplied inner service and the warm-up
    /// registered.
    /// </summary>
    /// <param name="inner">The inner service instance the caching decorator wraps.</param>
    /// <param name="configureWarmup">The warm-up options callback.</param>
    /// <returns>The built service provider.</returns>
    private static ServiceProvider BuildProvider(INotableDateService inner, Action<NotableDateCacheWarmupOptions> configureWarmup)
    {
        // Register the inner as the resolvable service, decorate it with the in-memory caching layer, then register
        // the warm-up over the decorated registration.
        var services = new ServiceCollection();
        services.AddSingleton<INotableDateService>(inner);
        services.AddCachedNotableDateService(cacheFactory: static _ => new InMemoryNotableDateCache());
        services.AddNotableDateCacheWarmup(configure: configureWarmup);
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
}
