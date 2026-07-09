// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SqliteRateCacheExtensions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.ExchangeRates.Caching;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Provides the fluent registration of a SQLite-backed exchange-rate cache onto an
/// <see cref="IFinancialServiceBuilder" />.
/// </summary>
public static class SqliteRateCacheExtensions
{
    /// <summary>The default configuration section bound into <see cref="SqliteRateCacheOptions" />.</summary>
    private const string DefaultCacheSection = "Financial:RateCache:Sqlite";

    /// <summary>
    /// Registers a <see cref="SqliteRateCache" /> bound to <paramref name="providerName" />, resolvable as an
    /// <see cref="IRateCache" /> and as a keyed <see cref="IRateCache" /> under the provider name.
    /// </summary>
    /// <param name="builder">The financial service builder.</param>
    /// <param name="providerName">The provider whose rates the cache stores.</param>
    /// <param name="configuration">
    /// An optional configuration root or section bound into <see cref="SqliteRateCacheOptions" />.
    /// </param>
    /// <param name="sectionName">
    /// The configuration section name. Defaults to <c>Financial:RateCache:Sqlite</c>.
    /// </param>
    /// <param name="configure">An optional callback applied after configuration binding.</param>
    /// <returns>The builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="builder" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="providerName" /> or <paramref name="sectionName" /> is empty or white space.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The cache is registered as a singleton keyed by <paramref name="providerName" /> so its keep-alive connection
    /// and per-pair write locks are shared across resolutions and the container disposes it on shutdown. The bound
    /// provider name is also applied to the options so a configuration section need not repeat it. Options are
    /// validated through <c>ValidateOnStart</c>, so misconfiguration fails fast at application startup.
    /// </para>
    /// <para>
    /// Call this once per provider to register several caches side by side. Point them at distinct
    /// <see cref="SqliteRateCacheOptions.DatabaseFilePath" /> values to isolate each provider in its own file,
    /// or at one shared file to hold every provider's series in a single database — the provider is the leading key
    /// column, so the series stay partitioned with no collisions. The first registration also backs the default
    /// <see cref="IRateCache" /> resolution; resolve a specific provider's cache by its key.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// // One shared database file holding several providers' series, each cache keyed by its provider name.
    /// services.AddFinancialService()
    ///         .AddSqliteRateCache("RBA", configure: o => o.DatabaseFilePath = "/var/cache/fx.db")
    ///         .AddSqliteRateCache("OFX", configure: o => o.DatabaseFilePath = "/var/cache/fx.db")
    ///         .AddSqliteRateCache("XE",  configure: o => o.DatabaseFilePath = "/var/cache/fx.db")
    ///         .AddCachedRateProvider<OfxRateProvider>("OFX",
    ///             cacheFactory: (sp, name) => sp.GetRequiredKeyedService<IRateCache>(name));
    ///
    /// // Resolve a specific provider's cache by key, or the first-registered one by default.
    /// var ofxCache = provider.GetRequiredKeyedService<IRateCache>("OFX");
    ///]]>
    /// </code>
    /// </example>
    public static IFinancialServiceBuilder AddSqliteRateCache(
        this IFinancialServiceBuilder builder,
        string providerName,
        IConfiguration? configuration = null,
        string sectionName = DefaultCacheSection,
        Action<SqliteRateCacheOptions>? configure = null)
    {
        ThrowHelper.ThrowIfNull(builder);
        ThrowHelper.ThrowIfNullOrWhiteSpace(providerName);
        ThrowHelper.ThrowIfNullOrWhiteSpace(sectionName);

        IServiceCollection services = builder.Services;

        OptionsBuilder<SqliteRateCacheOptions> optionsBuilder =
            services.AddOptions<SqliteRateCacheOptions>(providerName);
        if (configuration is not null)
            optionsBuilder.Bind(configuration.GetSection(sectionName));
        optionsBuilder.Configure(options =>
        {
            options.Provider = providerName;
            configure?.Invoke(options);
        });
        optionsBuilder
            .Validate(static options => options.TryValidate(out _), "SQLite exchange-rate cache options are invalid.")
            .ValidateOnStart();

        // Probe the database at host start when ValidateStorageOnStart is set, so a misconfigured or unwritable database
        // fails the start rather than the first lookup. The probe runs through the same ValidateOnStart wiring.
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<SqliteRateCacheOptions>, SqliteCacheStorageStartupValidator>());

        // Register the concrete cache as a singleton keyed by the provider name so each provider added gets its own
        // instance — its own keep-alive connection and per-pair locks — and the container disposes each on shutdown.
        // Keying is what lets several providers be registered side by side, including over one shared database file
        // where the leading provider column keeps their series partitioned.
        services.TryAddKeyedSingleton(providerName, (serviceProvider, key) =>
        {
            SqliteRateCacheOptions options =
                serviceProvider.GetRequiredService<IOptionsMonitor<SqliteRateCacheOptions>>().Get((string)key!);
            return new SqliteRateCache(
                options,
                serviceProvider.GetService<TimeProvider>(),
                serviceProvider.GetService<ILoggerFactory>()?.CreateLogger<SqliteRateCache>());
        });

        // Expose the provider's cache on the keyed IRateCache surface so a specific cached provider is resolvable
        // by name, and on the default surface for the single-provider convenience case where the first registered cache
        // wins (TryAdd leaves any earlier default in place).
        services.TryAddKeyedSingleton<IRateCache>(providerName, static (serviceProvider, key) => serviceProvider.GetRequiredKeyedService<SqliteRateCache>((string)key!));
        services.TryAddSingleton<IRateCache>(serviceProvider => serviceProvider.GetRequiredKeyedService<SqliteRateCache>(providerName));

        return builder;
    }
}
