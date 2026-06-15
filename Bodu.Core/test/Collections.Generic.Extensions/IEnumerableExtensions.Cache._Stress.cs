// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IEnumerableExtensions.Cache._Stress.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Infrastructure;

namespace Bodu.Collections.Generic.Extensions;

public sealed partial class IEnumerableExtensionsTests_Cache
{
    /// <summary>
    /// Gets or sets the test execution context, used to surface diagnostic counters from the stress run.
    /// </summary>
    public TestContext TestContext { get; set; } = default!;

    /// <summary>
    /// Verifies that a cached sequence populated for the first time concurrently from many readers yields the
    /// complete, correct sequence to every reader, never dropping the final element nor exposing a torn read of the
    /// shared cache. Each iteration starts a fresh cache and rendezvouses every reader on it simultaneously to
    /// maximize contention on the lock-free reader path.
    /// </summary>
    [TestMethod]
    [TestCategory("Stress")]
    public void Cache_WhenEnumeratedConcurrently_ShouldYieldEveryElementToEveryReader()
    {
        var readerCount = Math.Max(4, Environment.ProcessorCount);
        const int iterations = 20_000;
        const int length = 16;
        const int deadlockTimeoutMs = 120_000;

        int[] expected = Enumerable.Range(1, length).ToArray();

        using var barrier = new Barrier(readerCount + 1);
        IEnumerable<int>? current = null;
        var results = new List<int>?[readerCount];
        var errors = new Exception?[readerCount];
        var mismatches = 0;
        var faults = 0;
        Exception? firstException = null;
        string? firstMismatch = null;

        // Each reader repeatedly waits for the controller to publish a fresh cache, fully enumerates it, then hands
        // control back. Exceptions are captured rather than thrown so the barrier never desynchronizes.
        Task[] readers = Enumerable.Range(0, readerCount).Select(readerIndex => StartWorker(() =>
        {
            for (var i = 0; i < iterations; i++)
            {
                barrier.SignalAndWait();
                try
                {
                    results[readerIndex] = Volatile.Read(ref current)!.ToList();
                    errors[readerIndex] = null;
                }
                catch (Exception ex)
                {
                    results[readerIndex] = null;
                    errors[readerIndex] = ex;
                }

                barrier.SignalAndWait();
            }
        })).ToArray();

        // The controller publishes a fresh cache each iteration, releases the readers onto it, waits for them to
        // finish, then validates that every reader observed the full sequence.
        var controller = StartWorker(() =>
        {
            for (var i = 0; i < iterations; i++)
            {
                Volatile.Write(ref current, new TrackingEnumerable<int>(SpacedSequence(length)).Cache());

                barrier.SignalAndWait(); // release readers onto the fresh cache
                barrier.SignalAndWait(); // wait for every reader to finish this iteration

                for (var r = 0; r < readerCount; r++)
                {
                    if (errors[r] is not null)
                    {
                        Interlocked.CompareExchange(ref firstException, errors[r], null);
                        Interlocked.Increment(ref faults);
                    }
                    else if (results[r] is null || !results[r]!.SequenceEqual(expected))
                    {
                        Interlocked.Increment(ref mismatches);
                        firstMismatch ??= results[r] is null
                            ? "<null>"
                            : $"iter={i} len={results[r]!.Count} [{string.Join(",", results[r]!)}]";
                    }
                }
            }
        });

        var completed = Task.WaitAll([.. readers, controller], deadlockTimeoutMs);

        TestContext.WriteLine(
            $"Iterations={iterations}, Readers={readerCount}, Length={length}, " +
            $"Mismatches={mismatches}, Faults={faults}, Completed={completed}, FirstException={firstException}, FirstMismatch={firstMismatch}");

        Assert.IsTrue(completed, "Concurrent enumeration workers did not complete within the deadlock timeout.");
        Assert.AreEqual(0, faults, $"No reader should fault during concurrent enumeration. First exception: {firstException}");
        Assert.AreEqual(0, mismatches, "Every reader must observe the complete, correct sequence on every iteration.");
    }

    /// <summary>
    /// Starts a stress worker on a dedicated long-running thread so the thread pool cannot starve it of execution
    /// while sibling workers run CPU-bound hot loops.
    /// </summary>
    /// <param name="action">The worker body to execute.</param>
    /// <returns>A <see cref="Task" /> representing the running worker.</returns>
    private static Task StartWorker(Action action) =>
        Task.Factory.StartNew(action, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);

    /// <summary>
    /// Produces a sequence of <paramref name="count" /> ascending integers (1-based) with a short spin between each
    /// element, widening the window in which concurrent readers interleave around the writer's append.
    /// </summary>
    /// <param name="count">The number of elements to yield.</param>
    /// <returns>An <see cref="IEnumerable{T}" /> that yields <c>1</c> through <paramref name="count" />.</returns>
    private static IEnumerable<int> SpacedSequence(int count)
    {
        for (var i = 1; i <= count; i++)
        {
            Thread.SpinWait(40);
            yield return i;
        }
    }
}
