// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StripedLockSet.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;

namespace Bodu.Caching;

/// <summary>
/// Provides one lock object per key, created on first use and reused thereafter, so read-modify-write sequences for
/// distinct keys proceed concurrently while sequences for the same key serialize.
/// </summary>
/// <typeparam name="TKey">The key type the locks are striped by.</typeparam>
/// <remarks>
/// <para>
/// Every Bodu cache backend guards its per-key (currency pair, territory) read-modify-write sequence with this shape: a
/// <see cref="ConcurrentDictionary{TKey, TValue}" /> of plain lock objects populated by
/// <see cref="ConcurrentDictionary{TKey, TValue}.GetOrAdd(TKey, Func{TKey, TValue})" />. Lock objects are never
/// removed; the set grows to the number of distinct keys observed, which is bounded by the key domain a cache serves.
/// </para>
/// <para>
/// This type is a shared cache-infrastructure primitive: it is linked as source into each caching package that needs it
/// (namespace <c>Bodu.Caching</c>) and compiled <see langword="internal" /> per assembly, so no public surface is
/// exposed and no two assemblies collide on the type identity.
/// </para>
/// </remarks>
internal sealed class StripedLockSet<TKey>
    where TKey : notnull
{
    /// <summary>The lock objects, keyed by the striping key. One object is created per key on first use and reused thereafter.</summary>
    private readonly ConcurrentDictionary<TKey, object> _locks;

    /// <summary>
    /// Initializes a new instance of the <see cref="StripedLockSet{TKey}" /> class using the default equality comparer.
    /// </summary>
    public StripedLockSet()
    {
        _locks = new ConcurrentDictionary<TKey, object>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StripedLockSet{TKey}" /> class using the supplied comparer.
    /// </summary>
    /// <param name="comparer">The equality comparer the keys are striped by.</param>
    public StripedLockSet(IEqualityComparer<TKey> comparer)
    {
        _locks = new ConcurrentDictionary<TKey, object>(comparer);
    }

    /// <summary>
    /// Returns the lock object guarding the supplied key, creating it on first use.
    /// </summary>
    /// <param name="key">The key whose lock is required.</param>
    /// <returns>The per-key lock object.</returns>
    public object For(TKey key) =>
        _locks.GetOrAdd(key, static _ => new object());
}
