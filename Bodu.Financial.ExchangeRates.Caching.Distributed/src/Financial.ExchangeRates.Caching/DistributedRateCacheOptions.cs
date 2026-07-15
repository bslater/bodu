// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DistributedRateCacheOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Configures a distributed-cache-backed <see cref="IRateCache" />: the single provider inherited from
/// <see cref="RateCacheOptions" /> together with an optional key prefix applied to every entry the cache writes to the
/// backing <see cref="Microsoft.Extensions.Caching.Distributed.IDistributedCache" />.
/// </summary>
/// <remarks>
/// <para>
/// The backing store is supplied by the dependency-injection container as an
/// <see cref="Microsoft.Extensions.Caching.Distributed.IDistributedCache" /> (for example a Redis cache) rather than by
/// these options; this type therefore carries only the bound provider and an optional <see cref="KeyPrefix" /> used to
/// namespace the cache's entries so several applications, or several caches, can safely share one distributed store.
/// </para>
/// <para>
/// Freshness is supplied per call by the caching provider, so this type carries no caching duration of its own.
/// Server-side lifetime is a separate concern: each written blob is stamped with an absolute expiration of the caching
/// duration plus <see cref="EntryExpirationMargin" />, so a key whose pair stops being queried self-evicts from the
/// backing store instead of lingering forever. Set the margin to <see langword="null" /> to write entries without any
/// server-side expiration (the pre-margin behaviour).
/// </para>
/// </remarks>
public class DistributedRateCacheOptions
    : RateCacheOptions
{
    /// <summary>
    /// Gets or sets the prefix prepended to every cache key the cache writes.
    /// </summary>
    /// <value>The key prefix, or <see langword="null" /> to write keys without a prefix.</value>
    /// <remarks>
    /// <para>
    /// A prefix namespaces the cache's entries within a shared distributed store so that several applications, or
    /// several caches over different providers, do not collide. When set it is included verbatim at the front of every
    /// key; when unset, keys begin with the provider name.
    /// </para>
    /// <para>
    /// Validation rejects only a value that is non-null but consists solely of white space. It does not constrain
    /// length or screen for control characters, so a prefix that a particular
    /// <see cref="Microsoft.Extensions.Caching.Distributed.IDistributedCache" /> backend rejects as a key is not caught
    /// here and surfaces only when the backing store is exercised.
    /// </para>
    /// </remarks>
    public string? KeyPrefix { get; set; }

    /// <summary>
    /// Gets or sets the margin added to the caching duration when deriving each written blob's server-side absolute
    /// expiration, or <see langword="null" /> to write entries without any server-side expiration.
    /// </summary>
    /// <value>The expiration margin; defaults to one hour.</value>
    /// <remarks>
    /// <para>
    /// Every entry this cache serves must already be fresh under the per-call caching duration, so an entry evicted
    /// server-side at <c>duration + margin</c> would in any case have been filtered on read — served results are
    /// unchanged in any normal configuration. The margin keeps the server-side lifetime comfortably behind the
    /// application-side freshness (including the shared one-minute clock-skew tolerance), so eviction never races a
    /// legitimate read.
    /// </para>
    /// <para>
    /// One corner is worth knowing: a deployment that stores under one duration and later reads under a longer one (for
    /// example after raising the provider's expiry at runtime) could observe a server-side eviction where it previously
    /// saw a hit. Set the margin to <see langword="null" /> to opt out entirely and restore unbounded server-side
    /// lifetime.
    /// </para>
    /// </remarks>
    public TimeSpan? EntryExpirationMargin { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Validates the option values, throwing when a rule is violated.
    /// </summary>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <see cref="RateCacheOptions.Provider" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <see cref="RateCacheOptions.Provider" /> is empty or white space, or when <see cref="KeyPrefix" />
    /// is supplied but consists only of white space.
    /// </exception>
    public override void Validate()
    {
        base.Validate();

        if (KeyPrefix is not null && string.IsNullOrWhiteSpace(KeyPrefix))
        {
            throw new ArgumentException(CachingDistributedResourceStrings.Arg_Invalid_KeyPrefixWhiteSpace, nameof(KeyPrefix));
        }

        if (EntryExpirationMargin is { } margin && margin < TimeSpan.Zero)
        {
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, CachingDistributedResourceStrings.Arg_Invalid_EntryExpirationMarginNegative, margin),
                nameof(EntryExpirationMargin));
        }
    }

    /// <inheritdoc />
    public override bool TryValidate(out string? error)
    {
        if (!base.TryValidate(out error))
            return false;

        if (KeyPrefix is not null && string.IsNullOrWhiteSpace(KeyPrefix))
        {
            error = CachingDistributedResourceStrings.Arg_Invalid_KeyPrefixWhiteSpace;
            return false;
        }

        if (EntryExpirationMargin is { } margin && margin < TimeSpan.Zero)
        {
            error = string.Format(CultureInfo.CurrentCulture, CachingDistributedResourceStrings.Arg_Invalid_EntryExpirationMarginNegative, margin);
            return false;
        }

        error = null;
        return true;
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
    internal string BuildKey(CurrencyPair pair) =>
        string.Format(CultureInfo.InvariantCulture, "{0}{1}:{2}{3}", KeyPrefix, Provider, pair.From, pair.To);
}
