// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateCacheBaseLockingTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Verifies that the synchronous and asynchronous write paths of <see cref="RateCacheBase{TOptions}" /> share one
/// per-pair mutual-exclusion domain: same-pair writes serialize across the two surfaces while distinct pairs proceed
/// concurrently.
/// </summary>
[TestClass]
public sealed class RateCacheBaseLockingTests
{
    /// <summary>The fixed evaluation instant.</summary>
    private static readonly DateTimeOffset Now = new(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);

    /// <summary>The caching duration used by the tests.</summary>
    private static readonly TimeSpan Duration = TimeSpan.FromHours(24);

    /// <summary>The pair under test.</summary>
    private static readonly CurrencyPair Pair = new(CurrencyCode.AUD, CurrencyCode.USD);

    /// <summary>
    /// Verifies that an asynchronous store for a pair waits for a synchronous store of the same pair to release the
    /// per-pair lock, so the two surfaces cannot interleave a read-merge-write and lose rows.
    /// </summary>
    [TestMethod]
    public async Task StoreFetchedRangeAsync_WhenSyncStoreHoldsSamePairLock_ShouldWaitForRelease()
    {
        var cache = new GatedWriteRateCache("test");
        var date = new DateOnly(2026, 6, 1);

        // The sync store enters WriteState and blocks on the gate while holding the pair lock.
        Task syncStore = Task.Run(() => cache.Store(Pair, new[] { new CachedRate(date, 0.5m, Now) }, Duration, Now));
        Assert.IsTrue(cache.WriteEntered.Wait(TimeSpan.FromSeconds(30)), "the sync store must reach its write");

        // The async store for the same pair must queue on the shared lock rather than starting its write. Task.Run
        // keeps the call's synchronous prefix off the test thread, since a ValueTask executes synchronously until its
        // first genuine suspension.
        IRateCacheAsync seam = cache;
        Task<RateCacheWriteStatus> asyncStore = Task.Run(() => seam.StoreFetchedRangeAsync(
            Pair, new[] { new CachedRate(date.AddDays(1), 0.51m, Now) }, date.AddDays(1), date.AddDays(1), Duration, Now, CancellationToken.None).AsTask());

        Assert.IsFalse(asyncStore.Wait(TimeSpan.FromMilliseconds(250)), "the async store must not proceed while the sync store holds the pair lock");
        Assert.AreEqual(1, cache.WriteCount, "only the sync store may have entered its write");

        cache.ReleaseWrites();
        await syncStore;
        Assert.AreEqual(RateCacheWriteStatus.Stored, await asyncStore);
        Assert.AreEqual(2, cache.WriteCount, "both writes must complete after release");
        Assert.HasCount(2, cache.GetRates(Pair, Duration, Now), "no write may be lost across the two surfaces");
    }

    /// <summary>
    /// Verifies that writes for distinct pairs do not serialize against each other.
    /// </summary>
    [TestMethod]
    public async Task StoreFetchedRangeAsync_WhenDifferentPairHoldsLock_ShouldProceedConcurrently()
    {
        var cache = new GatedWriteRateCache("test");
        var other = new CurrencyPair(CurrencyCode.EUR, CurrencyCode.USD);
        var date = new DateOnly(2026, 6, 1);

        Task syncStore = Task.Run(() => cache.Store(Pair, new[] { new CachedRate(date, 0.5m, Now) }, Duration, Now));
        Assert.IsTrue(cache.WriteEntered.Wait(TimeSpan.FromSeconds(30)));

        // A different pair's async store must not queue behind the held lock. Its write blocks on the same gate, so
        // reaching WriteCount == 2 proves it entered concurrently. Task.Run keeps the call's synchronous prefix —
        // which runs all the way into the gated write when nothing suspends — off the test thread.
        IRateCacheAsync seam = cache;
        Task<RateCacheWriteStatus> asyncStore = Task.Run(() => seam.StoreFetchedRangeAsync(
            other, new[] { new CachedRate(date, 1.1m, Now) }, date, date, Duration, Now, CancellationToken.None).AsTask());

        Assert.IsTrue(SpinWait.SpinUntil(() => cache.WriteCount == 2, TimeSpan.FromSeconds(30)), "a distinct pair must write concurrently");

        cache.ReleaseWrites();
        await syncStore;
        Assert.AreEqual(RateCacheWriteStatus.Stored, await asyncStore);
    }

    /// <summary>
    /// Verifies under contention that interleaved synchronous and asynchronous same-pair stores never lose a row.
    /// </summary>
    [TestMethod]
    [TestCategory("Stress")]
    public async Task Store_WhenManyMixedSurfaceWritersContend_ShouldLoseNoRows()
    {
        var cache = new OpenWriteRateCache("test");
        IRateCacheAsync seam = cache;
        const int writers = 8;
        const int rowsPerWriter = 50;

        Task[] tasks = [.. Enumerable.Range(0, writers).Select(w => Task.Run(async () =>
        {
            for (int i = 0; i < rowsPerWriter; i++)
            {
                var date = new DateOnly(2026, 1, 1).AddDays((w * rowsPerWriter) + i);
                if (w % 2 == 0)
                    cache.Store(Pair, new[] { new CachedRate(date, 1m, Now) }, Duration, Now);
                else
                    await seam.StoreAsync(Pair, new[] { new CachedRate(date, 1m, Now) }, Duration, Now, CancellationToken.None);
            }
        }))];

        await Task.WhenAll(tasks);

        Assert.HasCount(writers * rowsPerWriter, cache.GetRates(Pair, Duration, Now), "every row from every surface must survive");
    }

    /// <summary>
    /// A cache whose writes block on a gate after entering, so a test can observe which writers hold the per-pair
    /// lock.
    /// </summary>
    private sealed class GatedWriteRateCache
        : RateCacheBase<RateCacheOptions>
    {
        /// <summary>The stored state per pair.</summary>
        private readonly Dictionary<CurrencyPair, CachePairState> _store = new();

        /// <summary>The gate every write blocks on after entering.</summary>
        private readonly ManualResetEventSlim _gate = new(initialState: false);

        /// <summary>The number of writes entered.</summary>
        private int _writeCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="GatedWriteRateCache" /> class.
        /// </summary>
        /// <param name="provider">The provider the cache is bound to.</param>
        public GatedWriteRateCache(string provider)
            : base(new RateCacheOptions { Provider = provider })
        {
        }

        /// <summary>Signalled when a write has entered and is blocked on the gate.</summary>
        /// <value>The write-entered event.</value>
        public ManualResetEventSlim WriteEntered { get; } = new(initialState: false);

        /// <summary>Gets the number of writes entered.</summary>
        /// <value>The write count.</value>
        public int WriteCount => Volatile.Read(ref _writeCount);

        /// <summary>Releases every blocked write.</summary>
        public void ReleaseWrites() => _gate.Set();

        /// <inheritdoc />
        internal override CachePairState ReadState(CurrencyPair pair)
        {
            lock (_store)
                return _store.TryGetValue(pair, out CachePairState? state) ? state : CachePairState.Empty;
        }

        /// <inheritdoc />
        internal override bool WriteState(CurrencyPair pair, CachePairState state)
        {
            Interlocked.Increment(ref _writeCount);
            WriteEntered.Set();
            _gate.Wait(TimeSpan.FromSeconds(30));

            lock (_store)
                _store[pair] = state;
            return true;
        }
    }

    /// <summary>
    /// A minimal in-memory cache derivation with unblocked writes, for the contention sweep.
    /// </summary>
    private sealed class OpenWriteRateCache
        : RateCacheBase<RateCacheOptions>
    {
        /// <summary>The stored state per pair.</summary>
        private readonly Dictionary<CurrencyPair, CachePairState> _store = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenWriteRateCache" /> class.
        /// </summary>
        /// <param name="provider">The provider the cache is bound to.</param>
        public OpenWriteRateCache(string provider)
            : base(new RateCacheOptions { Provider = provider })
        {
        }

        /// <inheritdoc />
        internal override CachePairState ReadState(CurrencyPair pair)
        {
            lock (_store)
                return _store.TryGetValue(pair, out CachePairState? state) ? state : CachePairState.Empty;
        }

        /// <inheritdoc />
        internal override bool WriteState(CurrencyPair pair, CachePairState state)
        {
            lock (_store)
                _store[pair] = state;
            return true;
        }
    }
}
