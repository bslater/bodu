// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SqliteNotableDateCacheExtensionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar.Caching;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the dependency-injection wiring of the SQLite-backed notable-date cache.
/// </summary>
[TestClass]
public sealed class SqliteNotableDateCacheExtensionsTests
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
        _databasePath = Path.Combine(Path.GetTempPath(), "bodu-calendar-sqlite-di-tests", $"{Guid.NewGuid():N}.db");
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
    /// Verifies that the registered cache resolves as an <see cref="INotableDateCache" />.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void AddSqliteNotableDateCache_WhenRegistered_ShouldResolveCache()
    {
        using ServiceProvider provider = BuildProvider(services =>
            services.AddSqliteNotableDateCache(configure: o => o.DatabaseFilePath = _databasePath));

        INotableDateCache cache = provider.GetRequiredService<INotableDateCache>();

        Assert.IsInstanceOfType<SqliteNotableDateCache>(cache);
    }

    /// <summary>
    /// Verifies that the concrete registration and the <see cref="INotableDateCache" /> registration resolve the
    /// same singleton, so the container disposes the one keep-alive connection exactly once.
    /// </summary>
    [TestMethod]
    public void AddSqliteNotableDateCache_WhenRegistered_ShouldResolveSameInstanceForConcreteAndInterface()
    {
        using ServiceProvider provider = BuildProvider(services =>
            services.AddSqliteNotableDateCache(configure: o => o.DatabaseFilePath = _databasePath));

        SqliteNotableDateCache concrete = provider.GetRequiredService<SqliteNotableDateCache>();
        INotableDateCache byInterface = provider.GetRequiredService<INotableDateCache>();

        Assert.AreSame(concrete, byInterface);
    }

    /// <summary>
    /// Verifies that options are bound from the supplied configuration section.
    /// </summary>
    [TestMethod]
    public void AddSqliteNotableDateCache_WhenConfigurationProvided_ShouldBindOptionsFromSection()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Calendar:NotableDateCache:Sqlite:DatabaseFilePath"] = _databasePath,
                ["Calendar:NotableDateCache:Sqlite:UseWriteAheadLogging"] = "false",
            })
            .Build();

        using ServiceProvider provider = BuildProvider(services => services.AddSqliteNotableDateCache(configuration));

        SqliteNotableDateCacheOptions options = provider.GetRequiredService<IOptions<SqliteNotableDateCacheOptions>>().Value;

        Assert.AreEqual(_databasePath, options.DatabaseFilePath);
        Assert.IsFalse(options.UseWriteAheadLogging);
    }

    /// <summary>
    /// Verifies that the configure callback is applied after configuration binding, so code wins over
    /// configuration for the same setting.
    /// </summary>
    [TestMethod]
    public void AddSqliteNotableDateCache_WhenConfigureCallbackProvided_ShouldApplyAfterBinding()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Calendar:NotableDateCache:Sqlite:DatabaseFilePath"] = "from-configuration.db",
            })
            .Build();

        using ServiceProvider provider = BuildProvider(services =>
            services.AddSqliteNotableDateCache(configuration, configure: o => o.DatabaseFilePath = _databasePath));

        SqliteNotableDateCacheOptions options = provider.GetRequiredService<IOptions<SqliteNotableDateCacheOptions>>().Value;

        Assert.AreEqual(_databasePath, options.DatabaseFilePath);
    }

    /// <summary>
    /// Verifies that a non-default section name is honoured.
    /// </summary>
    [TestMethod]
    public void AddSqliteNotableDateCache_WhenSectionNameProvided_ShouldBindFromThatSection()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Custom:Section:DatabaseFilePath"] = _databasePath,
            })
            .Build();

        using ServiceProvider provider = BuildProvider(services =>
            services.AddSqliteNotableDateCache(configuration, "Custom:Section"));

        SqliteNotableDateCacheOptions options = provider.GetRequiredService<IOptions<SqliteNotableDateCacheOptions>>().Value;

        Assert.AreEqual(_databasePath, options.DatabaseFilePath);
    }

    /// <summary>
    /// Verifies that options carrying neither a connection string nor a database file path fail validation when
    /// the cache is resolved.
    /// </summary>
    [TestMethod]
    public void AddSqliteNotableDateCache_WhenNoDatabaseLocation_ShouldThrowOnResolve()
    {
        using ServiceProvider provider = BuildProvider(services => services.AddSqliteNotableDateCache());

        _ = Assert.ThrowsExactly<OptionsValidationException>(() =>
        {
            _ = provider.GetRequiredService<INotableDateCache>();
        });
    }

    /// <summary>
    /// Verifies that a negative busy timeout fails validation when the cache is resolved.
    /// </summary>
    [TestMethod]
    public void AddSqliteNotableDateCache_WhenBusyTimeoutNegative_ShouldThrowOnResolve()
    {
        using ServiceProvider provider = BuildProvider(services => services.AddSqliteNotableDateCache(configure: o =>
        {
            o.DatabaseFilePath = _databasePath;
            o.BusyTimeout = TimeSpan.FromSeconds(-1);
        }));

        _ = Assert.ThrowsExactly<OptionsValidationException>(() =>
        {
            _ = provider.GetRequiredService<INotableDateCache>();
        });
    }

    /// <summary>
    /// Verifies that the storage probe reports failure when the database location is unusable.
    /// </summary>
    [TestMethod]
    public void AddSqliteNotableDateCache_WhenValidateStorageOnStartAndDatabaseUnusable_ShouldFailStartupValidation()
    {
        string unusablePath = Path.Combine(Path.GetDirectoryName(_databasePath)!, "missing-subdirectory", "x.db");

        using ServiceProvider provider = BuildProvider(services => services.AddSqliteNotableDateCache(configure: o =>
        {
            o.DatabaseFilePath = unusablePath;
            o.ValidateStorageOnStart = true;
        }));

        IStartupValidator startup = provider.GetRequiredService<IStartupValidator>();

        _ = Assert.ThrowsExactly<OptionsValidationException>(startup.Validate);
    }

    /// <summary>
    /// Verifies that the storage probe passes over a usable database.
    /// </summary>
    [TestMethod]
    public void AddSqliteNotableDateCache_WhenValidateStorageOnStartAndDatabaseUsable_ShouldPassStartupValidation()
    {
        using ServiceProvider provider = BuildProvider(services => services.AddSqliteNotableDateCache(configure: o =>
        {
            o.DatabaseFilePath = _databasePath;
            o.ValidateStorageOnStart = true;
        }));

        IStartupValidator startup = provider.GetRequiredService<IStartupValidator>();

        startup.Validate();
    }

    /// <summary>
    /// Verifies that the storage probe is skipped when it has not been opted into, so an unusable database does
    /// not fail startup for callers that never asked for the check.
    /// </summary>
    [TestMethod]
    public void AddSqliteNotableDateCache_WhenValidateStorageOnStartNotSet_ShouldSkipStorageProbe()
    {
        string unusablePath = Path.Combine(Path.GetDirectoryName(_databasePath)!, "missing-subdirectory", "x.db");

        using ServiceProvider provider = BuildProvider(services =>
            services.AddSqliteNotableDateCache(configure: o => o.DatabaseFilePath = unusablePath));

        IStartupValidator startup = provider.GetRequiredService<IStartupValidator>();

        startup.Validate();
    }

    /// <summary>
    /// Verifies that a second registration does not displace the first, matching the underlying
    /// <c>TryAdd</c> semantics.
    /// </summary>
    [TestMethod]
    public void AddSqliteNotableDateCache_WhenRegisteredTwice_ShouldKeepFirstRegistration()
    {
        using ServiceProvider provider = BuildProvider(services =>
        {
            services.AddSqliteNotableDateCache(configure: o => o.DatabaseFilePath = _databasePath);
            services.AddSqliteNotableDateCache(configure: o => o.DatabaseFilePath = _databasePath);
        });

        INotableDateCache first = provider.GetRequiredService<INotableDateCache>();
        INotableDateCache second = provider.GetRequiredService<INotableDateCache>();

        Assert.AreSame(first, second);
    }

    /// <summary>
    /// Verifies that the registration returns the same service collection so calls can be chained.
    /// </summary>
    [TestMethod]
    public void AddSqliteNotableDateCache_Always_ShouldReturnSameServiceCollection()
    {
        var services = new ServiceCollection();

        IServiceCollection returned = services.AddSqliteNotableDateCache();

        Assert.AreSame(services, returned);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> service collection throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void AddSqliteNotableDateCache_WhenServicesIsNull_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = SqliteNotableDateCacheExtensions.AddSqliteNotableDateCache(null!);
        });

        Assert.AreEqual("services", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a blank section name throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void AddSqliteNotableDateCache_WhenSectionNameIsWhiteSpace_ShouldThrowExactly()
    {
        var services = new ServiceCollection();

        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = services.AddSqliteNotableDateCache(sectionName: "   ");
        });

        Assert.AreEqual("sectionName", ex.ParamName);
    }

    /// <summary>
    /// Builds a service provider after applying the supplied registration against a fresh service collection.
    /// </summary>
    /// <param name="register">The registration callback.</param>
    /// <returns>The built service provider.</returns>
    private static ServiceProvider BuildProvider(Action<IServiceCollection> register)
    {
        var services = new ServiceCollection();
        register(services);
        return services.BuildServiceProvider();
    }
}
