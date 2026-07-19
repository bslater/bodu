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
/// <c>await using</c>). The sample is single-threaded so the output is stable on every run.
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
        Console.WriteLine("--- Bodu.Threading: async coordination primitives ---");

        // AsyncLazy<T> defers the factory until the first await, then caches the completed Task<T>. Awaiting the
        // same instance repeatedly returns the cached value without re-running the factory.
        var lazy = new AsyncLazy<int>(() =>
        {
            s_factoryRuns++;
            return 42;
        });

        var first = await lazy;
        var second = await lazy;
        Console.WriteLine($"AsyncLazy value  : {first} (second await: {second})");
        Console.WriteLine($"  factory runs   : {s_factoryRuns} (initialized once, then cached)");

        // AsyncManualResetEvent is an awaitable gate. Once Set, it stays signalled, so a WaitAsync on an
        // already-set event completes immediately - the pattern for "publish once, many awaiters proceed".
        var gate = new AsyncManualResetEvent(initialState: false);
        Console.WriteLine($"gate IsSet       : {gate.IsSet}");
        gate.Set();
        await gate.WaitAsync(); // completes synchronously because the gate is already open
        Console.WriteLine($"gate IsSet       : {gate.IsSet} (WaitAsync passed through)");

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
        Console.WriteLine($"guarded counter  : {counter} (5 lock/increment/release cycles)");

        Console.WriteLine();
    }
}
