// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CachingDatedExchangeRateProvider.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// A caching provider that wraps one or more named <see cref="IDatedExchangeRateProvider" /> sources supplied at
/// construction, serving fresh rates from an <see cref="IExchangeRateCache" /> and delegating to a wrapped source only
/// on a cache miss.
/// </summary>
/// <remarks>
/// <para>
/// The wrapped sources know nothing of caching; this provider implements the same contract the caller resolves, so it
/// can be inserted transparently. Each source has its own caching duration, taken from
/// <see cref="CachingExchangeRateOptions.ProviderExpiry" /> when present and otherwise
/// <see cref="CachingExchangeRateOptions.DefaultExpiry" />.
/// </para>
/// <para>
/// When several sources are supplied the provider behaves as a caching composite: for each lookup it consults the
/// sources in the order they were supplied and returns the first rate it can satisfy — from that source's fresh cache
/// or, failing that, by delegating to the source and caching the resolved observation. On a cache hit a
/// <see cref="FixedDatedExchangeRateProvider" /> is reconstructed from the fresh rows so date-resolution, inverse, and
/// same-currency identity handling are inherited rather than re-implemented.
/// </para>
/// <para>
/// Only single-date lookups flow through the cache. Any bulk or range method a concrete source exposes is outside the
/// <see cref="IDatedExchangeRateProvider" /> surface and is therefore not intercepted here.
/// </para>
/// </remarks>
public sealed class CachingDatedExchangeRateProvider
    : IDatedExchangeRateProvider
{
    /// <summary>
    /// The wrapped sources, in priority order, paired with the name they are cached under.
    /// </summary>
    private readonly KeyValuePair<string, IDatedExchangeRateProvider>[] _sources;

    /// <summary>
    /// The cache that serves fresh rates and stores resolved observations.
    /// </summary>
    private readonly IExchangeRateCache _cache;

    /// <summary>
    /// The options carrying the per-source and default caching durations.
    /// </summary>
    private readonly CachingExchangeRateOptions _options;

    /// <summary>
    /// The time source used to evaluate freshness and stamp newly cached rows.
    /// </summary>
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="CachingDatedExchangeRateProvider" /> class backed by a TOML
    /// file-system cache rooted at the directory named in <paramref name="options" />.
    /// </summary>
    /// <param name="sources">The named sources to wrap, in priority order.</param>
    /// <param name="options">The cache location and expiry options.</param>
    /// <param name="timeProvider">
    /// The time source used to evaluate freshness and stamp newly cached rows. <see langword="null" /> selects
    /// <see cref="TimeProvider.System" />.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="sources" /> or <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="sources" /> is empty, contains a null source, a blank name, or a duplicate name; or
    /// when <paramref name="options" /> fails validation.
    /// </exception>
    public CachingDatedExchangeRateProvider(
        IEnumerable<KeyValuePair<string, IDatedExchangeRateProvider>> sources,
        CachingExchangeRateOptions options,
        TimeProvider? timeProvider = null)
        : this(sources, CreateCache(options), options, timeProvider)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CachingDatedExchangeRateProvider" /> class backed by an explicit
    /// cache, used for testing and advanced composition.
    /// </summary>
    /// <param name="sources">The named sources to wrap, in priority order.</param>
    /// <param name="cache">The cache that serves fresh rates and stores resolved observations.</param>
    /// <param name="options">
    /// The expiry options. <see cref="CachingExchangeRateOptions.CacheDirectory" /> is ignored when a cache is
    /// supplied.
    /// </param>
    /// <param name="timeProvider">
    /// The time source used to evaluate freshness and stamp newly cached rows. <see langword="null" /> selects
    /// <see cref="TimeProvider.System" />.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="sources" />, <paramref name="cache" />, or <paramref name="options" /> is
    /// <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="sources" /> is empty, contains a null source, a blank name, or a duplicate name; or
    /// when <paramref name="options" /> fails validation.
    /// </exception>
    public CachingDatedExchangeRateProvider(
        IEnumerable<KeyValuePair<string, IDatedExchangeRateProvider>> sources,
        IExchangeRateCache cache,
        CachingExchangeRateOptions options,
        TimeProvider? timeProvider = null)
    {
        ThrowHelper.ThrowIfNull(sources);
        ThrowHelper.ThrowIfNull(cache);
        ThrowHelper.ThrowIfNull(options);
        options.Validate();

        _sources = ValidateSources(sources);
        _cache = cache;
        _options = options;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public ExchangeRateLookupResult GetRate(string fromIsoCode, string toIsoCode, DateOnly date, ExchangeRateLookupOptions? options = null)
    {
        return TryGetRate(fromIsoCode, toIsoCode, date, options, out ExchangeRateLookupResult result)
            ? result
            : throw new KeyNotFoundException(
                string.Format(CultureInfo.CurrentCulture, CachingResourceStrings.IO_KeyNotFound_ExchangeRate, fromIsoCode, toIsoCode, date));
    }

    /// <inheritdoc />
    public bool TryGetRate(string fromIsoCode, string toIsoCode, DateOnly date, ExchangeRateLookupOptions? options, out ExchangeRateLookupResult result)
    {
        var now = _timeProvider.GetUtcNow();

        foreach (KeyValuePair<string, IDatedExchangeRateProvider> source in _sources)
        {
            var duration = _options.GetExpiry(source.Key);

            if (TryServeFromCache(source.Key, duration, fromIsoCode, toIsoCode, date, options, now, out result))
                return true;

            if (source.Value.TryGetRate(fromIsoCode, toIsoCode, date, options, out result))
            {
                StoreResult(source.Key, duration, fromIsoCode, toIsoCode, result, now);
                return true;
            }
        }

        result = default;
        return false;
    }

    /// <summary>
    /// Builds the default TOML file-system cache from the supplied options.
    /// </summary>
    /// <param name="options">The cache location options.</param>
    /// <returns>A new cache instance.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    private static IExchangeRateCache CreateCache(CachingExchangeRateOptions options)
    {
        ThrowHelper.ThrowIfNull(options);
        return new TomlFileSystemExchangeRateCache(new FileSystemExchangeRateCacheOptions { CacheDirectory = options.CacheDirectory });
    }

    /// <summary>
    /// Snapshots and validates the supplied sources, rejecting an empty set, null sources, blank names, and duplicate
    /// names.
    /// </summary>
    /// <param name="sources">The sources to validate.</param>
    /// <returns>The validated sources as an array preserving the supplied order.</returns>
    /// <exception cref="ArgumentException">Thrown when the sources violate a rule.</exception>
    private static KeyValuePair<string, IDatedExchangeRateProvider>[] ValidateSources(IEnumerable<KeyValuePair<string, IDatedExchangeRateProvider>> sources)
    {
        KeyValuePair<string, IDatedExchangeRateProvider>[] snapshot = [.. sources];

        if (snapshot.Length == 0)
            throw new ArgumentException(CachingResourceStrings.Arg_Invalid_ProvidersEmpty, nameof(sources));

        HashSet<string> names = new(StringComparer.Ordinal);
        foreach (KeyValuePair<string, IDatedExchangeRateProvider> source in snapshot)
        {
            if (string.IsNullOrWhiteSpace(source.Key))
                throw new ArgumentException(CachingResourceStrings.Arg_Invalid_ProviderNameBlank, nameof(sources));

            if (source.Value is null)
                throw new ArgumentException(CachingResourceStrings.Arg_Invalid_ProviderNull, nameof(sources));

            if (!names.Add(source.Key))
            {
                throw new ArgumentException(
                    string.Format(CultureInfo.CurrentCulture, CachingResourceStrings.Arg_Invalid_DuplicateProviderName, source.Key),
                    nameof(sources));
            }
        }

        return snapshot;
    }

    /// <summary>
    /// Attempts to resolve the request from the fresh cached rows for a source (and its inverse, when inversion is
    /// permitted) by delegating to a <see cref="FixedDatedExchangeRateProvider" /> built from them.
    /// </summary>
    /// <param name="provider">The source name the rows are cached under.</param>
    /// <param name="duration">The duration cached rows for the source stay fresh.</param>
    /// <param name="fromIsoCode">The source-currency ISO code.</param>
    /// <param name="toIsoCode">The destination-currency ISO code.</param>
    /// <param name="date">The calendar date for which a rate is required.</param>
    /// <param name="options">The lookup rules to apply.</param>
    /// <param name="now">The instant against which cached rows are evaluated for freshness.</param>
    /// <param name="result">When this method returns <see langword="true" />, the resolved result.</param>
    /// <returns>
    /// <see langword="true" /> when the request was satisfied from the cache; otherwise <see langword="false" />.
    /// </returns>
    private bool TryServeFromCache(string provider, TimeSpan duration, string fromIsoCode, string toIsoCode, DateOnly date, ExchangeRateLookupOptions? options, DateTimeOffset now, out ExchangeRateLookupResult result)
    {
        ExchangeRatePair pair = new(fromIsoCode, toIsoCode);

        IReadOnlyList<CachedExchangeRate> direct = _cache.GetRates(provider, pair, duration, now);

        var allowInverse = options?.AllowInverse ?? ExchangeRateLookupOptions.Exact.AllowInverse;
        IReadOnlyList<CachedExchangeRate> inverse = allowInverse
            ? _cache.GetRates(provider, pair.Inverse(), duration, now)
            : Array.Empty<CachedExchangeRate>();

        if (direct.Count == 0 && inverse.Count == 0)
        {
            result = default;
            return false;
        }

        List<ExchangeRate> rates = new(direct.Count + inverse.Count);
        foreach (CachedExchangeRate rate in direct)
            rates.Add(new ExchangeRate(fromIsoCode, toIsoCode, rate.Date, rate.Rate, provider));
        foreach (CachedExchangeRate rate in inverse)
            rates.Add(new ExchangeRate(toIsoCode, fromIsoCode, rate.Date, rate.Rate, provider));

        FixedDatedExchangeRateProvider snapshot = new(rates);
        return snapshot.TryGetRate(fromIsoCode, toIsoCode, date, options, out result);
    }

    /// <summary>
    /// Stores the resolved observation for the requested pair under a source, stamped with the current caching instant.
    /// </summary>
    /// <param name="provider">The source name to cache the rate under.</param>
    /// <param name="duration">The duration cached rows for the source stay fresh.</param>
    /// <param name="fromIsoCode">The source-currency ISO code.</param>
    /// <param name="toIsoCode">The destination-currency ISO code.</param>
    /// <param name="result">The resolved lookup result returned by the wrapped source.</param>
    /// <param name="now">The instant to stamp the cached row with.</param>
    private void StoreResult(string provider, TimeSpan duration, string fromIsoCode, string toIsoCode, ExchangeRateLookupResult result, DateTimeOffset now)
    {
        ExchangeRatePair pair = new(fromIsoCode, toIsoCode);
        CachedExchangeRate[] rows = { new(result.Rate.Date, result.Rate.Rate, now) };
        _cache.Store(provider, pair, rows, duration, now);
    }
}
