// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateServiceCollectionExtensions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Provides <see cref="IServiceCollection" /> extension methods for registering the Bodu notable-date service.
/// </summary>
/// <remarks>
/// <para>
/// The service is registered as a singleton because a <see cref="NotableDateResource" /> is immutable and the resolver
/// holds no shared mutable state, so a single instance can be shared across the application safely. Registrations are
/// idempotent (<c>TryAdd</c> semantics): when an <see cref="INotableDateService" /> — or, for the keyed overloads, a
/// service under the same key — is already registered, the call leaves the existing registration in place.
/// </para>
/// <para>
/// <strong>When to use.</strong> Use <see cref="AddNotableDateService(IServiceCollection, NotableDateResource)" /> when
/// the resource is available at startup, the factory overloads when it must be built from other registered services,
/// the overloads accepting a <see cref="NotableDateServiceOptions" /> when the service needs collaborators (a custom
/// algorithm registry, collision resolver, adjustment handlers, or code-first providers), the keyed overloads when a
/// multi-tenant process serves several jurisdictions side by side, and
/// <see cref="AddReloadableNotableDateService(IServiceCollection, NotableDateResource)" /> when the data must be
/// swapped at runtime without restarting the host.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // Register a service over a bundled data pack at application startup.
/// builder.Services.AddNotableDateService(AmericasCalendarData.LoadResource("US"));
///
/// // Or register one service per jurisdiction and resolve by key.
/// builder.Services.AddNotableDateService("US", AmericasCalendarData.LoadResource("US"));
/// builder.Services.AddNotableDateService("AU", AsiaPacificCalendarData.LoadResource("AU"));
///
/// public sealed class PayrollCalendar([FromKeyedServices("US")] INotableDateService notableDates)
/// {
///     public bool IsBankHoliday(DateOnly date) =>
///         notableDates.Resolve(date, "US").Any(n => n.Category == NotableDateCategory.BankHoliday);
/// }
///]]>
/// </code>
/// </example>
/// <seealso cref="INotableDateService" /> <seealso cref="NotableDateResource" />
/// <seealso cref="NotableDateServiceOptions" />
/// <seealso href="../guides/calendar/dependency-injection.html">Calendar dependency injection (guide)</seealso>
public static class NotableDateServiceCollectionExtensions
{
    /// <summary>
    /// Registers an <see cref="INotableDateService" /> resolving against the supplied resource.
    /// </summary>
    /// <param name="services">The service collection to add the registration to.</param>
    /// <param name="resource">The loaded resource the service resolves against.</param>
    /// <returns>The same service collection, to allow chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="services" /> or <paramref name="resource" /> is <see langword="null" />.
    /// </exception>
    public static IServiceCollection AddNotableDateService(this IServiceCollection services, NotableDateResource resource)
    {
        ThrowHelper.ThrowIfNull(services);
        ThrowHelper.ThrowIfNull(resource);

        services.TryAddSingleton<INotableDateService>(_ => new NotableDateService(resource));
        return services;
    }

    /// <summary>
    /// Registers an <see cref="INotableDateService" /> resolving against the supplied resource with the supplied
    /// collaborators.
    /// </summary>
    /// <param name="services">The service collection to add the registration to.</param>
    /// <param name="resource">The loaded resource the service resolves against.</param>
    /// <param name="options">
    /// The collaborators composed into the service — for example a custom algorithm registry — or
    /// <see langword="null" /> for built-ins only.
    /// </param>
    /// <returns>The same service collection, to allow chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="services" /> or <paramref name="resource" /> is <see langword="null" />.
    /// </exception>
    public static IServiceCollection AddNotableDateService(this IServiceCollection services, NotableDateResource resource, NotableDateServiceOptions? options)
    {
        ThrowHelper.ThrowIfNull(services);
        ThrowHelper.ThrowIfNull(resource);

        services.TryAddSingleton<INotableDateService>(_ => new NotableDateService(resource, options));
        return services;
    }

    /// <summary>
    /// Registers an <see cref="INotableDateService" /> resolving against a resource produced by a factory.
    /// </summary>
    /// <param name="services">The service collection to add the registration to.</param>
    /// <param name="resourceFactory">A factory that produces the resource from the service provider.</param>
    /// <returns>The same service collection, to allow chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="services" /> or <paramref name="resourceFactory" /> is <see langword="null" />.
    /// </exception>
    public static IServiceCollection AddNotableDateService(this IServiceCollection services, Func<IServiceProvider, NotableDateResource> resourceFactory)
    {
        ThrowHelper.ThrowIfNull(services);
        ThrowHelper.ThrowIfNull(resourceFactory);

        services.TryAddSingleton<INotableDateService>(provider => new NotableDateService(resourceFactory(provider)));
        return services;
    }

    /// <summary>
    /// Registers an <see cref="INotableDateService" /> resolving against a resource produced by a factory, composed
    /// with collaborators produced by a second factory.
    /// </summary>
    /// <param name="services">The service collection to add the registration to.</param>
    /// <param name="resourceFactory">A factory that produces the resource from the service provider.</param>
    /// <param name="optionsFactory">
    /// A factory that produces the collaborators composed into the service, or <see langword="null" /> for built-ins
    /// only. The factory itself may also return <see langword="null" />.
    /// </param>
    /// <returns>The same service collection, to allow chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="services" /> or <paramref name="resourceFactory" /> is <see langword="null" />.
    /// </exception>
    public static IServiceCollection AddNotableDateService(
        this IServiceCollection services,
        Func<IServiceProvider, NotableDateResource> resourceFactory,
        Func<IServiceProvider, NotableDateServiceOptions?>? optionsFactory)
    {
        ThrowHelper.ThrowIfNull(services);
        ThrowHelper.ThrowIfNull(resourceFactory);

        services.TryAddSingleton<INotableDateService>(provider =>
            new NotableDateService(resourceFactory(provider), optionsFactory?.Invoke(provider)));
        return services;
    }

    /// <summary>
    /// Registers a keyed <see cref="INotableDateService" /> resolving against the supplied resource, so a multi-tenant
    /// process can register one service per jurisdiction and resolve them by key.
    /// </summary>
    /// <param name="services">The service collection to add the registration to.</param>
    /// <param name="serviceKey">The key the service is registered and resolved under.</param>
    /// <param name="resource">The loaded resource the service resolves against.</param>
    /// <param name="options">
    /// The collaborators composed into the service, or <see langword="null" /> for built-ins only.
    /// </param>
    /// <returns>The same service collection, to allow chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="services" />, <paramref name="serviceKey" />, or <paramref name="resource" /> is
    /// <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Consumers resolve the keyed service with <c>GetRequiredKeyedService&lt;INotableDateService&gt;(serviceKey)</c>
    /// or a <c>[FromKeyedServices(serviceKey)]</c> constructor parameter. Keyed registrations are independent of the
    /// unkeyed registration — registering both is supported.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddNotableDateService(
        this IServiceCollection services,
        string serviceKey,
        NotableDateResource resource,
        NotableDateServiceOptions? options = null)
    {
        ThrowHelper.ThrowIfNull(services);
        ThrowHelper.ThrowIfNull(serviceKey);
        ThrowHelper.ThrowIfNull(resource);

        services.TryAddKeyedSingleton<INotableDateService>(serviceKey, (_, _) => new NotableDateService(resource, options));
        return services;
    }

    /// <summary>
    /// Registers a keyed <see cref="INotableDateService" /> resolving against a resource produced by a factory.
    /// </summary>
    /// <param name="services">The service collection to add the registration to.</param>
    /// <param name="serviceKey">The key the service is registered and resolved under.</param>
    /// <param name="resourceFactory">A factory that produces the resource from the service provider.</param>
    /// <param name="optionsFactory">
    /// A factory that produces the collaborators composed into the service, or <see langword="null" /> for built-ins
    /// only. The factory itself may also return <see langword="null" />.
    /// </param>
    /// <returns>The same service collection, to allow chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="services" />, <paramref name="serviceKey" />, or <paramref name="resourceFactory" /> is
    /// <see langword="null" />.
    /// </exception>
    public static IServiceCollection AddNotableDateService(
        this IServiceCollection services,
        string serviceKey,
        Func<IServiceProvider, NotableDateResource> resourceFactory,
        Func<IServiceProvider, NotableDateServiceOptions?>? optionsFactory = null)
    {
        ThrowHelper.ThrowIfNull(services);
        ThrowHelper.ThrowIfNull(serviceKey);
        ThrowHelper.ThrowIfNull(resourceFactory);

        services.TryAddKeyedSingleton<INotableDateService>(serviceKey, (provider, _) =>
            new NotableDateService(resourceFactory(provider), optionsFactory?.Invoke(provider)));
        return services;
    }

    /// <summary>
    /// Registers a reloadable <see cref="INotableDateService" /> over a
    /// <see cref="MutableNotableDateResourceProvider" /> so the resolved data can be swapped at runtime.
    /// </summary>
    /// <param name="services">The service collection to add the registration to.</param>
    /// <param name="initialResource">The resource the service resolves against until it is reloaded.</param>
    /// <returns>The same service collection, to allow chaining.</returns>
    /// <remarks>
    /// <para>
    /// The provider is registered as a singleton under both <see cref="MutableNotableDateResourceProvider" /> and
    /// <see cref="INotableDateResourceProvider" />; injecting the former lets a caller reload the resource, after which
    /// the resolved <see cref="INotableDateService" /> reflects the new data on its next query.
    /// </para>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// // Register a reloadable service with an initial resource.
    /// builder.Services.AddReloadableNotableDateService(AmericasCalendarData.LoadResource("US"));
    ///
    /// // Later, swap in fresh data without restarting the host. Subsequent queries see the new resource.
    /// MutableNotableDateResourceProvider provider =
    ///     app.Services.GetRequiredService<MutableNotableDateResourceProvider>();
    /// provider.Reload(AmericasCalendarData.LoadResource("CA"));
    ///]]>
    /// </code>
    /// </example>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="services" /> or <paramref name="initialResource" /> is <see langword="null" />.
    /// </exception>
    public static IServiceCollection AddReloadableNotableDateService(this IServiceCollection services, NotableDateResource initialResource)
    {
        ThrowHelper.ThrowIfNull(services);
        ThrowHelper.ThrowIfNull(initialResource);

        return services.AddReloadableNotableDateService(_ => initialResource, options: null);
    }

    /// <summary>
    /// Registers a reloadable <see cref="INotableDateService" /> with the supplied collaborators propagated to each
    /// rebuilt inner service.
    /// </summary>
    /// <param name="services">The service collection to add the registration to.</param>
    /// <param name="initialResource">The resource the service resolves against until it is reloaded.</param>
    /// <param name="options">
    /// The collaborators propagated to each rebuilt inner service, or <see langword="null" /> for built-ins only.
    /// </param>
    /// <returns>The same service collection, to allow chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="services" /> or <paramref name="initialResource" /> is <see langword="null" />.
    /// </exception>
    public static IServiceCollection AddReloadableNotableDateService(
        this IServiceCollection services,
        NotableDateResource initialResource,
        NotableDateServiceOptions? options)
    {
        ThrowHelper.ThrowIfNull(services);
        ThrowHelper.ThrowIfNull(initialResource);

        return services.AddReloadableNotableDateService(_ => initialResource, options);
    }

    /// <summary>
    /// Registers a reloadable <see cref="INotableDateService" /> whose initial resource is produced by a factory.
    /// </summary>
    /// <param name="services">The service collection to add the registration to.</param>
    /// <param name="initialResourceFactory">A factory that produces the initial resource from the service provider.</param>
    /// <param name="options">
    /// The collaborators propagated to each rebuilt inner service, or <see langword="null" /> for built-ins only.
    /// </param>
    /// <returns>The same service collection, to allow chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="services" /> or <paramref name="initialResourceFactory" /> is <see langword="null" />.
    /// </exception>
    public static IServiceCollection AddReloadableNotableDateService(
        this IServiceCollection services,
        Func<IServiceProvider, NotableDateResource> initialResourceFactory,
        NotableDateServiceOptions? options = null)
    {
        ThrowHelper.ThrowIfNull(services);
        ThrowHelper.ThrowIfNull(initialResourceFactory);

        services.TryAddSingleton(sp => new MutableNotableDateResourceProvider(
            initialResourceFactory(sp),
            sp.GetService<ILoggerFactory>()?.CreateLogger<MutableNotableDateResourceProvider>()));
        services.TryAddSingleton<INotableDateResourceProvider>(sp => sp.GetRequiredService<MutableNotableDateResourceProvider>());
        services.TryAddSingleton<INotableDateService>(sp => new ReloadableNotableDateService(
            sp.GetRequiredService<MutableNotableDateResourceProvider>(),
            options,
            sp.GetService<ILoggerFactory>()?.CreateLogger<ReloadableNotableDateService>()));
        return services;
    }

    /// <summary>
    /// Registers a reloadable <see cref="INotableDateService" /> whose resource is rebuilt automatically whenever the
    /// monitored options change.
    /// </summary>
    /// <typeparam name="TOptions">The options type driving the resource; monitored via <see cref="IOptionsMonitor{TOptions}" />.</typeparam>
    /// <param name="services">The service collection to add the registration to.</param>
    /// <param name="resourceFactory">
    /// The factory producing the resource from the current options; invoked once for the initial load and again on
    /// every options change.
    /// </param>
    /// <param name="options">
    /// The collaborators propagated to each rebuilt inner service, or <see langword="null" /> for built-ins only.
    /// </param>
    /// <returns>The same service collection, to allow chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="services" /> or <paramref name="resourceFactory" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The registration binds the reloadable service to the options infrastructure: configure
    /// <typeparamref name="TOptions" /> through the standard <c>AddOptions&lt;TOptions&gt;()</c> surface (for example
    /// bound to a configuration section), and every change notification rebuilds the resource through
    /// <paramref name="resourceFactory" /> and swaps it into the live service. A factory failure during a change is
    /// logged and leaves the previously loaded resource in effect — a broken configuration edit never faults the
    /// reload thread or takes the calendar offline.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// builder.Services.AddOptions<CalendarOptions>().Bind(builder.Configuration.GetSection("Calendar"));
    /// builder.Services.AddReloadableNotableDateService<CalendarOptions>((sp, options) =>
    ///     AsiaPacificCalendarData.LoadResource(options.Territory));
    /// ]]>
    /// </code>
    /// </example>
    public static IServiceCollection AddReloadableNotableDateService<TOptions>(
        this IServiceCollection services,
        Func<IServiceProvider, TOptions, NotableDateResource> resourceFactory,
        NotableDateServiceOptions? options = null)
        where TOptions : class
    {
        ThrowHelper.ThrowIfNull(services);
        ThrowHelper.ThrowIfNull(resourceFactory);

        services.AddOptions();

        services.TryAddSingleton(sp => new MutableNotableDateResourceProvider(
            resourceFactory(sp, sp.GetRequiredService<IOptionsMonitor<TOptions>>().CurrentValue),
            sp.GetService<ILoggerFactory>()?.CreateLogger<MutableNotableDateResourceProvider>()));
        services.TryAddSingleton<INotableDateResourceProvider>(sp => sp.GetRequiredService<MutableNotableDateResourceProvider>());
        services.TryAddSingleton(sp => new NotableDateResourceReloadListener<TOptions>(
            sp,
            sp.GetRequiredService<IOptionsMonitor<TOptions>>(),
            sp.GetRequiredService<MutableNotableDateResourceProvider>(),
            resourceFactory,
            sp.GetService<ILoggerFactory>()?.CreateLogger<NotableDateResourceReloadListener<TOptions>>()));
        services.TryAddSingleton<INotableDateService>(sp =>
        {
            // Materialize the listener alongside the service so options changes are observed from the moment the
            // service is first resolved.
            _ = sp.GetRequiredService<NotableDateResourceReloadListener<TOptions>>();

            return new ReloadableNotableDateService(
                sp.GetRequiredService<MutableNotableDateResourceProvider>(),
                options,
                sp.GetService<ILoggerFactory>()?.CreateLogger<ReloadableNotableDateService>());
        });
        return services;
    }
}
