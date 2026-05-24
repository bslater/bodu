// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateServiceTests.Reload.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the behaviour of <see cref="NotableDateService.Reload" /> when override providers mutate after the service
/// has been constructed, including cache invalidation, idempotent reloads, and thread-safe concurrent use.
/// </summary>
public partial class NotableDateServiceTests
{
    /// <summary>
    /// Verifies that <see cref="NotableDateService.Reload" /> on a service with no override providers leaves the
    /// emitted notable dates unchanged.
    /// </summary>
    [TestMethod]
    public void Reload_WhenNoOverrideProvidersRegistered_ShouldBeIdempotent()
    {
        NotableDateRule baseRule = Fixed("Base", 6, 1);
        NotableDateService service = new(
            ruleProviders: new[] { (INotableDateRuleProvider)new InMemoryRuleProvider(baseRule) },
            workingDaysOfWeek: WorkingDaysOfWeek.MondayToFriday);

        IReadOnlyList<NotableDate> before = service.GetNotableDates(2026);

        service.Reload();

        IReadOnlyList<NotableDate> after = service.GetNotableDates(2026);

        Assert.AreEqual(before.Count, after.Count);
        Assert.IsTrue(after.Any(n => n.Name == "Base"));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateService.Reload" /> re-queries an override provider so that an addition made
    /// after construction appears in the resolved output for the next query.
    /// </summary>
    [TestMethod]
    public void Reload_WhenOverrideProviderAddsRuleAfterConstruction_ShouldExposeAdditionAfterReload()
    {
        NotableDateRule baseRule = Fixed("Base", 6, 1);
        MutableNotableDateRuleOverrideProvider overrides = new();

        NotableDateService service = new(
            ruleProviders: new[] { (INotableDateRuleProvider)new InMemoryRuleProvider(baseRule) },
            workingWeek: WeekPattern.MondayToFriday,
            options: new NotableDateServiceOptions { OverrideProviders = new[] { overrides } });

        Assert.IsFalse(service.GetNotableDates(2026).Any(n => n.Name == "Late Addition"));

        overrides.AddRule(Fixed("Late Addition", 9, 9));
        service.Reload();

        IReadOnlyList<NotableDate> after = service.GetNotableDates(2026);
        Assert.IsTrue(after.Any(n => n.Name == "Late Addition" && n.Date == new DateTime(2026, 9, 9)));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateService.Reload" /> re-queries an override provider so that a removal made
    /// after construction suppresses the targeted base rule on the next query.
    /// </summary>
    [TestMethod]
    public void Reload_WhenOverrideProviderRemovesRuleAfterConstruction_ShouldSuppressBaseRuleAfterReload()
    {
        NotableDateRule baseRule = Fixed("Holiday", 6, 1);
        MutableNotableDateRuleOverrideProvider overrides = new();

        NotableDateService service = new(
            ruleProviders: new[] { (INotableDateRuleProvider)new InMemoryRuleProvider(baseRule) },
            workingWeek: WeekPattern.MondayToFriday,
            options: new NotableDateServiceOptions { OverrideProviders = new[] { overrides } });

        Assert.IsTrue(service.GetNotableDates(2026).Any(n => n.Name == "Holiday"));

        overrides.RemoveRule("Holiday");
        service.Reload();

        Assert.IsFalse(service.GetNotableDates(2026).Any(n => n.Name == "Holiday"));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateService.Reload" /> clears the per-year cache so that subsequent queries
    /// rebuild rather than returning a stale snapshot.
    /// </summary>
    [TestMethod]
    public void Reload_WhenInvoked_ShouldClearPerYearCache()
    {
        NotableDateRule baseRule = Fixed("Base", 6, 1);
        MutableNotableDateRuleOverrideProvider overrides = new();

        NotableDateService service = new(
            ruleProviders: new[] { (INotableDateRuleProvider)new InMemoryRuleProvider(baseRule) },
            workingWeek: WeekPattern.MondayToFriday,
            options: new NotableDateServiceOptions { OverrideProviders = new[] { overrides } });

        IReadOnlyList<NotableDate> firstQuery = service.GetNotableDates(2026);

        overrides.AddRule(Fixed("New", 12, 1));
        service.Reload();

        IReadOnlyList<NotableDate> secondQuery = service.GetNotableDates(2026);

        Assert.AreEqual(firstQuery.Count + 1, secondQuery.Count, "Cache must be invalidated so the new rule contributes a fresh entry.");
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateService.Reload" /> picks up a fresh override addition even when the
    /// override provider's enumerable produces new instances on each enumeration.
    /// </summary>
    [TestMethod]
    public void Reload_WhenOverrideProviderEnumerableYieldsFreshInstances_ShouldStillReflectLatestState()
    {
        NotableDateRule baseRule = Fixed("Base", 6, 1);
        MutableNotableDateRuleOverrideProvider overrides = new();

        NotableDateService service = new(
            ruleProviders: new[] { (INotableDateRuleProvider)new InMemoryRuleProvider(baseRule) },
            workingWeek: WeekPattern.MondayToFriday,
            options: new NotableDateServiceOptions { OverrideProviders = new[] { overrides } });

        overrides.AddRule(Fixed("First", 9, 1));
        service.Reload();
        Assert.IsTrue(service.GetNotableDates(2026).Any(n => n.Name == "First"));

        overrides.Clear();
        overrides.AddRule(Fixed("Second", 10, 1));
        service.Reload();

        IReadOnlyList<NotableDate> emitted = service.GetNotableDates(2026);
        Assert.IsFalse(emitted.Any(n => n.Name == "First"), "Cleared override must no longer contribute.");
        Assert.IsTrue(emitted.Any(n => n.Name == "Second"));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateService.Reload" /> may be called concurrently with
    /// <see cref="NotableDateService.GetNotableDates(int, string?, Type?)" /> without throwing or producing torn
    /// reads.
    /// </summary>
    [TestMethod]
    public void Reload_WhenInvokedConcurrentlyWithQueries_ShouldNotThrow()
    {
        NotableDateRule baseRule = Fixed("Base", 6, 1);
        MutableNotableDateRuleOverrideProvider overrides = new();

        NotableDateService service = new(
            ruleProviders: new[] { (INotableDateRuleProvider)new InMemoryRuleProvider(baseRule) },
            workingWeek: WeekPattern.MondayToFriday,
            options: new NotableDateServiceOptions { OverrideProviders = new[] { overrides } });

        using CancellationTokenSource cts = new();
        Task readerA = Task.Run(() =>
        {
            while (!cts.IsCancellationRequested)
            {
                _ = service.GetNotableDates(2026);
            }
        });
        Task readerB = Task.Run(() =>
        {
            while (!cts.IsCancellationRequested)
            {
                _ = service.GetNotableDates(2027);
            }
        });

        for (int i = 0; i < 50; i++)
        {
            overrides.AddRule(Fixed($"R-{i}", (i % 12) + 1, ((i * 3) % 28) + 1));
            service.Reload();
        }

        cts.Cancel();
        Task.WaitAll(new[] { readerA, readerB }, TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateService.Reload" /> composes additions from multiple registered override
    /// providers so each provider's contribution surfaces in the resolved output.
    /// </summary>
    [TestMethod]
    public void Reload_WhenMultipleOverrideProvidersRegistered_ShouldComposeAllContributions()
    {
        MutableNotableDateRuleOverrideProvider providerA = new();
        MutableNotableDateRuleOverrideProvider providerB = new();

        NotableDateService service = new(
            ruleProviders: new[] { (INotableDateRuleProvider)new InMemoryRuleProvider(Fixed("Base", 6, 1)) },
            workingWeek: WeekPattern.MondayToFriday,
            options: new NotableDateServiceOptions { OverrideProviders = new INotableDateRuleOverrideProvider[] { providerA, providerB } });

        providerA.AddRule(Fixed("From A", 7, 1));
        providerB.AddRule(Fixed("From B", 8, 1));
        service.Reload();

        IReadOnlyList<NotableDate> emitted = service.GetNotableDates(2026);
        Assert.IsTrue(emitted.Any(n => n.Name == "From A"));
        Assert.IsTrue(emitted.Any(n => n.Name == "From B"));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateService.Reload" /> honours year-scoped removals through the rebuilt
    /// override snapshot.
    /// </summary>
    [TestMethod]
    public void Reload_WhenYearScopedRemovalAddedAndReloadCalled_ShouldSuppressOnlyMatchingYears()
    {
        MutableNotableDateRuleOverrideProvider overrides = new();
        NotableDateService service = new(
            ruleProviders: new[] { (INotableDateRuleProvider)new InMemoryRuleProvider(Fixed("Holiday", 6, 1)) },
            workingWeek: WeekPattern.MondayToFriday,
            options: new NotableDateServiceOptions { OverrideProviders = new[] { overrides } });

        overrides.RemoveRule("Holiday", fromYear: 2026, toYear: 2026);
        service.Reload();

        Assert.IsTrue(service.GetNotableDates(2025).Any(n => n.Name == "Holiday"));
        Assert.IsFalse(service.GetNotableDates(2026).Any(n => n.Name == "Holiday"));
        Assert.IsTrue(service.GetNotableDates(2027).Any(n => n.Name == "Holiday"));
    }

    /// <summary>
    /// Verifies that repeated <see cref="NotableDateService.Reload" /> calls are stable — successive reloads do not
    /// accumulate duplicate additions or removals.
    /// </summary>
    [TestMethod]
    public void Reload_WhenInvokedRepeatedly_ShouldNotAccumulateDuplicateRules()
    {
        MutableNotableDateRuleOverrideProvider overrides = new();
        NotableDateService service = new(
            ruleProviders: new[] { (INotableDateRuleProvider)new InMemoryRuleProvider(Fixed("Base", 6, 1)) },
            workingWeek: WeekPattern.MondayToFriday,
            options: new NotableDateServiceOptions { OverrideProviders = new[] { overrides } });

        overrides.AddRule(Fixed("Once", 9, 9));

        service.Reload();
        service.Reload();
        service.Reload();

        int matches = service.GetNotableDates(2026).Count(n => n.Name == "Once");
        Assert.AreEqual(1, matches);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateService.Reload" /> drops contributions that have since been cleared from
    /// the underlying override provider.
    /// </summary>
    [TestMethod]
    public void Reload_WhenOverrideContributionsCleared_ShouldDropThem()
    {
        MutableNotableDateRuleOverrideProvider overrides = new();
        NotableDateService service = new(
            ruleProviders: new[] { (INotableDateRuleProvider)new InMemoryRuleProvider(Fixed("Base", 6, 1)) },
            workingWeek: WeekPattern.MondayToFriday,
            options: new NotableDateServiceOptions { OverrideProviders = new[] { overrides } });

        overrides.AddRule(Fixed("Temporary", 7, 1));
        service.Reload();
        Assert.IsTrue(service.GetNotableDates(2026).Any(n => n.Name == "Temporary"));

        overrides.Clear();
        service.Reload();

        Assert.IsFalse(service.GetNotableDates(2026).Any(n => n.Name == "Temporary"));
    }

    /// <summary>
    /// Verifies that <see cref="MutableNotableDateRuleOverrideProvider.Changed" /> can be wired to
    /// <see cref="NotableDateService.Reload" /> so that mutations propagate without explicit reload calls.
    /// </summary>
    [TestMethod]
    public void Reload_WhenSubscribedToMutableOverrideChanged_ShouldAutoApplyMutations()
    {
        NotableDateRule baseRule = Fixed("Base", 6, 1);
        MutableNotableDateRuleOverrideProvider overrides = new();

        NotableDateService service = new(
            ruleProviders: new[] { (INotableDateRuleProvider)new InMemoryRuleProvider(baseRule) },
            workingWeek: WeekPattern.MondayToFriday,
            options: new NotableDateServiceOptions { OverrideProviders = new[] { overrides } });

        overrides.Changed += (_, _) => service.Reload();

        overrides.AddRule(Fixed("Auto", 4, 4));

        Assert.IsTrue(service.GetNotableDates(2026).Any(n => n.Name == "Auto"));
    }
}
