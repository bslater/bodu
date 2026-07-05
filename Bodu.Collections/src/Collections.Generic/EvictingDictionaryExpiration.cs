// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EvictingDictionaryExpiration.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

/// <summary>
/// Represents the immutable time-based expiration configuration for an <see cref="EvictingDictionary{TKey, TValue}" />.
/// </summary>
/// <remarks>
/// <para>
/// Supplying an <see cref="EvictingDictionaryExpiration" /> to an <see cref="EvictingDictionary{TKey, TValue}" />
/// constructor enables the time dimension of the cache: entries can carry a time-to-live after which they become
/// invisible to lookups and enumeration, independently of the capacity-triggered
/// <see cref="EvictingDictionaryPolicy" />. When no expiration configuration is supplied, the dictionary performs no
/// clock reads and behaves exactly as a capacity-only cache.
/// </para>
/// <para>
/// <see cref="TimeToLive" /> is the default lifetime applied to entries added without a per-entry override. When
/// <see langword="null" />, entries added through <see cref="EvictingDictionary{TKey, TValue}.Add(TKey, TValue)" /> or
/// the indexer never expire; only entries added through the time-to-live overloads (
/// <see cref="EvictingDictionary{TKey, TValue}.Add(TKey, TValue, TimeSpan)" /> and
/// <see cref="EvictingDictionary{TKey, TValue}.TryAdd(TKey, TValue, TimeSpan)" />) carry a lifetime.
/// </para>
/// <para>
/// <see cref="Kind" /> selects how lifetimes are measured. Under
/// <see cref="EvictingDictionaryExpirationKind.Absolute" /> an entry expires <see cref="TimeToLive" /> after it was
/// added or last updated. Under <see cref="EvictingDictionaryExpirationKind.Sliding" /> every successful read access —
/// <see cref="EvictingDictionary{TKey, TValue}.TryGetValue" />, the indexer getter, and
/// <see cref="EvictingDictionary{TKey, TValue}.ContainsKey" /> — restarts the countdown; enumeration and
/// <see cref="EvictingDictionary{TKey, TValue}.Touch" /> do not.
/// </para>
/// <para>
/// <see cref="TimeProvider" /> drives all clock reads, enabling deterministic testing with a fake time source. It
/// defaults to <see cref="System.TimeProvider.System" />.
/// </para>
/// </remarks>
public sealed class EvictingDictionaryExpiration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EvictingDictionaryExpiration" /> class with the specified default
    /// time-to-live, using absolute expiration and the system clock.
    /// </summary>
    /// <param name="timeToLive">
    /// The default lifetime applied to entries added without a per-entry override, or <see langword="null" /> for no
    /// default lifetime.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="timeToLive" /> is non-null and less than or equal to <see cref="TimeSpan.Zero" />.
    /// </exception>
    public EvictingDictionaryExpiration(TimeSpan? timeToLive)
        : this(timeToLive, EvictingDictionaryExpirationKind.Absolute, TimeProvider.System) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="EvictingDictionaryExpiration" /> class with the specified default
    /// time-to-live and expiration kind, using the system clock.
    /// </summary>
    /// <param name="timeToLive">
    /// The default lifetime applied to entries added without a per-entry override, or <see langword="null" /> for no
    /// default lifetime.
    /// </param>
    /// <param name="kind">The measurement mode for entry lifetimes.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="timeToLive" /> is non-null and less than or equal to <see cref="TimeSpan.Zero" />, or
    /// <paramref name="kind" /> is not a defined <see cref="EvictingDictionaryExpirationKind" /> value.
    /// </exception>
    public EvictingDictionaryExpiration(TimeSpan? timeToLive, EvictingDictionaryExpirationKind kind)
        : this(timeToLive, kind, TimeProvider.System) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="EvictingDictionaryExpiration" /> class with the specified default
    /// time-to-live, expiration kind, and time provider.
    /// </summary>
    /// <param name="timeToLive">
    /// The default lifetime applied to entries added without a per-entry override, or <see langword="null" /> for no
    /// default lifetime.
    /// </param>
    /// <param name="kind">The measurement mode for entry lifetimes.</param>
    /// <param name="timeProvider">
    /// The time source used for all clock reads. Must not be <see langword="null" />.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="timeToLive" /> is non-null and less than or equal to <see cref="TimeSpan.Zero" />, or
    /// <paramref name="kind" /> is not a defined <see cref="EvictingDictionaryExpirationKind" /> value.
    /// </exception>
    /// <exception cref="ArgumentNullException"><paramref name="timeProvider" /> is <see langword="null" />.</exception>
    public EvictingDictionaryExpiration(TimeSpan? timeToLive, EvictingDictionaryExpirationKind kind, TimeProvider timeProvider)
    {
        if (timeToLive.HasValue) ThrowHelper.ThrowIfZeroOrNegative(timeToLive.Value, nameof(timeToLive));
        ThrowHelper.ThrowIfEnumValueIsUndefined(kind);
        ThrowHelper.ThrowIfNull(timeProvider);

        TimeToLive = timeToLive;
        Kind = kind;
        TimeProvider = timeProvider;
    }

    /// <summary>
    /// Gets the default lifetime applied to entries added without a per-entry override.
    /// </summary>
    /// <value>
    /// The default time-to-live, or <see langword="null" /> when entries added without an explicit per-entry
    /// time-to-live never expire.
    /// </value>
    public TimeSpan? TimeToLive { get; }

    /// <summary>
    /// Gets the measurement mode for entry lifetimes.
    /// </summary>
    /// <value>
    /// <see cref="EvictingDictionaryExpirationKind.Absolute" /> when the countdown starts at add/update time;
    /// <see cref="EvictingDictionaryExpirationKind.Sliding" /> when successful read accesses restart it.
    /// </value>
    public EvictingDictionaryExpirationKind Kind { get; }

    /// <summary>
    /// Gets the time source used for all clock reads.
    /// </summary>
    /// <value>The configured <see cref="System.TimeProvider" />; never <see langword="null" />.</value>
    public TimeProvider TimeProvider { get; }
}
