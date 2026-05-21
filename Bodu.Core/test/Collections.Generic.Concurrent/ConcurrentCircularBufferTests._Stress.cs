// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentCircularBufferTests._Stress.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentCircularBufferTests
{

    // -----------------------------------------------------------------------------------------
    // The indexer must never return a value from an impossible generation.
    //
    // The reader loops buffer[i] over an observed Count while producers and consumers concurrently
    // mutate the buffer. Because Count and indexed access are separate operations, an
    // ArgumentOutOfRangeException is allowed when the buffer shrinks between the two operations.
    //
    // For successful reads, the test samples the latest published generation after the indexer
    // returns. A returned generation greater than that value would mean the indexer surfaced a
    // future or torn value that could not have been published when the read completed.
    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that the indexer does not return a future or torn-generation value while concurrent producers and consumers churn
    /// the buffer.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenContendedWithEnqueueAndDequeue_ShouldReturnAValueThatExisted()
    {
        const int capacity = 32;
        const int minimumOccupancy = capacity / 2;
        const int targetSuccessfulReads = 10_000;
        const int timeoutMs = 5_000;
        const int deadlockTimeoutMs = timeoutMs + 2_000;

        var buffer = new ConcurrentCircularBuffer<TestItem>(capacity, allowOverwrite: true);
        using var cts = new CancellationTokenSource();
        using var startGate = new ManualResetEventSlim(false);

        var generation = 0;
        var futureValueViolations = 0;
        var unexpectedFaults = 0;
        var reads = 0;
        var writeAttempts = 0;
        var dequeueAttempts = 0;

        Exception? unexpectedException = null;

        // Seed the buffer before contention begins. This gives the indexer a populated starting
        // window and avoids the test degenerating into an immediate empty-buffer race.
        for (var i = 0; i < capacity; i++)
        {
            var gen = Interlocked.Increment(ref generation);
            Assert.IsTrue(buffer.TryEnqueue(new TestItem(gen)));
        }

        var writerThreads = Math.Max(2, Environment.ProcessorCount / 2);
        var readerThreads = 2;
        var consumerThreads = 1;

        // Producers continuously publish new generations. With overwrite enabled, this keeps the
        // buffer changing even if consumers are unable to keep up.
        IEnumerable<Task> writers = Enumerable.Range(0, writerThreads).Select(_ =>
            Task.Run(() =>
            {
                startGate.Wait();

                while (!cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        var gen = Interlocked.Increment(ref generation);
                        buffer.TryEnqueue(new TestItem(gen));
                        Interlocked.Increment(ref writeAttempts);
                    }
                    catch (Exception ex)
                    {
                        Interlocked.CompareExchange(ref unexpectedException, ex, null);
                        Interlocked.Increment(ref unexpectedFaults);
                        cts.Cancel();
                    }
                }
            }));

        // Consumers create dequeue pressure, but deliberately stop draining below a low-water
        // mark. This preserves contention while ensuring indexers can complete a deterministic
        // number of successful reads.
        IEnumerable<Task> consumers = Enumerable.Range(0, consumerThreads).Select(_ =>
            Task.Run(() =>
            {
                startGate.Wait();

                while (!cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        if (buffer.Count > minimumOccupancy)
                        {
                            buffer.TryDequeue(out TestItem? _);
                            Interlocked.Increment(ref dequeueAttempts);
                        }
                        else
                        {
                            Thread.Yield();
                        }
                    }
                    catch (Exception ex)
                    {
                        Interlocked.CompareExchange(ref unexpectedException, ex, null);
                        Interlocked.Increment(ref unexpectedFaults);
                        cts.Cancel();
                    }
                }
            }));

        // Readers observe Count and then index into the buffer. Count and indexer access are
        // intentionally separate operations so this path exercises the real race surface.
        IEnumerable<Task> indexers = Enumerable.Range(0, readerThreads).Select(_ =>
            Task.Run(() =>
            {
                startGate.Wait();

                while (!cts.Token.IsCancellationRequested)
                {
                    var count = buffer.Count;

                    for (var i = 0; i < count; i++)
                    {
                        try
                        {
                            TestItem item = buffer[i];

                            // Sample generation after the indexer returns. Sampling before the
                            // call would incorrectly classify legitimate concurrent writes as
                            // future-value violations.
                            var latest = Volatile.Read(ref generation);

                            if (item is not null && item.Value > latest)
                            {
                                Interlocked.Increment(ref futureValueViolations);
                                cts.Cancel();
                            }

                            // Stop once the test has observed enough successful reads to prove
                            // the indexer was exercised under contention, rather than relying
                            // only on elapsed time.
                            if (Interlocked.Increment(ref reads) >= targetSuccessfulReads)
                                cts.Cancel();
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            // Expected race: Count was observed before indexing, but the buffer
                            // shrank before the indexed read completed.
                        }
                        catch (Exception ex)
                        {
                            // Any other exception is unexpected and should fail the test. Capture
                            // the first exception so the assertion message remains useful.
                            Interlocked.CompareExchange(ref unexpectedException, ex, null);
                            Interlocked.Increment(ref unexpectedFaults);
                            cts.Cancel();
                        }
                    }

                    Thread.Yield();
                }
            }));

        Task[] allTasks = writers.Concat(consumers).Concat(indexers).ToArray();

        // Release all workers together so the test exercises real contention rather than a staged
        // producer/consumer/read sequence.
        startGate.Set();

        var completedBeforeTimeout = SpinWait.SpinUntil(
            () =>
                Volatile.Read(ref reads) >= targetSuccessfulReads ||
                Volatile.Read(ref unexpectedFaults) > 0 ||
                Volatile.Read(ref futureValueViolations) > 0,
            timeoutMs);

        cts.Cancel();

        var completed = Task.WaitAll(allTasks, deadlockTimeoutMs);

        TestContext.WriteLine(
            $"Reads={reads}, WriteAttempts={writeAttempts}, DequeueAttempts={dequeueAttempts}, " +
            $"FutureValueViolations={futureValueViolations}, UnexpectedFaults={unexpectedFaults}, " +
            $"Completed={completed}, FirstException={unexpectedException}");

        Assert.IsTrue(
            completedBeforeTimeout,
            $"Indexer did not complete {targetSuccessfulReads} successful reads within {timeoutMs} ms.");

        Assert.IsTrue(
            completed,
            $"Tasks did not complete within {deadlockTimeoutMs} ms.");

        Assert.AreEqual(
            0,
            unexpectedFaults,
            $"No unexpected exceptions are allowed during indexer contention. First exception: {unexpectedException}");

        Assert.AreEqual(
            0,
            futureValueViolations,
            "Indexer surfaced a value from a future generation that could not have existed when the call completed.");

        Assert.IsGreaterThan(
            0, writeAttempts,
            "Writers must have exercised TryEnqueue during the indexer stress run.");

        Assert.IsGreaterThan(
            0, dequeueAttempts,
            "Consumers must have exercised TryDequeue during the indexer stress run.");

        Assert.IsGreaterThanOrEqualTo(
            targetSuccessfulReads, reads,
            "Indexer must have completed the required number of successful reads.");
    }

    // -----------------------------------------------------------------------------------------
    // Public state properties under concurrent mutation.
    //
    // Producers, consumers, and AllowOverwrite togglers mutate the buffer while inspector workers
    // repeatedly read public state. Because each property is read independently while mutations are
    // active, the test does not assert multi-property snapshots such as Count == ToArray().Length
    // during the hot phase.
    //
    // The relevant invariants are:
    //   - Count never escapes [0, Capacity];
    //   - Capacity remains stable;
    //   - AllowOverwrite can be read safely while other workers toggle it;
    //   - public state inspection does not throw under sustained contention;
    //   - once workers quiesce, Count agrees with ToArray().Length.
    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that public state properties remain safe and internally bounded while producers,
    /// consumers, and overwrite-mode togglers concurrently mutate the buffer.
    /// </summary>
    [TestMethod]
    public void StateProperties_WhenReadDuringConcurrentMutation_ShouldRemainInternallyConsistent()
    {
        const int capacity = 32;
        const int durationMs = 2_000;
        const int deadlockTimeoutMs = durationMs + 5_000;

        var buffer = new ConcurrentCircularBuffer<TestItem>(capacity, allowOverwrite: true);
        using var cts = new CancellationTokenSource();
        using var startGate = new ManualResetEventSlim(false);

        var faults = 0;
        var countViolations = 0;
        var capacityViolations = 0;
        var writeAttempts = 0;
        var readAttempts = 0;
        var toggleOperations = 0;
        var inspectionAttempts = 0;

        Exception? firstException = null;

        var workerCount = Math.Max(4, Environment.ProcessorCount * 2);

        Task StartWorker(Action action) =>
            Task.Factory.StartNew(
                action,
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);

        // Seed the buffer so inspectors observe both populated and drained states as workers churn.
        for (var i = 0; i < capacity / 2; i++)
            buffer.Enqueue(new TestItem(i));

        IEnumerable<Task> writers = Enumerable.Range(0, workerCount / 2).Select(workerIndex =>
            StartWorker(() =>
            {
                startGate.Wait();

                var value = workerIndex * 1_000_000;

                while (!cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        buffer.TryEnqueue(new TestItem(++value));
                        Interlocked.Increment(ref writeAttempts);
                    }
                    catch (Exception ex)
                    {
                        Interlocked.CompareExchange(ref firstException, ex, null);
                        Interlocked.Increment(ref faults);
                        cts.Cancel();
                    }

                    Thread.Yield();
                }
            }));

        IEnumerable<Task> readers = Enumerable.Range(0, workerCount / 2).Select(_ =>
            StartWorker(() =>
            {
                startGate.Wait();

                while (!cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        buffer.TryDequeue(out TestItem? _);
                        Interlocked.Increment(ref readAttempts);
                    }
                    catch (Exception ex)
                    {
                        Interlocked.CompareExchange(ref firstException, ex, null);
                        Interlocked.Increment(ref faults);
                        cts.Cancel();
                    }

                    Thread.Yield();
                }
            }));

        IEnumerable<Task> togglers = Enumerable.Range(0, 2).Select(_ =>
            StartWorker(() =>
            {
                startGate.Wait();

                var value = 0;

                while (!cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        buffer.AllowOverwrite = ++value % 2 == 0;
                        Interlocked.Increment(ref toggleOperations);
                    }
                    catch (Exception ex)
                    {
                        Interlocked.CompareExchange(ref firstException, ex, null);
                        Interlocked.Increment(ref faults);
                        cts.Cancel();
                    }

                    Thread.SpinWait(100);
                }
            }));

        IEnumerable<Task> inspectors = Enumerable.Range(0, workerCount).Select(workerIndex =>
            StartWorker(() =>
            {
                startGate.Wait();

                while (!cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        var count = buffer.Count;
                        var observedCapacity = buffer.Capacity;
                        _ = buffer.AllowOverwrite;

                        if (count < 0 || count > observedCapacity)
                            Interlocked.Increment(ref countViolations);

                        if (observedCapacity != capacity)
                            Interlocked.Increment(ref capacityViolations);

                        Interlocked.Increment(ref inspectionAttempts);
                    }
                    catch (Exception ex)
                    {
                        Interlocked.CompareExchange(ref firstException, ex, null);
                        Interlocked.Increment(ref faults);
                        cts.Cancel();
                    }

                    Thread.Yield();
                }
            }));

        Task[] allTasks = writers.Concat(readers).Concat(togglers).Concat(inspectors).ToArray();

        // Release all roles together so property reads race with live mutation and mode changes.
        startGate.Set();

        Thread.Sleep(durationMs);
        cts.Cancel();

        var completed = Task.WaitAll(allTasks, deadlockTimeoutMs);

        var finalCount = buffer.Count;
        TestItem[] snapshot = buffer.ToArray();

        TestContext.WriteLine(
            $"Count={finalCount}, Capacity={buffer.Capacity}, WorkerCount={workerCount}, " +
            $"WriteAttempts={writeAttempts}, ReadAttempts={readAttempts}, ToggleOperations={toggleOperations}, " +
            $"InspectionAttempts={inspectionAttempts}, CountViolations={countViolations}, " +
            $"CapacityViolations={capacityViolations}, Faults={faults}, Completed={completed}, " +
            $"FirstException={firstException}");

        Assert.IsTrue(
            completed,
            $"Tasks did not complete within {deadlockTimeoutMs} ms — possible deadlock, blocked operation, or livelock.");

        Assert.AreEqual(
            0,
            faults,
            $"Public state properties, mutation operations, and AllowOverwrite toggling must not throw. First exception: {firstException}");

        Assert.AreEqual(
            0,
            countViolations,
            "Count must remain within [0, Capacity] while public state properties are read under mutation pressure.");

        Assert.AreEqual(
            0,
            capacityViolations,
            "Capacity must remain stable while the buffer is under concurrent mutation pressure.");

        Assert.IsGreaterThan(
            0, writeAttempts,
            "Writers must have exercised TryEnqueue during the state-property stress run.");

        Assert.IsGreaterThan(
            0, readAttempts,
            "Readers must have exercised TryDequeue during the state-property stress run.");

        Assert.IsGreaterThan(
            0, toggleOperations,
            "Togglers must have exercised AllowOverwrite transitions during the state-property stress run.");

        Assert.IsGreaterThan(
            0, inspectionAttempts,
            "Inspectors must have exercised public state properties during the stress run.");

        Assert.IsTrue(
            finalCount >= 0 && finalCount <= buffer.Capacity,
            "Final Count must remain within [0, Capacity].");

        Assert.HasCount(
            finalCount,
            snapshot,
            "Count and ToArray().Length must agree once all workers have quiesced.");
    }
    /// <summary>
    /// Verifies that under sustained concurrent enqueue, dequeue, and inspection pressure, the buffer maintains the accounting
    /// invariant <c>Count == enqueueSuccesses − logicalRemovals</c> without faults.
    /// </summary>
    [TestMethod]
    [DataRow(10, true)]
    [DataRow(50, true)]
    [DataRow(10, false)]
    [DataRow(50, false)]
    public void StressTest_WhenAccessingBuffer_ShouldNotCorruptInternalState(int capacity, bool allowOverwrite)
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(capacity, allowOverwrite);
        using var cts = new CancellationTokenSource();
        using var startGate = new ManualResetEventSlim(false);

        var sequence = 0;
        var dequeueAttempts = 0;
        var dequeueSuccesses = 0;
        var enqueueAttempts = 0;
        var enqueueSuccesses = 0;
        var inspectionAttempts = 0;
        var faults = 0;

        Exception? firstException = null;

        // Keep the role count high enough to create contention, but run each worker as a dedicated
        // long-running task so writer hot loops cannot starve readers or inspectors on the thread pool.
        var workersPerRole = Math.Max(2, Environment.ProcessorCount);
        const int durationMs = 2_000;
        const int deadlockTimeoutMs = durationMs + 5_000;

        Task StartWorker(Action action) =>
            Task.Factory.StartNew(
                action,
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);

        // In overwrite mode, evictions are logical removals. Counting them together with
        // successful TryDequeue calls allows the final accounting invariant to reconcile.
        Action<TestItem?> evictedHandler = _ => Interlocked.Increment(ref dequeueSuccesses);
        buffer.ItemEvicted += evictedHandler;

        try
        {
            IEnumerable<Task> readers = Enumerable.Range(0, workersPerRole).Select(workerIndex =>
                StartWorker(() =>
                {
                    startGate.Wait();

                    while (!cts.Token.IsCancellationRequested)
                    {
                        try
                        {
                            Interlocked.Increment(ref dequeueAttempts);

                            if (buffer.TryDequeue(out TestItem? _))
                                Interlocked.Increment(ref dequeueSuccesses);
                            else
                                Thread.Yield();
                        }
                        catch (Exception ex)
                        {
                            Interlocked.CompareExchange(ref firstException, ex, null);
                            Interlocked.Increment(ref faults);
                            cts.Cancel();
                        }
                    }
                }));

            IEnumerable<Task> writers = Enumerable.Range(0, workersPerRole).Select(workerIndex =>
                StartWorker(() =>
                {
                    startGate.Wait();

                    while (!cts.Token.IsCancellationRequested)
                    {
                        try
                        {
                            Interlocked.Increment(ref enqueueAttempts);

                            var value = Interlocked.Increment(ref sequence);
                            TestItem item = value % 2 == 0
                                ? new TestItem(value)
                                : new TestItem(-value);

                            if (buffer.TryEnqueue(item))
                                Interlocked.Increment(ref enqueueSuccesses);
                            else
                                Thread.Yield();
                        }
                        catch (Exception ex)
                        {
                            Interlocked.CompareExchange(ref firstException, ex, null);
                            Interlocked.Increment(ref faults);
                            cts.Cancel();
                        }
                    }
                }));

            IEnumerable<Task> inspectors = Enumerable.Range(0, workersPerRole).Select(workerIndex =>
                StartWorker(() =>
                {
                    startGate.Wait();

                    while (!cts.Token.IsCancellationRequested)
                    {
                        try
                        {
                            _ = buffer.Count;
                            _ = buffer.Capacity;

                            if (buffer.TryPeek(out TestItem? head))
                                buffer.Contains(head);

                            Interlocked.Increment(ref inspectionAttempts);
                        }
                        catch (Exception ex)
                        {
                            Interlocked.CompareExchange(ref firstException, ex, null);
                            Interlocked.Increment(ref faults);
                            cts.Cancel();
                        }

                        Thread.Yield();
                    }
                }));

            Task[] allTasks = writers.Concat(readers).Concat(inspectors).ToArray();

            // Release all worker roles together so enqueue, dequeue, inspection, and eviction
            // accounting are exercised under real contention.
            startGate.Set();

            Thread.Sleep(durationMs);
            cts.Cancel();

            var completed = Task.WaitAll(allTasks, deadlockTimeoutMs);

            TestContext.WriteLine(
                $"Count={buffer.Count}, Capacity={buffer.Capacity}, WorkersPerRole={workersPerRole}, " +
                $"EnqueueAttempts={enqueueAttempts}, EnqueueSuccesses={enqueueSuccesses}, " +
                $"DequeueAttempts={dequeueAttempts}, LogicalRemovals={dequeueSuccesses}, " +
                $"InspectionAttempts={inspectionAttempts}, Faults={faults}, Completed={completed}, " +
                $"FirstException={firstException}");

            try
            {
                TestItem[] snapshot = buffer.ToArray();
                TestContext.WriteLine(
                    $"[Snapshot] Items: {string.Join(", ", snapshot.Select(x => x?.Value.ToString() ?? "null"))}");
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"Snapshot failed after quiescence: {ex}");
                Interlocked.CompareExchange(ref firstException, ex, null);
                Interlocked.Increment(ref faults);
            }

            Assert.IsTrue(
                completed,
                $"Tasks did not complete within {deadlockTimeoutMs} ms — possible deadlock, blocked operation, or livelock.");

            Assert.AreEqual(
                0,
                faults,
                $"Unexpected exception occurred during concurrent access. First exception: {firstException}");

            Assert.IsGreaterThan(
                0, enqueueAttempts,
                "Writers must have exercised TryEnqueue during the stress run.");

            Assert.IsGreaterThan(
                0, dequeueAttempts,
                "Readers must have exercised TryDequeue during the stress run.");

            Assert.IsGreaterThan(
                0, inspectionAttempts,
                "Inspectors must have exercised read-side members during the stress run.");

            Assert.IsTrue(
                buffer.Count >= 0 && buffer.Count <= buffer.Capacity,
                "Buffer count must remain within [0, Capacity].");

            Assert.AreEqual(
                enqueueSuccesses - dequeueSuccesses,
                buffer.Count,
                "Buffer count mismatches successfully enqueued items minus logical removals.");
        }
        finally
        {
            buffer.ItemEvicted -= evictedHandler;
        }
    }

    // -----------------------------------------------------------------------------------------
    // AllowOverwrite toggled under sustained load.
    //
    // Writers use the throwing Enqueue() path while other workers toggle AllowOverwrite and
    // readers drain concurrently. InvalidOperationException is expected only when a writer sees
    // AllowOverwrite == false while the buffer is full.
    //
    // Warmup verifies both deterministic API behaviours outside the race, but the concurrent
    // assertions are based only on work performed during the stress phase.
    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that high-frequency toggling of <see cref="ConcurrentCircularBuffer{T}.AllowOverwrite" /> under writer/reader
    /// load surfaces only <see cref="InvalidOperationException" /> from throwing enqueues, never any state-corruption exception.
    /// </summary>
    [TestMethod]
    public void StressTest_WhenAllowOverwriteToggledConcurrently_ShouldNeverCorruptState()
    {
        const int capacity = 8;
        const int writerCount = 4;
        const int readerCount = 2;
        const int togglerCount = 2;
        const int workerCount = writerCount + readerCount + togglerCount;
        const int durationMs = 2_000;
        const int startupTimeoutMs = 5_000;
        const int participationTimeoutMs = 5_000;
        const int deadlockTimeoutMs = durationMs + startupTimeoutMs + participationTimeoutMs + 5_000;
        const int minimumToggleOperations = 64;

        var buffer = new ConcurrentCircularBuffer<TestItem>(capacity, allowOverwrite: false);
        using var cts = new CancellationTokenSource();
        using var startGate = new ManualResetEventSlim(false);
        using var firstToggleObserved = new ManualResetEventSlim(false);
        using var workersReady = new CountdownEvent(workerCount);

        var unexpectedFaults = 0;
        var expectedEnqueueFaults = 0;
        var enqueueSuccesses = 0;
        var enqueueAttempts = 0;
        var readAttempts = 0;
        var toggleOperations = 0;

        var warmupExpectedEnqueueFaults = 0;
        var warmupEnqueueSuccesses = 0;

        Exception? firstUnexpectedException = null;

        // Start from a full buffer to maximise rejection pressure when AllowOverwrite is false.
        for (var i = 0; i < capacity; i++)
            buffer.Enqueue(new TestItem(i));

        // Deterministic warmup proves both throwing Enqueue branches are reachable without
        // allowing those observations to satisfy the concurrent-phase assertions.
        buffer.AllowOverwrite = false;
        try
        {
            buffer.Enqueue(new TestItem(-1));
        }
        catch (InvalidOperationException)
        {
            warmupExpectedEnqueueFaults++;
        }

        buffer.AllowOverwrite = true;
        buffer.Enqueue(new TestItem(-2));
        warmupEnqueueSuccesses++;

        buffer.AllowOverwrite = false;

        // Dedicate a long-running thread to each worker so the CI thread pool cannot starve the
        // toggler tasks of a timeslice before the start gate opens.
        Task CreateWorker(Action action) =>
            Task.Factory.StartNew(
                action,
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);

        // Writers use Enqueue rather than TryEnqueue so the throwing path is exercised while
        // AllowOverwrite changes and readers concurrently alter fullness.
        Task[] writers = Enumerable.Range(0, writerCount)
            .Select(_ => CreateWorker(() =>
            {
                workersReady.Signal();
                startGate.Wait();

                var value = 0;

                while (!cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        Interlocked.Increment(ref enqueueAttempts);
                        buffer.Enqueue(new TestItem(++value));
                        Interlocked.Increment(ref enqueueSuccesses);
                    }
                    catch (InvalidOperationException)
                    {
                        Interlocked.Increment(ref expectedEnqueueFaults);
                    }
                    catch (Exception ex)
                    {
                        Interlocked.CompareExchange(ref firstUnexpectedException, ex, null);
                        Interlocked.Increment(ref unexpectedFaults);
                        cts.Cancel();
                    }

                    Thread.Yield();
                }
            }))
            .ToArray();

        Task[] readers = Enumerable.Range(0, readerCount)
            .Select(_ => CreateWorker(() =>
            {
                workersReady.Signal();
                startGate.Wait();

                while (!cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        buffer.TryDequeue(out TestItem? _);
                        Interlocked.Increment(ref readAttempts);
                    }
                    catch (Exception ex)
                    {
                        Interlocked.CompareExchange(ref firstUnexpectedException, ex, null);
                        Interlocked.Increment(ref unexpectedFaults);
                        cts.Cancel();
                    }

                    Thread.Yield();
                }
            }))
            .ToArray();

        // Togglers alternate AllowOverwrite at high frequency, creating transitions that race
        // with throwing Enqueue and reader-driven fullness changes.
        //
        // The loop deliberately continues after cancellation until a minimum number of toggle
        // operations has been observed. This prevents the stress test from falsely failing when
        // the scheduler delays the toggler tasks until the rest of the workload is already stopping.
        Task[] togglers = Enumerable.Range(0, togglerCount)
            .Select(_ => CreateWorker(() =>
            {
                workersReady.Signal();
                startGate.Wait();

                var value = 0;

                while (Volatile.Read(ref unexpectedFaults) == 0 &&
                       (!cts.Token.IsCancellationRequested ||
                        Volatile.Read(ref toggleOperations) < minimumToggleOperations))
                {
                    try
                    {
                        buffer.AllowOverwrite = ++value % 2 == 0;

                        var operations = Interlocked.Increment(ref toggleOperations);
                        if (operations == 1)
                            firstToggleObserved.Set();
                    }
                    catch (Exception ex)
                    {
                        Interlocked.CompareExchange(ref firstUnexpectedException, ex, null);
                        Interlocked.Increment(ref unexpectedFaults);
                        cts.Cancel();
                    }

                    Thread.Yield();
                }
            }))
            .ToArray();

        Task[] allTasks = writers.Concat(readers).Concat(togglers).ToArray();

        // Do not open the gate until every long-running worker is scheduled on a thread and
        // parked at the gate. This guarantees that the stress window is dominated by concurrent
        // work rather than by thread-pool startup latency on a busy CI runner.
        var workersStarted = workersReady.Wait(startupTimeoutMs);

        // Release writers, readers, and togglers together so the throwing Enqueue path races
        // with AllowOverwrite transitions and capacity changes.
        startGate.Set();

        // Do not start the timed stress window until at least one toggle has actually occurred.
        // This keeps the test focused on concurrent AllowOverwrite transitions rather than on
        // whether the toggler tasks happened to receive a timeslice before cancellation.
        var togglersStarted = workersStarted && firstToggleObserved.Wait(participationTimeoutMs);

        if (togglersStarted)
            Thread.Sleep(durationMs);

        cts.Cancel();

        var completed = Task.WaitAll(allTasks, deadlockTimeoutMs);

        TestContext.WriteLine(
            $"Count={buffer.Count}, Capacity={buffer.Capacity}, EnqueueAttempts={enqueueAttempts}, " +
            $"EnqueueSuccesses={enqueueSuccesses}, ExpectedEnqueueFaults={expectedEnqueueFaults}, " +
            $"ReadAttempts={readAttempts}, ToggleOperations={toggleOperations}, " +
            $"MinimumToggleOperations={minimumToggleOperations}, WorkersStarted={workersStarted}, " +
            $"TogglersStarted={togglersStarted}, WarmupEnqueueSuccesses={warmupEnqueueSuccesses}, " +
            $"WarmupExpectedEnqueueFaults={warmupExpectedEnqueueFaults}, UnexpectedFaults={unexpectedFaults}, " +
            $"Completed={completed}, FirstUnexpectedException={firstUnexpectedException}");

        Assert.IsTrue(
            completed,
            $"Tasks did not complete within {deadlockTimeoutMs} ms — possible deadlock, blocked operation, or livelock.");

        Assert.AreEqual(
            0,
            unexpectedFaults,
            $"Only InvalidOperationException is acceptable from Enqueue when AllowOverwrite is false. First unexpected exception: {firstUnexpectedException}");

        Assert.IsTrue(
            workersStarted,
            $"All {workerCount} workers must reach the start gate within {startupTimeoutMs} ms before the stress window opens.");

        Assert.IsTrue(
            togglersStarted,
            $"Togglers must begin exercising AllowOverwrite transitions within {participationTimeoutMs} ms.");

        Assert.IsGreaterThan(
            0, enqueueAttempts,
            "Writers must have exercised Enqueue during the stress run.");

        Assert.IsGreaterThan(
            0, readAttempts,
            "Readers must have exercised TryDequeue during the stress run.");

        Assert.IsGreaterThanOrEqualTo(
            minimumToggleOperations, toggleOperations,
            $"Togglers must have exercised at least {minimumToggleOperations} AllowOverwrite transitions during the stress run. Actual={toggleOperations}.");

        Assert.AreEqual(
            1,
            warmupExpectedEnqueueFaults,
            "Warmup must verify that Enqueue rejects a full non-overwrite buffer.");

        Assert.AreEqual(
            1,
            warmupEnqueueSuccesses,
            "Warmup must verify that Enqueue succeeds in overwrite mode.");

        Assert.IsTrue(
            buffer.Count >= 0 && buffer.Count <= buffer.Capacity,
            "Count must remain within [0, Capacity] throughout AllowOverwrite toggling.");
    }

    // -----------------------------------------------------------------------------------------
    // Minimum capacity under processor-count-scaled thread pressure.
    //
    // At the minimum supported capacity, every enqueue, overwrite, peek, and dequeue operation
    // targets a very small slot set. This maximises pressure on the buffer's synchronisation
    // around head, tail, count, and slot contents.
    //
    // Each worker performs both enqueue and dequeue operations. This avoids scheduler-dependent
    // starvation where writer-only workers can monopolise the run before reader-only workers are
    // scheduled.
    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that at the minimum capacity under processor-scaled thread contention, enqueue/dequeue calls complete without
    /// faults and the count stays within bounds.
    /// </summary>
    [TestMethod]
    public void StressTest_WhenCapacityIsMin_ShouldRemainStableUnderMaxContention()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(MinCapacity, allowOverwrite: true);
        using var cts = new CancellationTokenSource();
        using var startGate = new ManualResetEventSlim(false);

        var faults = 0;
        var countViolations = 0;
        var writeAttempts = 0;
        var readAttempts = 0;

        Exception? firstException = null;

        // Use many workers, but have each worker exercise both mutating paths so the test cannot
        // pass through writer-only scheduler dominance while never reaching TryDequeue.
        var workerCount = Math.Max(4, Environment.ProcessorCount * 2);
        const int durationMs = 2_000;
        const int deadlockTimeoutMs = durationMs + 5_000;

        Task StartWorker(Action action) =>
            Task.Factory.StartNew(
                action,
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);

        IEnumerable<Task> workers = Enumerable.Range(0, workerCount).Select(workerIndex =>
            StartWorker(() =>
            {
                startGate.Wait();

                var value = workerIndex * 1_000_000;

                while (!cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        buffer.TryEnqueue(new TestItem(++value));
                        Interlocked.Increment(ref writeAttempts);

                        var countAfterWrite = buffer.Count;
                        if (countAfterWrite < 0 || countAfterWrite > MinCapacity)
                            Interlocked.Increment(ref countViolations);

                        buffer.TryDequeue(out TestItem? _);
                        Interlocked.Increment(ref readAttempts);

                        var countAfterRead = buffer.Count;
                        if (countAfterRead < 0 || countAfterRead > MinCapacity)
                            Interlocked.Increment(ref countViolations);
                    }
                    catch (Exception ex)
                    {
                        Interlocked.CompareExchange(ref firstException, ex, null);
                        Interlocked.Increment(ref faults);
                        cts.Cancel();
                    }

                    // Keep the loop hot, but yield periodically so no single worker monopolises
                    // execution on loaded CI runners.
                    Thread.Yield();
                }
            }));

        Task[] allTasks = workers.ToArray();

        // Release all workers together so every operation competes over the minimum-capacity buffer.
        startGate.Set();

        Thread.Sleep(durationMs);
        cts.Cancel();

        var completed = Task.WaitAll(allTasks, deadlockTimeoutMs);

        TestContext.WriteLine(
            $"Count={buffer.Count}, Capacity={buffer.Capacity}, WorkerCount={workerCount}, " +
            $"WriteAttempts={writeAttempts}, ReadAttempts={readAttempts}, Faults={faults}, " +
            $"CountViolations={countViolations}, Completed={completed}, FirstException={firstException}");

        Assert.IsTrue(
            completed,
            $"Tasks did not complete within {deadlockTimeoutMs} ms — possible deadlock, blocked operation, or livelock.");

        Assert.AreEqual(
            0,
            faults,
            $"No exceptions expected from TryEnqueue or TryDequeue under minimum-capacity churn. First exception: {firstException}");

        Assert.IsGreaterThan(
            0, writeAttempts,
            "Workers must have exercised TryEnqueue during the stress run.");

        Assert.IsGreaterThan(
            0, readAttempts,
            "Workers must have exercised TryDequeue during the stress run.");

        Assert.AreEqual(
            0,
            countViolations,
            $"Count must remain within [0, {MinCapacity}] at all times.");

        Assert.AreEqual(
            MinCapacity,
            buffer.Capacity,
            $"Capacity must remain {MinCapacity} throughout.");

        Assert.IsTrue(
            buffer.Count >= 0 && buffer.Count <= buffer.Capacity,
            "Final Count must remain within [0, Capacity].");
    }

    // -----------------------------------------------------------------------------------------
    // Clear() as an active concurrent participant.
    //
    // Clear() removes items silently, without ItemEvicted and without a successful dequeue.
    // Therefore enqueue-minus-dequeue accounting is intentionally not asserted here.
    //
    // Each worker performs enqueue, dequeue, and periodic Clear operations. This avoids
    // scheduler-dependent starvation where writer/reader-only workers can monopolise the run
    // before clearer-only workers are scheduled.
    //
    // The relevant invariants are:
    //   - Clear, enqueue, and dequeue do not throw under sustained contention;
    //   - Count never escapes [0, Capacity];
    //   - once all workers have quiesced, Count agrees with ToArray().Length;
    //   - the buffer remains usable after the stress run.
    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that concurrent <see cref="ConcurrentCircularBuffer{T}.Clear" /> calls interleaved with producers and consumers
    /// maintain count bounds, consistent post-quiescence state, and do not throw.
    /// </summary>
    [TestMethod]
    [DataRow(8, true)]
    [DataRow(8, false)]
    public void StressTest_WhenClearInterleavesConcurrently_ShouldMaintainInvariantsUnderFullLoad(
        int capacity,
        bool allowOverwrite)
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(capacity, allowOverwrite);
        using var cts = new CancellationTokenSource();
        using var startGate = new ManualResetEventSlim(false);

        var countViolations = 0;
        var faults = 0;
        var writeAttempts = 0;
        var readAttempts = 0;
        var clearAttempts = 0;

        Exception? firstException = null;

        var workerCount = Math.Max(4, Environment.ProcessorCount * 2);
        const int durationMs = 2_000;
        const int deadlockTimeoutMs = durationMs + 5_000;

        Task StartWorker(Action action) =>
            Task.Factory.StartNew(
                action,
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);

        IEnumerable<Task> workers = Enumerable.Range(0, workerCount).Select(workerIndex =>
            StartWorker(() =>
            {
                startGate.Wait();

                var value = workerIndex * 1_000_000;
                var iteration = 0;

                while (!cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        // Enqueue first so Clear has live state to remove, especially when many
                        // workers race from an initially empty buffer.
                        buffer.TryEnqueue(new TestItem(++value));
                        Interlocked.Increment(ref writeAttempts);

                        var countAfterWrite = buffer.Count;
                        if (countAfterWrite < 0 || countAfterWrite > buffer.Capacity)
                            Interlocked.Increment(ref countViolations);

                        // Dequeue also remains active so Clear races with both producers and consumers.
                        buffer.TryDequeue(out TestItem? _);
                        Interlocked.Increment(ref readAttempts);

                        var countAfterRead = buffer.Count;
                        if (countAfterRead < 0 || countAfterRead > buffer.Capacity)
                            Interlocked.Increment(ref countViolations);

                        // Clear periodically rather than on every operation. This keeps the buffer
                        // churning through non-empty states while still making Clear an active
                        // participant in the concurrent workload.
                        if (++iteration % 8 == 0)
                        {
                            buffer.Clear();
                            Interlocked.Increment(ref clearAttempts);

                            var countAfterClear = buffer.Count;
                            if (countAfterClear < 0 || countAfterClear > buffer.Capacity)
                                Interlocked.Increment(ref countViolations);
                        }
                    }
                    catch (Exception ex)
                    {
                        Interlocked.CompareExchange(ref firstException, ex, null);
                        Interlocked.Increment(ref faults);
                        cts.Cancel();
                    }

                    Thread.Yield();
                }
            }));

        Task[] allTasks = workers.ToArray();

        // Release all workers together so Clear races with active enqueue and dequeue operations.
        startGate.Set();

        Thread.Sleep(durationMs);
        cts.Cancel();

        var completed = Task.WaitAll(allTasks, deadlockTimeoutMs);

        var finalCount = buffer.Count;
        TestItem[] snapshot = buffer.ToArray();

        TestContext.WriteLine(
            $"Count={finalCount}, Capacity={buffer.Capacity}, WorkerCount={workerCount}, " +
            $"WriteAttempts={writeAttempts}, ReadAttempts={readAttempts}, ClearAttempts={clearAttempts}, " +
            $"CountViolations={countViolations}, Faults={faults}, Completed={completed}, FirstException={firstException}");

        Assert.IsTrue(
            completed,
            $"Tasks did not complete within {deadlockTimeoutMs} ms — possible deadlock, blocked operation, or livelock.");

        Assert.AreEqual(
            0,
            faults,
            $"Clear, TryEnqueue, and TryDequeue must not throw under concurrent pressure. First exception: {firstException}");

        Assert.IsGreaterThan(
            0, writeAttempts,
            "Workers must have exercised TryEnqueue during the stress run.");

        Assert.IsGreaterThan(
            0, readAttempts,
            "Workers must have exercised TryDequeue during the stress run.");

        Assert.IsGreaterThan(
            0, clearAttempts,
            "Workers must have exercised Clear during the stress run.");

        Assert.AreEqual(
            0,
            countViolations,
            "Count must remain within [0, Capacity] during concurrent Clear.");

        Assert.IsTrue(
            finalCount >= 0 && finalCount <= buffer.Capacity,
            "Final Count must be within valid bounds.");

        Assert.HasCount(
            finalCount,
            snapshot,
            "Count and ToArray().Length must be consistent once all tasks have quiesced.");

        // Confirm the buffer is fully operational after sustained Clear/mutation stress.
        buffer.Clear();
        buffer.Enqueue(new TestItem(int.MaxValue));

        Assert.AreEqual(1, buffer.Count, "Buffer must accept new items after stress.");
        Assert.AreEqual(int.MaxValue, buffer.Dequeue().Value, "Buffer must dequeue the correct item after stress.");
    }

    // -----------------------------------------------------------------------------------------
    // Enumerator snapshots under concurrent mutation.
    //
    // The enumerator is a public snapshot surface, separate from direct ToArray calls. It should
    // remain safe while other workers enqueue, dequeue, and overwrite concurrently.
    //
    // Each worker performs mutation and enumeration in the same loop. This avoids scheduler-
    // dependent starvation where mutator-only workers can monopolise the run before enumerator
    // workers are scheduled.
    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that enumeration during concurrent mutation does not throw and never produces more
    /// items than the buffer capacity.
    /// </summary>
    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void StressTest_WhenEnumeratingDuringConcurrentMutation_ShouldProduceStableBoundedSnapshots(
        bool allowOverwrite)
    {
        const int capacity = 32;
        const int durationMs = 2_000;
        const int deadlockTimeoutMs = durationMs + 5_000;

        var buffer = new ConcurrentCircularBuffer<TestItem>(capacity, allowOverwrite);
        using var cts = new CancellationTokenSource();
        using var startGate = new ManualResetEventSlim(false);

        var faults = 0;
        var writeAttempts = 0;
        var readAttempts = 0;
        var enumerationAttempts = 0;
        var enumerationLengthViolations = 0;
        var maxEnumeratedLength = 0;

        Exception? firstException = null;

        var workerCount = Math.Max(4, Environment.ProcessorCount * 2);

        Task StartWorker(Action action) =>
            Task.Factory.StartNew(
                action,
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);

        // Seed the buffer so early enumerations see a non-empty snapshot.
        for (var i = 0; i < capacity / 2; i++)
            buffer.Enqueue(new TestItem(i));

        IEnumerable<Task> workers = Enumerable.Range(0, workerCount).Select(workerIndex =>
            StartWorker(() =>
            {
                startGate.Wait();

                var value = workerIndex * 1_000_000;
                var iteration = 0;

                while (!cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        // Mutate before enumeration so each snapshot races with live buffer churn.
                        buffer.TryEnqueue(new TestItem(++value));
                        Interlocked.Increment(ref writeAttempts);

                        // Dequeue periodically so non-overwrite mode does not settle into a permanently
                        // full buffer with writers only observing failed TryEnqueue attempts.
                        if (++iteration % 2 == 0)
                        {
                            buffer.TryDequeue(out TestItem? _);
                            Interlocked.Increment(ref readAttempts);
                        }

                        // Enumerate through the public IEnumerable<T> surface. Null/default entries
                        // are allowed for TestItem because the buffer is reference-type constrained and
                        // snapshot fallback paths may use default(T); the invariant here is safety and
                        // bounded snapshot size.
                        var enumerated = 0;
                        foreach (TestItem? item in buffer)
                        {
                            _ = item;
                            enumerated++;

                            if (enumerated > capacity)
                            {
                                Interlocked.Increment(ref enumerationLengthViolations);
                                cts.Cancel();
                                break;
                            }
                        }

                        Interlocked.Increment(ref enumerationAttempts);

                        var observedMax = Volatile.Read(ref maxEnumeratedLength);
                        while (enumerated > observedMax &&
                               Interlocked.CompareExchange(ref maxEnumeratedLength, enumerated, observedMax) != observedMax)
                        {
                            observedMax = Volatile.Read(ref maxEnumeratedLength);
                        }
                    }
                    catch (Exception ex)
                    {
                        Interlocked.CompareExchange(ref firstException, ex, null);
                        Interlocked.Increment(ref faults);
                        cts.Cancel();
                    }

                    Thread.Yield();
                }
            }));

        Task[] allTasks = workers.ToArray();

        // Release all workers together so enumeration occurs while other workers mutate the buffer.
        startGate.Set();

        Thread.Sleep(durationMs);
        cts.Cancel();

        var completed = Task.WaitAll(allTasks, deadlockTimeoutMs);

        TestContext.WriteLine(
            $"AllowOverwrite={allowOverwrite}, Count={buffer.Count}, Capacity={buffer.Capacity}, WorkerCount={workerCount}, " +
            $"WriteAttempts={writeAttempts}, ReadAttempts={readAttempts}, EnumerationAttempts={enumerationAttempts}, " +
            $"MaxEnumeratedLength={maxEnumeratedLength}, EnumerationLengthViolations={enumerationLengthViolations}, " +
            $"Faults={faults}, Completed={completed}, FirstException={firstException}");

        Assert.IsTrue(
            completed,
            $"Tasks did not complete within {deadlockTimeoutMs} ms — possible deadlock, blocked operation, or livelock.");

        Assert.AreEqual(
            0,
            faults,
            $"Enumeration, TryEnqueue, and TryDequeue must not throw under concurrent mutation. First exception: {firstException}");

        Assert.AreEqual(
            0,
            enumerationLengthViolations,
            "Enumeration must never produce more items than Capacity.");

        Assert.IsGreaterThan(
            0, writeAttempts,
            "Workers must have exercised TryEnqueue during the enumeration stress run.");

        Assert.IsGreaterThan(
            0, readAttempts,
            "Workers must have exercised TryDequeue during the enumeration stress run.");

        Assert.IsGreaterThan(
            0, enumerationAttempts,
            "Workers must have exercised enumeration during the stress run.");

        Assert.IsLessThanOrEqualTo(
            capacity, maxEnumeratedLength,
            "The largest enumerated snapshot must not exceed Capacity.");

        Assert.IsTrue(
            buffer.Count >= 0 && buffer.Count <= buffer.Capacity,
            "Final Count must remain within [0, Capacity].");
    }

    // -----------------------------------------------------------------------------------------
    // ItemEvicted handler lifecycle changes under concurrent eviction load.
    //
    // One stable handler remains subscribed for the whole stress run, giving deterministic proof
    // that evictions are delivered while other handlers are repeatedly subscribed and unsubscribed.
    //
    // Transient handlers race add/remove operations against active evictions. The test detects
    // unsafe event-field manipulation, corrupted invocation lists, unexpected event-delivery
    // exceptions, and workers that fail to terminate after cancellation.
    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that concurrent subscribe/unsubscribe of <see cref="ConcurrentCircularBuffer{T}.ItemEvicted" /> handlers does
    /// not throw, corrupt event delivery, or prevent workers from terminating.
    /// </summary>
    [TestMethod]
    public void StressTest_WhenEventHandlersSubscribedAndUnsubscribedConcurrently_ShouldDeliverEventsConsistently()
    {
        const int capacity = 4;
        const int writerCount = 2;
        const int subscriberCount = 4;
        const int workerCount = writerCount + subscriberCount;
        const int durationMs = 2_000;
        const int startupTimeoutMs = 5_000;
        const int deadlockTimeoutMs = durationMs + 5_000;

        var buffer = new ConcurrentCircularBuffer<TestItem>(capacity, allowOverwrite: true);

        using var cts = new CancellationTokenSource();
        using var startGate = new ManualResetEventSlim(false);
        using var workersReady = new CountdownEvent(workerCount);
        using var subscribersExercised = new CountdownEvent(subscriberCount);

        var faults = 0;
        var stableHandlerEvictions = 0;
        var transientHandlerEvictions = 0;
        var subscribeOperations = 0;
        var unsubscribeOperations = 0;
        var writeAttempts = 0;

        Exception? firstException = null;

        for (var i = 0; i < capacity; i++)
        {
            buffer.Enqueue(new TestItem(i));
        }

        Action<TestItem?> stableHandler = _ => Interlocked.Increment(ref stableHandlerEvictions);
        buffer.ItemEvicted += stableHandler;

        try
        {
            Task CreateWorker(Action action)
            {
                return Task.Factory.StartNew(
                    action,
                    CancellationToken.None,
                    TaskCreationOptions.LongRunning,
                    TaskScheduler.Default);
            }

            Task[] writers = Enumerable.Range(0, writerCount)
                .Select(_ => CreateWorker(() =>
                {
                    workersReady.Signal();
                    startGate.Wait();

                    var value = 0;

                    while (!cts.Token.IsCancellationRequested)
                    {
                        try
                        {
                            buffer.TryEnqueue(new TestItem(++value));
                            Interlocked.Increment(ref writeAttempts);
                        }
                        catch (Exception ex)
                        {
                            Interlocked.CompareExchange(ref firstException, ex, null);
                            Interlocked.Increment(ref faults);
                            cts.Cancel();
                        }

                        Thread.Yield();
                    }
                }))
                .ToArray();

            Task[] subscribers = Enumerable.Range(0, subscriberCount)
                .Select(_ => CreateWorker(() =>
                {
                    workersReady.Signal();
                    startGate.Wait();

                    var signaledFirstSubscribe = false;
                    Action<TestItem?> transientHandler = _ => Interlocked.Increment(ref transientHandlerEvictions);

                    while (!cts.Token.IsCancellationRequested)
                    {
                        var subscribed = false;

                        try
                        {
                            buffer.ItemEvicted += transientHandler;
                            subscribed = true;

                            Interlocked.Increment(ref subscribeOperations);

                            if (!signaledFirstSubscribe)
                            {
                                subscribersExercised.Signal();
                                signaledFirstSubscribe = true;
                            }

                            Thread.Yield();
                        }
                        catch (Exception ex)
                        {
                            Interlocked.CompareExchange(ref firstException, ex, null);
                            Interlocked.Increment(ref faults);
                            cts.Cancel();
                        }
                        finally
                        {
                            if (subscribed)
                            {
                                try
                                {
                                    buffer.ItemEvicted -= transientHandler;
                                    Interlocked.Increment(ref unsubscribeOperations);
                                }
                                catch (Exception ex)
                                {
                                    Interlocked.CompareExchange(ref firstException, ex, null);
                                    Interlocked.Increment(ref faults);
                                    cts.Cancel();
                                }
                            }
                        }

                        Thread.Yield();
                    }
                }))
                .ToArray();

            Task[] allTasks = writers.Concat(subscribers).ToArray();

            var allWorkersReachedStartGate = workersReady.Wait(startupTimeoutMs);

            if (!allWorkersReachedStartGate)
            {
                cts.Cancel();
                startGate.Set();
                Task.WaitAll(allTasks, deadlockTimeoutMs);

                Assert.Fail(
                    $"Not all stress workers reached the start gate within {startupTimeoutMs} ms. " +
                    $"This indicates test scheduling starvation rather than event-handler failure.");
            }

            startGate.Set();

            var allSubscribersStarted = subscribersExercised.Wait(startupTimeoutMs);

            if (!allSubscribersStarted)
            {
                cts.Cancel();
                Task.WaitAll(allTasks, deadlockTimeoutMs);

                Assert.Fail(
                    $"Transient subscriber workers did not perform their first subscription within {startupTimeoutMs} ms. " +
                    $"SubscribeOperations={subscribeOperations}, UnsubscribeOperations={unsubscribeOperations}, " +
                    $"WriteAttempts={writeAttempts}, Faults={faults}, FirstException={firstException}");
            }

            Thread.Sleep(durationMs);
            cts.Cancel();

            var completed = Task.WaitAll(allTasks, deadlockTimeoutMs);

            TestContext.WriteLine(
                $"Count={buffer.Count}, Capacity={buffer.Capacity}, WriteAttempts={writeAttempts}, " +
                $"StableHandlerEvictions={stableHandlerEvictions}, TransientHandlerEvictions={transientHandlerEvictions}, " +
                $"SubscribeOperations={subscribeOperations}, UnsubscribeOperations={unsubscribeOperations}, " +
                $"Faults={faults}, Completed={completed}, FirstException={firstException}");

            Assert.IsTrue(
                completed,
                $"Tasks did not complete within {deadlockTimeoutMs} ms — possible deadlock, blocked operation, or livelock.");

            Assert.AreEqual(
                0,
                faults,
                $"No exceptions expected while subscribing, unsubscribing, or delivering ItemEvicted. First exception: {firstException}");

            Assert.IsGreaterThan(
                0, writeAttempts,
                "Writers must have exercised enqueue/eviction pressure during the stress run.");

            Assert.IsGreaterThan(
                0, subscribeOperations,
                "Subscribers must have exercised ItemEvicted subscription during the stress run.");

            Assert.IsGreaterThan(
                0, unsubscribeOperations,
                "Subscribers must have exercised ItemEvicted unsubscription during the stress run.");

            Assert.IsGreaterThan(
                0, stableHandlerEvictions,
                "A registered ItemEvicted handler must receive at least some eviction events during concurrent handler churn.");

            Assert.IsTrue(
                buffer.Count >= 0 && buffer.Count <= buffer.Capacity,
                "Count must remain within [0, Capacity] after event-handler stress.");
        }
        finally
        {
            buffer.ItemEvicted -= stableHandler;
        }
    }

    // -----------------------------------------------------------------------------------------
    // High worker-count stress with deadlock-detection timeout.
    //
    // This test runs many concurrent workers against the same buffer to increase scheduling
    // pressure and exercise common lock-ordering, retry-loop, and visibility failures under
    // contention.
    //
    // Each worker performs writer, reader, and inspector operations. This avoids scheduler-
    // dependent starvation where writer-only workers can monopolise the run before reader or
    // inspector workers are scheduled.
    //
    // The test uses a bounded Task.WaitAll overload so a deadlock or non-terminating operation
    // surfaces as an assertion failure rather than hanging the test runner indefinitely.
    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that concurrent workers can enqueue, dequeue, and inspect under high concurrency,
    /// and that all workers terminate within the deadlock timeout without unexpected faults.
    /// </summary>
    [TestMethod]
    public void StressTest_WhenHighConcurrency_ShouldNotDeadlockOrLivelock()
    {
        const int capacity = 64;
        const int durationMs = 3_000;
        const int deadlockTimeoutMs = durationMs + 5_000;

        var buffer = new ConcurrentCircularBuffer<TestItem>(capacity, allowOverwrite: true);
        using var cts = new CancellationTokenSource();
        using var startGate = new ManualResetEventSlim(false);

        var faults = 0;
        var writeAttempts = 0;
        var readAttempts = 0;
        var inspectionAttempts = 0;

        Exception? firstException = null;

        // Use a high number of workers. Each worker performs all operation categories so the test
        // cannot pass through writer-only scheduler dominance while never reaching reader or
        // inspector paths.
        var workerCount = Math.Max(8, Environment.ProcessorCount * 4);

        Task StartWorker(Action action) =>
            Task.Factory.StartNew(
                action,
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);

        IEnumerable<Task> workers = Enumerable.Range(0, workerCount).Select(workerIndex =>
            StartWorker(() =>
            {
                startGate.Wait();

                var value = workerIndex * 1_000_000;

                while (!cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        // Writer path: publish a new reference-type payload into the buffer.
                        buffer.TryEnqueue(new TestItem(++value));
                        Interlocked.Increment(ref writeAttempts);

                        // Reader path: attempt to remove an item. Success is not required; the
                        // stress invariant is that the operation remains safe under contention.
                        buffer.TryDequeue(out TestItem? _);
                        Interlocked.Increment(ref readAttempts);

                        // Inspector path: exercise non-mutating members while other workers are
                        // concurrently mutating the same buffer.
                        _ = buffer.Count;
                        _ = buffer.Capacity;
                        buffer.ToArray();
                        buffer.TryPeek(out TestItem? _);

                        Interlocked.Increment(ref inspectionAttempts);
                    }
                    catch (Exception ex)
                    {
                        Interlocked.CompareExchange(ref firstException, ex, null);
                        Interlocked.Increment(ref faults);
                        cts.Cancel();
                    }

                    // Keep the loop hot, but yield periodically so no single worker monopolises
                    // execution on loaded CI runners.
                    Thread.Yield();
                }
            }));

        Task[] allTasks = workers.ToArray();

        // Release all workers together so the test creates actual contention rather than a staged
        // producer/consumer/inspector sequence.
        startGate.Set();

        Thread.Sleep(durationMs);
        cts.Cancel();

        // A false result means at least one worker did not leave its loop after cancellation,
        // consistent with a deadlock, blocked internal operation, or non-terminating retry path.
        var completed = Task.WaitAll(allTasks, deadlockTimeoutMs);

        TestContext.WriteLine(
            $"Count={buffer.Count}, Capacity={buffer.Capacity}, WorkerCount={workerCount}, " +
            $"WriteAttempts={writeAttempts}, ReadAttempts={readAttempts}, InspectionAttempts={inspectionAttempts}, " +
            $"Faults={faults}, Completed={completed}, FirstException={firstException}");

        Assert.IsTrue(
            completed,
            $"Not all tasks completed within {deadlockTimeoutMs} ms — possible deadlock, blocked operation, or livelock.");

        Assert.AreEqual(
            0,
            faults,
            $"No exceptions expected during high-concurrency stress. First exception: {firstException}");

        Assert.IsGreaterThan(
            0, writeAttempts,
            "Workers must have exercised TryEnqueue during the stress run.");

        Assert.IsGreaterThan(
            0, readAttempts,
            "Workers must have exercised TryDequeue during the stress run.");

        Assert.IsGreaterThan(
            0, inspectionAttempts,
            "Workers must have exercised non-mutating members during the stress run.");

        Assert.IsTrue(
            buffer.Count >= 0 && buffer.Count <= buffer.Capacity,
            "Count must remain within [0, Capacity] after high-concurrency stress.");
    }

    // -----------------------------------------------------------------------------------------
    // Mixed public API surface under concurrent load.
    //
    // This test deliberately mixes throwing and non-throwing APIs with snapshot, copy, inspection,
    // containment, and Clear operations. It is not intended to prove a precise accounting invariant,
    // because Clear removes items silently and throwing APIs may legitimately fail depending on the
    // instantaneous state.
    //
    // The invariant is exception safety: only documented InvalidOperationException failures from
    // state-dependent throwing APIs are allowed. All other exception types indicate corruption,
    // invalid bounds, or an unsafe interleaving.
    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that mixed public API usage under concurrent load only raises expected
    /// <see cref="InvalidOperationException" /> failures from state-dependent throwing APIs.
    /// </summary>
    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void StressTest_WhenMixedPublicApiSurfaceIsUsedConcurrently_ShouldOnlyRaiseExpectedExceptions(
        bool allowOverwrite)
    {
        const int capacity = 16;
        const int durationMs = 2_000;
        const int deadlockTimeoutMs = durationMs + 5_000;

        var buffer = new ConcurrentCircularBuffer<TestItem>(capacity, allowOverwrite);
        using var cts = new CancellationTokenSource();
        using var startGate = new ManualResetEventSlim(false);

        var unexpectedFaults = 0;
        var expectedInvalidOperationFaults = 0;
        var countViolations = 0;
        var snapshotLengthViolations = 0;

        var tryEnqueueAttempts = 0;
        var enqueueAttempts = 0;
        var tryDequeueAttempts = 0;
        var dequeueAttempts = 0;
        var tryPeekAttempts = 0;
        var containsAttempts = 0;
        var toArrayAttempts = 0;
        var copyToAttempts = 0;
        var clearAttempts = 0;
        var inspectionAttempts = 0;

        Exception? firstUnexpectedException = null;

        var workerCount = Math.Max(4, Environment.ProcessorCount * 2);

        Task StartWorker(Action action) =>
            Task.Factory.StartNew(
                action,
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);

        // Seed the buffer so the mixed workload starts from a meaningful state rather than an
        // entirely empty buffer.
        for (var i = 0; i < capacity / 2; i++)
            buffer.Enqueue(new TestItem(i));

        IEnumerable<Task> workers = Enumerable.Range(0, workerCount).Select(workerIndex =>
            StartWorker(() =>
            {
                startGate.Wait();

                var value = workerIndex * 1_000_000;
                var operation = workerIndex;

                while (!cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        switch (operation++ % 10)
                        {
                            case 0:
                                // Non-throwing enqueue path.
                                buffer.TryEnqueue(new TestItem(++value));
                                Interlocked.Increment(ref tryEnqueueAttempts);
                                break;

                            case 1:
                                // Throwing enqueue path. InvalidOperationException is expected only
                                // when overwrite is disabled and the buffer is full.
                                Interlocked.Increment(ref enqueueAttempts);
                                try
                                {
                                    buffer.Enqueue(new TestItem(++value));
                                }
                                catch (InvalidOperationException) when (!allowOverwrite)
                                {
                                    Interlocked.Increment(ref expectedInvalidOperationFaults);
                                }

                                break;

                            case 2:
                                // Non-throwing dequeue path.
                                buffer.TryDequeue(out TestItem? _);
                                Interlocked.Increment(ref tryDequeueAttempts);
                                break;

                            case 3:
                                // Throwing dequeue path. InvalidOperationException is expected when
                                // the buffer is empty at the instant of the call.
                                Interlocked.Increment(ref dequeueAttempts);
                                try
                                {
                                    buffer.Dequeue();
                                }
                                catch (InvalidOperationException)
                                {
                                    Interlocked.Increment(ref expectedInvalidOperationFaults);
                                }

                                break;

                            case 4:
                                // Non-throwing peek path.
                                buffer.TryPeek(out TestItem? _);
                                Interlocked.Increment(ref tryPeekAttempts);
                                break;

                            case 5:
                                // Contains is exercised with a valid reference-type payload. The item
                                // need not be present; this is an exception-safety stress path.
                                buffer.Contains(new TestItem(value));
                                Interlocked.Increment(ref containsAttempts);
                                break;

                            case 6:
                                // ToArray must remain safe and bounded while other workers mutate.
                                TestItem[] snapshot = buffer.ToArray();
                                Interlocked.Increment(ref toArrayAttempts);

                                if (snapshot.Length > capacity)
                                    Interlocked.Increment(ref snapshotLengthViolations);

                                break;

                            case 7:
                                // CopyTo uses a capacity-sized destination, which should always be
                                // sufficient because Count must never exceed Capacity.
                                var destination = new TestItem[capacity];
                                buffer.CopyTo(destination, 0);
                                Interlocked.Increment(ref copyToAttempts);
                                break;

                            case 8:
                                // Clear silently removes items and therefore prevents this mixed test
                                // from using enqueue-minus-dequeue accounting.
                                buffer.Clear();
                                Interlocked.Increment(ref clearAttempts);
                                break;

                            default:
                                // Basic read-side inspection paths.
                                var count = buffer.Count;
                                _ = buffer.Capacity;
                                Interlocked.Increment(ref inspectionAttempts);

                                if (count < 0 || count > buffer.Capacity)
                                    Interlocked.Increment(ref countViolations);

                                break;
                        }
                    }
                    catch (InvalidOperationException ex)
                    {
                        // Dequeue is handled inside its own case. Enqueue in overwrite mode should not
                        // throw InvalidOperationException, and non-throwing APIs should not throw it
                        // either, so any uncaught InvalidOperationException is unexpected.
                        Interlocked.CompareExchange(ref firstUnexpectedException, ex, null);
                        Interlocked.Increment(ref unexpectedFaults);
                        cts.Cancel();
                    }
                    catch (Exception ex)
                    {
                        Interlocked.CompareExchange(ref firstUnexpectedException, ex, null);
                        Interlocked.Increment(ref unexpectedFaults);
                        cts.Cancel();
                    }

                    Thread.Yield();
                }
            }));

        Task[] allTasks = workers.ToArray();

        // Release all workers together so the mixed public API surface is exercised under contention.
        startGate.Set();

        Thread.Sleep(durationMs);
        cts.Cancel();

        var completed = Task.WaitAll(allTasks, deadlockTimeoutMs);

        TestContext.WriteLine(
            $"AllowOverwrite={allowOverwrite}, Count={buffer.Count}, Capacity={buffer.Capacity}, WorkerCount={workerCount}, " +
            $"TryEnqueueAttempts={tryEnqueueAttempts}, EnqueueAttempts={enqueueAttempts}, " +
            $"TryDequeueAttempts={tryDequeueAttempts}, DequeueAttempts={dequeueAttempts}, " +
            $"TryPeekAttempts={tryPeekAttempts}, ContainsAttempts={containsAttempts}, ToArrayAttempts={toArrayAttempts}, " +
            $"CopyToAttempts={copyToAttempts}, ClearAttempts={clearAttempts}, InspectionAttempts={inspectionAttempts}, " +
            $"ExpectedInvalidOperationFaults={expectedInvalidOperationFaults}, UnexpectedFaults={unexpectedFaults}, " +
            $"CountViolations={countViolations}, SnapshotLengthViolations={snapshotLengthViolations}, " +
            $"Completed={completed}, FirstUnexpectedException={firstUnexpectedException}");

        Assert.IsTrue(
            completed,
            $"Tasks did not complete within {deadlockTimeoutMs} ms — possible deadlock, blocked operation, or livelock.");

        Assert.AreEqual(
            0,
            unexpectedFaults,
            $"Only documented InvalidOperationException failures are expected from state-dependent APIs. First unexpected exception: {firstUnexpectedException}");

        Assert.AreEqual(
            0,
            countViolations,
            "Count must remain within [0, Capacity] during mixed API stress.");

        Assert.AreEqual(
            0,
            snapshotLengthViolations,
            "ToArray must never return more items than Capacity during mixed API stress.");

        Assert.IsGreaterThan(0, tryEnqueueAttempts, "Workers must have exercised TryEnqueue.");
        Assert.IsGreaterThan(0, enqueueAttempts, "Workers must have exercised Enqueue.");
        Assert.IsGreaterThan(0, tryDequeueAttempts, "Workers must have exercised TryDequeue.");
        Assert.IsGreaterThan(0, dequeueAttempts, "Workers must have exercised Dequeue.");
        Assert.IsGreaterThan(0, tryPeekAttempts, "Workers must have exercised TryPeek.");
        Assert.IsGreaterThan(0, containsAttempts, "Workers must have exercised Contains.");
        Assert.IsGreaterThan(0, toArrayAttempts, "Workers must have exercised ToArray.");
        Assert.IsGreaterThan(0, copyToAttempts, "Workers must have exercised CopyTo.");
        Assert.IsGreaterThan(0, clearAttempts, "Workers must have exercised Clear.");
        Assert.IsGreaterThan(0, inspectionAttempts, "Workers must have exercised Count and Capacity.");

        Assert.IsTrue(
            buffer.Count >= 0 && buffer.Count <= buffer.Capacity,
            "Final Count must remain within [0, Capacity].");
    }

    // -----------------------------------------------------------------------------------------
    // ToArray() and CopyTo() exercised in the hot path alongside concurrent mutations.
    //
    // Snapshot operations must be safe while mutations are active. A capacity-sized CopyTo
    // destination should always be sufficient because Count must never exceed Capacity.
    //
    // Each worker performs enqueue, dequeue, ToArray, and CopyTo operations. This avoids
    // scheduler-dependent starvation where mutator-only workers can monopolise the run before
    // snapshotter or copier workers are scheduled.
    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.ToArray" /> and <see cref="ConcurrentCircularBuffer{T}.CopyTo" />
    /// interleaved with live mutations never throw and return arrays of length within capacity.
    /// </summary>
    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void StressTest_WhenSnapshotOperationsInterleaveWithMutations_ShouldNeverThrowOrCorrupt(
        bool allowOverwrite)
    {
        const int capacity = 16;
        const int durationMs = 2_000;
        const int deadlockTimeoutMs = durationMs + 5_000;

        var buffer = new ConcurrentCircularBuffer<TestItem>(capacity, allowOverwrite);
        using var cts = new CancellationTokenSource();
        using var startGate = new ManualResetEventSlim(false);

        var faults = 0;
        var snapshotLengthViolations = 0;
        var writeAttempts = 0;
        var readAttempts = 0;
        var snapshotAttempts = 0;
        var copyAttempts = 0;

        Exception? firstException = null;

        var workerCount = Math.Max(4, Environment.ProcessorCount * 2);

        Task StartWorker(Action action) =>
            Task.Factory.StartNew(
                action,
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);

        // Seed the buffer so snapshot and copy operations immediately encounter a non-trivial state.
        for (var i = 0; i < capacity / 2; i++)
            buffer.Enqueue(new TestItem(i));

        IEnumerable<Task> workers = Enumerable.Range(0, workerCount).Select(workerIndex =>
            StartWorker(() =>
            {
                startGate.Wait();

                var value = workerIndex * 1_000_000;

                while (!cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        // Mutator path: keep the buffer changing while snapshot operations run.
                        buffer.TryEnqueue(new TestItem(++value));
                        Interlocked.Increment(ref writeAttempts);

                        buffer.TryDequeue(out TestItem? _);
                        Interlocked.Increment(ref readAttempts);

                        // Snapshot path: ToArray must remain safe and bounded under live mutation.
                        TestItem[] snapshot = buffer.ToArray();
                        Interlocked.Increment(ref snapshotAttempts);

                        if (snapshot.Length > capacity)
                            Interlocked.Increment(ref snapshotLengthViolations);

                        // Copy path: a capacity-sized destination must remain sufficient because
                        // Count must never exceed Capacity.
                        var destination = new TestItem[capacity];
                        buffer.CopyTo(destination, 0);
                        Interlocked.Increment(ref copyAttempts);
                    }
                    catch (Exception ex)
                    {
                        Interlocked.CompareExchange(ref firstException, ex, null);
                        Interlocked.Increment(ref faults);
                        cts.Cancel();
                    }

                    Thread.Yield();
                }
            }));

        Task[] allTasks = workers.ToArray();

        // Release all workers together so snapshot/copy operations execute in the hot mutation path.
        startGate.Set();

        Thread.Sleep(durationMs);
        cts.Cancel();

        var completed = Task.WaitAll(allTasks, deadlockTimeoutMs);

        TestContext.WriteLine(
            $"Count={buffer.Count}, Capacity={buffer.Capacity}, WorkerCount={workerCount}, " +
            $"WriteAttempts={writeAttempts}, ReadAttempts={readAttempts}, SnapshotAttempts={snapshotAttempts}, " +
            $"CopyAttempts={copyAttempts}, Faults={faults}, LengthViolations={snapshotLengthViolations}, " +
            $"Completed={completed}, FirstException={firstException}");

        Assert.IsTrue(
            completed,
            $"Tasks did not complete within {deadlockTimeoutMs} ms — possible deadlock, blocked operation, or livelock.");

        Assert.AreEqual(
            0,
            faults,
            $"TryEnqueue, TryDequeue, ToArray, and CopyTo must not throw during snapshot stress. First exception: {firstException}");

        Assert.AreEqual(
            0,
            snapshotLengthViolations,
            "ToArray must never return more items than Capacity.");

        Assert.IsGreaterThan(
            0, writeAttempts,
            "Workers must have exercised TryEnqueue during the stress run.");

        Assert.IsGreaterThan(
            0, readAttempts,
            "Workers must have exercised TryDequeue during the stress run.");

        Assert.IsGreaterThan(
            0, snapshotAttempts,
            "Workers must have exercised ToArray during the stress run.");

        Assert.IsGreaterThan(
            0, copyAttempts,
            "Workers must have exercised CopyTo during the stress run.");

        Assert.IsTrue(
            buffer.Count >= 0 && buffer.Count <= buffer.Capacity,
            "Final Count must remain within [0, Capacity].");
    }

    // -----------------------------------------------------------------------------------------
    // Throwing Enqueue() and Dequeue() paths under concurrent load.
    //
    // TryEnqueue/TryDequeue never throw for ordinary full/empty states, so this test exercises
    // the throwing API paths. InvalidOperationException is the only expected exception type for
    // full-buffer Enqueue in non-overwrite mode or empty-buffer Dequeue.
    //
    // Each worker performs both Enqueue and Dequeue operations. This avoids scheduler-dependent
    // starvation where writer-only workers can monopolise the run before reader-only workers are
    // scheduled.
    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that the throwing <see cref="ConcurrentCircularBuffer{T}.Enqueue" /> and
    /// <see cref="ConcurrentCircularBuffer{T}.Dequeue" /> paths only raise <see cref="InvalidOperationException" />; in
    /// overwrite mode, <see cref="ConcurrentCircularBuffer{T}.Enqueue" /> never throws.
    /// </summary>
    [TestMethod]
    [DataRow(10, true)]
    [DataRow(10, false)]
    public void StressTest_WhenThrowingApiPathsUsed_ShouldOnlyRaiseExpectedExceptions(
        int capacity,
        bool allowOverwrite)
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(capacity, allowOverwrite);
        using var cts = new CancellationTokenSource();
        using var startGate = new ManualResetEventSlim(false);

        var unexpectedEnqueueFaults = 0;
        var unexpectedDequeueFaults = 0;
        var expectedEnqueueFaults = 0;
        var expectedDequeueFaults = 0;
        var enqueueAttempts = 0;
        var dequeueAttempts = 0;
        var enqueueSuccesses = 0;
        var dequeueSuccesses = 0;

        var warmupExpectedEnqueueFaults = 0;

        Exception? firstUnexpectedException = null;

        var workerCount = Math.Max(4, Environment.ProcessorCount * 2);
        const int durationMs = 2_000;
        const int deadlockTimeoutMs = durationMs + 5_000;

        Task StartWorker(Action action) =>
            Task.Factory.StartNew(
                action,
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);

        // Deterministic warmup for non-overwrite mode: prove the rejected Enqueue path is valid
        // without letting that observation satisfy the concurrent-phase counters.
        if (!allowOverwrite)
        {
            for (var i = 0; i < capacity; i++)
                buffer.Enqueue(new TestItem(-i - 1));

            try
            {
                buffer.Enqueue(new TestItem(-100));
            }
            catch (InvalidOperationException)
            {
                warmupExpectedEnqueueFaults++;
            }

            while (buffer.TryDequeue(out TestItem? _))
            {
                // Drain back to empty so the stress phase starts from a neutral state.
            }
        }

        IEnumerable<Task> workers = Enumerable.Range(0, workerCount).Select(workerIndex =>
            StartWorker(() =>
            {
                startGate.Wait();

                var value = workerIndex * 1_000_000;

                while (!cts.Token.IsCancellationRequested)
                {
                    // Exercise the throwing Enqueue path. In non-overwrite mode,
                    // InvalidOperationException is expected when the buffer is full. In overwrite
                    // mode, Enqueue should not fail due to fullness.
                    try
                    {
                        Interlocked.Increment(ref enqueueAttempts);
                        buffer.Enqueue(new TestItem(++value));
                        Interlocked.Increment(ref enqueueSuccesses);
                    }
                    catch (InvalidOperationException)
                    {
                        Interlocked.Increment(ref expectedEnqueueFaults);
                    }
                    catch (Exception ex)
                    {
                        Interlocked.CompareExchange(ref firstUnexpectedException, ex, null);
                        Interlocked.Increment(ref unexpectedEnqueueFaults);
                        cts.Cancel();
                    }

                    // Exercise the throwing Dequeue path independently of Enqueue. This must still
                    // run even when Enqueue threw above, otherwise full-buffer races can starve the
                    // Dequeue half of the test.
                    try
                    {
                        Interlocked.Increment(ref dequeueAttempts);
                        buffer.Dequeue();
                        Interlocked.Increment(ref dequeueSuccesses);
                    }
                    catch (InvalidOperationException)
                    {
                        Interlocked.Increment(ref expectedDequeueFaults);
                    }
                    catch (Exception ex)
                    {
                        Interlocked.CompareExchange(ref firstUnexpectedException, ex, null);
                        Interlocked.Increment(ref unexpectedDequeueFaults);
                        cts.Cancel();
                    }

                    Thread.Yield();
                }
            }));

        Task[] allTasks = workers.ToArray();

        // Release all workers together so throwing Enqueue and Dequeue paths are exercised under
        // real contention.
        startGate.Set();

        Thread.Sleep(durationMs);
        cts.Cancel();

        var completed = Task.WaitAll(allTasks, deadlockTimeoutMs);

        TestContext.WriteLine(
            $"Count={buffer.Count}, Capacity={buffer.Capacity}, WorkerCount={workerCount}, " +
            $"EnqueueAttempts={enqueueAttempts}, EnqueueSuccesses={enqueueSuccesses}, " +
            $"ExpectedEnqueueFaults={expectedEnqueueFaults}, UnexpectedEnqueueFaults={unexpectedEnqueueFaults}, " +
            $"DequeueAttempts={dequeueAttempts}, DequeueSuccesses={dequeueSuccesses}, " +
            $"ExpectedDequeueFaults={expectedDequeueFaults}, UnexpectedDequeueFaults={unexpectedDequeueFaults}, " +
            $"WarmupExpectedEnqueueFaults={warmupExpectedEnqueueFaults}, Completed={completed}, " +
            $"FirstUnexpectedException={firstUnexpectedException}");

        Assert.IsTrue(
            completed,
            $"Tasks did not complete within {deadlockTimeoutMs} ms — possible deadlock, blocked operation, or livelock.");

        Assert.AreEqual(
            0,
            unexpectedEnqueueFaults,
            $"Enqueue must only throw InvalidOperationException. First unexpected exception: {firstUnexpectedException}");

        Assert.AreEqual(
            0,
            unexpectedDequeueFaults,
            $"Dequeue must only throw InvalidOperationException. First unexpected exception: {firstUnexpectedException}");

        Assert.IsGreaterThan(
            0, enqueueAttempts,
            "Workers must have exercised Enqueue during the stress run.");

        Assert.IsGreaterThan(
            0, dequeueAttempts,
            "Workers must have exercised Dequeue during the stress run.");

        Assert.IsTrue(
            buffer.Count >= 0 && buffer.Count <= buffer.Capacity,
            "Count must remain within [0, Capacity].");

        if (allowOverwrite)
        {
            Assert.AreEqual(
                0,
                expectedEnqueueFaults,
                "Enqueue must never throw InvalidOperationException in overwrite mode.");
        }
        else
        {
            Assert.AreEqual(
                1,
                warmupExpectedEnqueueFaults,
                "Warmup must verify that Enqueue rejects a full non-overwrite buffer.");
        }

        // expectedDequeueFaults > 0 is intentionally not asserted. This stress test verifies
        // exception type safety if the empty-buffer path is reached; deterministic empty-buffer
        // behaviour belongs in the focused Dequeue_WhenBufferIsEmpty_ShouldThrowInvalidOperation test.
    }

    // -----------------------------------------------------------------------------------------
    // ToArray must never return a torn or impossible generation window.
    //
    // Each successful enqueue publishes an item tagged with a globally monotonic generation.
    // Readers repeatedly call ToArray while producers and consumers concurrently mutate the buffer.
    //
    // For each non-empty snapshot, ignoring documented default/null fallback entries, the test
    // verifies two invariants:
    //   - observed generations remain strictly increasing in snapshot order;
    //   - max - min generation remains less than the buffer capacity.
    //
    // A violation indicates that ToArray combined slots from incompatible logical windows, such as
    // returning a stale slot from an older generation alongside slots from a newer generation.
    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.ToArray" /> does not return a torn generation window while the buffer
    /// is under concurrent enqueue, dequeue, and overwrite pressure.
    /// </summary>
    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void ToArray_WhenUnderConcurrentMutationPressure_ShouldNeverReturnTornGenerationMix(
        bool allowOverwrite)
    {
        const int capacity = 32;
        const int minimumOccupancy = capacity / 2;

        // Target is an early-exit optimisation only. It must not be treated as correctness.
        const int targetValidatedSnapshots = 10_000;

        // Minimum is the actual participation threshold. The invariant is validated by the
        // absence of violations, not by hitting an exact throughput number on every machine.
        const int minimumValidatedSnapshots = 1_024;

        const int timeoutMs = 5_000;
        const int deadlockTimeoutMs = timeoutMs + 2_000;

        var buffer = new ConcurrentCircularBuffer<TestItem>(capacity, allowOverwrite);
        using var cts = new CancellationTokenSource();
        using var startGate = new ManualResetEventSlim(false);

        var generation = 0;
        var monotonicityViolations = 0;
        var windowViolations = 0;
        var snapshotFaults = 0;
        var mutationFaults = 0;
        var snapshotsTaken = 0;
        var snapshotsValidated = 0;
        var writeAttempts = 0;
        var readAttempts = 0;

        Exception? unexpectedException = null;

        // Serialise successful generation publication with enqueue. This ensures generation
        // order matches insertion order and prevents failed TryEnqueue attempts from creating
        // artificial generation gaps in the allowOverwrite:false case.
        var writerLock = new object();

        // Start from a full buffer so overwrite mode immediately exercises eviction and
        // non-overwrite mode begins from the pressure point where writers must wait for consumers.
        for (var i = 0; i < capacity; i++)
        {
            var gen = ++generation;
            Assert.IsTrue(buffer.TryEnqueue(new TestItem(gen)));
        }

        var writerThreads = Math.Max(2, Environment.ProcessorCount / 2);
        var readerThreads = 2;
        var consumerThreads = 1;

        // Producers continuously attempt to publish the next generation. The generation is only
        // committed after TryEnqueue succeeds, so snapshots never contain gaps caused solely by
        // failed enqueue attempts.
        IEnumerable<Task> writers = Enumerable.Range(0, writerThreads).Select(_ =>
            Task.Run(() =>
            {
                startGate.Wait();

                while (!cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        var enqueued = false;

                        lock (writerLock)
                        {
                            var next = generation + 1;

                            if (buffer.TryEnqueue(new TestItem(next)))
                            {
                                generation = next;
                                enqueued = true;
                            }
                        }

                        Interlocked.Increment(ref writeAttempts);

                        if (!enqueued)
                            Thread.Yield();
                    }
                    catch (Exception ex)
                    {
                        Interlocked.CompareExchange(ref unexpectedException, ex, null);
                        Interlocked.Increment(ref mutationFaults);
                        cts.Cancel();
                    }
                }
            }));

        // Consumers create removal pressure. The low-water mark keeps the buffer populated enough
        // for readers to validate meaningful snapshots while still allowing concurrent dequeue
        // and refill churn, especially when allowOverwrite is false.
        IEnumerable<Task> consumers = Enumerable.Range(0, consumerThreads).Select(_ =>
            Task.Run(() =>
            {
                startGate.Wait();

                while (!cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        if (buffer.Count > minimumOccupancy)
                        {
                            buffer.TryDequeue(out TestItem? _);
                            Interlocked.Increment(ref readAttempts);
                        }
                        else
                        {
                            Thread.Yield();
                        }
                    }
                    catch (Exception ex)
                    {
                        Interlocked.CompareExchange(ref unexpectedException, ex, null);
                        Interlocked.Increment(ref mutationFaults);
                        cts.Cancel();
                    }
                }
            }));

        // Readers repeatedly snapshot the buffer and validate that the returned generation window
        // is coherent. Null/default entries are ignored because ToArray may use documented fallback
        // values for slots that could not be stabilised within its retry budget.
        IEnumerable<Task> readers = Enumerable.Range(0, readerThreads).Select(_ =>
            Task.Run(() =>
            {
                startGate.Wait();

                while (!cts.Token.IsCancellationRequested)
                {
                    TestItem[] snap;

                    try
                    {
                        snap = buffer.ToArray();
                    }
                    catch (Exception ex)
                    {
                        Interlocked.CompareExchange(ref unexpectedException, ex, null);
                        Interlocked.Increment(ref snapshotFaults);
                        cts.Cancel();
                        continue;
                    }

                    Interlocked.Increment(ref snapshotsTaken);

                    // Find the first non-null item. A snapshot containing only fallback/default
                    // entries does not provide a useful generation window to validate.
                    var firstNonNull = 0;
                    while (firstNonNull < snap.Length && snap[firstNonNull] is null)
                        firstNonNull++;

                    if (firstNonNull == snap.Length)
                        continue;

                    TestItem first = snap[firstNonNull]!;
                    var min = first.Value;
                    var max = first.Value;
                    var previous = first.Value;
                    var nonNullCount = 1;
                    var monotonic = true;

                    for (var i = firstNonNull + 1; i < snap.Length; i++)
                    {
                        TestItem? item = snap[i];

                        if (item is null)
                            continue;

                        var value = item.Value;

                        if (value <= previous)
                            monotonic = false;

                        if (value < min)
                            min = value;

                        if (value > max)
                            max = value;

                        previous = value;
                        nonNullCount++;
                    }

                    // A single non-null value cannot prove ordering or window coherence, so only
                    // count snapshots with at least two observed values as validated snapshots.
                    if (nonNullCount < 2)
                        continue;

                    if (!monotonic)
                    {
                        Interlocked.Increment(ref monotonicityViolations);
                        cts.Cancel();
                    }

                    if (max - min >= capacity)
                    {
                        Interlocked.Increment(ref windowViolations);
                        cts.Cancel();
                    }

                    if (Interlocked.Increment(ref snapshotsValidated) >= targetValidatedSnapshots)
                        cts.Cancel();
                }
            }));

        Task[] allTasks = writers.Concat(consumers).Concat(readers).ToArray();

        // Release all workers together so ToArray is tested under actual concurrent mutation.
        startGate.Set();

        var reachedTargetOrFault = SpinWait.SpinUntil(
            () =>
                Volatile.Read(ref snapshotsValidated) >= targetValidatedSnapshots ||
                Volatile.Read(ref snapshotFaults) > 0 ||
                Volatile.Read(ref mutationFaults) > 0 ||
                Volatile.Read(ref monotonicityViolations) > 0 ||
                Volatile.Read(ref windowViolations) > 0,
            timeoutMs);

        cts.Cancel();

        var completed = Task.WaitAll(allTasks, deadlockTimeoutMs);

        TestContext.WriteLine(
            $"AllowOverwrite={allowOverwrite}, SnapshotsTaken={snapshotsTaken}, SnapshotsValidated={snapshotsValidated}, " +
            $"TargetValidatedSnapshots={targetValidatedSnapshots}, MinimumValidatedSnapshots={minimumValidatedSnapshots}, " +
            $"ReachedTargetOrFault={reachedTargetOrFault}, WriteAttempts={writeAttempts}, ReadAttempts={readAttempts}, " +
            $"MutationFaults={mutationFaults}, MonotonicityViolations={monotonicityViolations}, " +
            $"WindowViolations={windowViolations}, SnapshotFaults={snapshotFaults}, Completed={completed}, " +
            $"FirstException={unexpectedException}");

        Assert.IsTrue(
            completed,
            $"Tasks did not complete within {deadlockTimeoutMs} ms.");

        Assert.AreEqual(
            0,
            mutationFaults,
            $"Producer/consumer workers must not throw during ToArray stress. First exception: {unexpectedException}");

        Assert.AreEqual(
            0,
            snapshotFaults,
            $"ToArray must not throw under concurrent mutation pressure. First exception: {unexpectedException}");

        Assert.AreEqual(
            0,
            monotonicityViolations,
            "ToArray returned a snapshot whose generations were not strictly increasing — torn read.");

        Assert.AreEqual(
            0,
            windowViolations,
            "ToArray returned a snapshot spanning more generations than the buffer capacity — torn read.");

        Assert.IsGreaterThan(
            0, writeAttempts,
            "Writers must have attempted to publish generations during the stress run.");

        Assert.IsGreaterThan(
            0, readAttempts,
            "Consumers must have exercised TryDequeue during the stress run.");

        Assert.IsGreaterThan(
            0, snapshotsTaken,
            "Readers must have exercised ToArray during the stress run.");

        Assert.IsGreaterThanOrEqualTo(
            minimumValidatedSnapshots, snapshotsValidated,
            $"ToArray must have produced enough meaningful snapshots to validate the concurrency invariant. " +
            $"Expected at least {minimumValidatedSnapshots}, actual {snapshotsValidated}.");
    }

    // -----------------------------------------------------------------------------------------
    // TryPeek under concurrent enqueue, dequeue, and overwrite pressure.
    //
    // Producers publish globally monotonic generations while consumers remove items and overwrite
    // mode allows writers to evict older values. Peekers repeatedly call TryPeek and verify that
    // successful peeks never expose a null item or a generation that could not have been published
    // by the time TryPeek returned.
    //
    // This does not require the peeked value to remain present after the call. Concurrent consumers
    // may legally remove it immediately after TryPeek succeeds.
    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.TryPeek(out T)" /> remains safe under concurrent enqueue,
    /// dequeue, and overwrite pressure, and never returns an impossible generation value.
    /// </summary>
    [TestMethod]
    public void TryPeek_WhenContendedWithEnqueueDequeueAndOverwrite_ShouldReturnPlausibleValue()
    {
        const int capacity = 32;
        const int minimumOccupancy = capacity / 2;

        // Target is an early-exit optimisation only. The minimum below is the participation gate.
        const int targetSuccessfulPeeks = 10_000;
        const int minimumSuccessfulPeeks = 1_024;

        const int timeoutMs = 5_000;
        const int deadlockTimeoutMs = timeoutMs + 2_000;

        var buffer = new ConcurrentCircularBuffer<TestItem>(capacity, allowOverwrite: true);
        using var cts = new CancellationTokenSource();
        using var startGate = new ManualResetEventSlim(false);

        var generation = 0;
        var writeAttempts = 0;
        var dequeueAttempts = 0;
        var peekAttempts = 0;
        var successfulPeeks = 0;
        var emptyPeeks = 0;
        var nullPeekViolations = 0;
        var futureValueViolations = 0;
        var unexpectedFaults = 0;

        Exception? unexpectedException = null;

        // Seed the buffer before contention begins so TryPeek has an immediately meaningful state.
        for (var i = 0; i < capacity; i++)
        {
            var gen = Interlocked.Increment(ref generation);
            Assert.IsTrue(buffer.TryEnqueue(new TestItem(gen)));
        }

        var writerThreads = Math.Max(2, Environment.ProcessorCount / 2);
        var peekerThreads = 2;
        var consumerThreads = 1;

        IEnumerable<Task> writers = Enumerable.Range(0, writerThreads).Select(_ =>
            Task.Run(() =>
            {
                startGate.Wait();

                while (!cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        var gen = Interlocked.Increment(ref generation);
                        buffer.TryEnqueue(new TestItem(gen));
                        Interlocked.Increment(ref writeAttempts);
                    }
                    catch (Exception ex)
                    {
                        Interlocked.CompareExchange(ref unexpectedException, ex, null);
                        Interlocked.Increment(ref unexpectedFaults);
                        cts.Cancel();
                    }

                    Thread.Yield();
                }
            }));

        // Consumers keep the buffer moving but avoid draining below a low-water mark so TryPeek can
        // produce enough successful observations without relying on scheduler luck.
        IEnumerable<Task> consumers = Enumerable.Range(0, consumerThreads).Select(_ =>
            Task.Run(() =>
            {
                startGate.Wait();

                while (!cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        if (buffer.Count > minimumOccupancy)
                        {
                            buffer.TryDequeue(out TestItem? _);
                            Interlocked.Increment(ref dequeueAttempts);
                        }
                        else
                        {
                            Thread.Yield();
                        }
                    }
                    catch (Exception ex)
                    {
                        Interlocked.CompareExchange(ref unexpectedException, ex, null);
                        Interlocked.Increment(ref unexpectedFaults);
                        cts.Cancel();
                    }
                }
            }));

        IEnumerable<Task> peekers = Enumerable.Range(0, peekerThreads).Select(_ =>
            Task.Run(() =>
            {
                startGate.Wait();

                while (!cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        Interlocked.Increment(ref peekAttempts);

                        if (buffer.TryPeek(out TestItem? item))
                        {
                            if (item is null)
                            {
                                Interlocked.Increment(ref nullPeekViolations);
                                cts.Cancel();
                                continue;
                            }

                            // Sample after TryPeek returns. Sampling before the call would classify
                            // legitimate concurrent writes as future values.
                            var latest = Volatile.Read(ref generation);

                            if (item.Value > latest)
                            {
                                Interlocked.Increment(ref futureValueViolations);
                                cts.Cancel();
                            }

                            if (Interlocked.Increment(ref successfulPeeks) >= targetSuccessfulPeeks)
                                cts.Cancel();
                        }
                        else
                        {
                            Interlocked.Increment(ref emptyPeeks);
                        }
                    }
                    catch (Exception ex)
                    {
                        Interlocked.CompareExchange(ref unexpectedException, ex, null);
                        Interlocked.Increment(ref unexpectedFaults);
                        cts.Cancel();
                    }

                    Thread.Yield();
                }
            }));

        Task[] allTasks = writers.Concat(consumers).Concat(peekers).ToArray();

        // Release all workers together so TryPeek races with live enqueue, dequeue, and overwrite.
        startGate.Set();

        var reachedTargetOrFault = SpinWait.SpinUntil(
            () =>
                Volatile.Read(ref successfulPeeks) >= targetSuccessfulPeeks ||
                Volatile.Read(ref unexpectedFaults) > 0 ||
                Volatile.Read(ref nullPeekViolations) > 0 ||
                Volatile.Read(ref futureValueViolations) > 0,
            timeoutMs);

        cts.Cancel();

        var completed = Task.WaitAll(allTasks, deadlockTimeoutMs);

        TestContext.WriteLine(
            $"SuccessfulPeeks={successfulPeeks}, TargetSuccessfulPeeks={targetSuccessfulPeeks}, " +
            $"MinimumSuccessfulPeeks={minimumSuccessfulPeeks}, ReachedTargetOrFault={reachedTargetOrFault}, " +
            $"PeekAttempts={peekAttempts}, EmptyPeeks={emptyPeeks}, WriteAttempts={writeAttempts}, " +
            $"DequeueAttempts={dequeueAttempts}, NullPeekViolations={nullPeekViolations}, " +
            $"FutureValueViolations={futureValueViolations}, UnexpectedFaults={unexpectedFaults}, " +
            $"Completed={completed}, FirstException={unexpectedException}");

        Assert.IsTrue(
            completed,
            $"Tasks did not complete within {deadlockTimeoutMs} ms.");

        Assert.AreEqual(
            0,
            unexpectedFaults,
            $"TryPeek, TryEnqueue, and TryDequeue must not throw under concurrent pressure. First exception: {unexpectedException}");

        Assert.AreEqual(
            0,
            nullPeekViolations,
            "TryPeek returned success with a null/default item.");

        Assert.AreEqual(
            0,
            futureValueViolations,
            "TryPeek surfaced a value from a future generation that could not have existed when the call completed.");

        Assert.IsGreaterThan(
            0, writeAttempts,
            "Writers must have exercised TryEnqueue during the TryPeek stress run.");

        Assert.IsGreaterThan(
            0, dequeueAttempts,
            "Consumers must have exercised TryDequeue during the TryPeek stress run.");

        Assert.IsGreaterThan(
            0, peekAttempts,
            "Peekers must have exercised TryPeek during the stress run.");

        Assert.IsGreaterThanOrEqualTo(
            minimumSuccessfulPeeks, successfulPeeks,
            $"TryPeek must have produced enough successful observations to validate the concurrency invariant. " +
            $"Expected at least {minimumSuccessfulPeeks}, actual {successfulPeeks}.");

        Assert.IsTrue(
            buffer.Count >= 0 && buffer.Count <= buffer.Capacity,
            "Final Count must remain within [0, Capacity].");
    }

}
