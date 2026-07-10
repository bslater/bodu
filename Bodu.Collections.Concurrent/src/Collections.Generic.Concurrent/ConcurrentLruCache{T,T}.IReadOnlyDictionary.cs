// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentLruCache{T,T}.IReadOnlyDictionary.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Collections.Generic.Concurrent;

public sealed partial class ConcurrentLruCache<TKey, TValue> :
    IReadOnlyDictionary<TKey, TValue>
{
    /// <summary>
    /// Gets a point-in-time snapshot of the cache's keys.
    /// </summary>
    /// <value>A new read-only collection holding the keys present when the property was read.</value>
    /// <remarks>
    /// Each read takes a fresh dictionary snapshot and allocates; later mutations are not reflected. Reading the
    /// property does not set accessed flags and does not contribute to the telemetry counters.
    /// </remarks>
    public IReadOnlyCollection<TKey> Keys
    {
        get
        {
            KeyValuePair<TKey, TValue>[] snapshot = ToArray();
            var keys = new TKey[snapshot.Length];
            for (int i = 0; i < snapshot.Length; i++)
                keys[i] = snapshot[i].Key;

            return Array.AsReadOnly(keys);
        }
    }

    /// <summary>
    /// Gets a point-in-time snapshot of the cache's values.
    /// </summary>
    /// <value>A new read-only collection holding the values present when the property was read.</value>
    /// <remarks>
    /// Each read takes a fresh dictionary snapshot and allocates; later mutations are not reflected. Reading the
    /// property does not set accessed flags and does not contribute to the telemetry counters.
    /// </remarks>
    public IReadOnlyCollection<TValue> Values
    {
        get
        {
            KeyValuePair<TKey, TValue>[] snapshot = ToArray();
            var values = new TValue[snapshot.Length];
            for (int i = 0; i < snapshot.Length; i++)
                values[i] = snapshot[i].Value;

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
    /// The property is retrieved and no entry exists for <paramref name="key" />.
    /// </exception>
    /// <remarks>
    /// The getter behaves as <see cref="TryGetValue" /> (lock-free; a hit sets the accessed flag and counts toward
    /// <see cref="HitCount" />, a miss counts toward <see cref="MissCount" /> before throwing); the setter behaves as
    /// <see cref="Add(TKey, TValue)" /> (add-or-replace).
    /// </remarks>
    public TValue this[TKey key]
    {
        get => TryGetValue(key, out TValue? value)
            ? value
            : throw new KeyNotFoundException(string.Format(CultureInfo.CurrentCulture, ConcurrentCollectionsResourceStrings.KeyNotFound_Dictionary, key));

        set => Add(key, value);
    }
}
