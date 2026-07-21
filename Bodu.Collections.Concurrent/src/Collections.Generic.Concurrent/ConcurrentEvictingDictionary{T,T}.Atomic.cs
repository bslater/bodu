// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentEvictingDictionary{T,T}.Atomic.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

/// <content> Declares the <see cref="System.Collections.Concurrent.ConcurrentDictionary{TKey, TValue}" />-style atomic
/// compare-and-update primitives. Each member observes and mutates the owning segment under a single stripe lock,
/// preserving the type's single-flight and post-commit eviction discipline. </content>
public sealed partial class ConcurrentEvictingDictionary<TKey, TValue>
{
    /// <summary>
    /// Atomically updates the value associated with <paramref name="key" /> to <paramref name="newValue" /> only when
    /// its current value equals <paramref name="comparisonValue" />.
    /// </summary>
    /// <param name="key">The key whose value is compared and possibly replaced.</param>
    /// <param name="newValue">The value to store when the comparison succeeds.</param>
    /// <param name="comparisonValue">The value the current entry must equal for the update to occur.</param>
    /// <returns>
    /// <see langword="true" /> if a live entry existed for <paramref name="key" /> and its value equaled
    /// <paramref name="comparisonValue" /> (in which case it was replaced); otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// <para>
    /// The comparison uses <see cref="EqualityComparer{TValue}.Default" />. A successful update counts as an access for
    /// the configured <see cref="EvictingDictionaryPolicy" /> — identical to the replace path of
    /// <see cref="Add(TKey, TValue)" /> — repositioning the key for recency-tracked policies and, when time-based
    /// expiration is configured, starting a fresh lease. A missing or expired key yields <see langword="false" />; an
    /// expired-but-unpurged entry is lazily removed as an eviction before the method returns.
    /// </para>
    /// <para>
    /// The whole operation is atomic and locks only the segment that owns <paramref name="key" />. Any eviction it
    /// surfaces (an expired entry) raises <see cref="ItemEvicted" /> after the segment lock has been released.
    /// </para>
    /// </remarks>
    public bool TryUpdate(TKey key, TValue newValue, TValue comparisonValue)
    {
        ThrowHelper.ThrowIfNull(key);

        List<KeyValuePair<TKey, TValue>>? evicted = null;
        int index = GetSegmentIndex(key);
        bool updated;
        lock (_locks[index])
        {
            updated = _segments[index].TryUpdate(key, newValue, comparisonValue, ref evicted);
        }

        OnEvicted(evicted);
        return updated;
    }

    /// <summary>
    /// Adds a key/value pair if the key does not already exist, or updates an existing entry by applying
    /// <paramref name="updateValueFactory" /> to its current value, and returns the resulting value.
    /// </summary>
    /// <param name="key">The key of the element to add or update.</param>
    /// <param name="addValue">The value to add when <paramref name="key" /> is absent.</param>
    /// <param name="updateValueFactory">
    /// The function that produces the new value from the key and its existing value when <paramref name="key" /> is
    /// present.
    /// </param>
    /// <returns>
    /// The new value stored for <paramref name="key" />: <paramref name="addValue" /> on the add path, or the value
    /// produced by <paramref name="updateValueFactory" /> on the update path.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="key" /> or <paramref name="updateValueFactory" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The update factory is invoked <em>inside</em> the owning segment's lock, so it runs at most once per call even
    /// under concurrent contention — the single-flight behavior shared with <see cref="GetOrAdd(TKey, TValue)" />. The
    /// factory blocks every other operation on the same segment while it runs, so keep it short and never call back
    /// into this dictionary from it. If the factory throws, nothing is added or updated and the exception propagates.
    /// </para>
    /// <para>
    /// The update path counts as an access for the configured <see cref="EvictingDictionaryPolicy" /> and starts a
    /// fresh expiration lease; the add path evicts per the policy when the owning segment is full. The whole operation
    /// is atomic and locks only the segment that owns <paramref name="key" />.
    /// </para>
    /// </remarks>
    public TValue AddOrUpdate(TKey key, TValue addValue, Func<TKey, TValue, TValue> updateValueFactory)
    {
        ThrowHelper.ThrowIfNull(key);
        ThrowHelper.ThrowIfNull(updateValueFactory);

        return AddOrUpdateCore(key, addValue, addValueFactory: null, updateValueFactory);
    }

    /// <summary>
    /// Adds a key/value pair produced by <paramref name="addValueFactory" /> if the key does not already exist, or
    /// updates an existing entry by applying <paramref name="updateValueFactory" /> to its current value, and returns
    /// the resulting value.
    /// </summary>
    /// <param name="key">The key of the element to add or update.</param>
    /// <param name="addValueFactory">
    /// The function that produces the value to add when <paramref name="key" /> is absent.
    /// </param>
    /// <param name="updateValueFactory">
    /// The function that produces the new value from the key and its existing value when <paramref name="key" /> is
    /// present.
    /// </param>
    /// <returns>
    /// The new value stored for <paramref name="key" />: the value produced by <paramref name="addValueFactory" /> on
    /// the add path, or the value produced by <paramref name="updateValueFactory" /> on the update path.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="key" />, <paramref name="addValueFactory" />, or <paramref name="updateValueFactory" /> is
    /// <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Both factories are invoked <em>inside</em> the owning segment's lock, so exactly one runs per call and it runs
    /// at most once — the single-flight behavior shared with <see cref="GetOrAdd(TKey, Func{TKey, TValue})" />. A
    /// factory blocks every other operation on the same segment while it runs, so keep it short and never call back
    /// into this dictionary from it. If a factory throws, nothing is added or updated and the exception propagates.
    /// </para>
    /// <para>
    /// The update path counts as an access for the configured <see cref="EvictingDictionaryPolicy" /> and starts a
    /// fresh expiration lease; the add path evicts per the policy when the owning segment is full. The whole operation
    /// is atomic and locks only the segment that owns <paramref name="key" />.
    /// </para>
    /// </remarks>
    public TValue AddOrUpdate(TKey key, Func<TKey, TValue> addValueFactory, Func<TKey, TValue, TValue> updateValueFactory)
    {
        ThrowHelper.ThrowIfNull(key);
        ThrowHelper.ThrowIfNull(addValueFactory);
        ThrowHelper.ThrowIfNull(updateValueFactory);

        return AddOrUpdateCore(key, default!, addValueFactory, updateValueFactory);
    }

    /// <summary>
    /// Implements the shared add-or-update path for the
    /// <see cref="AddOrUpdate(TKey, TValue, Func{TKey, TValue, TValue})" /> overloads. The applicable factory runs
    /// inside the owning segment's lock so it is invoked at most once.
    /// </summary>
    /// <param name="key">The key of the element to add or update.</param>
    /// <param name="addValue">
    /// The value used on the add path when <paramref name="addValueFactory" /> is <see langword="null" />.
    /// </param>
    /// <param name="addValueFactory">
    /// The add-value factory, or <see langword="null" /> to use <paramref name="addValue" />.
    /// </param>
    /// <param name="updateValueFactory">The update-value factory applied when the key is present.</param>
    /// <returns>The new value stored for <paramref name="key" />.</returns>
    private TValue AddOrUpdateCore(TKey key, TValue addValue, Func<TKey, TValue>? addValueFactory, Func<TKey, TValue, TValue> updateValueFactory)
    {
        List<KeyValuePair<TKey, TValue>>? evicted = null;
        int index = GetSegmentIndex(key);
        TValue result;

        try
        {
            lock (_locks[index])
            {
                result = _segments[index].AddOrUpdate(key, addValue, addValueFactory, updateValueFactory, ref evicted);
            }
        }
        finally
        {
            // A throwing factory must not swallow evictions already committed by the lookup (an expired entry may have
            // been lazily removed before the factory ran).
            OnEvicted(evicted);
        }

        return result;
    }
}
