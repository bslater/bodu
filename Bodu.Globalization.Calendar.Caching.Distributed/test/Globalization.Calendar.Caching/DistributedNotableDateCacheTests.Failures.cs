// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DistributedNotableDateCacheTests.Failures.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Bodu.Globalization.Calendar.Caching;

/// <summary>
/// Verifies how <see cref="DistributedNotableDateCache" /> behaves when the backing store is unreachable or holds
/// unreadable content, and how <c>Clear</c> handles the keys it wrote.
/// </summary>
/// <remarks>
/// A distributed cache is a best-effort accelerator: an unreachable store should degrade a lookup to a miss rather
/// than fault the caller's calendar query. That is only true by default, though - the same faults are surfaced when
/// <c>ThrowOnStorageFailure</c> is set, which is the switch an operator flips when a silent miss would hide a real
/// outage. Both directions are asserted here, because a swallow that cannot be turned off is indistinguishable from a
/// bug.
/// </remarks>
public sealed partial class DistributedNotableDateCacheTests
{
    /// <summary>
    /// Creates a cache over an in-memory distributed store.
    /// </summary>
    /// <param name="store">Receives the backing store.</param>
    /// <param name="options">The cache options, or <see langword="null" /> for the defaults.</param>
    /// <returns>The cache.</returns>
    private static DistributedNotableDateCache CreateOverMemory(
        out IDistributedCache store,
        DistributedNotableDateCacheOptions? options = null)
    {
        store = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        return new DistributedNotableDateCache(store, options ?? new DistributedNotableDateCacheOptions());
    }

    /// <summary>
    /// Builds a single-year cache entry.
    /// </summary>
    /// <param name="territory">The territory code.</param>
    /// <returns>The entry.</returns>
    private static NotableDateCacheEntry Entry(string territory) =>
        new(territory, 2026, "v1", new[] { NotableDateCacheTestFactory.ObservedHoliday(2026) }, Now);

    /// <summary>
    /// Verifies that <see cref="DistributedNotableDateCache.Clear" /> removes every key the cache wrote, so a later
    /// read is a miss rather than a stale hit.
    /// </summary>
    [TestMethod]
    public void Clear_WhenKeysWereWritten_ShouldRemoveThemFromTheStore()
    {
        DistributedNotableDateCache cache = CreateOverMemory(out IDistributedCache store);
        _ = cache.StoreYear(Entry("US"), TimeSpan.FromDays(30), Now);
        _ = cache.StoreYear(Entry("CA"), TimeSpan.FromDays(30), Now);

        cache.Clear();

        Assert.IsNull(cache.GetYear("US", 2026, "v1", TimeSpan.FromDays(30), Now));
        Assert.IsNull(cache.GetYear("CA", 2026, "v1", TimeSpan.FromDays(30), Now));
        _ = store;
    }

    /// <summary>
    /// Verifies that clearing a cache that has written nothing is a no-op rather than an error.
    /// </summary>
    [TestMethod]
    public void Clear_WhenNothingWasWritten_ShouldSucceed()
    {
        DistributedNotableDateCache cache = CreateOverMemory(out _);

        cache.Clear();
    }

    /// <summary>
    /// Verifies that an unreachable store degrades <c>Clear</c> to a no-op by default, so a shutdown path does not
    /// fault on a cache that was already unavailable.
    /// </summary>
    [TestMethod]
    public void Clear_WhenStoreIsUnreachable_ShouldSwallowByDefault()
    {
        var cache = new DistributedNotableDateCache(new ThrowingDistributedCache(), new DistributedNotableDateCacheOptions());
        _ = cache.StoreYear(Entry("US"), TimeSpan.FromDays(30), Now);

        cache.Clear();
    }

    /// <summary>
    /// Verifies that an unreachable store degrades a read to a miss by default, so a calendar query still answers
    /// from the source rather than faulting.
    /// </summary>
    [TestMethod]
    public void GetYear_WhenStoreIsUnreachable_ShouldReportAMissByDefault()
    {
        var cache = new DistributedNotableDateCache(new ThrowingDistributedCache(), new DistributedNotableDateCacheOptions());

        Assert.IsNull(cache.GetYear("US", 2026, "v1", TimeSpan.FromDays(30), Now));
    }

    /// <summary>
    /// Verifies that an unreachable store is surfaced on read when the operator has opted into strict failures.
    /// </summary>
    [TestMethod]
    public void GetYear_WhenStoreIsUnreachable_ForThrowOnStorageFailure_ShouldThrow()
    {
        var cache = new DistributedNotableDateCache(
            new ThrowingDistributedCache(),
            new DistributedNotableDateCacheOptions { ThrowOnStorageFailure = true });

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = cache.GetYear("US", 2026, "v1", TimeSpan.FromDays(30), Now);
        });
    }

    /// <summary>
    /// Verifies that a write to an unreachable store reports that nothing was persisted, rather than claiming
    /// success.
    /// </summary>
    [TestMethod]
    public void StoreYear_WhenStoreIsUnreachable_ShouldReportNotStored()
    {
        var cache = new DistributedNotableDateCache(new ThrowingDistributedCache(), new DistributedNotableDateCacheOptions());

        Assert.AreNotEqual(
            NotableDateCacheWriteStatus.Stored,
            cache.StoreYear(Entry("US"), TimeSpan.FromDays(30), Now));
    }

    /// <summary>
    /// Verifies that a blob the cache cannot deserialize is treated as a miss rather than propagating a JSON fault -
    /// content written by a different version of the schema must not make the cache unusable.
    /// </summary>
    [TestMethod]
    public void GetYear_WhenStoredBlobIsNotValidJson_ShouldReportAMiss()
    {
        var options = new DistributedNotableDateCacheOptions();
        DistributedNotableDateCache cache = CreateOverMemory(out IDistributedCache store, options);
        store.Set(options.BuildKey("US"), "not json"u8.ToArray());

        Assert.IsNull(cache.GetYear("US", 2026, "v1", TimeSpan.FromDays(30), Now));
    }

    /// <summary>
    /// Verifies that a corrupt blob is still treated as a miss when strict failures are enabled, because a
    /// deserialization fault is a content problem rather than a store outage - the distinction the read path draws
    /// between a <c>JsonException</c> and everything else.
    /// </summary>
    [TestMethod]
    public void GetYear_WhenStoredBlobIsNotValidJson_ForThrowOnStorageFailure_ShouldStillReportAMiss()
    {
        var options = new DistributedNotableDateCacheOptions { ThrowOnStorageFailure = true };
        DistributedNotableDateCache cache = CreateOverMemory(out IDistributedCache store, options);
        store.Set(options.BuildKey("US"), "not json"u8.ToArray());

        Assert.IsNull(cache.GetYear("US", 2026, "v1", TimeSpan.FromDays(30), Now));
    }

    /// <summary>
    /// Verifies that an empty stored blob is treated as a miss, which is how a removed key surfaces on stores that
    /// return an empty array rather than <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void GetYear_WhenStoredBlobIsEmpty_ShouldReportAMiss()
    {
        var options = new DistributedNotableDateCacheOptions();
        DistributedNotableDateCache cache = CreateOverMemory(out IDistributedCache store, options);
        store.Set(options.BuildKey("US"), Array.Empty<byte>());

        Assert.IsNull(cache.GetYear("US", 2026, "v1", TimeSpan.FromDays(30), Now));
    }

    /// <summary>
    /// Verifies that a stored territory blob is present in the backing store under the key the options build, so a
    /// second process configured the same way finds it.
    /// </summary>
    [TestMethod]
    public void StoreYear_WhenPersisted_ShouldWriteUnderTheConfiguredKey()
    {
        var options = new DistributedNotableDateCacheOptions();
        DistributedNotableDateCache cache = CreateOverMemory(out IDistributedCache store, options);

        _ = cache.StoreYear(Entry("US"), TimeSpan.FromDays(30), Now);

        Assert.IsNotNull(store.Get(options.BuildKey("US")));
    }

    /// <summary>
    /// Verifies that a removal failure part-way through <c>Clear</c> is swallowed and the remaining keys are still
    /// attempted, so one unremovable key does not strand the rest.
    /// </summary>
    /// <remarks>
    /// The clear loop iterates the keys a successful write recorded, so a store that could never write anything
    /// never enters it - which is why this needs a store that serves writes and fails only removals.
    /// </remarks>
    [TestMethod]
    public void Clear_WhenRemovalFails_ShouldSwallowAndContinue()
    {
        IDistributedCache inner = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        var store = new FaultingDistributedCache(
            inner,
            FaultingDistributedCache.Operation.Remove,
            () => new InvalidOperationException("remove failed"));

        var cache = new DistributedNotableDateCache(store, new DistributedNotableDateCacheOptions());
        _ = cache.StoreYear(Entry("US"), TimeSpan.FromDays(30), Now);
        _ = cache.StoreYear(Entry("CA"), TimeSpan.FromDays(30), Now);

        cache.Clear();

        // Both blobs are still present: the removals failed, and that is reported rather than thrown.
        Assert.IsNotNull(inner.Get(new DistributedNotableDateCacheOptions().BuildKey("US")));
        Assert.IsNotNull(inner.Get(new DistributedNotableDateCacheOptions().BuildKey("CA")));
    }

    /// <summary>
    /// Verifies that a removal failure during <c>Clear</c> is surfaced when strict failures are enabled.
    /// </summary>
    [TestMethod]
    public void Clear_WhenRemovalFails_ForThrowOnStorageFailure_ShouldThrow()
    {
        IDistributedCache inner = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        var store = new FaultingDistributedCache(
            inner,
            FaultingDistributedCache.Operation.Remove,
            () => new InvalidOperationException("remove failed"));

        var cache = new DistributedNotableDateCache(store, new DistributedNotableDateCacheOptions { ThrowOnStorageFailure = true });
        _ = cache.StoreYear(Entry("US"), TimeSpan.FromDays(30), Now);

        _ = Assert.ThrowsExactly<InvalidOperationException>(cache.Clear);
    }

    /// <summary>
    /// Verifies that a cancellation raised by the backing store propagates rather than being swallowed as a storage
    /// failure, on every operation.
    /// </summary>
    /// <remarks>
    /// Cancellation is the caller's instruction, not the store's failure. Reporting it as a cache miss would let a
    /// cancelled request quietly fall through to the source and do the very work the cancellation was meant to stop,
    /// which is why each path rethrows it ahead of its swallow clause.
    /// </remarks>
    /// <param name="operation">The operation the store faults on, named rather than typed so the test method's
    /// signature does not expose the internal stand-in's enumeration.</param>
    [TestMethod]
    [DataRow("Get")]
    [DataRow("Set")]
    [DataRow("Remove")]
    public void Operations_WhenStoreRaisesCancellation_ShouldPropagate(string operation)
    {
        var faultOn = Enum.Parse<FaultingDistributedCache.Operation>(operation);

        IDistributedCache inner = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        var store = new FaultingDistributedCache(inner, faultOn, () => new OperationCanceledException());
        var cache = new DistributedNotableDateCache(store, new DistributedNotableDateCacheOptions());

        _ = Assert.ThrowsExactly<OperationCanceledException>(() =>
        {
            switch (faultOn)
            {
                case FaultingDistributedCache.Operation.Get:
                    _ = cache.GetYear("US", 2026, "v1", TimeSpan.FromDays(30), Now);
                    break;

                case FaultingDistributedCache.Operation.Set:
                    _ = cache.StoreYear(Entry("US"), TimeSpan.FromDays(30), Now);
                    break;

                default:
                    _ = cache.StoreYear(Entry("US"), TimeSpan.FromDays(30), Now);
                    cache.Clear();
                    break;
            }
        });
    }
}
