// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CachingDatedExchangeRateProvider.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// A caching provider that wraps one or more named <see cref="IDatedExchangeRateProvider" /> sources supplied at
/// construction, serving fresh rates from an <see cref="IExchangeRateCache" /> and delegating to a source only on a
/// cache miss.
/// </summary>
/// <remarks>
/// <para>
/// The wrapped sources know nothing of caching; this provider implements the same contract the caller resolves, so it
/// can be inserted transparently. The caching mechanism — per-source expiry, single-date and range serving, and
/// store-on-miss — lives in <see cref="CachingExchangeRateProviderBase" />; this type supplies the ordered set of named
/// sources.
/// </para>
/// <para>
/// When several sources are supplied the provider behaves as a caching composite, consulting the sources in the order
/// they were supplied and returning the first that can satisfy the request.
/// </para>
/// </remarks>
public sealed class CachingDatedExchangeRateProvider
    : CachingExchangeRateProviderBase
{
    /// <summary>
    /// The wrapped sources, in priority order, paired with the name they are cached under.
    /// </summary>
    private readonly KeyValuePair<string, IDatedExchangeRateProvider>[] _sources;

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
    /// The expiry options. <see cref="CachingExchangeRateOptions.CacheDirectory" /> is ignored when a cache is supplied.
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
        : base(cache, options, timeProvider)
    {
        _sources = ValidateSources(sources);
    }

    /// <inheritdoc />
    protected override IReadOnlyList<KeyValuePair<string, IDatedExchangeRateProvider>> Sources => _sources;

    /// <summary>
    /// Builds the default TOML file-system cache from the supplied options.
    /// </summary>
    /// <param name="options">The cache location options.</param>
    /// <returns>A new cache instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options" /> is <see langword="null" />.</exception>
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
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="sources" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">Thrown when the sources violate a rule.</exception>
    private static KeyValuePair<string, IDatedExchangeRateProvider>[] ValidateSources(IEnumerable<KeyValuePair<string, IDatedExchangeRateProvider>> sources)
    {
        ThrowHelper.ThrowIfNull(sources);

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
}
