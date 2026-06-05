// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateServiceCollectionExtensions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu;
using Bodu.Globalization.Calendar;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides <see cref="IServiceCollection" /> extension methods for registering the Bodu v2 notable-date service.
/// </summary>
/// <remarks>
/// <para>
/// The service is registered as a singleton because a <see cref="NotableDateResource" /> is immutable and the resolver
/// holds no shared mutable state, so a single instance can be shared across the application safely.
/// </para>
/// <para>
/// <strong>When to use.</strong> Use <see cref="AddNotableDateService(IServiceCollection, NotableDateResource)" /> when
/// the resource is available at startup, the factory overload when it must be built from other registered services, and
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
/// // Consume it anywhere through constructor injection.
/// public sealed class PayrollCalendar(INotableDateService notableDates)
/// {
///     public bool IsBankHoliday(DateOnly date) =>
///         notableDates.Resolve(date, "US").Any(n => n.Category == NotableDateCategory.BankHoliday);
/// }
///]]>
/// </code>
/// </example>
/// <seealso cref="INotableDateService" />
/// <seealso cref="NotableDateResource" />
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

        services.AddSingleton<INotableDateService>(new NotableDateService(resource));
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

        services.AddSingleton<INotableDateService>(provider => new NotableDateService(resourceFactory(provider)));
        return services;
    }

    /// <summary>
    /// Registers a reloadable <see cref="INotableDateService" /> over a <see cref="MutableNotableDateResourceProvider" />
    /// so the resolved data can be swapped at runtime.
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
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="services" /> or <paramref name="initialResource" /> is <see langword="null" />.
    /// </exception>
    public static IServiceCollection AddReloadableNotableDateService(this IServiceCollection services, NotableDateResource initialResource)
    {
        ThrowHelper.ThrowIfNull(services);
        ThrowHelper.ThrowIfNull(initialResource);

        MutableNotableDateResourceProvider provider = new(initialResource);
        services.AddSingleton(provider);
        services.AddSingleton<INotableDateResourceProvider>(provider);
        services.AddSingleton<INotableDateService>(_ => new ReloadableNotableDateService(provider));
        return services;
    }
}
