// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ICachedExchangeRateSourceBuilder.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching.DependencyInjection;

/// <summary>
/// Builds the ordered set of named sources a caching exchange-rate provider wraps, resolving each source from the
/// service provider at registration time.
/// </summary>
/// <remarks>
/// Sources are consulted in the order they are added; the first to satisfy a lookup wins.
/// </remarks>
public interface ICachedExchangeRateSourceBuilder
{
    /// <summary>
    /// Adds a named source resolved by a factory.
    /// </summary>
    /// <param name="name">The identifier the source's rates are cached under.</param>
    /// <param name="source">A factory that resolves the source from the service provider.</param>
    /// <returns>The same builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name" /> is empty or white space.</exception>
    ICachedExchangeRateSourceBuilder AddSource(string name, Func<IServiceProvider, IDatedExchangeRateProvider> source);

    /// <summary>
    /// Adds a named source resolved from the container by its concrete type.
    /// </summary>
    /// <typeparam name="TProvider">The concrete provider type to resolve.</typeparam>
    /// <param name="name">The identifier the source's rates are cached under.</param>
    /// <returns>The same builder, for chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name" /> is empty or white space.</exception>
    ICachedExchangeRateSourceBuilder AddSource<TProvider>(string name)
        where TProvider : class, IDatedExchangeRateProvider;
}
