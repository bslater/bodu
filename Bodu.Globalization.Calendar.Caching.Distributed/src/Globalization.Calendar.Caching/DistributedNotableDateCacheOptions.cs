// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DistributedNotableDateCacheOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

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
    /// Validates the option values, throwing when a rule is violated.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when <see cref="KeyPrefix" /> is white space.</exception>
    public override void Validate()
    {
        base.Validate();

        if (KeyPrefix is not null && string.IsNullOrWhiteSpace(KeyPrefix))
            throw new ArgumentException(CalendarCachingDistributedResourceStrings.Arg_Invalid_KeyPrefixWhitespace, nameof(KeyPrefix));
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
