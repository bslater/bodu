// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StripedAsyncLockSet{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;

namespace Bodu.Caching;

/// <summary>
/// Provides one asynchronous-capable mutual-exclusion primitive per key, created on first use and reused thereafter, so
/// synchronous and asynchronous read-modify-write sequences for the same key serialize against each other while
/// sequences for distinct keys proceed concurrently.
/// </summary>
/// <typeparam name="TKey">The key type the locks are striped by.</typeparam>
/// <remarks>
/// <para>
/// Unlike a <see cref="Monitor" />-based lock, the underlying <see cref="SemaphoreSlim" /> can be awaited, so an
/// asynchronous write path can hold the same per-key exclusion a synchronous path does without blocking a thread-pool
/// thread across I/O. The semaphore is not reentrant: a holder that re-acquires its own key deadlocks, so callers must
/// not nest same-key acquisitions (the cache bases never do).
/// </para>
/// <para>
/// Lock objects are never removed; the set grows to the number of distinct keys observed, which is bounded by the key
/// domain a cache serves. This type is a shared cache-infrastructure primitive: it is linked as source into each
/// caching package that needs it (namespace <c>Bodu.Caching</c>) and compiled <see langword="internal" /> per assembly,
/// so no public surface is exposed and no two assemblies collide on the type identity.
/// </para>
/// </remarks>
internal sealed class StripedAsyncLockSet<TKey>
    where TKey : notnull
{
    /// <summary>The per-key semaphores, created on first use and reused thereafter.</summary>
    private readonly ConcurrentDictionary<TKey, SemaphoreSlim> _locks;

    /// <summary>
    /// Initializes a new instance of the <see cref="StripedAsyncLockSet{TKey}" /> class using the default equality
    /// comparer.
    /// </summary>
    public StripedAsyncLockSet()
    {
        _locks = new ConcurrentDictionary<TKey, SemaphoreSlim>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StripedAsyncLockSet{TKey}" /> class using the supplied comparer.
    /// </summary>
    /// <param name="comparer">The equality comparer the keys are striped by.</param>
    public StripedAsyncLockSet(IEqualityComparer<TKey> comparer)
    {
        _locks = new ConcurrentDictionary<TKey, SemaphoreSlim>(comparer);
    }

    /// <summary>
    /// Acquires the key's lock synchronously, blocking until it is available.
    /// </summary>
    /// <param name="key">The key whose lock is required.</param>
    /// <returns>A releaser that must be disposed to release the lock.</returns>
    public Releaser Acquire(TKey key)
    {
        SemaphoreSlim gate = For(key);
        gate.Wait();
        return new Releaser(gate);
    }

    /// <summary>
    /// Acquires the key's lock asynchronously, without blocking a thread while waiting.
    /// </summary>
    /// <param name="key">The key whose lock is required.</param>
    /// <param name="cancellationToken">Cancels the wait; a cancelled wait acquires nothing.</param>
    /// <returns>A releaser that must be disposed to release the lock.</returns>
    /// <exception cref="OperationCanceledException">
    /// Thrown when <paramref name="cancellationToken" /> is cancelled before the lock is acquired; nothing is held in
    /// that case, so the caller has nothing to release.
    /// </exception>
    public async ValueTask<Releaser> AcquireAsync(TKey key, CancellationToken cancellationToken)
    {
        SemaphoreSlim gate = For(key);
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        return new Releaser(gate);
    }

    /// <summary>
    /// Returns the semaphore guarding the supplied key, creating it on first use.
    /// </summary>
    /// <param name="key">The key whose lock is required.</param>
    /// <returns>The per-key semaphore.</returns>
    private SemaphoreSlim For(TKey key) =>
        _locks.GetOrAdd(key, static _ => new SemaphoreSlim(1, 1));

    /// <summary>
    /// Releases a held per-key lock exactly once when disposed.
    /// </summary>
    public struct Releaser : IDisposable
    {
        /// <summary>The semaphore to release, or <see langword="null" /> once released.</summary>
        private SemaphoreSlim? _gate;

        /// <summary>
        /// Initializes a new instance of the <see cref="Releaser" /> struct over a held semaphore.
        /// </summary>
        /// <param name="gate">The held semaphore.</param>
        internal Releaser(SemaphoreSlim gate)
        {
            _gate = gate;
        }

        /// <summary>
        /// Releases the held lock; further disposals are no-ops.
        /// </summary>
        public void Dispose()
        {
            _gate?.Release();
            _gate = null;
        }
    }
}
