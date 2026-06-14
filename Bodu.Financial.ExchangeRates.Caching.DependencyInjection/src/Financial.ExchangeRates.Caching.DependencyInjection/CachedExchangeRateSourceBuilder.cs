// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CachedExchangeRateSourceBuilder.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;

namespace Bodu.Financial.ExchangeRates.Caching.DependencyInjection;

/// <summary>
/// The default <see cref="ICachedExchangeRateSourceBuilder" />, accumulating named source factories in the order they
/// are added.
/// </summary>
internal sealed class CachedExchangeRateSourceBuilder
    : ICachedExchangeRateSourceBuilder
{
    /// <summary>
    /// The accumulated named source factories, in insertion order.
    /// </summary>
    private readonly List<KeyValuePair<string, Func<IServiceProvider, IDatedExchangeRateProvider>>> _sources = new();

    /// <summary>
    /// Gets the accumulated named source factories, in insertion order.
    /// </summary>
    /// <returns>The named source factories.</returns>
    public IReadOnlyList<KeyValuePair<string, Func<IServiceProvider, IDatedExchangeRateProvider>>> Sources => _sources;

    /// <inheritdoc />
    public ICachedExchangeRateSourceBuilder AddSource(string name, Func<IServiceProvider, IDatedExchangeRateProvider> source)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(name);
        ThrowHelper.ThrowIfNull(source);

        _sources.Add(new KeyValuePair<string, Func<IServiceProvider, IDatedExchangeRateProvider>>(name, source));
        return this;
    }

    /// <inheritdoc />
    public ICachedExchangeRateSourceBuilder AddSource<TProvider>(string name)
        where TProvider : class, IDatedExchangeRateProvider =>
        AddSource(name, static serviceProvider => serviceProvider.GetRequiredService<TProvider>());
}
