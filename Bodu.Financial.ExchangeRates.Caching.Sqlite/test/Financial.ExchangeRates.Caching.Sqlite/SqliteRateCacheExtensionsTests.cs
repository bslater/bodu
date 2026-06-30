// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SqliteRateCacheExtensionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bodu.Financial.ExchangeRates.Caching.Sqlite;

/// <summary>
/// Verifies the dependency-injection wiring of the SQLite-backed exchange-rate cache.
/// </summary>
[TestClass]
public sealed class SqliteRateCacheExtensionsTests
{
    /// <summary>
    /// The isolated database file for the current test.
    /// </summary>
    private string _databasePath = null!;

    /// <summary>
    /// Creates an isolated database file path for the current test.
    /// </summary>
    [TestInitialize]
    public void Initialize()
    {
        _databasePath = Path.Combine(Path.GetTempPath(), "bodu-sqlite-di-tests", $"{Guid.NewGuid():N}.db");
        Directory.CreateDirectory(Path.GetDirectoryName(_databasePath)!);
    }

    /// <summary>
    /// Removes the isolated database file created for the current test.
    /// </summary>
    [TestCleanup]
    public void Cleanup()
    {
        // Remove the database and any WAL sidecar files (-wal / -shm) left when write-ahead logging is enabled.
        foreach (string candidate in new[] { _databasePath, _databasePath + "-wal", _databasePath + "-shm" })
        {
            try
            {
                if (File.Exists(candidate))
                    File.Delete(candidate);
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
    }

    /// <summary>
    /// Verifies that the registered cache resolves as an <see cref="IExchangeRateCache" /> bound to the supplied
    /// provider.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void AddSqliteRateCache_WhenRegistered_ShouldResolveCacheBoundToProvider()
    {
        ServiceProvider provider = BuildProvider(builder =>
            builder.AddSqliteRateCache("RBA", configure: o => o.DatabaseFilePath = _databasePath));

        IExchangeRateCache cache = provider.GetRequiredService<IExchangeRateCache>();

        Assert.AreEqual("RBA", cache.Provider);
    }

    /// <summary>
    /// Verifies that the registered cache is also resolvable as a keyed service under the provider name and is the same
    /// singleton as the default registration.
    /// </summary>
    [TestMethod]
    public void AddSqliteRateCache_WhenRegistered_ShouldResolveKeyedSameInstance()
    {
        ServiceProvider provider = BuildProvider(builder =>
            builder.AddSqliteRateCache("RBA", configure: o => o.DatabaseFilePath = _databasePath));

        IExchangeRateCache byDefault = provider.GetRequiredService<IExchangeRateCache>();
        IExchangeRateCache byKey = provider.GetRequiredKeyedService<IExchangeRateCache>("RBA");

        Assert.AreSame(byDefault, byKey);
    }

    /// <summary>
    /// Verifies that registering two providers against one shared database file resolves a distinct keyed cache for each,
    /// so several providers can be wired side by side over a single file.
    /// </summary>
    [TestMethod]
    public void AddSqliteRateCache_WhenTwoProvidersShareOneFile_ShouldResolveDistinctKeyedCaches()
    {
        ServiceProvider provider = BuildProvider(builder => builder
            .AddSqliteRateCache("RBA", configure: o => o.DatabaseFilePath = _databasePath)
            .AddSqliteRateCache("OFX", configure: o => o.DatabaseFilePath = _databasePath));

        IExchangeRateCache rba = provider.GetRequiredKeyedService<IExchangeRateCache>("RBA");
        IExchangeRateCache ofx = provider.GetRequiredKeyedService<IExchangeRateCache>("OFX");

        Assert.AreEqual("RBA", rba.Provider);
        Assert.AreEqual("OFX", ofx.Provider);
        Assert.AreNotSame(rba, ofx);
    }

    /// <summary>
    /// Verifies that registering the same provider twice is idempotent: the keyed registration is added once, so a
    /// single cache instance resolves for that provider.
    /// </summary>
    [TestMethod]
    public void AddSqliteRateCache_WhenSameProviderRegisteredTwice_ShouldResolveSingleInstance()
    {
        ServiceProvider provider = BuildProvider(builder => builder
            .AddSqliteRateCache("RBA", configure: o => o.DatabaseFilePath = _databasePath)
            .AddSqliteRateCache("RBA", configure: o => o.DatabaseFilePath = _databasePath));

        IExchangeRateCache first = provider.GetRequiredKeyedService<IExchangeRateCache>("RBA");
        IExchangeRateCache second = provider.GetRequiredKeyedService<IExchangeRateCache>("RBA");

        Assert.AreSame(first, second);
        Assert.AreEqual("RBA", first.Provider);
    }

    /// <summary>
    /// Verifies that, with several providers registered, the default <see cref="IExchangeRateCache" /> resolution serves
    /// the first-registered provider's cache.
    /// </summary>
    [TestMethod]
    public void AddSqliteRateCache_WhenMultipleProvidersRegistered_ShouldResolveFirstAsDefault()
    {
        ServiceProvider provider = BuildProvider(builder => builder
            .AddSqliteRateCache("RBA", configure: o => o.DatabaseFilePath = _databasePath)
            .AddSqliteRateCache("OFX", configure: o => o.DatabaseFilePath = _databasePath));

        IExchangeRateCache byDefault = provider.GetRequiredService<IExchangeRateCache>();

        Assert.AreEqual("RBA", byDefault.Provider);
    }

    /// <summary>
    /// Verifies that two providers sharing one file keep their series partitioned: a rate stored through one provider's
    /// cache is invisible to the other provider's cache.
    /// </summary>
    [TestMethod]
    public void AddSqliteRateCache_WhenTwoProvidersShareOneFile_ShouldIsolateSeriesByProvider()
    {
        ServiceProvider provider = BuildProvider(builder => builder
            .AddSqliteRateCache("RBA", configure: o => o.DatabaseFilePath = _databasePath)
            .AddSqliteRateCache("OFX", configure: o => o.DatabaseFilePath = _databasePath));
        var pair = new ExchangeRatePair(CurrencyCode.AUD, CurrencyCode.USD);
        DateTimeOffset now = DateTimeOffset.UtcNow;

        provider.GetRequiredKeyedService<IExchangeRateCache>("RBA")
            .Store(pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5m, now) }, TimeSpan.FromHours(24), now);

        IReadOnlyList<CachedExchangeRate> ofxRows = provider.GetRequiredKeyedService<IExchangeRateCache>("OFX")
            .GetRates(pair, TimeSpan.FromHours(24), now);

        Assert.IsEmpty(ofxRows);
    }

    /// <summary>
    /// Verifies that a cache resolved through dependency injection logs its best-effort degradation through the
    /// container's logger, confirming the registration wires an <see cref="ILogger" /> into the cache.
    /// </summary>
    [TestMethod]
    public void AddSqliteRateCache_WhenResolvedCacheDegrades_ShouldLogThroughContainerLogger()
    {
        // A file whose contents are not a valid SQLite database makes the cache's schema initialization fail and degrade.
        File.WriteAllText(_databasePath, "this is not a sqlite database file");
        var loggerProvider = new CapturingLoggerProvider();

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddProvider(loggerProvider));
        services.AddFinancialService().AddSqliteRateCache("RBA", configure: o => o.DatabaseFilePath = _databasePath);

        using ServiceProvider provider = services.BuildServiceProvider();

        // Resolving constructs the cache over the corrupt file, whose swallowed failure is logged at Warning.
        _ = provider.GetRequiredKeyedService<IExchangeRateCache>("RBA");

        bool loggedWarning = false;
        foreach ((LogLevel Level, EventId EventId, string Message, Exception? Exception) entry in loggerProvider.Logger.Entries)
        {
            if (entry.Level == LogLevel.Warning && entry.EventId.Id == 4520)
                loggedWarning = true;
        }

        Assert.IsTrue(loggedWarning, "the DI-resolved cache reports degradation through the container logger");
    }

    /// <summary>
    /// Verifies that the resolved cache persists and serves rates against the configured database file.
    /// </summary>
    [TestMethod]
    public void AddSqliteRateCache_WhenResolved_ShouldPersistToConfiguredDatabase()
    {
        ServiceProvider provider = BuildProvider(builder =>
            builder.AddSqliteRateCache("RBA", configure: o => o.DatabaseFilePath = _databasePath));
        IExchangeRateCache cache = provider.GetRequiredService<IExchangeRateCache>();
        DateTimeOffset now = DateTimeOffset.UtcNow;

        cache.Store(new ExchangeRatePair(CurrencyCode.AUD, CurrencyCode.USD), new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5m, now) }, TimeSpan.FromHours(24), now);

        Assert.HasCount(1, cache.GetRates(new ExchangeRatePair(CurrencyCode.AUD, CurrencyCode.USD), TimeSpan.FromHours(24), now));
        Assert.IsTrue(File.Exists(_databasePath));
    }

    /// <summary>
    /// Verifies that the database location is bound from configuration.
    /// </summary>
    [TestMethod]
    public void AddSqliteRateCache_WhenConfigurationProvided_ShouldBindDatabasePath()
    {
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Financial:ExchangeRateCache:Sqlite:DatabaseFilePath"] = _databasePath })
            .Build();

        ServiceProvider provider = BuildProvider(builder => builder.AddSqliteRateCache("RBA", config));
        IExchangeRateCache cache = provider.GetRequiredService<IExchangeRateCache>();
        DateTimeOffset now = DateTimeOffset.UtcNow;

        cache.Store(new ExchangeRatePair(CurrencyCode.AUD, CurrencyCode.USD), new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5m, now) }, TimeSpan.FromHours(24), now);

        Assert.IsTrue(File.Exists(_databasePath));
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> builder is rejected.
    /// </summary>
    [TestMethod]
    public void AddSqliteRateCache_WhenBuilderIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = SqliteRateCacheExtensions.AddSqliteRateCache(null!, "RBA");
        });

        Assert.AreEqual("builder", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a blank provider name is rejected.
    /// </summary>
    [TestMethod]
    public void AddSqliteRateCache_WhenProviderNameIsBlank_ShouldThrowArgumentException()
    {
        var services = new ServiceCollection();
        IFinancialServiceBuilder builder = services.AddFinancialService();

        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = builder.AddSqliteRateCache("  ");
        });

        Assert.AreEqual("providerName", ex.ParamName);
    }

    /// <summary>
    /// Verifies that invalid options — here, no database location supplied — fail fast through <c>ValidateOnStart</c>
    /// when the cache is resolved.
    /// </summary>
    [TestMethod]
    public void AddSqliteRateCache_WhenOptionsInvalid_ShouldThrowOnResolve()
    {
        ServiceProvider provider = BuildProvider(builder => builder.AddSqliteRateCache("RBA"));

        _ = Assert.ThrowsExactly<OptionsValidationException>(() =>
        {
            _ = provider.GetRequiredService<IExchangeRateCache>();
        });
    }

    /// <summary>
    /// Verifies that valid options pass <c>ValidateOnStart</c> and resolve the cache.
    /// </summary>
    [TestMethod]
    public void AddSqliteRateCache_WhenOptionsValid_ShouldResolveCache()
    {
        ServiceProvider provider = BuildProvider(builder =>
            builder.AddSqliteRateCache("RBA", configure: o => o.DatabaseFilePath = _databasePath));

        Assert.IsNotNull(provider.GetRequiredService<IExchangeRateCache>());
    }

    /// <summary>
    /// Verifies that, with <see cref="ExchangeRateCacheOptions.ValidateStorageOnStart" /> set over a database that
    /// cannot be opened, the startup validation the host runs fails, so a misconfigured database fails the host start
    /// rather than the first lookup.
    /// </summary>
    [TestMethod]
    public void AddSqliteRateCache_WhenValidateStorageOnStartAndDatabaseUnusable_ShouldFailStartupValidation()
    {
        string unusablePath = Path.Combine(Path.GetDirectoryName(_databasePath)!, "missing-subdirectory", "x.db");
        ServiceProvider provider = BuildProvider(builder =>
            builder.AddSqliteRateCache("RBA", configure: o =>
            {
                o.DatabaseFilePath = unusablePath;
                o.ValidateStorageOnStart = true;
            }));

        IStartupValidator startup = provider.GetRequiredService<IStartupValidator>();

        _ = Assert.ThrowsExactly<OptionsValidationException>(startup.Validate);
    }

    /// <summary>
    /// Verifies that, with <see cref="ExchangeRateCacheOptions.ValidateStorageOnStart" /> set over a usable database,
    /// the startup validation the host runs passes.
    /// </summary>
    [TestMethod]
    public void AddSqliteRateCache_WhenValidateStorageOnStartAndDatabaseUsable_ShouldPassStartupValidation()
    {
        ServiceProvider provider = BuildProvider(builder =>
            builder.AddSqliteRateCache("RBA", configure: o =>
            {
                o.DatabaseFilePath = _databasePath;
                o.ValidateStorageOnStart = true;
            }));

        IStartupValidator startup = provider.GetRequiredService<IStartupValidator>();

        startup.Validate();
    }

    /// <summary>
    /// Builds a service provider after applying the supplied registration against a fresh financial builder.
    /// </summary>
    /// <param name="register">The registration callback.</param>
    /// <returns>The built service provider.</returns>
    private static ServiceProvider BuildProvider(Action<IFinancialServiceBuilder> register)
    {
        var services = new ServiceCollection();
        register(services.AddFinancialService());
        return services.BuildServiceProvider();
    }
}
