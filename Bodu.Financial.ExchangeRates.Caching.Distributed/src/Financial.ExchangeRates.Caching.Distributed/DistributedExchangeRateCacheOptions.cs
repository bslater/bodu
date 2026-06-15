// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DistributedExchangeRateCacheOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Financial.ExchangeRates.Caching.Distributed;

/// <summary>
/// Configures a distributed-cache-backed <see cref="IExchangeRateCache" />: the single provider inherited from
/// <see cref="ExchangeRateCacheOptions" /> together with an optional key prefix applied to every entry the cache writes
/// to the backing <see cref="Microsoft.Extensions.Caching.Distributed.IDistributedCache" />.
/// </summary>
/// <remarks>
/// <para>
/// The backing store is supplied by the dependency-injection container as an
/// <see cref="Microsoft.Extensions.Caching.Distributed.IDistributedCache" /> (for example a Redis cache) rather than by
/// these options; this type therefore carries only the bound provider and an optional <see cref="KeyPrefix" /> used to
/// namespace the cache's entries so several applications, or several caches, can safely share one distributed store.
/// </para>
/// <para>
/// Expiry is not a storage concern — it is supplied per call by the caching provider — so this type carries no
/// duration. Cache entries are written without an absolute or sliding expiration; the cache's own freshness filtering
/// prunes stale rows and coverage windows when a pair is next written.
/// </para>
/// </remarks>
public class DistributedExchangeRateCacheOptions
    : ExchangeRateCacheOptions
{
    /// <summary>
    /// Gets or sets the prefix prepended to every cache key the cache writes.
    /// </summary>
    /// <value>The key prefix, or <see langword="null" /> to write keys without a prefix.</value>
    /// <returns>The configured key prefix, or <see langword="null" /> when none is set.</returns>
    /// <remarks>
    /// A prefix namespaces the cache's entries within a shared distributed store so that several applications, or
    /// several caches over different providers, do not collide. When set it is included verbatim at the front of every
    /// key; when unset, keys begin with the provider name.
    /// </remarks>
    public string? KeyPrefix { get; set; }

    /// <summary>
    /// Validates the option values, throwing when a rule is violated.
    /// </summary>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <see cref="ExchangeRateCacheOptions.Provider" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <see cref="ExchangeRateCacheOptions.Provider" /> is empty or white space, or when
    /// <see cref="KeyPrefix" /> is supplied but consists only of white space.
    /// </exception>
    public override void Validate()
    {
        base.Validate();

        if (KeyPrefix is not null && string.IsNullOrWhiteSpace(KeyPrefix))
        {
            throw new ArgumentException(CachingDistributedResourceStrings.Arg_Invalid_KeyPrefixWhiteSpace, nameof(KeyPrefix));
        }
    }

    /// <summary>
    /// Builds the stable, collision-free cache key for a pair from the optional prefix, the bound provider, and the
    /// pair's currency codes.
    /// </summary>
    /// <param name="pair">The currency pair the key identifies.</param>
    /// <returns>The cache key for <paramref name="pair" />.</returns>
    /// <remarks>
    /// The key is <c>{prefix}{provider}:{from}{to}</c>. The provider and the two three-letter ISO codes are joined with
    /// a colon and concatenated so that no two distinct pairs (or providers) can ever map to the same key.
    /// </remarks>
    internal string BuildKey(ExchangeRatePair pair) =>
        string.Format(CultureInfo.InvariantCulture, "{0}{1}:{2}{3}", KeyPrefix, Provider, pair.FromIsoCode, pair.ToIsoCode);
}
