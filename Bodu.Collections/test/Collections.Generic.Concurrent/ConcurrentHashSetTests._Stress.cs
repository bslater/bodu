// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentHashSetTests._Stress.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentHashSetTests
{
    /// <summary>
    /// Starts a stress worker on a dedicated long-running thread so the thread pool cannot starve it of execution
    /// while sibling workers run CPU-bound hot loops.
    /// </summary>
    private static Task StartWorker(Action action) =>
        Task.Factory.StartNew(action, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);

    /// <summary>
    /// Verifies that when every worker owns a disjoint set of keys, no uncontended add or remove ever fails and the
    /// set is empty once every worker finishes.
    /// </summary>
    [TestMethod]
    [TestCategory("Stress")]
    public void Stress_WhenWorkersOwnDisjointKeys_ShouldNeverFailUncontendedOperations()
    {
        const int workerCount = 8;
        const int keysPerWorker = 250;
        const int iterations = 40;

        var set = new ConcurrentHashSet<int>();
        using var startGate = new ManualResetEventSlim(false);

        int addFailures = 0;
        int removeFailures = 0;
        int faults = 0;
        Exception? firstException = null;

        Task[] tasks = Enumerable.Range(0, workerCount).Select(workerId =>
            StartWorker(() =>
            {
                startGate.Wait();
                int baseKey = workerId * keysPerWorker;

                try
                {
                    for (int iteration = 0; iteration < iterations; iteration++)
                    {
                        for (int k = 0; k < keysPerWorker; k++)
                        {
                            if (!set.Add(baseKey + k))
                                Interlocked.Increment(ref addFailures);
                        }

                        for (int k = 0; k < keysPerWorker; k++)
                        {
                            if (!set.Remove(baseKey + k))
                                Interlocked.Increment(ref removeFailures);
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

        TestContext.WriteLine(
            $"AddFailures={addFailures}, RemoveFailures={removeFailures}, Faults={faults}, " +
            $"FinalCount={set.Count}, Completed={completed}, FirstException={firstException}");

        Assert.IsTrue(completed, "Workers did not complete within the deadlock timeout.");
        Assert.AreEqual(0, faults, $"No exception is expected. First exception: {firstException}");
        Assert.AreEqual(0, addFailures, "Every add of a key owned exclusively by one worker must succeed.");
        Assert.AreEqual(0, removeFailures, "Every remove of a key owned exclusively by one worker must succeed.");
        Assert.AreEqual(0, set.Count, "The set must be empty after every worker removes its own keys.");
    }

    /// <summary>
    /// Verifies that when many workers race to add the same keys, each key is added by exactly one worker.
    /// </summary>
    [TestMethod]
    [TestCategory("Stress")]
    public void Stress_WhenWorkersRaceToAddSameKeys_ShouldYieldExactlyOneWinnerPerKey()
    {
        const int workerCount = 16;
        const int keyCount = 750;

        var set = new ConcurrentHashSet<int>();
        using var startGate = new ManualResetEventSlim(false);

        int winners = 0;
        int faults = 0;
        Exception? firstException = null;

        Task[] tasks = Enumerable.Range(0, workerCount).Select(_ =>
            StartWorker(() =>
            {
                startGate.Wait();

                try
                {
                    for (int k = 0; k < keyCount; k++)
                    {
                        if (set.Add(k))
                            Interlocked.Increment(ref winners);
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

        TestContext.WriteLine(
            $"Winners={winners}, Faults={faults}, Count={set.Count}, Completed={completed}, " +
            $"FirstException={firstException}");

        Assert.IsTrue(completed, "Workers did not complete within the deadlock timeout.");
        Assert.AreEqual(0, faults, $"No exception is expected. First exception: {firstException}");
        Assert.AreEqual(keyCount, winners, "Each key must be added successfully by exactly one worker.");
        Assert.AreEqual(keyCount, set.Count, "The set must contain every contested key exactly once.");
    }

    /// <summary>
    /// Verifies that concurrent add, remove, and contains operations never throw and leave the set in a self-consistent
    /// state once the workers quiesce.
    /// </summary>
    [TestMethod]
    [TestCategory("Stress")]
    public void Stress_WhenAddRemoveContainsRunConcurrently_ShouldRemainConsistent()
    {
        const int keySpace = 1000;
        const int workerCount = 12;
        const int durationMs = 2_000;

        var set = new ConcurrentHashSet<int>();
        using var cts = new CancellationTokenSource();
        using var startGate = new ManualResetEventSlim(false);

        int faults = 0;
        int operations = 0;
        int seed = 0;
        Exception? firstException = null;

        Task[] tasks = Enumerable.Range(0, workerCount).Select(_ =>
            StartWorker(() =>
            {
                startGate.Wait();
                var random = new Random(Interlocked.Increment(ref seed));

                try
                {
                    while (!cts.Token.IsCancellationRequested)
                    {
                        int key = random.Next(keySpace);
                        switch (random.Next(3))
                        {
                            case 0:
                                set.Add(key);
                                break;
                            case 1:
                                set.Remove(key);
                                break;
                            default:
                                set.Contains(key);
                                break;
                        }

                        Interlocked.Increment(ref operations);
                    }
                }
                catch (Exception ex)
                {
                    Interlocked.CompareExchange(ref firstException, ex, null);
                    Interlocked.Increment(ref faults);
                    cts.Cancel();
                }
            })).ToArray();

        startGate.Set();
        Thread.Sleep(durationMs);
        cts.Cancel();
        bool completed = Task.WaitAll(tasks, 60_000);

        int finalCount = set.Count;
        int[] snapshot = set.ToArray();

        TestContext.WriteLine(
            $"Operations={operations}, Faults={faults}, FinalCount={finalCount}, " +
            $"SnapshotLength={snapshot.Length}, Completed={completed}, FirstException={firstException}");

        Assert.IsTrue(completed, "Workers did not complete within the deadlock timeout.");
        Assert.AreEqual(0, faults, $"No exception is expected. First exception: {firstException}");
        Assert.IsGreaterThan(0, operations, "Workers must have exercised the set during the stress run.");
        Assert.IsTrue(finalCount is >= 0 and <= keySpace, "Count must remain within the legal key range.");
        Assert.HasCount(finalCount, snapshot, "Count and ToArray().Length must agree once the workers quiesce.");
    }

    /// <summary>
    /// Verifies that snapshots taken via <see cref="ConcurrentHashSet{T}.ToArray" /> during concurrent mutation always
    /// contain only distinct, legal elements.
    /// </summary>
    [TestMethod]
    [TestCategory("Stress")]
    public void Stress_WhenSnapshotTakenDuringChurn_ShouldAlwaysBeCoherent()
    {
        const int keySpace = 600;
        const int durationMs = 2_000;

        var set = new ConcurrentHashSet<int>(Enumerable.Range(0, keySpace / 2));
        using var cts = new CancellationTokenSource();
        using var startGate = new ManualResetEventSlim(false);

        int faults = 0;
        int coherenceViolations = 0;
        int snapshots = 0;
        int seed = 1_000;
        Exception? firstException = null;

        IEnumerable<Task> writers = Enumerable.Range(0, 4).Select(_ =>
            StartWorker(() =>
            {
                startGate.Wait();
                var random = new Random(Interlocked.Increment(ref seed));

                try
                {
                    while (!cts.Token.IsCancellationRequested)
                    {
                        int key = random.Next(keySpace);
                        if (random.Next(2) == 0)
                            set.Add(key);
                        else
                            set.Remove(key);
                    }
                }
                catch (Exception ex)
                {
                    Interlocked.CompareExchange(ref firstException, ex, null);
                    Interlocked.Increment(ref faults);
                    cts.Cancel();
                }
            }));

        IEnumerable<Task> readers = Enumerable.Range(0, 4).Select(_ =>
            StartWorker(() =>
            {
                startGate.Wait();

                try
                {
                    while (!cts.Token.IsCancellationRequested)
                    {
                        int[] snapshot = set.ToArray();
                        var seen = new HashSet<int>();
                        foreach (int value in snapshot)
                        {
                            if (value < 0 || value >= keySpace || !seen.Add(value))
                                Interlocked.Increment(ref coherenceViolations);
                        }

                        Interlocked.Increment(ref snapshots);
                    }
                }
                catch (Exception ex)
                {
                    Interlocked.CompareExchange(ref firstException, ex, null);
                    Interlocked.Increment(ref faults);
                    cts.Cancel();
                }
            }));

        Task[] tasks = writers.Concat(readers).ToArray();

        startGate.Set();
        Thread.Sleep(durationMs);
        cts.Cancel();
        bool completed = Task.WaitAll(tasks, 60_000);

        TestContext.WriteLine(
            $"Snapshots={snapshots}, CoherenceViolations={coherenceViolations}, Faults={faults}, " +
            $"Completed={completed}, FirstException={firstException}");

        Assert.IsTrue(completed, "Workers did not complete within the deadlock timeout.");
        Assert.AreEqual(0, faults, $"No exception is expected. First exception: {firstException}");
        Assert.AreEqual(0, coherenceViolations, "Every snapshot must contain only distinct, in-range elements.");
        Assert.IsGreaterThan(0, snapshots, "Readers must have captured snapshots during the stress run.");
    }

    /// <summary>
    /// Verifies that concurrent <see cref="ConcurrentHashSet{T}.Clear" /> calls interleaved with adds never throw and
    /// leave the set self-consistent.
    /// </summary>
    [TestMethod]
    [TestCategory("Stress")]
    public void Stress_WhenClearRacesWithAdd_ShouldNeverCorruptState()
    {
        const int durationMs = 2_000;

        var set = new ConcurrentHashSet<int>();
        using var cts = new CancellationTokenSource();
        using var startGate = new ManualResetEventSlim(false);

        int faults = 0;
        int addOperations = 0;
        int clearOperations = 0;
        int seed = 5_000;
        Exception? firstException = null;

        IEnumerable<Task> adders = Enumerable.Range(0, 6).Select(_ =>
            StartWorker(() =>
            {
                startGate.Wait();
                var random = new Random(Interlocked.Increment(ref seed));

                try
                {
                    while (!cts.Token.IsCancellationRequested)
                    {
                        set.Add(random.Next(1_000));
                        Interlocked.Increment(ref addOperations);
                    }
                }
                catch (Exception ex)
                {
                    Interlocked.CompareExchange(ref firstException, ex, null);
                    Interlocked.Increment(ref faults);
                    cts.Cancel();
                }
            }));

        Task clearer = StartWorker(() =>
        {
            startGate.Wait();

            try
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    set.Clear();
                    Interlocked.Increment(ref clearOperations);
                    Thread.Yield();
                }
            }
            catch (Exception ex)
            {
                Interlocked.CompareExchange(ref firstException, ex, null);
                Interlocked.Increment(ref faults);
                cts.Cancel();
            }
        });

        Task[] tasks = adders.Append(clearer).ToArray();

        startGate.Set();
        Thread.Sleep(durationMs);
        cts.Cancel();
        bool completed = Task.WaitAll(tasks, 60_000);

        int finalCount = set.Count;
        int[] snapshot = set.ToArray();

        TestContext.WriteLine(
            $"AddOperations={addOperations}, ClearOperations={clearOperations}, Faults={faults}, " +
            $"FinalCount={finalCount}, Completed={completed}, FirstException={firstException}");

        Assert.IsTrue(completed, "Workers did not complete within the deadlock timeout.");
        Assert.AreEqual(0, faults, $"No exception is expected. First exception: {firstException}");
        Assert.IsGreaterThan(0, addOperations, "Adders must have exercised Add during the stress run.");
        Assert.IsGreaterThan(0, clearOperations, "The clearer must have exercised Clear during the stress run.");
        Assert.HasCount(finalCount, snapshot, "Count and ToArray().Length must agree once the workers quiesce.");
    }

    /// <summary>
    /// Verifies that forcing many internal table resizes while readers iterate the set concurrently never loses an
    /// element or faults a reader.
    /// </summary>
    [TestMethod]
    [TestCategory("Stress")]
    public void Stress_WhenResizeHappensUnderConcurrentReaders_ShouldNotLoseElements()
    {
        const int adderCount = 4;
        const int keysPerAdder = 5_000;
        const int totalKeys = adderCount * keysPerAdder;

        // A deliberately tiny initial capacity forces many GrowTable cycles while readers run.
        var set = new ConcurrentHashSet<int>(capacity: 4);
        using var cts = new CancellationTokenSource();
        using var startGate = new ManualResetEventSlim(false);

        int faults = 0;
        int readerOperations = 0;
        Exception? firstException = null;

        Task[] adders = Enumerable.Range(0, adderCount).Select(adderId =>
            StartWorker(() =>
            {
                startGate.Wait();
                int baseKey = adderId * keysPerAdder;

                try
                {
                    for (int k = 0; k < keysPerAdder; k++)
                        set.Add(baseKey + k);
                }
                catch (Exception ex)
                {
                    Interlocked.CompareExchange(ref firstException, ex, null);
                    Interlocked.Increment(ref faults);
                    cts.Cancel();
                }
            })).ToArray();

        Task[] readers = Enumerable.Range(0, 3).Select(_ =>
            StartWorker(() =>
            {
                startGate.Wait();
                var random = new Random();

                try
                {
                    while (!cts.Token.IsCancellationRequested)
                    {
                        set.Contains(random.Next(totalKeys));

                        int enumerated = 0;
                        foreach (int _ in set)
                            enumerated++;

                        Interlocked.Increment(ref readerOperations);
                    }
                }
                catch (Exception ex)
                {
                    Interlocked.CompareExchange(ref firstException, ex, null);
                    Interlocked.Increment(ref faults);
                    cts.Cancel();
                }
            })).ToArray();

        startGate.Set();
        bool addersCompleted = Task.WaitAll(adders, 60_000);
        cts.Cancel();
        bool readersCompleted = Task.WaitAll(readers, 60_000);

        TestContext.WriteLine(
            $"ReaderOperations={readerOperations}, Faults={faults}, Count={set.Count}, " +
            $"AddersCompleted={addersCompleted}, ReadersCompleted={readersCompleted}, FirstException={firstException}");

        Assert.IsTrue(addersCompleted && readersCompleted, "Workers did not complete within the deadlock timeout.");
        Assert.AreEqual(0, faults, $"No exception is expected. First exception: {firstException}");
        Assert.AreEqual(totalKeys, set.Count, "Every distinct key must survive the table resizes.");
        Assert.IsGreaterThan(0, readerOperations, "Readers must have iterated the set during the resizes.");

        for (int k = 0; k < totalKeys; k++)
            Assert.IsTrue(set.Contains(k), $"Key {k} was lost across a table resize.");
    }

    /// <summary>
    /// Verifies that, across a range of thread counts and per-thread workloads, every worker that adds a private block
    /// of keys sees all of them retained afterward, with an exact final count and no faults.
    /// </summary>
    [TestMethod]
    [TestCategory("Stress")]
    [DataRow(2, 2_000)]
    [DataRow(4, 2_000)]
    [DataRow(8, 1_000)]
    public void Stress_WhenManyThreadsAddPrivateKeyBlocks_ShouldRetainEveryKey(int threadCount, int keysPerThread)
    {
        var set = new ConcurrentHashSet<long>(capacity: 4);
        using var startGate = new ManualResetEventSlim(false);

        int faults = 0;
        int addFailures = 0;
        Exception? firstException = null;

        Task[] workers = Enumerable.Range(0, threadCount).Select(threadId =>
            StartWorker(() =>
            {
                startGate.Wait();
                long baseKey = (long)threadId * keysPerThread;

                try
                {
                    for (int k = 0; k < keysPerThread; k++)
                    {
                        if (!set.Add(baseKey + k))
                            Interlocked.Increment(ref addFailures);
                    }
                }
                catch (Exception ex)
                {
                    Interlocked.CompareExchange(ref firstException, ex, null);
                    Interlocked.Increment(ref faults);
                }
            })).ToArray();

        startGate.Set();
        bool completed = Task.WaitAll(workers, 60_000);

        int expectedCount = threadCount * keysPerThread;

        TestContext.WriteLine(
            $"ThreadCount={threadCount}, KeysPerThread={keysPerThread}, AddFailures={addFailures}, " +
            $"Faults={faults}, Count={set.Count}, Expected={expectedCount}, Completed={completed}, " +
            $"FirstException={firstException}");

        Assert.IsTrue(completed, "Workers did not complete within the deadlock timeout.");
        Assert.AreEqual(0, faults, $"No exception is expected. First exception: {firstException}");
        Assert.AreEqual(0, addFailures, "Every add of a privately-owned key must succeed.");
        Assert.AreEqual(expectedCount, set.Count, "The set must retain every key added across all threads.");
        Assert.HasCount(expectedCount, set.ToArray());
    }

    /// <summary>
    /// Verifies that when every key hashes to the same bucket — funnelling all writers through a single stripe lock
    /// and a single chain — every privately-owned key is still added exactly once and retained.
    /// </summary>
    [TestMethod]
    [TestCategory("Stress")]
    public void Stress_WhenAllThreadsAddCollidingKeys_ShouldRetainEveryKey()
    {
        const int workerCount = 4;
        const int keysPerWorker = 500;

        var set = new ConcurrentHashSet<int>(new ConstantHashComparer());
        using var startGate = new ManualResetEventSlim(false);

        int faults = 0;
        int addFailures = 0;
        Exception? firstException = null;

        Task[] workers = Enumerable.Range(0, workerCount).Select(workerId =>
            StartWorker(() =>
            {
                startGate.Wait();
                int baseKey = workerId * keysPerWorker;

                try
                {
                    for (int k = 0; k < keysPerWorker; k++)
                    {
                        if (!set.Add(baseKey + k))
                            Interlocked.Increment(ref addFailures);
                    }
                }
                catch (Exception ex)
                {
                    Interlocked.CompareExchange(ref firstException, ex, null);
                    Interlocked.Increment(ref faults);
                }
            })).ToArray();

        startGate.Set();
        bool completed = Task.WaitAll(workers, 60_000);

        int expectedCount = workerCount * keysPerWorker;

        TestContext.WriteLine(
            $"AddFailures={addFailures}, Faults={faults}, Count={set.Count}, Expected={expectedCount}, " +
            $"Completed={completed}, FirstException={firstException}");

        Assert.IsTrue(completed, "Workers did not complete within the deadlock timeout.");
        Assert.AreEqual(0, faults, $"No exception is expected. First exception: {firstException}");
        Assert.AreEqual(0, addFailures, "Every privately-owned colliding key must be added exactly once.");
        Assert.AreEqual(expectedCount, set.Count, "Every colliding key must be retained.");

        for (int k = 0; k < expectedCount; k++)
            Assert.IsTrue(set.Contains(k), $"Colliding key {k} was lost.");
    }

    /// <summary>
    /// Verifies that a lock-free <see cref="ConcurrentHashSet{T}.Contains" /> never fails to observe a stable key
    /// while concurrent adders trigger a steady stream of internal table resizes.
    /// </summary>
    [TestMethod]
    [TestCategory("Stress")]
    public void Stress_WhenContainsRacesResize_ShouldAlwaysSeeStableKeys()
    {
        const int stableKeyCount = 200;
        const int adderCount = 4;
        const int keysPerAdder = 5_000;

        // A tiny initial capacity forces many GrowTable cycles while readers probe the stable keys.
        var set = new ConcurrentHashSet<int>(capacity: 4);
        for (int k = 0; k < stableKeyCount; k++)
            set.Add(-(k + 1));

        using var cts = new CancellationTokenSource();
        using var startGate = new ManualResetEventSlim(false);

        int faults = 0;
        int falseNegatives = 0;
        int readerOperations = 0;
        Exception? firstException = null;

        Task[] adders = Enumerable.Range(0, adderCount).Select(adderId =>
            StartWorker(() =>
            {
                startGate.Wait();
                int baseKey = adderId * keysPerAdder;

                try
                {
                    for (int k = 0; k < keysPerAdder; k++)
                        set.Add(baseKey + k);
                }
                catch (Exception ex)
                {
                    Interlocked.CompareExchange(ref firstException, ex, null);
                    Interlocked.Increment(ref faults);
                    cts.Cancel();
                }
            })).ToArray();

        Task[] readers = Enumerable.Range(0, 3).Select(_ =>
            StartWorker(() =>
            {
                startGate.Wait();
                var random = new Random();

                try
                {
                    while (!cts.Token.IsCancellationRequested)
                    {
                        int stableKey = -(random.Next(stableKeyCount) + 1);
                        if (!set.Contains(stableKey))
                            Interlocked.Increment(ref falseNegatives);

                        Interlocked.Increment(ref readerOperations);
                    }
                }
                catch (Exception ex)
                {
                    Interlocked.CompareExchange(ref firstException, ex, null);
                    Interlocked.Increment(ref faults);
                    cts.Cancel();
                }
            })).ToArray();

        startGate.Set();
        bool addersCompleted = Task.WaitAll(adders, 60_000);
        cts.Cancel();
        bool readersCompleted = Task.WaitAll(readers, 60_000);

        TestContext.WriteLine(
            $"ReaderOperations={readerOperations}, FalseNegatives={falseNegatives}, Faults={faults}, " +
            $"AddersCompleted={addersCompleted}, ReadersCompleted={readersCompleted}, FirstException={firstException}");

        Assert.IsTrue(addersCompleted && readersCompleted, "Workers did not complete within the deadlock timeout.");
        Assert.AreEqual(0, faults, $"No exception is expected. First exception: {firstException}");
        Assert.AreEqual(0, falseNegatives, "A stable key must never be missed by a lock-free Contains during resize.");
        Assert.IsGreaterThan(0, readerOperations, "Readers must have queried the set during the resizes.");
    }

    /// <summary>
    /// Verifies that the bulk mutating set-algebra members run concurrently with single-element add and remove
    /// operations without throwing, deadlocking, or leaving the set internally inconsistent.
    /// </summary>
    [TestMethod]
    [TestCategory("Stress")]
    public void Stress_WhenBulkSetOperationsRaceWithMutations_ShouldNeverThrow()
    {
        const int keySpace = 500;
        const int durationMs = 2_000;

        var set = new ConcurrentHashSet<int>(Enumerable.Range(0, keySpace / 2));
        using var cts = new CancellationTokenSource();
        using var startGate = new ManualResetEventSlim(false);

        int faults = 0;
        int bulkOperations = 0;
        int seed = 9_000;
        Exception? firstException = null;

        IEnumerable<Task> mutators = Enumerable.Range(0, 4).Select(_ =>
            StartWorker(() =>
            {
                startGate.Wait();
                var random = new Random(Interlocked.Increment(ref seed));

                try
                {
                    while (!cts.Token.IsCancellationRequested)
                    {
                        int key = random.Next(keySpace);
                        if (random.Next(2) == 0)
                            set.Add(key);
                        else
                            set.Remove(key);
                    }
                }
                catch (Exception ex)
                {
                    Interlocked.CompareExchange(ref firstException, ex, null);
                    Interlocked.Increment(ref faults);
                    cts.Cancel();
                }
            }));

        IEnumerable<Task> bulkWorkers = Enumerable.Range(0, 4).Select(_ =>
            StartWorker(() =>
            {
                startGate.Wait();
                var random = new Random(Interlocked.Increment(ref seed));

                try
                {
                    while (!cts.Token.IsCancellationRequested)
                    {
                        int[] other = Enumerable.Range(0, 20).Select(n => random.Next(keySpace)).ToArray();
                        switch (random.Next(4))
                        {
                            case 0:
                                set.UnionWith(other);
                                break;
                            case 1:
                                set.ExceptWith(other);
                                break;
                            case 2:
                                set.IntersectWith(other);
                                break;
                            default:
                                set.SymmetricExceptWith(other);
                                break;
                        }

                        Interlocked.Increment(ref bulkOperations);
                    }
                }
                catch (Exception ex)
                {
                    Interlocked.CompareExchange(ref firstException, ex, null);
                    Interlocked.Increment(ref faults);
                    cts.Cancel();
                }
            }));

        Task[] tasks = mutators.Concat(bulkWorkers).ToArray();

        startGate.Set();
        Thread.Sleep(durationMs);
        cts.Cancel();
        bool completed = Task.WaitAll(tasks, 60_000);

        int finalCount = set.Count;
        int[] snapshot = set.ToArray();

        TestContext.WriteLine(
            $"BulkOperations={bulkOperations}, Faults={faults}, FinalCount={finalCount}, " +
            $"SnapshotLength={snapshot.Length}, Completed={completed}, FirstException={firstException}");

        Assert.IsTrue(completed, "Workers did not complete within the deadlock timeout.");
        Assert.AreEqual(0, faults, $"No exception is expected. First exception: {firstException}");
        Assert.IsGreaterThan(0, bulkOperations, "Bulk workers must have exercised the set-algebra members.");
        Assert.HasCount(finalCount, snapshot, "Count and ToArray().Length must agree once the workers quiesce.");
    }

    /// <summary>
    /// Verifies that the read-only set-algebra predicates run concurrently with single-element add and remove
    /// operations without throwing.
    /// </summary>
    [TestMethod]
    [TestCategory("Stress")]
    public void Stress_WhenSetPredicatesRaceWithMutations_ShouldNeverThrow()
    {
        const int keySpace = 500;
        const int durationMs = 2_000;

        var set = new ConcurrentHashSet<int>(Enumerable.Range(0, keySpace / 2));
        using var cts = new CancellationTokenSource();
        using var startGate = new ManualResetEventSlim(false);

        int faults = 0;
        int predicateOperations = 0;
        int seed = 11_000;
        Exception? firstException = null;

        IEnumerable<Task> mutators = Enumerable.Range(0, 4).Select(_ =>
            StartWorker(() =>
            {
                startGate.Wait();
                var random = new Random(Interlocked.Increment(ref seed));

                try
                {
                    while (!cts.Token.IsCancellationRequested)
                    {
                        int key = random.Next(keySpace);
                        if (random.Next(2) == 0)
                            set.Add(key);
                        else
                            set.Remove(key);
                    }
                }
                catch (Exception ex)
                {
                    Interlocked.CompareExchange(ref firstException, ex, null);
                    Interlocked.Increment(ref faults);
                    cts.Cancel();
                }
            }));

        IEnumerable<Task> predicateWorkers = Enumerable.Range(0, 4).Select(_ =>
            StartWorker(() =>
            {
                startGate.Wait();
                var random = new Random(Interlocked.Increment(ref seed));

                try
                {
                    while (!cts.Token.IsCancellationRequested)
                    {
                        int[] other = Enumerable.Range(0, 20).Select(n => random.Next(keySpace)).ToArray();
                        switch (random.Next(6))
                        {
                            case 0:
                                set.IsSubsetOf(other);
                                break;
                            case 1:
                                set.IsSupersetOf(other);
                                break;
                            case 2:
                                set.IsProperSubsetOf(other);
                                break;
                            case 3:
                                set.IsProperSupersetOf(other);
                                break;
                            case 4:
                                set.Overlaps(other);
                                break;
                            default:
                                set.SetEquals(other);
                                break;
                        }

                        Interlocked.Increment(ref predicateOperations);
                    }
                }
                catch (Exception ex)
                {
                    Interlocked.CompareExchange(ref firstException, ex, null);
                    Interlocked.Increment(ref faults);
                    cts.Cancel();
                }
            }));

        Task[] tasks = mutators.Concat(predicateWorkers).ToArray();

        startGate.Set();
        Thread.Sleep(durationMs);
        cts.Cancel();
        bool completed = Task.WaitAll(tasks, 60_000);

        TestContext.WriteLine(
            $"PredicateOperations={predicateOperations}, Faults={faults}, Completed={completed}, " +
            $"FirstException={firstException}");

        Assert.IsTrue(completed, "Workers did not complete within the deadlock timeout.");
        Assert.AreEqual(0, faults, $"No exception is expected. First exception: {firstException}");
        Assert.IsGreaterThan(0, predicateOperations, "Predicate workers must have exercised the set-algebra members.");
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.Clear" /> interleaved with the bulk set-algebra members never
    /// throws and leaves the set self-consistent.
    /// </summary>
    [TestMethod]
    [TestCategory("Stress")]
    public void Stress_WhenClearRacesWithBulkOperations_ShouldNeverCorruptState()
    {
        const int keySpace = 500;
        const int durationMs = 2_000;

        var set = new ConcurrentHashSet<int>();
        using var cts = new CancellationTokenSource();
        using var startGate = new ManualResetEventSlim(false);

        int faults = 0;
        int bulkOperations = 0;
        int clearOperations = 0;
        int seed = 13_000;
        Exception? firstException = null;

        IEnumerable<Task> bulkWorkers = Enumerable.Range(0, 6).Select(_ =>
            StartWorker(() =>
            {
                startGate.Wait();
                var random = new Random(Interlocked.Increment(ref seed));

                try
                {
                    while (!cts.Token.IsCancellationRequested)
                    {
                        int[] other = Enumerable.Range(0, 20).Select(n => random.Next(keySpace)).ToArray();
                        if (random.Next(2) == 0)
                            set.UnionWith(other);
                        else
                            set.ExceptWith(other);

                        Interlocked.Increment(ref bulkOperations);
                    }
                }
                catch (Exception ex)
                {
                    Interlocked.CompareExchange(ref firstException, ex, null);
                    Interlocked.Increment(ref faults);
                    cts.Cancel();
                }
            }));

        Task clearer = StartWorker(() =>
        {
            startGate.Wait();

            try
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    set.Clear();
                    Interlocked.Increment(ref clearOperations);
                    Thread.Yield();
                }
            }
            catch (Exception ex)
            {
                Interlocked.CompareExchange(ref firstException, ex, null);
                Interlocked.Increment(ref faults);
                cts.Cancel();
            }
        });

        Task[] tasks = bulkWorkers.Append(clearer).ToArray();

        startGate.Set();
        Thread.Sleep(durationMs);
        cts.Cancel();
        bool completed = Task.WaitAll(tasks, 60_000);

        int finalCount = set.Count;
        int[] snapshot = set.ToArray();

        TestContext.WriteLine(
            $"BulkOperations={bulkOperations}, ClearOperations={clearOperations}, Faults={faults}, " +
            $"FinalCount={finalCount}, Completed={completed}, FirstException={firstException}");

        Assert.IsTrue(completed, "Workers did not complete within the deadlock timeout.");
        Assert.AreEqual(0, faults, $"No exception is expected. First exception: {firstException}");
        Assert.IsGreaterThan(0, bulkOperations, "Bulk workers must have exercised the set-algebra members.");
        Assert.IsGreaterThan(0, clearOperations, "The clearer must have exercised Clear during the stress run.");
        Assert.HasCount(finalCount, snapshot, "Count and ToArray().Length must agree once the workers quiesce.");
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.ToArray" /> taken while <see cref="ConcurrentHashSet{T}.Clear" />
    /// and adds churn the set always returns a coherent snapshot of distinct, in-range elements.
    /// </summary>
    [TestMethod]
    [TestCategory("Stress")]
    public void Stress_WhenToArrayRacesWithClear_ShouldReturnCoherentSnapshot()
    {
        const int keySpace = 600;
        const int durationMs = 2_000;

        var set = new ConcurrentHashSet<int>();
        using var cts = new CancellationTokenSource();
        using var startGate = new ManualResetEventSlim(false);

        int faults = 0;
        int coherenceViolations = 0;
        int snapshots = 0;
        int seed = 15_000;
        Exception? firstException = null;

        IEnumerable<Task> writers = Enumerable.Range(0, 4).Select(_ =>
            StartWorker(() =>
            {
                startGate.Wait();
                var random = new Random(Interlocked.Increment(ref seed));

                try
                {
                    while (!cts.Token.IsCancellationRequested)
                    {
                        if (random.Next(20) == 0)
                            set.Clear();
                        else
                            set.Add(random.Next(keySpace));
                    }
                }
                catch (Exception ex)
                {
                    Interlocked.CompareExchange(ref firstException, ex, null);
                    Interlocked.Increment(ref faults);
                    cts.Cancel();
                }
            }));

        IEnumerable<Task> readers = Enumerable.Range(0, 4).Select(_ =>
            StartWorker(() =>
            {
                startGate.Wait();

                try
                {
                    while (!cts.Token.IsCancellationRequested)
                    {
                        int[] snapshot = set.ToArray();
                        var seen = new HashSet<int>();
                        foreach (int value in snapshot)
                        {
                            if (value < 0 || value >= keySpace || !seen.Add(value))
                                Interlocked.Increment(ref coherenceViolations);
                        }

                        Interlocked.Increment(ref snapshots);
                    }
                }
                catch (Exception ex)
                {
                    Interlocked.CompareExchange(ref firstException, ex, null);
                    Interlocked.Increment(ref faults);
                    cts.Cancel();
                }
            }));

        Task[] tasks = writers.Concat(readers).ToArray();

        startGate.Set();
        Thread.Sleep(durationMs);
        cts.Cancel();
        bool completed = Task.WaitAll(tasks, 60_000);

        TestContext.WriteLine(
            $"Snapshots={snapshots}, CoherenceViolations={coherenceViolations}, Faults={faults}, " +
            $"Completed={completed}, FirstException={firstException}");

        Assert.IsTrue(completed, "Workers did not complete within the deadlock timeout.");
        Assert.AreEqual(0, faults, $"No exception is expected. First exception: {firstException}");
        Assert.AreEqual(0, coherenceViolations, "Every snapshot must contain only distinct, in-range elements.");
        Assert.IsGreaterThan(0, snapshots, "Readers must have captured snapshots during the stress run.");
    }
}
