// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateCachingServiceBuilderExtensionsTests.SourceBuilder.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.DependencyInjection;
using Bodu.Financial.ExchangeRates.Caching;
using Microsoft.Extensions.DependencyInjection;

namespace Bodu.Financial.ExchangeRates.Caching.DependencyInjection;

public sealed partial class ExchangeRateCachingServiceBuilderExtensionsTests
{
    /// <summary>
    /// Verifies that the fluent source builder registers a caching provider wrapping a factory-resolved source.
    /// </summary>
    [TestMethod]
    public void AddCachedExchangeRateProvider_WithSourceBuilderFactory_ShouldWrapSource()
    {
        ServiceProvider provider = BuildProvider(builder =>
            builder.AddCachedExchangeRateProvider(s => s.AddSource("Test", _ => FixedProvider(new DateOnly(2023, 1, 3), 0.5m))));

        IDatedExchangeRateProvider resolved = provider.GetRequiredService<IDatedExchangeRateProvider>();

        Assert.IsInstanceOfType<CachingDatedExchangeRateProvider>(resolved);
        Assert.AreEqual(0.5m, resolved.GetRate("AUD", "USD", new DateOnly(2023, 1, 3), ExchangeRateLookupOptions.Exact).Rate.Rate);
    }

    /// <summary>
    /// Verifies that <c>AddSource&lt;TProvider&gt;</c> resolves the concrete source type from the container.
    /// </summary>
    [TestMethod]
    public void AddCachedExchangeRateProvider_WithSourceBuilderGeneric_ShouldResolveSourceFromContainer()
    {
        ServiceProvider provider = BuildProvider(builder =>
        {
            builder.Services.AddSingleton(FixedProvider(new DateOnly(2023, 1, 3), 0.5m));
            builder.AddCachedExchangeRateProvider(s => s.AddSource<FixedDatedExchangeRateProvider>("Test"));
        });

        IDatedExchangeRateProvider resolved = provider.GetRequiredService<IDatedExchangeRateProvider>();

        Assert.IsInstanceOfType<CachingDatedExchangeRateProvider>(resolved);
        Assert.AreEqual(0.5m, resolved.GetRate("AUD", "USD", new DateOnly(2023, 1, 3), ExchangeRateLookupOptions.Exact).Rate.Rate);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> source-builder callback is rejected.
    /// </summary>
    [TestMethod]
    public void AddCachedExchangeRateProvider_WhenSourceBuilderIsNull_ShouldThrowArgumentNullException()
    {
        var services = new ServiceCollection();
        IFinancialServiceBuilder builder = services.AddBoduFinancial();

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = builder.AddCachedExchangeRateProvider((Action<ICachedExchangeRateSourceBuilder>)null!);
        });

        Assert.AreEqual("configureSources", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the range method is wired through DI: a resolved caching provider serves a range and caches it so a
    /// repeat call is served without a second fetch from the source.
    /// </summary>
    [TestMethod]
    public async Task GetRatesAsync_ThroughResolvedProvider_ShouldServeThenCacheRange()
    {
        var directory = Path.Combine(Path.GetTempPath(), "bodu-di-range", Guid.NewGuid().ToString("N"));
        try
        {
            RangeCountingProvider source = new(new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 6));
            ServiceProvider provider = BuildProvider(builder =>
                builder.AddCachedExchangeRateProvider(
                    s => s.AddSource("Test", _ => source),
                    configure: o => o.CacheDirectory = directory));

            IDatedExchangeRateProvider resolved = provider.GetRequiredService<IDatedExchangeRateProvider>();

            IReadOnlyList<ExchangeRate> first = await resolved.GetRatesAsync("AUD", "USD", new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 6));
            IReadOnlyList<ExchangeRate> second = await resolved.GetRatesAsync("AUD", "USD", new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 6));

            Assert.AreEqual(2, first.Count);
            Assert.AreEqual(2, second.Count);
            Assert.AreEqual(1, source.GetRatesAsyncCallCount);
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>
    /// An <see cref="IDatedExchangeRateProvider" /> over two fixed AUD/USD observations that counts range fetches.
    /// </summary>
    private sealed class RangeCountingProvider
        : IDatedExchangeRateProvider
    {
        private readonly FixedDatedExchangeRateProvider _inner;

        public RangeCountingProvider(DateOnly first, DateOnly second) =>
            _inner = new FixedDatedExchangeRateProvider(new[]
            {
                new ExchangeRate("AUD", "USD", first, 0.5m, "Test"),
                new ExchangeRate("AUD", "USD", second, 0.51m, "Test"),
            });

        public int GetRatesAsyncCallCount { get; private set; }

        public ExchangeRateLookupResult GetRate(string fromIsoCode, string toIsoCode, DateOnly date, ExchangeRateLookupOptions? options = null) =>
            _inner.GetRate(fromIsoCode, toIsoCode, date, options);

        public bool TryGetRate(string fromIsoCode, string toIsoCode, DateOnly date, ExchangeRateLookupOptions? options, out ExchangeRateLookupResult result) =>
            _inner.TryGetRate(fromIsoCode, toIsoCode, date, options, out result);

        public ValueTask<IReadOnlyList<ExchangeRate>> GetRatesAsync(string fromIsoCode, string toIsoCode, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
        {
            GetRatesAsyncCallCount++;
            return _inner.GetRatesAsync(fromIsoCode, toIsoCode, startDate, endDate, cancellationToken);
        }
    }
}
