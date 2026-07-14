// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DistributedNotableDateCacheOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Globalization.Calendar.Caching;

/// <summary>
/// Configures a distributed <see cref="INotableDateCache" />: the optional key prefix its per-territory blobs are
/// namespaced under. The backing store itself is supplied through dependency injection as an
/// <see cref="Microsoft.Extensions.Caching.Distributed.IDistributedCache" />.
/// </summary>
public class DistributedNotableDateCacheOptions
    : NotableDateCacheOptions
{
    /// <summary>
    /// Gets or sets the prefix prepended to every cache key, so several logical caches can share one distributed store
    /// without colliding.
    /// </summary>
    /// <value>The key prefix, or <see langword="null" /> for no prefix.</value>
    public string? KeyPrefix { get; set; }

    /// <summary>
    /// Gets or sets the margin added to the time-to-live when deriving each written territory blob's server-side
    /// absolute expiration, or <see langword="null" /> to write entries without any server-side expiration.
    /// </summary>
    /// <value>The expiration margin; defaults to one hour.</value>
    /// <remarks>
    /// Every entry this cache serves must already be fresh under the per-call time-to-live, so a blob evicted
    /// server-side at <c>ttl + margin</c> would in any case have been filtered on read — served results are unchanged
    /// in any normal configuration, while a territory that stops being queried self-evicts from the backing store
    /// instead of lingering forever. A deployment that stores under one time-to-live and later reads under a longer
    /// one could observe a server-side eviction where it previously saw a hit; set the margin to
    /// <see langword="null" /> to opt out and restore unbounded server-side lifetime.
    /// </remarks>
    public TimeSpan? EntryExpirationMargin { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Validates the option values, throwing when a rule is violated.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when <see cref="KeyPrefix" /> is white space, or when <see cref="EntryExpirationMargin" /> is negative.
    /// </exception>
    public override void Validate()
    {
        base.Validate();

        if (KeyPrefix is not null && string.IsNullOrWhiteSpace(KeyPrefix))
            throw new ArgumentException(CalendarCachingDistributedResourceStrings.Arg_Invalid_KeyPrefixWhitespace, nameof(KeyPrefix));

        if (EntryExpirationMargin is { } margin && margin < TimeSpan.Zero)
        {
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, CalendarCachingDistributedResourceStrings.Arg_Invalid_EntryExpirationMarginNegative, margin),
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
            error = CalendarCachingDistributedResourceStrings.Arg_Invalid_KeyPrefixWhitespace;
            return false;
        }

        if (EntryExpirationMargin is { } margin && margin < TimeSpan.Zero)
        {
            error = string.Format(CultureInfo.CurrentCulture, CalendarCachingDistributedResourceStrings.Arg_Invalid_EntryExpirationMarginNegative, margin);
            return false;
        }

        error = null;
        return true;
    }

    /// <summary>
    /// Builds the distributed cache key for a normalized territory.
    /// </summary>
    /// <param name="territory">The normalized territory.</param>
    /// <returns>The cache key.</returns>
    internal string BuildKey(string territory) =>
        $"{KeyPrefix}notable-dates:{territory}";
}
