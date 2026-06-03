// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateServiceCollectionExtensions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu;
using Bodu.Globalization.Calendar.V2;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides <see cref="IServiceCollection" /> extension methods for registering the Bodu v2 notable-date service.
/// </summary>
/// <remarks>
/// <para>
/// The service is registered as a singleton because a <see cref="NotableDateResource" /> is immutable and the resolver
/// holds no shared mutable state, so a single instance can be shared across the application safely.
/// </para>
/// </remarks>
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
}
