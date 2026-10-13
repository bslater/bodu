// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CountingNotableDateServiceTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Caching;

/// <summary>
/// Verifies the <see cref="CountingNotableDateService" /> test double, whose invocation count the caching tests assert
/// while the service under test resolves years on several threads at once.
/// </summary>
[TestClass]
public sealed partial class CountingNotableDateServiceTests
{
    /// <summary>The number of threads that call a double at once.</summary>
    internal const int Threads = 4;

    /// <summary>The number of calls each thread makes.</summary>
    internal const int CallsPerThread = 250_000;

    /// <summary>The total number of calls <see cref="RunOnThreadsAtOnce" /> makes.</summary>
    internal const int TotalCalls = Threads * CallsPerThread;

    /// <summary>
    /// Runs <paramref name="call" /> <see cref="CallsPerThread" /> times on each of <see cref="Threads" /> threads,
    /// released together so that their calls overlap.
    /// </summary>
    /// <param name="call">The call each thread repeats.</param>
    internal static void RunOnThreadsAtOnce(Action call)
    {
        using var start = new Barrier(Threads);
        Thread[] threads = [.. Enumerable.Range(0, Threads).Select(_ => new Thread(() =>
        {
            start.SignalAndWait();
            for (int i = 0; i < CallsPerThread; i++)
                call();
        }))];

        foreach (Thread thread in threads)
            thread.Start();

        foreach (Thread thread in threads)
            thread.Join();
    }
}
