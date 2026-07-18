// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentEvictingDictionary{T,T}.IReadOnlyDictionary.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Collections.Generic.Concurrent;

public sealed partial class ConcurrentEvictingDictionary<TKey, TValue> :
    IReadOnlyDictionary<TKey, TValue>
{
    /// <summary>
    /// Gets a point-in-time snapshot of the dictionary's live keys.
    /// </summary>
    /// <value>A new read-only collection holding the keys present when the property was read.</value>
    /// <remarks>
    /// Unlike <see cref="EvictingDictionary{TKey, TValue}.Keys" />, this is a snapshot rather than a live view: reading
    /// the property acquires every segment lock and copies the keys (mirroring
    /// <see cref="System.Collections.Concurrent.ConcurrentDictionary{TKey, TValue}.Keys" />), so each read allocates
    /// and later mutations are not reflected. Expired-but-unpurged entries are excluded.
    /// </remarks>
    public IReadOnlyCollection<TKey> Keys
    {
        get
        {
            int locksAcquired = 0;
            TKey[] keys;
            try
            {
                AcquireAllLocks(ref locksAcquired);
                keys = SnapshotNoLocks(static (key, _) => key);
            }
            finally
            {
                ReleaseLocks(locksAcquired);
            }

            return Array.AsReadOnly(keys);
        }
    }

    /// <summary>
    /// Gets a point-in-time snapshot of the dictionary's live values.
    /// </summary>
    /// <value>A new read-only collection holding the values present when the property was read.</value>
    /// <remarks>
    /// Unlike <see cref="EvictingDictionary{TKey, TValue}.Values" />, this is a snapshot rather than a live view:
    /// reading the property acquires every segment lock and copies the values (mirroring
    /// <see cref="System.Collections.Concurrent.ConcurrentDictionary{TKey, TValue}.Values" />), so each read allocates
    /// and later mutations are not reflected. Expired-but-unpurged entries are excluded.
    /// </remarks>
    public IReadOnlyCollection<TValue> Values
    {
        get
        {
            int locksAcquired = 0;
            TValue[] values;
            try
            {
                AcquireAllLocks(ref locksAcquired);
                values = SnapshotNoLocks(static (_, value) => value);
            }
            finally
            {
                ReleaseLocks(locksAcquired);
            }

            return Array.AsReadOnly(values);
        }
    }

    /// <inheritdoc />
    IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;

    /// <inheritdoc />
    IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;

    /// <summary>
    /// Gets or sets the value associated with the specified key.
    /// </summary>
    /// <param name="key">The key of the value to get or set.</param>
    /// <value>The value associated with <paramref name="key" />.</value>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    /// <exception cref="KeyNotFoundException">
    /// The property is retrieved and no live entry exists for <paramref name="key" />.
    /// </exception>
    /// <remarks>
    /// The getter behaves as <see cref="TryGetValue" /> (a hit counts as a policy access, increments
    /// <see cref="TotalTouches" />, and slides a sliding deadline); the setter behaves as
    /// <see cref="Add(TKey, TValue)" /> (add-or-replace). Each accessor is individually atomic, locking only the
    /// segment that owns <paramref name="key" />.
    /// </remarks>
    public TValue this[TKey key]
    {
        get => TryGetValue(key, out TValue? value)
            ? value
            : throw new KeyNotFoundException(string.Format(CultureInfo.CurrentCulture, ConcurrentCollectionsResourceStrings.KeyNotFound_Dictionary, key));

        set => Add(key, value);
    }
}
