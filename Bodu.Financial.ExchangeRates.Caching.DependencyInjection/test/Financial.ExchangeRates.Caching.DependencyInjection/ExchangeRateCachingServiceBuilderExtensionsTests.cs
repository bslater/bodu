// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateCachingServiceBuilderExtensionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.DependencyInjection;
using Bodu.Financial.ExchangeRates.Caching;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bodu.Financial.ExchangeRates.Caching.DependencyInjection;

/// <summary>
/// Verifies the dependency-injection wiring of the caching exchange-rate provider.
/// </summary>
[TestClass]
public sealed class ExchangeRateCachingServiceBuilderExtensionsTests
{
    /// <summary>
    /// Verifies that the registered <see cref="IDatedExchangeRateProvider" /> is a caching provider.
    /// </summary>
    [TestMethod]
    public void AddCachedExchangeRateProvider_WhenRegistered_ShouldResolveCachingProvider()
    {
        ServiceProvider provider = BuildProvider(builder =>
            builder.AddCachedExchangeRateProvider(_ => new[] { Source("Test", FixedProvider(new DateOnly(2023, 1, 3), 0.5m)) }));

        Assert.IsInstanceOfType<CachingDatedExchangeRateProvider>(provider.GetRequiredService<IDatedExchangeRateProvider>());
    }

    /// <summary>
    /// Verifies that the registered caching provider wraps the supplied source and serves its rate.
    /// </summary>
    [TestMethod]
    public void AddCachedExchangeRateProvider_WhenResolved_ShouldServeWrappedSource()
    {
        ServiceProvider provider = BuildProvider(builder =>
            builder.AddCachedExchangeRateProvider(_ => new[] { Source("Test", FixedProvider(new DateOnly(2023, 1, 3), 0.5m)) }));

        IDatedExchangeRateProvider resolved = provider.GetRequiredService<IDatedExchangeRateProvider>();
        ExchangeRateLookupResult result = resolved.GetRate("AUD", "USD", new DateOnly(2023, 1, 3), ExchangeRateLookupOptions.Exact);

        Assert.AreEqual(0.5m, result.Rate.Rate);
    }

    /// <summary>
    /// Verifies that the cache is registered as a singleton <see cref="TomlFileSystemExchangeRateCache" />.
    /// </summary>
    [TestMethod]
    public void AddCachedExchangeRateProvider_WhenRegistered_ShouldRegisterTomlCacheSingleton()
    {
        ServiceProvider provider = BuildProvider(builder =>
            builder.AddCachedExchangeRateProvider(_ => new[] { Source("Test", FixedProvider(new DateOnly(2023, 1, 3), 0.5m)) }));

        Assert.IsInstanceOfType<TomlFileSystemExchangeRateCache>(provider.GetRequiredService<IExchangeRateCache>());
    }

    /// <summary>
    /// Verifies that the cache directory is bound from the configuration section.
    /// </summary>
    [TestMethod]
    public void AddCachedExchangeRateProvider_WhenConfigurationProvided_ShouldBindCacheDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), "bodu-di-bound");
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Financial:ExchangeRateCache:CacheDirectory"] = directory })
            .Build();

        ServiceProvider provider = BuildProvider(builder =>
            builder.AddCachedExchangeRateProvider(_ => new[] { Source("Test", FixedProvider(new DateOnly(2023, 1, 3), 0.5m)) }, config));

        var cache = (TomlFileSystemExchangeRateCache)provider.GetRequiredService<IExchangeRateCache>();
        Assert.AreEqual(directory, cache.Directory);
    }

    /// <summary>
    /// Verifies that the <c>configure</c> delegate is applied to the caching options.
    /// </summary>
    [TestMethod]
    public void AddCachedExchangeRateProvider_WhenConfigured_ShouldApplyOptions()
    {
        var directory = Path.Combine(Path.GetTempPath(), "bodu-di-configure");
        ServiceProvider provider = BuildProvider(builder =>
            builder.AddCachedExchangeRateProvider(
                _ => new[] { Source("Test", FixedProvider(new DateOnly(2023, 1, 3), 0.5m)) },
                configure: o => o.CacheDirectory = directory));

        var cache = (TomlFileSystemExchangeRateCache)provider.GetRequiredService<IExchangeRateCache>();
        Assert.AreEqual(directory, cache.Directory);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> builder is rejected.
    /// </summary>
    [TestMethod]
    public void AddCachedExchangeRateProvider_WhenBuilderIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = ExchangeRateCachingServiceBuilderExtensions.AddCachedExchangeRateProvider(null!, _ => Array.Empty<KeyValuePair<string, IDatedExchangeRateProvider>>());
        });

        Assert.AreEqual("builder", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> sources factory is rejected.
    /// </summary>
    [TestMethod]
    public void AddCachedExchangeRateProvider_WhenSourcesIsNull_ShouldThrowArgumentNullException()
    {
        var services = new ServiceCollection();
        IFinancialServiceBuilder builder = services.AddBoduFinancial();

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = builder.AddCachedExchangeRateProvider(null!);
        });

        Assert.AreEqual("sources", ex.ParamName);
    }

    /// <summary>
    /// Builds a service provider after applying the supplied registration against a fresh financial builder.
    /// </summary>
    /// <param name="register">The registration callback.</param>
    /// <returns>The built service provider.</returns>
    private static ServiceProvider BuildProvider(Action<IFinancialServiceBuilder> register)
    {
        var services = new ServiceCollection();
        register(services.AddBoduFinancial());
        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Builds a named source entry.
    /// </summary>
    /// <param name="name">The source name.</param>
    /// <param name="provider">The source provider.</param>
    /// <returns>The named source entry.</returns>
    private static KeyValuePair<string, IDatedExchangeRateProvider> Source(string name, IDatedExchangeRateProvider provider) =>
        new(name, provider);

    /// <summary>
    /// Builds a fixed provider resolving a single AUD/USD observation.
    /// </summary>
    /// <param name="date">The observation date.</param>
    /// <param name="rate">The rate.</param>
    /// <returns>A new fixed provider.</returns>
    private static FixedDatedExchangeRateProvider FixedProvider(DateOnly date, decimal rate) =>
        new(new[] { new ExchangeRate("AUD", "USD", date, rate, "Test") });
}
