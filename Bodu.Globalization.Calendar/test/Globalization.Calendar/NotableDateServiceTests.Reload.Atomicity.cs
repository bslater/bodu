// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateServiceTests.Reload.Atomicity.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Stress-style atomicity tests for <see cref="NotableDateService.Reload" />. The existing
/// <see cref="NotableDateServiceTests.Reload_WhenInvokedConcurrentlyWithQueries_ShouldNotThrow" />
/// covers non-crash; this partial pins the stricter contract: every observed snapshot must be
/// wholly one rule-set version, and the lazy range pipeline must never persist a build derived
/// from a previous rule-set version after <see cref="NotableDateService.Reload" /> returns.
/// </summary>
public partial class NotableDateServiceTests
{
    /// <summary>
    /// Verifies that after each call to <see cref="NotableDateService.Reload" /> returns on the
    /// invoking thread, every subsequent query on that thread observes the post-reload rule set.
    /// Pins the basic happens-before contract that Reload acts as a release barrier with respect
    /// to cache invalidation.
    /// </summary>
    [TestMethod]
    public void Reload_WhenCalledThenQueriedOnSameThread_ShouldReflectLatestProviderState()
    {
        MutableNotableDateRuleOverrideProvider overrides = new();
        NotableDateService service = new(
            ruleProviders: [(INotableDateRuleProvider)new InMemoryRuleProvider()],
            workingWeek: WeekPattern.MondayToFriday,
            options: new NotableDateServiceOptions { OverrideProviders = [overrides] });

        for (int i = 0; i < 100; i++)
        {
            overrides.Clear();
            overrides.AddRule(Fixed($"Version:{i}", ((i % 12) + 1), 1));

            service.Reload();

            IReadOnlyList<NotableDate> yearResult = service.GetNotableDates(2026);
            IReadOnlyList<NotableDate> rangeResult = service.ResolveNotableDatesInRange(
                new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));

            Assert.AreEqual(1, yearResult.Count, $"Year query: iteration {i}.");
            Assert.AreEqual($"Version:{i}", yearResult[0].Name);

            Assert.AreEqual(1, rangeResult.Count, $"Range query: iteration {i}.");
            Assert.AreEqual($"Version:{i}", rangeResult[0].Name);
        }
    }

    /// <summary>
    /// Verifies that under concurrent <see cref="NotableDateService.Reload" /> and query load, no
    /// reader observes a mixed snapshot. Each query result must contain entries from exactly one
    /// rule-set version — the per-iteration "A:" or "B:" prefix discriminates which.
    /// </summary>
    [TestMethod]
    public void Reload_WhenAlternatingRuleSetsUnderConcurrentReaders_ShouldNotProduceMixedSnapshots()
    {
        MutableNotableDateRuleOverrideProvider overrides = new();

        // Initial state: rule set A
        overrides.AddRule(Fixed("A:NewYear", 1, 1));
        overrides.AddRule(Fixed("A:Midyear", 7, 4));

        NotableDateService service = new(
            ruleProviders: [(INotableDateRuleProvider)new InMemoryRuleProvider()],
            workingWeek: WeekPattern.MondayToFriday,
            options: new NotableDateServiceOptions { OverrideProviders = [overrides] });

        service.Reload();

        using CancellationTokenSource cts = new();
        List<Exception> readerFailures = new();
        object failuresGate = new();

        void AssertCoherent(IReadOnlyList<NotableDate> snapshot, string operation)
        {
            if (snapshot.Count == 0)
                return;

            char prefix = snapshot[0].Name[0];
            foreach (NotableDate notable in snapshot)
            {
                if (notable.Name[0] != prefix)
                {
                    lock (failuresGate)
                    {
                        readerFailures.Add(new InvalidOperationException(
                            $"{operation}: mixed-prefix snapshot containing both '{prefix}:' and '{notable.Name[0]}:' rules. " +
                            $"Snapshot: [{string.Join(", ", snapshot.Select(n => n.Name))}]"));
                    }

                    return;
                }
            }
        }

        Task[] readers = Enumerable.Range(0, 4).Select(_ => Task.Run(() =>
        {
            try
            {
                while (!cts.IsCancellationRequested)
                {
                    AssertCoherent(service.GetNotableDates(2026), nameof(service.GetNotableDates));
                    AssertCoherent(
                        service.ResolveNotableDatesInRange(new DateTime(2026, 1, 1), new DateTime(2026, 12, 31)),
                        nameof(service.ResolveNotableDatesInRange));
                }
            }
            catch (Exception ex)
            {
                lock (failuresGate)
                {
                    readerFailures.Add(ex);
                }
            }
        })).ToArray();

        bool useA = false;
        for (int i = 0; i < 100; i++)
        {
            useA = !useA;
            overrides.Clear();
            if (useA)
            {
                overrides.AddRule(Fixed("A:NewYear", 1, 1));
                overrides.AddRule(Fixed("A:Midyear", 7, 4));
            }
            else
            {
                overrides.AddRule(Fixed("B:NewYear", 1, 1));
                overrides.AddRule(Fixed("B:Midyear", 7, 4));
            }

            service.Reload();
        }

        cts.Cancel();
        Task.WaitAll(readers, TimeSpan.FromSeconds(5));

        if (readerFailures.Count > 0)
            throw new AggregateException("Mixed-snapshot observation detected.", readerFailures);
    }
}
