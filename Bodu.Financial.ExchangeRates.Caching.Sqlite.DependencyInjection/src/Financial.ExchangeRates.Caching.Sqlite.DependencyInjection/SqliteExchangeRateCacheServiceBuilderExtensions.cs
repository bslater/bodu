// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SqliteExchangeRateCacheServiceBuilderExtensions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.DependencyInjection;
using Bodu.Financial.ExchangeRates.Caching.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Bodu.Financial.ExchangeRates.Caching.Sqlite.DependencyInjection;

/// <summary>
/// Provides the fluent registration of a SQLite-backed exchange-rate cache onto an
/// <see cref="IFinancialServiceBuilder" />.
/// </summary>
public static class SqliteExchangeRateCacheServiceBuilderExtensions
{
    /// <summary>The default configuration section bound into <see cref="SqliteExchangeRateCacheOptions" />.</summary>
    private const string DefaultCacheSection = "Financial:ExchangeRateCache:Sqlite";

    /// <summary>
    /// Registers a <see cref="SqliteExchangeRateCache" /> bound to <paramref name="providerName" />, resolvable as an
    /// <see cref="IExchangeRateCache" /> and as a keyed <see cref="IExchangeRateCache" /> under the provider name.
    /// </summary>
    /// <param name="builder">The financial service builder.</param>
    /// <param name="providerName">The provider whose rates the cache stores.</param>
    /// <param name="configuration">
    /// An optional configuration root or section bound into <see cref="SqliteExchangeRateCacheOptions" />.
    /// </param>
    /// <param name="sectionName">
    /// The configuration section name. Defaults to <c>Financial:ExchangeRateCache:Sqlite</c>.
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
    /// The cache is registered as a singleton so its keep-alive connection and per-pair write locks are shared across
    /// resolutions and the container disposes it on shutdown. The bound provider name is also applied to the options so
    /// a configuration section need not repeat it. Options are validated through <c>ValidateOnStart</c>, so
    /// misconfiguration fails fast at application startup.
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// services.AddBoduFinancial()
    ///         .AddSqliteExchangeRateCache("RBA", configure: o => o.DatabaseFilePath = "/var/cache/rba.db");
    ///
    /// // Resolve the cache, or wrap a source provider with a CachingExchangeRateProvider over it.
    /// var cache = provider.GetRequiredService<IExchangeRateCache>();
    ///]]>
    /// </code>
    /// </example>
    public static IFinancialServiceBuilder AddSqliteExchangeRateCache(
        this IFinancialServiceBuilder builder,
        string providerName,
        IConfiguration? configuration = null,
        string sectionName = DefaultCacheSection,
        Action<SqliteExchangeRateCacheOptions>? configure = null)
    {
        ThrowHelper.ThrowIfNull(builder);
        ThrowHelper.ThrowIfNullOrWhiteSpace(providerName);
        ThrowHelper.ThrowIfNullOrWhiteSpace(sectionName);

        IServiceCollection services = builder.Services;

        OptionsBuilder<SqliteExchangeRateCacheOptions> optionsBuilder =
            services.AddOptions<SqliteExchangeRateCacheOptions>(providerName);
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
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<SqliteExchangeRateCacheOptions>, SqliteCacheStorageStartupValidator>());

        // Register the concrete cache once as a singleton so a single instance — and its single keep-alive connection
        // and per-pair locks — backs every resolution and the container disposes it on shutdown.
        services.TryAddSingleton(serviceProvider =>
        {
            SqliteExchangeRateCacheOptions options =
                serviceProvider.GetRequiredService<IOptionsMonitor<SqliteExchangeRateCacheOptions>>().Get(providerName);
            return new SqliteExchangeRateCache(options);
        });

        // Expose the same singleton on both the default and the keyed IExchangeRateCache surface so a specific cached
        // provider is resolvable by name without creating a second instance over the same database.
        services.TryAddSingleton<IExchangeRateCache>(static serviceProvider => serviceProvider.GetRequiredService<SqliteExchangeRateCache>());
        services.TryAddKeyedSingleton<IExchangeRateCache>(providerName, static (serviceProvider, _) => serviceProvider.GetRequiredService<SqliteExchangeRateCache>());

        return builder;
    }
}
