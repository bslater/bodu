// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ServiceProviderExtensions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;

namespace Bodu.Financial.DependencyInjection;

/// <summary>
/// Provides the connector that promotes the container-registered <see cref="ICurrencyLookup" /> to the ambient
/// <see cref="CurrencyResolution" /> default, so the runtime-tagged <see cref="Money" /> resolves currencies through
/// the application's configured catalogue.
/// </summary>
public static class ServiceProviderExtensions
{
    /// <summary>
    /// Installs the container's <see cref="ICurrencyLookup" /> as the process-wide ambient currency resolver. Call once
    /// at start-up, after the service provider is built, when a custom lookup should back runtime <see cref="Money" />
    /// construction and parsing.
    /// </summary>
    /// <param name="provider">The built service provider.</param>
    /// <returns>The same <paramref name="provider" />, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="provider" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// This is a composition-root operation: it mutates process-wide state via
    /// <see cref="CurrencyResolution.SetDefault(ICurrencyLookup)" />. Omitting the call leaves the registry-backed
    /// default in place, so existing applications behave identically without it.
    /// </remarks>
    public static IServiceProvider UseBoduFinancialCurrencyResolution(this IServiceProvider provider)
    {
        ThrowHelper.ThrowIfNull(provider);

        CurrencyResolution.SetDefault(provider.GetRequiredService<ICurrencyLookup>());
        return provider;
    }
}
