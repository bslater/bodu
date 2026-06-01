// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MutableNotableDateRuleOverrideProviderTests.Concurrency.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public partial class MutableNotableDateRuleOverrideProviderTests
{
    /// <summary>
    /// Verifies that concurrent <see cref="MutableNotableDateRuleOverrideProvider.AddRule" /> calls from multiple
    /// threads do not lose entries and result in a final addition count equal to the total number of writes.
    /// </summary>
    [TestMethod]
    public void AddRule_WhenInvokedConcurrentlyFromMultipleThreads_ShouldNotLoseEntries()
    {
        MutableNotableDateRuleOverrideProvider provider = new();
        const int threadCount = 8;
        const int rulesPerThread = 250;

        Parallel.For(0, threadCount, threadIndex =>
        {
            for (var i = 0; i < rulesPerThread; i++)
            {
                provider.AddRule(Fixed($"T{threadIndex}-R{i}"));
            }
        });

        Assert.AreEqual(threadCount * rulesPerThread, provider.GetAdditions().Count());
    }

    /// <summary>
    /// Verifies that a <see cref="MutableNotableDateRuleOverrideProvider.GetAdditions" /> snapshot enumerated while
    /// concurrent <see cref="MutableNotableDateRuleOverrideProvider.AddRule" /> calls are in flight does not throw
    /// and continues to yield a consistent view of the state observed at snapshot time.
    /// </summary>
    [TestMethod]
    public void GetAdditions_WhenEnumeratedDuringConcurrentMutation_ShouldNotThrow()
    {
        MutableNotableDateRuleOverrideProvider provider = new();
        for (var i = 0; i < 100; i++)
        {
            provider.AddRule(Fixed($"Seed-{i}"));
        }

        IEnumerable<NotableDateRule> snapshot = provider.GetAdditions();

        using CancellationTokenSource cts = new();
        var mutator = Task.Run(() =>
        {
            var n = 0;
            while (!cts.IsCancellationRequested)
            {
                provider.AddRule(Fixed($"Live-{n++}"));
            }
        });

        try
        {
            // Enumerate the snapshot multiple times while the writer thread mutates the provider.
            for (var iteration = 0; iteration < 50; iteration++)
            {
                var observed = snapshot.Count();
                Assert.IsTrue(observed >= 100, $"Snapshot must observe at least the seed entries; got {observed}.");
            }
        }
        finally
        {
            cts.Cancel();
            mutator.Wait(TimeSpan.FromSeconds(5));
        }
    }

    /// <summary>
    /// Verifies that <see cref="MutableNotableDateRuleOverrideProvider.Changed" /> handlers may themselves mutate the
    /// provider without causing re-entrancy deadlock.
    /// </summary>
    [TestMethod]
    public void Changed_WhenHandlerMutatesProvider_ShouldNotDeadlock()
    {
        MutableNotableDateRuleOverrideProvider provider = new();
        var handlerInvocations = 0;
        provider.Changed += (_, _) =>
        {
            if (Interlocked.Increment(ref handlerInvocations) == 1)
            {
                provider.RemoveRule("From handler");
            }
        };

        provider.AddRule(Fixed("Initial"));

        Assert.AreEqual(2, handlerInvocations, "Handler must be invoked once for the initial AddRule and once for the re-entrant RemoveRule.");
        Assert.AreEqual(1, provider.GetRemovals().Count());
    }
}
