// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FaultingDistributedCache.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Caching.Distributed;

namespace Bodu.Globalization.Calendar.Caching;

/// <summary>
/// An <see cref="IDistributedCache" /> that serves reads and writes normally but faults on a chosen operation.
/// </summary>
/// <remarks>
/// <see cref="ThrowingDistributedCache" /> fails everything, which models a store that was never reachable. Several
/// paths only exist for a store that <em>was</em> reachable and then failed part-way - the clear loop, for instance,
/// iterates the keys a successful write recorded, so a cache that could not write anything never enters it. This
/// stand-in keeps the successful operations working so those paths are reachable, and lets a test choose both which
/// operation fails and with what.
/// </remarks>
/// <param name="inner">The store serving the operations that are not faulted.</param>
/// <param name="faultOn">The operation that faults.</param>
/// <param name="fault">A factory for the exception each faulted call raises.</param>
internal sealed class FaultingDistributedCache(
    IDistributedCache inner,
    FaultingDistributedCache.Operation faultOn,
    Func<Exception> fault)
    : IDistributedCache
{
    /// <summary>
    /// Identifies the operation that faults.
    /// </summary>
    public enum Operation
    {
        /// <summary>Reads fault.</summary>
        Get,

        /// <summary>Writes fault.</summary>
        Set,

        /// <summary>Removals fault.</summary>
        Remove,
    }

    /// <inheritdoc />
    public byte[]? Get(string key) =>
        Guard(Operation.Get) ? throw fault() : inner.Get(key);

    /// <inheritdoc />
    public Task<byte[]?> GetAsync(string key, CancellationToken token = default) =>
        Guard(Operation.Get) ? throw fault() : inner.GetAsync(key, token);

    /// <inheritdoc />
    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        if (Guard(Operation.Set))
            throw fault();

        inner.Set(key, value, options);
    }

    /// <inheritdoc />
    public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default) =>
        Guard(Operation.Set) ? throw fault() : inner.SetAsync(key, value, options, token);

    /// <inheritdoc />
    public void Refresh(string key) =>
        inner.Refresh(key);

    /// <inheritdoc />
    public Task RefreshAsync(string key, CancellationToken token = default) =>
        inner.RefreshAsync(key, token);

    /// <inheritdoc />
    public void Remove(string key)
    {
        if (Guard(Operation.Remove))
            throw fault();

        inner.Remove(key);
    }

    /// <inheritdoc />
    public Task RemoveAsync(string key, CancellationToken token = default) =>
        Guard(Operation.Remove) ? throw fault() : inner.RemoveAsync(key, token);

    /// <summary>
    /// Determines whether the specified operation is the faulted one.
    /// </summary>
    /// <param name="operation">The operation being attempted.</param>
    /// <returns><see langword="true" /> when the call should fault.</returns>
    private bool Guard(Operation operation) =>
        operation == faultOn;
}
