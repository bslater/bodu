// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FileExchangeRateCacheOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Configures a file-backed <see cref="IExchangeRateCache" />: the single provider inherited from
/// <see cref="ExchangeRateCacheOptions" /> together with the directory its cache files are written under.
/// </summary>
/// <remarks>
/// Expiry is not a storage concern — it is supplied per call by the caching provider — so this type carries only the
/// storage location in addition to the bound provider.
/// </remarks>
public class FileExchangeRateCacheOptions
    : ExchangeRateCacheOptions
{
    /// <summary>
    /// Gets or sets the directory used by the on-disk cache.
    /// </summary>
    /// <value>
    /// The cache directory, or <see langword="null" /> to use a <c>bodu-exchange-rates</c> folder under the system
    /// temporary path.
    /// </value>
    public string? CacheDirectory { get; set; }
}
