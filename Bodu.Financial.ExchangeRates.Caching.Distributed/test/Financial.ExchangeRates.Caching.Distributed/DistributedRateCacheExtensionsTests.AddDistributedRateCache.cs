// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DistributedRateCacheExtensionsTests.AddDistributedRateCache.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Bodu.Financial.ExchangeRates.Caching.Distributed;

public sealed partial class DistributedRateCacheExtensionsTests
{
    /// <summary>
    /// Verifies that the registered cache resolves as an <see cref="IExchangeRateCache" /> bound to the supplied
    /// provider when an <see cref="IDistributedCache" /> is already registered.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void AddDistributedRateCache_WhenRegistered_ShouldResolveCacheBoundToProvider()
    {
        ServiceProvider provider = BuildProvider(services =>
        {
            services.AddDistributedMemoryCache();
            services.AddFinancialService().AddDistributedRateCache("RBA");
        });

        IExchangeRateCache cache = provider.GetRequiredService<IExchangeRateCache>();

        Assert.AreEqual("RBA", cache.Provider);
    }

    /// <summary>
    /// Verifies that the registered cache is also resolvable as a keyed service under the provider name and is the same
    /// singleton as the default registration.
    /// </summary>
    [TestMethod]
    public void AddDistributedRateCache_WhenRegistered_ShouldResolveKeyedSameInstance()
    {
        ServiceProvider provider = BuildProvider(services =>
        {
            services.AddDistributedMemoryCache();
            services.AddFinancialService().AddDistributedRateCache("RBA");
        });

        IExchangeRateCache byDefault = provider.GetRequiredService<IExchangeRateCache>();
        IExchangeRateCache byKey = provider.GetRequiredKeyedService<IExchangeRateCache>("RBA");

        Assert.AreSame(byDefault, byKey);
    }

    /// <summary>
    /// Verifies that the resolved cache persists and serves rates through the registered <see cref="IDistributedCache" />.
    /// </summary>
    [TestMethod]
    public void AddDistributedRateCache_WhenResolved_ShouldPersistThroughDistributedCache()
    {
        ServiceProvider provider = BuildProvider(services =>
        {
            services.AddDistributedMemoryCache();
            services.AddFinancialService().AddDistributedRateCache("RBA");
        });
        IExchangeRateCache cache = provider.GetRequiredService<IExchangeRateCache>();
        DateTimeOffset now = DateTimeOffset.UtcNow;

        cache.Store(new CurrencyPair(CurrencyCode.AUD, CurrencyCode.USD), new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5m, now) }, TimeSpan.FromHours(24), now);

        Assert.HasCount(1, cache.GetRates(new CurrencyPair(CurrencyCode.AUD, CurrencyCode.USD), TimeSpan.FromHours(24), now));
    }

    /// <summary>
    /// Verifies that the key prefix is bound from configuration so the cache writes under the prefixed key.
    /// </summary>
    [TestMethod]
    public void AddDistributedRateCache_WhenConfigurationProvided_ShouldBindKeyPrefix()
    {
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Financial:ExchangeRateCache:Distributed:KeyPrefix"] = "fx:" })
            .Build();

        ServiceProvider provider = BuildProvider(services =>
        {
            services.AddDistributedMemoryCache();
            services.AddFinancialService().AddDistributedRateCache("RBA", config);
        });
        IDistributedCache distributedCache = provider.GetRequiredService<IDistributedCache>();
        IExchangeRateCache cache = provider.GetRequiredService<IExchangeRateCache>();
        DateTimeOffset now = DateTimeOffset.UtcNow;

        cache.Store(new CurrencyPair(CurrencyCode.AUD, CurrencyCode.USD), new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5m, now) }, TimeSpan.FromHours(24), now);

        // The configured "fx:" prefix must be applied to the underlying distributed-cache key.
        Assert.IsNotNull(distributedCache.Get("fx:RBA:AUDUSD"));
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> builder is rejected by <c>AddDistributedRateCache</c>.
    /// </summary>
    [TestMethod]
    public void AddDistributedRateCache_WhenBuilderIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = DistributedRateCacheExtensions.AddDistributedRateCache(null!, "RBA");
        });

        Assert.AreEqual("builder", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a blank provider name is rejected by <c>AddDistributedRateCache</c>.
    /// </summary>
    [TestMethod]
    public void AddDistributedRateCache_WhenProviderNameIsBlank_ShouldThrowArgumentException()
    {
        var services = new ServiceCollection();
        IFinancialServiceBuilder builder = services.AddFinancialService();

        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = builder.AddDistributedRateCache("  ");
        });

        Assert.AreEqual("providerName", ex.ParamName);
    }

    /// <summary>
    /// Verifies that invalid options — here, a white-space key prefix — fail fast through <c>ValidateOnStart</c> when
    /// the cache is resolved.
    /// </summary>
    [TestMethod]
    public void AddDistributedRateCache_WhenOptionsInvalid_ShouldThrowOnResolve()
    {
        ServiceProvider provider = BuildProvider(services =>
        {
            services.AddDistributedMemoryCache();
            services.AddFinancialService().AddDistributedRateCache("RBA", configure: o => o.KeyPrefix = "   ");
        });

        _ = Assert.ThrowsExactly<OptionsValidationException>(() =>
        {
            _ = provider.GetRequiredService<IExchangeRateCache>();
        });
    }

    /// <summary>
    /// Verifies that valid options pass <c>ValidateOnStart</c> and resolve the cache.
    /// </summary>
    [TestMethod]
    public void AddDistributedRateCache_WhenOptionsValid_ShouldResolveCache()
    {
        ServiceProvider provider = BuildProvider(services =>
        {
            services.AddDistributedMemoryCache();
            services.AddFinancialService().AddDistributedRateCache("RBA", configure: o => o.KeyPrefix = "fx:");
        });

        Assert.IsNotNull(provider.GetRequiredService<IExchangeRateCache>());
    }

    /// <summary>
    /// Verifies that, with <see cref="ExchangeRateCacheOptions.ValidateStorageOnStart" /> set over an unreachable
    /// backing store, the startup validation the host runs fails, so an unreachable distributed cache fails the host
    /// start rather than the first lookup.
    /// </summary>
    [TestMethod]
    public void AddDistributedRateCache_WhenValidateStorageOnStartAndStoreUnreachable_ShouldFailStartupValidation()
    {
        ServiceProvider provider = BuildProvider(services =>
        {
            services.AddSingleton<IDistributedCache>(new ThrowingDistributedCache());
            services.AddFinancialService().AddDistributedRateCache("RBA", configure: o => o.ValidateStorageOnStart = true);
        });

        IStartupValidator startup = provider.GetRequiredService<IStartupValidator>();

        _ = Assert.ThrowsExactly<OptionsValidationException>(startup.Validate);
    }

    /// <summary>
    /// Verifies that, with <see cref="ExchangeRateCacheOptions.ValidateStorageOnStart" /> set over a reachable backing
    /// store, the startup validation the host runs passes.
    /// </summary>
    [TestMethod]
    public void AddDistributedRateCache_WhenValidateStorageOnStartAndStoreReachable_ShouldPassStartupValidation()
    {
        ServiceProvider provider = BuildProvider(services =>
        {
            services.AddDistributedMemoryCache();
            services.AddFinancialService().AddDistributedRateCache("RBA", configure: o => o.ValidateStorageOnStart = true);
        });

        IStartupValidator startup = provider.GetRequiredService<IStartupValidator>();

        startup.Validate();
    }
}
