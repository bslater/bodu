// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentHashSetTests.Perf.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentHashSetTests
{
    /// <summary>
    /// Verifies that inserting a large number of keys single-threaded completes well within a generous time bound
    /// and retains every key, guarding against an accidental complexity regression in the table-growth path.
    /// </summary>
    [TestMethod]
    [TestCategory("Stress")]
    public void Perf_WhenInsertingManyKeys_ShouldCompleteQuicklyAndRetainEveryKey()
    {
        const int keyCount = 100_000;
        var set = new ConcurrentHashSet<int>();

        var stopwatch = Stopwatch.StartNew();
        for (int i = 0; i < keyCount; i++)
            set.Add(i);
        stopwatch.Stop();

        TestContext.WriteLine($"Inserted {keyCount} keys single-threaded in {stopwatch.ElapsedMilliseconds} ms.");

        Assert.AreEqual(keyCount, set.Count, "Every inserted key must be retained.");
        Assert.IsLessThan(
            10_000,
            stopwatch.ElapsedMilliseconds,
            "Inserting 100k keys taking seconds suggests a complexity regression in the resize path.");
    }

    /// <summary>
    /// Verifies that probing a large number of present keys single-threaded completes well within a generous time
    /// bound and finds every key, guarding against a regression in the lock-free containment path.
    /// </summary>
    [TestMethod]
    [TestCategory("Stress")]
    public void Perf_WhenProbingManyKeys_ShouldCompleteQuicklyAndFindEveryKey()
    {
        const int keyCount = 100_000;
        var set = new ConcurrentHashSet<int>(Enumerable.Range(0, keyCount));

        int hits = 0;
        var stopwatch = Stopwatch.StartNew();
        for (int i = 0; i < keyCount; i++)
        {
            if (set.Contains(i))
                hits++;
        }
        stopwatch.Stop();

        TestContext.WriteLine($"Probed {keyCount} present keys in {stopwatch.ElapsedMilliseconds} ms.");

        Assert.AreEqual(keyCount, hits, "Every present key must be found.");
        Assert.IsLessThan(
            10_000,
            stopwatch.ElapsedMilliseconds,
            "Probing 100k present keys taking seconds suggests a regression in the lock-free Contains path.");
    }

    /// <summary>
    /// Verifies that many threads concurrently adding and then removing private blocks of keys completes within a
    /// generous time bound and leaves the set empty.
    /// </summary>
    [TestMethod]
    [TestCategory("Stress")]
    public void Perf_WhenManyThreadsAddAndRemove_ShouldCompleteQuicklyAndQuiesceEmpty()
    {
        const int threadCount = 8;
        const int keysPerThread = 50_000;
        var set = new ConcurrentHashSet<int>();

        var stopwatch = Stopwatch.StartNew();
        Parallel.For(0, threadCount, threadId =>
        {
            int baseKey = threadId * keysPerThread;
            for (int k = 0; k < keysPerThread; k++)
                set.Add(baseKey + k);
            for (int k = 0; k < keysPerThread; k++)
                set.Remove(baseKey + k);
        });
        stopwatch.Stop();

        TestContext.WriteLine(
            $"{threadCount} threads each cycled {keysPerThread} keys in {stopwatch.ElapsedMilliseconds} ms.");

        Assert.AreEqual(0, set.Count, "Every thread removed its own keys, so the set must end empty.");
        Assert.IsLessThan(
            20_000,
            stopwatch.ElapsedMilliseconds,
            "A concurrent add/remove cycle taking many seconds suggests a contention or complexity regression.");
    }
}
