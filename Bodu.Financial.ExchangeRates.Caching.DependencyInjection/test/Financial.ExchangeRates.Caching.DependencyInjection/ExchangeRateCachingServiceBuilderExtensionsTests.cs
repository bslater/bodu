// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateCachingServiceBuilderExtensionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Bodu.Financial.ExchangeRates.Caching.DependencyInjection;

/// <summary>
/// Verifies the dependency-injection wiring of the single-provider caching exchange-rate provider.
/// </summary>
[TestClass]
public sealed partial class ExchangeRateCachingServiceBuilderExtensionsTests
{
    /// <summary>
    /// The isolated cache directory for the current test.
    /// </summary>
    private string _directory = null!;

    /// <summary>
    /// Creates an isolated cache directory for the current test.
    /// </summary>
    [TestInitialize]
    public void Initialize() =>
        _directory = Path.Combine(Path.GetTempPath(), "bodu-di-tests", Guid.NewGuid().ToString("N"));

    /// <summary>
    /// Removes the isolated cache directory created for the current test.
    /// </summary>
    [TestCleanup]
    public void Cleanup()
    {
        try
        {
            if (Directory.Exists(_directory))
                Directory.Delete(_directory, recursive: true);
        }
        catch (IOException)
        {
            // Best-effort cleanup.
        }
        catch (UnauthorizedAccessException)
        {
            // Best-effort cleanup.
        }
    }

    /// <summary>
    /// Verifies that the registered cached provider resolves on both the dated and timeless surfaces as the same instance.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void AddCachedExchangeRateProvider_WhenRegistered_ShouldResolveBothSurfaces()
    {
        ServiceProvider provider = BuildProvider(builder =>
            builder.AddCachedExchangeRateProvider<StubRbaProvider>("RBA", configure: o => o.CacheDirectory = _directory));

        IDatedExchangeRateProvider dated = provider.GetRequiredService<IDatedExchangeRateProvider>();
        IExchangeRateProvider timeless = provider.GetRequiredService<IExchangeRateProvider>();

        Assert.IsInstanceOfType<CachingExchangeRateProvider>(dated);
        Assert.AreSame<object>(dated, timeless);
    }

    /// <summary>
    /// Verifies that the registered caching provider serves the wrapped source's rate.
    /// </summary>
    [TestMethod]
    public void AddCachedExchangeRateProvider_WhenResolved_ShouldServeWrappedSource()
    {
        ServiceProvider provider = BuildProvider(builder =>
            builder.AddCachedExchangeRateProvider<StubRbaProvider>("RBA", configure: o => o.CacheDirectory = _directory));

        IDatedExchangeRateProvider resolved = provider.GetRequiredService<IDatedExchangeRateProvider>();
        ExchangeRateLookupResult result = resolved.GetRate("AUD", "USD", new DateOnly(2023, 1, 3), ExchangeRateLookupOptions.Exact);

        Assert.AreEqual(0.5m, result.Rate.Rate);
    }

    /// <summary>
    /// Verifies that the configured cache directory is used, writing the source's rate under a per-provider subdirectory.
    /// </summary>
    [TestMethod]
    public void AddCachedExchangeRateProvider_WhenConfigured_ShouldCacheToConfiguredDirectory()
    {
        ServiceProvider provider = BuildProvider(builder =>
            builder.AddCachedExchangeRateProvider<StubRbaProvider>("RBA", configure: o => o.CacheDirectory = _directory));
        IDatedExchangeRateProvider resolved = provider.GetRequiredService<IDatedExchangeRateProvider>();

        _ = resolved.GetRate("AUD", "USD", new DateOnly(2023, 1, 3), ExchangeRateLookupOptions.Exact);

        Assert.IsTrue(File.Exists(Path.Combine(_directory, "RBA", "AUDUSD.toml")));
    }

    /// <summary>
    /// Verifies that the cache directory is bound from configuration.
    /// </summary>
    [TestMethod]
    public void AddCachedExchangeRateProvider_WhenConfigurationProvided_ShouldBindCacheDirectory()
    {
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Financial:ExchangeRateCache:CacheDirectory"] = _directory })
            .Build();

        ServiceProvider provider = BuildProvider(builder => builder.AddCachedExchangeRateProvider<StubRbaProvider>("RBA", config));
        IDatedExchangeRateProvider resolved = provider.GetRequiredService<IDatedExchangeRateProvider>();

        _ = resolved.GetRate("AUD", "USD", new DateOnly(2023, 1, 3), ExchangeRateLookupOptions.Exact);

        Assert.IsTrue(File.Exists(Path.Combine(_directory, "RBA", "AUDUSD.toml")));
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> builder is rejected.
    /// </summary>
    [TestMethod]
    public void AddCachedExchangeRateProvider_WhenBuilderIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = ExchangeRateCachingServiceBuilderExtensions.AddCachedExchangeRateProvider<StubRbaProvider>(null!, "RBA");
        });

        Assert.AreEqual("builder", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a blank provider name is rejected.
    /// </summary>
    [TestMethod]
    public void AddCachedExchangeRateProvider_WhenProviderNameIsBlank_ShouldThrowArgumentException()
    {
        var services = new ServiceCollection();
        IFinancialServiceBuilder builder = services.AddBoduFinancial();

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = builder.AddCachedExchangeRateProvider<StubRbaProvider>("  ");
        });

        Assert.AreEqual("providerName", ex.ParamName);
    }

    /// <summary>
    /// Verifies that invalid cache options fail fast through <c>ValidateOnStart</c> when the cached provider is resolved.
    /// </summary>
    [TestMethod]
    public void AddCachedExchangeRateProvider_WhenOptionsInvalid_ShouldThrowOnResolve()
    {
        ServiceProvider provider = BuildProvider(builder =>
            builder.AddCachedExchangeRateProvider<StubRbaProvider>("RBA", configure: o => o.DefaultExpiry = TimeSpan.Zero));

        _ = Assert.ThrowsExactly<OptionsValidationException>(() =>
        {
            _ = provider.GetRequiredService<IDatedExchangeRateProvider>();
        });
    }

    /// <summary>
    /// Verifies that valid cache options pass <c>ValidateOnStart</c> and resolve the cached provider.
    /// </summary>
    [TestMethod]
    public void AddCachedExchangeRateProvider_WhenOptionsValid_ShouldResolveProvider()
    {
        ServiceProvider provider = BuildProvider(builder =>
            builder.AddCachedExchangeRateProvider<StubRbaProvider>("RBA", configure: o => o.CacheDirectory = _directory));

        Assert.IsNotNull(provider.GetRequiredService<IDatedExchangeRateProvider>());
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
    /// A parameterless <see cref="IDatedExchangeRateProvider" /> source resolving a single AUD/USD observation.
    /// </summary>
    private sealed class StubRbaProvider
        : IDatedExchangeRateProvider
    {
        /// <summary>
        /// The fixed provider backing the stub.
        /// </summary>
        private static readonly FixedDatedExchangeRateProvider Inner =
            new(new[] { new ExchangeRate("AUD", "USD", new DateOnly(2023, 1, 3), 0.5m, "RBA") });

        /// <inheritdoc />
        public ExchangeRateLookupResult GetRate(string fromIsoCode, string toIsoCode, DateOnly date, ExchangeRateLookupOptions? options = null) =>
            Inner.GetRate(fromIsoCode, toIsoCode, date, options);

        /// <inheritdoc />
        public bool TryGetRate(string fromIsoCode, string toIsoCode, DateOnly date, ExchangeRateLookupOptions? options, out ExchangeRateLookupResult result) =>
            Inner.TryGetRate(fromIsoCode, toIsoCode, date, options, out result);

        /// <inheritdoc />
        public ValueTask<IReadOnlyList<ExchangeRate>> GetRatesAsync(string fromIsoCode, string toIsoCode, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default) =>
            Inner.GetRatesAsync(fromIsoCode, toIsoCode, startDate, endDate, cancellationToken);
    }
}
