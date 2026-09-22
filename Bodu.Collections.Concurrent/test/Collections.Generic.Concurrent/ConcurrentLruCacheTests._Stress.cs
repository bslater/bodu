// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentLruCacheTests._Stress.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentLruCacheTests
{
    /// <summary>
    /// Starts a stress worker on a dedicated long-running thread so the thread pool cannot starve it of execution
    /// while sibling workers run CPU-bound hot loops.
    /// </summary>
    private static Task StartWorker(Action action) =>
        Task.Factory.StartNew(action, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);

    /// <summary>
    /// Verifies that under mixed add, read, and remove churn from many workers, no operation faults, the live count
    /// never exceeds the documented transient bound (capacity plus in-flight writers), and a final converged
    /// maintenance run brings the count within the capacity.
    /// </summary>
    [TestMethod]
    [TestCategory("Stress")]
    public void Stress_WhenMixedChurnRuns_ShouldHonorTransientCapacityBoundAndConverge()
    {
        const int workerCount = 8;
        const int operationsPerWorker = 50_000;
        const int capacity = 128;
        const int keySpace = 512;

        var cache = new ConcurrentLruCache<int, int>(capacity);
        using var startGate = new ManualResetEventSlim(false);

        int faults = 0;
        int boundViolations = 0;
        Exception? firstException = null;

        Task[] tasks = Enumerable.Range(0, workerCount).Select(workerId =>
            StartWorker(() =>
            {
                startGate.Wait();
                var random = new Random(workerId * 7919 + 17);

                try
                {
                    for (int i = 0; i < operationsPerWorker; i++)
                    {
                        int key = random.Next(keySpace);
                        switch (random.Next(10))
                        {
                            case < 4:
                                cache.Add(key, i);
                                break;

                            case < 8:
                                _ = cache.TryGetValue(key, out _);
                                break;

                            case < 9:
                                _ = cache.TryRemove(key, out _);
                                break;

                            default:
                                if (cache.Count > capacity + workerCount)
                                    Interlocked.Increment(ref boundViolations);
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Interlocked.CompareExchange(ref firstException, ex, null);
                    Interlocked.Increment(ref faults);
                }
            })).ToArray();

        startGate.Set();
        bool completed = Task.WaitAll(tasks, 60_000);

        cache.RunMaintenance();
        int convergedCount = cache.Count;

        TestContext.WriteLine(
            $"Faults={faults}, BoundViolations={boundViolations}, ConvergedCount={convergedCount}, " +
            $"Evictions={cache.EvictionCount}, HitRatio={cache.HitRatio:F3}, Completed={completed}, FirstException={firstException}");

        Assert.IsTrue(completed, "Workers did not complete within the deadlock timeout.");
        Assert.AreEqual(0, faults, $"No exception is expected. First exception: {firstException}");
        Assert.AreEqual(0, boundViolations, "The live count must never exceed capacity plus the number of in-flight writers.");
        Assert.IsTrue(convergedCount <= capacity, $"Converged count {convergedCount} exceeded capacity {capacity}.");
    }

    /// <summary>
    /// Verifies that under sustained unique-key overflow pressure every admission beyond the survivors produces
    /// exactly one eviction notification, so admissions, evictions, and the converged count reconcile exactly.
    /// </summary>
    [TestMethod]
    [TestCategory("Stress")]
    public void Stress_WhenOverflowPressureSustained_ShouldReconcileEvictionsWithAdmissions()
    {
        const int workerCount = 4;
        const int addsPerWorker = 25_000;
        const int capacity = 64;

        var cache = new ConcurrentLruCache<long, int>(capacity);
        using var startGate = new ManualResetEventSlim(false);

        long evictionCallbacks = 0;
        int faults = 0;
        Exception? firstException = null;
        cache.ItemEvicted += (_, _) => Interlocked.Increment(ref evictionCallbacks);

        Task[] tasks = Enumerable.Range(0, workerCount).Select(workerId =>
            StartWorker(() =>
            {
                startGate.Wait();

                try
                {
                    // Every worker adds a disjoint, strictly increasing key range, so every add is a new key.
                    long baseKey = workerId * (long)addsPerWorker;
                    for (int i = 0; i < addsPerWorker; i++)
                        cache.Add(baseKey + i, i);
                }
                catch (Exception ex)
                {
                    Interlocked.CompareExchange(ref firstException, ex, null);
                    Interlocked.Increment(ref faults);
                }
            })).ToArray();

        startGate.Set();
        bool completed = Task.WaitAll(tasks, 60_000);

        cache.RunMaintenance();

        long totalAdds = workerCount * (long)addsPerWorker;
        int finalCount = cache.Count;
        TestContext.WriteLine(
            $"Faults={faults}, TotalAdds={totalAdds}, FinalCount={finalCount}, EvictionCount={cache.EvictionCount}, " +
            $"EvictionCallbacks={evictionCallbacks}, Completed={completed}, FirstException={firstException}");

        Assert.IsTrue(completed, "Workers did not complete within the deadlock timeout.");
        Assert.AreEqual(0, faults, $"No exception is expected. First exception: {firstException}");
        Assert.AreEqual(totalAdds - finalCount, cache.EvictionCount, "Every admission beyond the survivors must be exactly one eviction.");
        Assert.AreEqual(cache.EvictionCount, evictionCallbacks, "Every eviction must produce exactly one event callback.");
        Assert.IsTrue(finalCount <= capacity, $"Converged count {finalCount} exceeded capacity {capacity}.");
    }

    /// <summary>
    /// Verifies that lock-free readers racing writers never observe a torn or foreign value: every hit returns the
    /// value that was stored for that exact key.
    /// </summary>
    [TestMethod]
    [TestCategory("Stress")]
    public void Stress_WhenReadersRaceWriters_ShouldNeverObserveForeignValues()
    {
        const int readerCount = 6;
        const int writerCount = 2;
        const int keySpace = 256;
        const int durationMs = 2_000;

        var cache = new ConcurrentLruCache<int, long>(capacity: 128);
        using var startGate = new ManualResetEventSlim(false);
        using var cancellation = new CancellationTokenSource(durationMs);

        int faults = 0;
        int foreignValues = 0;
        Exception? firstException = null;

        Task[] writers = Enumerable.Range(0, writerCount).Select(workerId =>
            StartWorker(() =>
            {
                startGate.Wait();
                var random = new Random(workerId * 6151 + 11);

                try
                {
                    while (!cancellation.IsCancellationRequested)
                    {
                        int key = random.Next(keySpace);

                        // The stored value encodes its key, so any cross-key leak is detectable by readers.
                        cache.Add(key, ((long)key << 32) | (uint)random.Next());
                    }
                }
                catch (Exception ex)
                {
                    Interlocked.CompareExchange(ref firstException, ex, null);
                    Interlocked.Increment(ref faults);
                }
            })).ToArray();

        Task[] readers = Enumerable.Range(0, readerCount).Select(workerId =>
            StartWorker(() =>
            {
                startGate.Wait();
                var random = new Random(workerId * 24593 + 29);

                try
                {
                    while (!cancellation.IsCancellationRequested)
                    {
                        int key = random.Next(keySpace);
                        if (cache.TryGetValue(key, out long value) && (int)(value >> 32) != key)
                            Interlocked.Increment(ref foreignValues);
                    }
                }
                catch (Exception ex)
                {
                    Interlocked.CompareExchange(ref firstException, ex, null);
                    Interlocked.Increment(ref faults);
                }
            })).ToArray();

        startGate.Set();
        bool completed = Task.WaitAll([.. writers, .. readers], 60_000);

        TestContext.WriteLine(
            $"Faults={faults}, ForeignValues={foreignValues}, Hits={cache.HitCount}, Misses={cache.MissCount}, " +
            $"Completed={completed}, FirstException={firstException}");

        Assert.IsTrue(completed, "Workers did not complete within the deadlock timeout.");
        Assert.AreEqual(0, faults, $"No exception is expected. First exception: {firstException}");
        Assert.AreEqual(0, foreignValues, "A hit must always return the value stored for that exact key.");
    }

    /// <summary>
    /// Verifies that telemetry counters lose no increments while readers and writers churn concurrently: total
    /// lookups reconcile exactly with the sum of hits and misses.
    /// </summary>
    [TestMethod]
    [TestCategory("Stress")]
    public void Stress_WhenTelemetryRacesChurn_ShouldReconcileLookupTotals()
    {
        const int workerCount = 8;
        const int lookupsPerWorker = 100_000;

        var cache = new ConcurrentLruCache<int, int>(capacity: 96);
        using var startGate = new ManualResetEventSlim(false);

        int faults = 0;
        Exception? firstException = null;

        Task[] tasks = Enumerable.Range(0, workerCount).Select(workerId =>
            StartWorker(() =>
            {
                startGate.Wait();
                var random = new Random(workerId * 104729 + 3);

                try
                {
                    for (int i = 0; i < lookupsPerWorker; i++)
                    {
                        int key = random.Next(512);
                        if (!cache.TryGetValue(key, out _) && (i & 7) == 0)
                            cache.Add(key, i);
                    }
                }
                catch (Exception ex)
                {
                    Interlocked.CompareExchange(ref firstException, ex, null);
                    Interlocked.Increment(ref faults);
                }
            })).ToArray();

        startGate.Set();
        bool completed = Task.WaitAll(tasks, 60_000);

        long totalLookups = workerCount * (long)lookupsPerWorker;
        TestContext.WriteLine(
            $"Faults={faults}, Hits={cache.HitCount}, Misses={cache.MissCount}, HitRatio={cache.HitRatio:F3}, " +
            $"Completed={completed}, FirstException={firstException}");

        Assert.IsTrue(completed, "Workers did not complete within the deadlock timeout.");
        Assert.AreEqual(0, faults, $"No exception is expected. First exception: {firstException}");
        Assert.AreEqual(totalLookups, cache.HitCount + cache.MissCount, "Every lookup must land in exactly one striped counter.");
    }
}
