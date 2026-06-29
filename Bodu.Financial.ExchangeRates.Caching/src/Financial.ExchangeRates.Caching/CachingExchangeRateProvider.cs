// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CachingExchangeRateProvider.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// A caching provider that wraps a single inner <see cref="IDatedExchangeRateProvider" /> over a single-provider
/// <see cref="IExchangeRateCache" />, serving fresh rates from the cache and delegating to the inner provider only on a
/// miss.
/// </summary>
/// <remarks>
/// <para>
/// Use this to add read-through caching to one rate source. To group several cached sources behind a single entry point
/// — with priority-fallback, averaging, or per-pair routing — compose them with an
/// <see cref="AggregatingExchangeRateProvider" />.
/// </para>
/// <para>
/// The provider is storage-agnostic: it does not choose or construct a cache, so the storage structure — TOML or JSON
/// files, the on-disk layout and date partitioning, an in-memory cache, SQLite, or a distributed cache — is the
/// caller's decision. Supply any <see cref="IExchangeRateCache" />, already bound to its provider, to the constructor.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// var options = new CachingExchangeRateOptions { DefaultExpiry = TimeSpan.FromHours(12) };
///
/// // Read-through caching over a TOML file cache (the caller picks the storage).
/// var fileCache = new TomlFileExchangeRateCache(
///     new FileExchangeRateCacheOptions { Provider = "RBA", CacheDirectory = "/var/cache/fx" });
/// IDatedExchangeRateProvider cachedRba = new CachingExchangeRateProvider(rba, fileCache, options);
///
/// // Or any other IExchangeRateCache — for example an in-memory cache.
/// IDatedExchangeRateProvider cachedEcb = new CachingExchangeRateProvider(ecb, new InMemoryExchangeRateCache("ECB"), options);
///]]>
/// </code>
/// </example>
public sealed class CachingExchangeRateProvider
    : CachingExchangeRateProviderBase
{
    /// <summary>The inner provider consulted on a cache miss.</summary>
    private readonly IDatedExchangeRateProvider _inner;

    /// <summary>
    /// Initializes a new instance of the <see cref="CachingExchangeRateProvider" /> class wrapping
    /// <paramref name="inner" /> over the supplied <paramref name="cache" />.
    /// </summary>
    /// <param name="inner">The inner provider consulted on a cache miss.</param>
    /// <param name="cache">The single-provider cache that serves fresh rates and stores resolved observations.</param>
    /// <param name="options">The options carrying the caching durations and timeless-surface lookup options.</param>
    /// <param name="timeProvider">
    /// The time source used to evaluate freshness and stamp newly cached rows. <see langword="null" /> selects
    /// <see cref="TimeProvider.System" />.
    /// </param>
    /// <param name="logger">
    /// The logger that records cache diagnostics, or <see langword="null" /> to disable logging.
    /// </param>
    /// <param name="ownsInner">
    /// <see langword="true" /> to dispose <paramref name="inner" /> (when it is <see cref="IDisposable" />) as part of
    /// disposing this provider; otherwise <see langword="false" /> to leave the inner's lifetime to its owner.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="inner" />, <paramref name="cache" />, or <paramref name="options" /> is
    /// <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="options" /> fails validation.</exception>
    public CachingExchangeRateProvider(
        IDatedExchangeRateProvider inner,
        IExchangeRateCache cache,
        CachingExchangeRateOptions options,
        TimeProvider? timeProvider = null,
        ILogger? logger = null,
        bool ownsInner = false)
        : base(cache, options, timeProvider, logger, ownsInner)
    {
        ThrowHelper.ThrowIfNull(inner);

        _inner = inner;
    }

    /// <inheritdoc />
    protected override IDatedExchangeRateProvider Inner => _inner;
}
