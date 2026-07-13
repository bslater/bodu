// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SqliteNotableDateCacheExtensions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar.Caching;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Provides the fluent registration of a SQLite-backed notable-date cache onto an <see cref="IServiceCollection" />.
/// </summary>
public static class SqliteNotableDateCacheExtensions
{
    /// <summary>The default configuration section bound into <see cref="SqliteNotableDateCacheOptions" />.</summary>
    private const string DefaultCacheSection = "Calendar:NotableDateCache:Sqlite";

    /// <summary>
    /// Registers a <see cref="SqliteNotableDateCache" /> as the <see cref="INotableDateCache" /> used by the caching
    /// notable-date service.
    /// </summary>
    /// <param name="services">The service collection to add the registration to.</param>
    /// <param name="configuration">
    /// An optional configuration root or section bound into <see cref="SqliteNotableDateCacheOptions" />.
    /// </param>
    /// <param name="sectionName">The configuration section name. Defaults to <c>Calendar:NotableDateCache:Sqlite</c>.</param>
    /// <param name="configure">An optional callback applied after configuration binding.</param>
    /// <returns>The same service collection, to allow chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="sectionName" /> is empty or white space.</exception>
    /// <remarks>
    /// The cache is registered as a singleton so its keep-alive connection is shared and the container disposes it on
    /// shutdown. Compose it with the caching service by resolving the registered cache — for example
    /// <c>AddCachedNotableDateService(cacheFactory: sp =&gt; sp.GetRequiredService&lt;INotableDateCache&gt;())</c>.
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// services.AddNotableDateService(AmericasCalendarData.LoadResource("US"));
    /// services.AddSqliteNotableDateCache(configure: o => o.DatabaseFilePath = "/var/cache/holidays.db");
    /// services.AddCachedNotableDateService(cacheFactory: sp => sp.GetRequiredService<INotableDateCache>());
    ///]]>
    /// </code>
    /// </example>
    public static IServiceCollection AddSqliteNotableDateCache(
        this IServiceCollection services,
        IConfiguration? configuration = null,
        string sectionName = DefaultCacheSection,
        Action<SqliteNotableDateCacheOptions>? configure = null)
    {
        ThrowHelper.ThrowIfNull(services);
        ThrowHelper.ThrowIfNullOrWhiteSpace(sectionName);

        OptionsBuilder<SqliteNotableDateCacheOptions> optionsBuilder = services.AddOptions<SqliteNotableDateCacheOptions>();
        if (configuration is not null)
            optionsBuilder.Bind(configuration.GetSection(sectionName));
        if (configure is not null)
            optionsBuilder.Configure(configure);
        optionsBuilder
            .Validate(static options => options.TryValidate(out _), "SQLite notable-date cache options are invalid.")
            .ValidateOnStart();

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<SqliteNotableDateCacheOptions>, SqliteCacheStorageStartupValidator>());

        services.TryAddSingleton(serviceProvider =>
        {
            SqliteNotableDateCacheOptions options = serviceProvider.GetRequiredService<IOptions<SqliteNotableDateCacheOptions>>().Value;
            return new SqliteNotableDateCache(
                options,
                serviceProvider.GetService<TimeProvider>(),
                serviceProvider.GetService<ILoggerFactory>()?.CreateLogger<SqliteNotableDateCache>());
        });
        services.TryAddSingleton<INotableDateCache>(static serviceProvider => serviceProvider.GetRequiredService<SqliteNotableDateCache>());

        return services;
    }
}
