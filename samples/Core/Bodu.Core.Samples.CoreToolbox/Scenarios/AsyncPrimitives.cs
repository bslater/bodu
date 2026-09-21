// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsyncPrimitives.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Threading;

namespace Bodu.Core.Samples.CoreToolbox.Scenarios;

/// <summary>
/// Demonstrates three of the <c>Bodu.Threading</c> async coordination primitives in a single deterministic flow:
/// <see cref="AsyncLazy{T}" /> (run an initializer at most once and await its result), <see cref="AsyncManualResetEvent" />
/// (an awaitable gate that stays open once set), and <see cref="AsyncLock" /> (a mutual-exclusion guard released by
/// disposing the token returned from <c>await mutex.LockAsync()</c>). The sample is single-threaded so the output is
/// stable on every run.
/// </summary>
public static class AsyncPrimitives
{
    // Counts how many times the AsyncLazy factory actually runs - the evidence that it initializes at most once.
    private static int s_factoryRuns;

    /// <summary>
    /// Awaits each primitive in turn and prints the observable outcome.
    /// </summary>
    /// <returns>A task that completes when the scenario has finished.</returns>
    public static async Task RunAsync()
    {
        SampleConsole.Scenario(
            "Bodu.Threading - async coordination primitives",
            what: "Exercises the async coordination primitives - waits that are awaited rather than blocked on - " +
                  "with every wait completing deterministically.",
            why: "The BCL's coordination types predate async and block a thread while waiting, which on a thread " +
                 "pool is the shape that produces starvation: threads sitting idle inside a wait cannot run the " +
                 "work that would release them. These primitives suspend the continuation instead, so a waiting " +
                 "operation costs no thread at all. That is the entire reason to prefer them over a lock or a " +
                 "ManualResetEventSlim in asynchronous code.",
            expect: "Every wait completes and each primitive reports the state a correct run produces. Nothing " +
                    "here depends on timing - the scenario signals before it waits where it can, so the " +
                    "transcript is the same on every run rather than merely usually.");

        // AsyncLazy<T> defers the factory until the first await, then caches the completed Task<T>. Awaiting the
        // same instance repeatedly returns the cached value without re-running the factory.
        var lazy = new AsyncLazy<int>(() =>
        {
            s_factoryRuns++;
            return 42;
        });

        var first = await lazy;
        var second = await lazy;
        Console.WriteLine($"  AsyncLazy value  : {first} (second await: {second})  (both awaits see the same value - AsyncLazy caches the completed task, not just the result)");
        Console.WriteLine($"  factory runs   : {s_factoryRuns} (initialized once, then cached)  (expected 1 - two awaits, one initialization; concurrent awaiters join the in-flight task rather than racing)");

        // AsyncManualResetEvent is an awaitable gate. Once Set, it stays signalled, so a WaitAsync on an
        // already-set event completes immediately - the pattern for "publish once, many awaiters proceed".
        var gate = new AsyncManualResetEvent(initialState: false);
        Console.WriteLine($"  gate IsSet       : {gate.IsSet}  (expected False - nothing has signalled it yet)");
        gate.Set();
        await gate.WaitAsync(); // completes synchronously because the gate is already open
        Console.WriteLine($"  gate IsSet       : {gate.IsSet} (WaitAsync passed through)  (expected True - the awaiting continuation resumed on the signal, having held no thread while it waited)");

        // AsyncLock is an async-friendly mutex: LockAsync yields a releaser disposed by 'await using', so the
        // critical section is bounded by scope. Here it guards a simple increment loop.
        var mutex = new AsyncLock();
        var counter = 0;
        for (var i = 0; i < 5; i++)
        {
            // LockAsync yields the releaser asynchronously; the releaser itself is synchronously disposable,
            // so the critical section is bounded by a plain 'using'.
            using (await mutex.LockAsync())
            {
                // Only one holder is inside this block at a time; the increment is race-free.
                counter++;
            }
        }
        Console.WriteLine($"  guarded counter  : {counter} (5 lock/increment/release cycles)  (expected 5 - AsyncLock serialises the increments without blocking a thread while waiting for the lock)");

        Console.WriteLine();
    }
}
