// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateCachingExtensions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar.Caching;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Provides <see cref="IServiceCollection" /> extension methods that wrap a registered
/// <see cref="INotableDateService" /> with a caching decorator.
/// </summary>
/// <remarks>
/// <para>
/// The registration decorates the existing <see cref="INotableDateService" /> in place: the previously registered
/// service becomes the inner service a <see cref="CachingNotableDateService" /> computes misses through, so consumers
/// continue to inject <see cref="INotableDateService" /> and transparently gain caching. An
/// <see cref="INotableDateResourceProvider" /> registered by <c>AddReloadableNotableDateService</c> is observed
/// automatically, so a reload invalidates the cache.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// builder.Services.AddNotableDateService(AmericasCalendarData.LoadResource("US"));
/// builder.Services.AddCachedNotableDateService(configure: o => o.Ttl = TimeSpan.FromDays(7));
///]]>
/// </code>
/// </example>
public static class NotableDateCachingExtensions
{
    /// <summary>The default configuration section bound into <see cref="NotableDateCachingOptions" />.</summary>
    private const string DefaultCacheSection = "Calendar:NotableDateCache";

    /// <summary>
    /// Wraps the registered <see cref="INotableDateService" /> with a caching decorator, using the supplied cache or a
    /// default per-territory TOML file cache.
    /// </summary>
    /// <param name="services">The service collection to modify.</param>
    /// <param name="configure">An optional callback applied to the caching options.</param>
    /// <param name="cacheFactory">
    /// An optional factory producing the <see cref="INotableDateCache" /> from the service provider. When
    /// <see langword="null" />, a default <see cref="TomlNotableDateCache" /> under the options' <c>CacheDirectory</c>
    /// is used. Supply a factory to choose the storage structure — for example an in-memory, JSON, SQLite, or
    /// distributed cache.
    /// </param>
    /// <returns>The same service collection, to allow chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no <see cref="INotableDateService" /> has been registered.
    /// </exception>
    public static IServiceCollection AddCachedNotableDateService(
        this IServiceCollection services,
        Action<NotableDateCachingOptions>? configure = null,
        Func<IServiceProvider, INotableDateCache>? cacheFactory = null) =>
        AddCachedNotableDateService(services, configuration: null, DefaultCacheSection, configure, cacheFactory);

    /// <summary>
    /// Wraps the registered <see cref="INotableDateService" /> with a caching decorator, binding the caching options
    /// from configuration.
    /// </summary>
    /// <param name="services">The service collection to modify.</param>
    /// <param name="configuration">An optional configuration root or section bound into the caching options.</param>
    /// <param name="sectionName">The configuration section name. Defaults to <c>Calendar:NotableDateCache</c>.</param>
    /// <param name="configure">An optional callback applied after configuration binding.</param>
    /// <param name="cacheFactory">
    /// An optional factory producing the <see cref="INotableDateCache" />. When <see langword="null" />, a default
    /// per-territory TOML file cache is used.
    /// </param>
    /// <returns>The same service collection, to allow chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="sectionName" /> is empty or white space.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no <see cref="INotableDateService" /> has been registered.
    /// </exception>
    public static IServiceCollection AddCachedNotableDateService(
        this IServiceCollection services,
        IConfiguration? configuration,
        string sectionName = DefaultCacheSection,
        Action<NotableDateCachingOptions>? configure = null,
        Func<IServiceProvider, INotableDateCache>? cacheFactory = null)
    {
        ThrowHelper.ThrowIfNull(services);
        ThrowHelper.ThrowIfNullOrWhiteSpace(sectionName);

        ServiceDescriptor? existing = null;
        for (int i = services.Count - 1; i >= 0; i--)
        {
            if (services[i].ServiceType == typeof(INotableDateService))
            {
                existing = services[i];
                services.RemoveAt(i);
                break;
            }
        }

        if (existing is null)
            throw new InvalidOperationException(CalendarCachingResourceStrings.Op_Invalid_ServiceNotRegistered);

        OptionsBuilder<NotableDateCachingOptions> optionsBuilder = services.AddOptions<NotableDateCachingOptions>();
        if (configuration is not null)
            optionsBuilder.Bind(configuration.GetSection(sectionName));
        if (configure is not null)
            optionsBuilder.Configure(configure);
        optionsBuilder.Validate(static o => o.TryValidate(out _), "Caching notable-date options are invalid.").ValidateOnStart();

        services.AddSingleton<INotableDateService>(serviceProvider =>
        {
            INotableDateService inner = (INotableDateService)CreateInner(serviceProvider, existing);
            NotableDateCachingOptions options = serviceProvider.GetRequiredService<IOptions<NotableDateCachingOptions>>().Value;
            INotableDateCache cache = cacheFactory is not null
                ? cacheFactory(serviceProvider)
                : CreateDefaultCache(serviceProvider, options);

            return new CachingNotableDateService(
                inner,
                cache,
                options,
                serviceProvider.GetService<INotableDateResourceProvider>(),
                serviceProvider.GetService<TimeProvider>(),
                serviceProvider.GetService<ILoggerFactory>(),
                ownsCache: cacheFactory is null);
        });

        return services;
    }

    /// <summary>
    /// Builds the default per-territory TOML file cache from the caching options.
    /// </summary>
    /// <param name="serviceProvider">The service provider supplying ambient services.</param>
    /// <param name="options">The caching options carrying the cache directory.</param>
    /// <returns>A new <see cref="TomlNotableDateCache" />.</returns>
    private static INotableDateCache CreateDefaultCache(IServiceProvider serviceProvider, NotableDateCachingOptions options) =>
        new TomlNotableDateCache(
            new FileNotableDateCacheOptions { CacheDirectory = options.CacheDirectory },
            serviceProvider.GetService<TimeProvider>(),
            serviceProvider.GetService<ILoggerFactory>()?.CreateLogger<TomlNotableDateCache>());

    /// <summary>
    /// Materializes the inner service instance from the descriptor that was decorated.
    /// </summary>
    /// <param name="serviceProvider">The service provider used to resolve or construct the instance.</param>
    /// <param name="descriptor">The removed <see cref="INotableDateService" /> descriptor.</param>
    /// <returns>The inner service instance.</returns>
    private static object CreateInner(IServiceProvider serviceProvider, ServiceDescriptor descriptor)
    {
        if (descriptor.ImplementationInstance is not null)
            return descriptor.ImplementationInstance;

        if (descriptor.ImplementationFactory is not null)
            return descriptor.ImplementationFactory(serviceProvider);

        return ActivatorUtilities.GetServiceOrCreateInstance(serviceProvider, descriptor.ImplementationType!);
    }
}
