// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateCacheDirectoryContext.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Supplies the inputs a directory resolver needs to choose the folder that holds a currency pair's cache file or
/// files: the configured cache root, the bound provider, and the currency pair.
/// </summary>
/// <remarks>
/// Passed to the directory delegate of an <see cref="RateCacheFileLayout" /> so a consumer can shape the folder
/// hierarchy — for example a flat folder, a per-provider folder, or a per-provider-then-per-pair tree.
/// </remarks>
public readonly record struct RateCacheDirectoryContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RateCacheDirectoryContext" /> class. Initializes a new instance of
    /// the <see cref="RateCacheDirectoryContext" /> struct.
    /// </summary>
    /// <param name="root">The resolved cache root directory.</param>
    /// <param name="provider">The provider the cache is bound to.</param>
    /// <param name="pair">The currency pair the folder holds.</param>
    public RateCacheDirectoryContext(string root, string provider, CurrencyPair pair)
    {
        Root = root;
        Provider = provider;
        Pair = pair;
    }

    /// <summary>
    /// Gets the resolved cache root directory.
    /// </summary>
    /// <value>The cache root directory.</value>
    public string Root { get; }

    /// <summary>
    /// Gets the provider the cache is bound to.
    /// </summary>
    /// <value>The provider identifier.</value>
    public string Provider { get; }

    /// <summary>
    /// Gets the currency pair the resolved folder holds.
    /// </summary>
    /// <value>The currency pair.</value>
    public CurrencyPair Pair { get; }
}
