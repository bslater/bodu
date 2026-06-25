// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SqliteRateCacheExtensionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
        try
        {
            if (File.Exists(_databasePath))
                File.Delete(_databasePath);
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
