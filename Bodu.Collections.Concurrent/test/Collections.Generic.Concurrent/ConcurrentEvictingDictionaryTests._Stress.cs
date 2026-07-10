// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentEvictingDictionaryTests._Stress.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentEvictingDictionaryTests
{
    /// <summary>
    /// Starts a stress worker on a dedicated long-running thread so the thread pool cannot starve it of execution
    /// while sibling workers run CPU-bound hot loops.
    /// </summary>
    private static Task StartWorker(Action action) =>
        Task.Factory.StartNew(action, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);

    /// <summary>
    /// Verifies that under mixed add, read, touch, and remove churn from many workers, no operation faults and the
    /// dictionary never stores more entries than its capacity.
    /// </summary>
    [TestMethod]
    [TestCategory("Stress")]
    public void Stress_WhenMixedChurnRuns_ShouldNeverExceedCapacityOrFault()
    {
        const int workerCount = 8;
        const int operationsPerWorker = 50_000;
        const int capacity = 128;
        const int keySpace = 512;

        var dictionary = new ConcurrentEvictingDictionary<int, int>(capacity);
        using var startGate = new ManualResetEventSlim(false);

        int faults = 0;
        int capacityViolations = 0;
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
                                dictionary.Add(key, i);
                                break;

                            case < 7:
                                _ = dictionary.TryGetValue(key, out _);
                                break;

                            case < 8:
                                _ = dictionary.Touch(key);
                                break;

                            case < 9:
                                _ = dictionary.TryRemove(key, out _);
                                break;

                            default:
                                if (dictionary.ApproximateCount > capacity)
                                    Interlocked.Increment(ref capacityViolations);
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

        int finalCount = dictionary.Count;
        TestContext.WriteLine(
            $"Faults={faults}, CapacityViolations={capacityViolations}, FinalCount={finalCount}, " +
            $"Evictions={dictionary.EvictionCount}, Touches={dictionary.TotalTouches}, Completed={completed}, FirstException={firstException}");

        Assert.IsTrue(completed, "Workers did not complete within the deadlock timeout.");
        Assert.AreEqual(0, faults, $"No exception is expected. First exception: {firstException}");
        Assert.AreEqual(0, capacityViolations, "The stored count must never exceed the capacity.");
        Assert.IsTrue(finalCount <= capacity, $"Final count {finalCount} exceeded capacity {capacity}.");
    }

    /// <summary>
    /// Verifies that when many workers race factory-based GetOrAdd calls over a shared key space, each key's factory
    /// runs exactly once per generation and every caller observes a value produced by a factory.
    /// </summary>
    [TestMethod]
    [TestCategory("Stress")]
    public void Stress_WhenGetOrAddRaces_ShouldInvokeFactoryExactlyOncePerKey()
    {
        const int workerCount = 8;
        const int keyCount = 64;
        const int roundsPerWorker = 500;

        // Capacity covers the whole key space, so no factory result is ever evicted and re-created.
        var dictionary = new ConcurrentEvictingDictionary<int, int>(capacity: keyCount);
        using var startGate = new ManualResetEventSlim(false);

        int[] factoryInvocations = new int[keyCount];
        int faults = 0;
        int wrongValues = 0;
        Exception? firstException = null;

        Task[] tasks = Enumerable.Range(0, workerCount).Select(workerId =>
            StartWorker(() =>
            {
                startGate.Wait();
                var random = new Random(workerId * 104729 + 3);

                try
                {
                    for (int round = 0; round < roundsPerWorker; round++)
                    {
                        int key = random.Next(keyCount);
                        int value = dictionary.GetOrAdd(key, k =>
                        {
                            Interlocked.Increment(ref factoryInvocations[k]);
                            return k * 1000;
                        });

                        if (value != key * 1000)
                            Interlocked.Increment(ref wrongValues);
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

        int multipleInvocations = factoryInvocations.Count(count => count > 1);
        TestContext.WriteLine(
            $"Faults={faults}, WrongValues={wrongValues}, MultipleInvocations={multipleInvocations}, " +
            $"Completed={completed}, FirstException={firstException}");

        Assert.IsTrue(completed, "Workers did not complete within the deadlock timeout.");
        Assert.AreEqual(0, faults, $"No exception is expected. First exception: {firstException}");
        Assert.AreEqual(0, wrongValues, "Every caller must observe a factory-produced value.");
        Assert.AreEqual(0, multipleInvocations, "The single-flight guarantee requires at most one factory invocation per key.");
    }

    /// <summary>
    /// Verifies that under sustained overflow pressure every logical admission beyond the capacity produces exactly
    /// one eviction notification, so admissions and evictions reconcile with the final count.
    /// </summary>
    [TestMethod]
    [TestCategory("Stress")]
    public void Stress_WhenOverflowPressureSustained_ShouldReconcileEvictionsWithAdmissions()
    {
        const int workerCount = 4;
        const int addsPerWorker = 25_000;
        const int capacity = 64;

        var dictionary = new ConcurrentEvictingDictionary<long, int>(capacity, EvictingDictionaryPolicy.FirstInFirstOut);
        using var startGate = new ManualResetEventSlim(false);

        long evictionCallbacks = 0;
        int faults = 0;
        Exception? firstException = null;
        dictionary.ItemEvicted += (_, _) => Interlocked.Increment(ref evictionCallbacks);

        Task[] tasks = Enumerable.Range(0, workerCount).Select(workerId =>
            StartWorker(() =>
            {
                startGate.Wait();

                try
                {
                    // Every worker adds a disjoint, strictly increasing key range, so every add is a new key.
                    long baseKey = workerId * (long)addsPerWorker;
                    for (int i = 0; i < addsPerWorker; i++)
                        dictionary.Add(baseKey + i, i);
                }
                catch (Exception ex)
                {
                    Interlocked.CompareExchange(ref firstException, ex, null);
                    Interlocked.Increment(ref faults);
                }
            })).ToArray();

        startGate.Set();
        bool completed = Task.WaitAll(tasks, 60_000);

        long totalAdds = workerCount * (long)addsPerWorker;
        int finalCount = dictionary.Count;
        TestContext.WriteLine(
            $"Faults={faults}, TotalAdds={totalAdds}, FinalCount={finalCount}, EvictionCount={dictionary.EvictionCount}, " +
            $"EvictionCallbacks={evictionCallbacks}, Completed={completed}, FirstException={firstException}");

        Assert.IsTrue(completed, "Workers did not complete within the deadlock timeout.");
        Assert.AreEqual(0, faults, $"No exception is expected. First exception: {firstException}");
        Assert.AreEqual(totalAdds - finalCount, dictionary.EvictionCount, "Every admission beyond the final count must be one eviction.");
        Assert.AreEqual(dictionary.EvictionCount, evictionCallbacks, "Every eviction must produce exactly one event callback.");
        Assert.IsTrue(finalCount <= capacity, $"Final count {finalCount} exceeded capacity {capacity}.");
    }

    /// <summary>
    /// Verifies that snapshot reads (enumeration, ToArray, Count) stay coherent while writers churn: every observed
    /// snapshot length is bounded by the capacity and no snapshot read throws.
    /// </summary>
    [TestMethod]
    [TestCategory("Stress")]
    public void Stress_WhenSnapshotsRaceWriters_ShouldStayCoherentAndBounded()
    {
        const int writerCount = 4;
        const int capacity = 96;
        const int durationMs = 2_000;

        var dictionary = new ConcurrentEvictingDictionary<int, int>(capacity);
        using var startGate = new ManualResetEventSlim(false);
        using var cancellation = new CancellationTokenSource(durationMs);

        int faults = 0;
        int oversizedSnapshots = 0;
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
                        int key = random.Next(1024);
                        dictionary.Add(key, key);
                        _ = dictionary.TryRemove(random.Next(1024), out _);
                    }
                }
                catch (Exception ex)
                {
                    Interlocked.CompareExchange(ref firstException, ex, null);
                    Interlocked.Increment(ref faults);
                }
            })).ToArray();

        Task reader = StartWorker(() =>
        {
            startGate.Wait();

            try
            {
                while (!cancellation.IsCancellationRequested)
                {
                    KeyValuePair<int, int>[] snapshot = dictionary.ToArray();
                    if (snapshot.Length > capacity)
                        Interlocked.Increment(ref oversizedSnapshots);

                    int enumerated = 0;
                    foreach (KeyValuePair<int, int> pair in dictionary)
                        enumerated++;

                    if (enumerated > capacity)
                        Interlocked.Increment(ref oversizedSnapshots);

                    if (dictionary.Count > capacity)
                        Interlocked.Increment(ref oversizedSnapshots);
                }
            }
            catch (Exception ex)
            {
                Interlocked.CompareExchange(ref firstException, ex, null);
                Interlocked.Increment(ref faults);
            }
        });

        startGate.Set();
        bool completed = Task.WaitAll([.. writers, reader], 60_000);

        TestContext.WriteLine(
            $"Faults={faults}, OversizedSnapshots={oversizedSnapshots}, FinalCount={dictionary.Count}, " +
            $"Completed={completed}, FirstException={firstException}");

        Assert.IsTrue(completed, "Workers did not complete within the deadlock timeout.");
        Assert.AreEqual(0, faults, $"No exception is expected. First exception: {firstException}");
        Assert.AreEqual(0, oversizedSnapshots, "No snapshot may ever observe more than Capacity entries.");
    }

    /// <summary>
    /// Verifies that concurrent time-to-live churn — adds with short lifetimes racing explicit purges and reads — never
    /// faults and converges to an empty dictionary once everything has expired and been purged.
    /// </summary>
    [TestMethod]
    [TestCategory("Stress")]
    public void Stress_WhenTtlChurnRuns_ShouldNeverFaultAndConvergeToEmpty()
    {
        const int workerCount = 4;
        const int operationsPerWorker = 20_000;

        var expiration = new EvictingDictionaryExpiration(TimeSpan.FromMilliseconds(5));
        var dictionary = new ConcurrentEvictingDictionary<int, int>(capacity: 256, expiration);
        using var startGate = new ManualResetEventSlim(false);

        int faults = 0;
        Exception? firstException = null;

        Task[] tasks = Enumerable.Range(0, workerCount).Select(workerId =>
            StartWorker(() =>
            {
                startGate.Wait();
                var random = new Random(workerId * 24593 + 29);

                try
                {
                    for (int i = 0; i < operationsPerWorker; i++)
                    {
                        int key = random.Next(512);
                        switch (random.Next(4))
                        {
                            case 0:
                                dictionary.Add(key, i);
                                break;

                            case 1:
                                _ = dictionary.TryGetValue(key, out _);
                                break;

                            case 2:
                                _ = dictionary.TryAdd(key, i, TimeSpan.FromMilliseconds(1));
                                break;

                            default:
                                _ = dictionary.RemoveExpired();
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

        // Everything written carries a millisecond-scale TTL; after the workers stop, one settling purge must
        // reconcile the store to empty.
        Thread.Sleep(50);
        _ = dictionary.RemoveExpired();

        TestContext.WriteLine(
            $"Faults={faults}, FinalCount={dictionary.Count}, Evictions={dictionary.EvictionCount}, " +
            $"Completed={completed}, FirstException={firstException}");

        Assert.IsTrue(completed, "Workers did not complete within the deadlock timeout.");
        Assert.AreEqual(0, faults, $"No exception is expected. First exception: {firstException}");
        Assert.AreEqual(0, dictionary.Count, "Every TTL-carrying entry must have expired and been purged.");
        Assert.IsTrue(dictionary.IsEmpty);
    }
}
