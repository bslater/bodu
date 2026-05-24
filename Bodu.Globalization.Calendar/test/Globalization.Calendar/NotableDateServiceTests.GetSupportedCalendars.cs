// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateServiceTests.GetSupportedCalendars.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the calendar-enumeration contract of <see cref="NotableDateService.GetSupportedCalendars" />.
/// </summary>
public partial class NotableDateServiceTests
{
    /// <summary>
    /// Verifies that <see cref="NotableDateService.GetSupportedCalendars" /> returns no entries when every rule omits
    /// the <see cref="NotableDateRule.CalendarType" /> property.
    /// </summary>
    [TestMethod]
    public void GetSupportedCalendars_WhenAllRulesGlobal_ShouldReturnEmpty()
    {
        NotableDateService service = new(
            ruleProviders: new[] { (INotableDateRuleProvider)new InMemoryRuleProvider(Fixed("Global", 1, 1)) },
            workingDaysOfWeek: WorkingDaysOfWeek.MondayToFriday);

        Assert.AreEqual(0, service.GetSupportedCalendars().Count);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateService.GetSupportedCalendars" /> projects through every authored calendar
    /// and returns a distinct set.
    /// </summary>
    [TestMethod]
    public void GetSupportedCalendars_WhenRulesAcrossMultipleCalendars_ShouldReturnDistinctSet()
    {
        NotableDateRule gregorian = new()
        {
            Name = "Gregorian",
            Strategy = DateResolutionStrategy.Fixed,
            Category = NotableDateCategory.Holiday,
            Month = 1,
            Day = 1,
            CalendarType = typeof(GregorianCalendar),
        };
        NotableDateRule hebrew = new()
        {
            Name = "Hebrew",
            Strategy = DateResolutionStrategy.Fixed,
            Category = NotableDateCategory.Holiday,
            Month = 1,
            Day = 1,
            CalendarType = typeof(HebrewCalendar),
        };

        NotableDateService service = new(
            ruleProviders: new[] { (INotableDateRuleProvider)new InMemoryRuleProvider(gregorian, hebrew) },
            workingDaysOfWeek: WorkingDaysOfWeek.MondayToFriday);

        IReadOnlyCollection<Type> calendars = service.GetSupportedCalendars();

        CollectionAssert.AreEquivalent(new[] { typeof(GregorianCalendar), typeof(HebrewCalendar) }, calendars.ToList());
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateService.GetSupportedCalendars" /> elides rules with no calendar type.
    /// </summary>
    [TestMethod]
    public void GetSupportedCalendars_WhenMixedScopedAndGlobalRules_ShouldOnlyReportScopedCalendars()
    {
        NotableDateRule gregorian = new()
        {
            Name = "Gregorian",
            Strategy = DateResolutionStrategy.Fixed,
            Category = NotableDateCategory.Holiday,
            Month = 1,
            Day = 1,
            CalendarType = typeof(GregorianCalendar),
        };

        NotableDateService service = new(
            ruleProviders: new[] { (INotableDateRuleProvider)new InMemoryRuleProvider(gregorian, Fixed("Global", 2, 1)) },
            workingDaysOfWeek: WorkingDaysOfWeek.MondayToFriday);

        IReadOnlyCollection<Type> calendars = service.GetSupportedCalendars();

        Assert.AreEqual(1, calendars.Count);
        Assert.IsTrue(calendars.Contains(typeof(GregorianCalendar)));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateService.GetSupportedCalendars" /> deduplicates by reference identity so that
    /// multiple rules referencing the same calendar type collapse to a single entry.
    /// </summary>
    [TestMethod]
    public void GetSupportedCalendars_WhenMultipleRulesShareCalendar_ShouldCollapseToOneEntry()
    {
        NotableDateRule a = new()
        {
            Name = "A",
            Strategy = DateResolutionStrategy.Fixed,
            Category = NotableDateCategory.Holiday,
            Month = 1,
            Day = 1,
            CalendarType = typeof(GregorianCalendar),
        };
        NotableDateRule b = new()
        {
            Name = "B",
            Strategy = DateResolutionStrategy.Fixed,
            Category = NotableDateCategory.Holiday,
            Month = 2,
            Day = 1,
            CalendarType = typeof(GregorianCalendar),
        };

        NotableDateService service = new(
            ruleProviders: new[] { (INotableDateRuleProvider)new InMemoryRuleProvider(a, b) },
            workingDaysOfWeek: WorkingDaysOfWeek.MondayToFriday);

        Assert.AreEqual(1, service.GetSupportedCalendars().Count);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateService.GetSupportedCalendars" /> returns an empty collection when no rule
    /// providers are registered.
    /// </summary>
    [TestMethod]
    public void GetSupportedCalendars_WhenNoRuleProviders_ShouldReturnEmpty()
    {
        NotableDateService service = new(
            ruleProviders: Array.Empty<INotableDateRuleProvider>(),
            workingDaysOfWeek: WorkingDaysOfWeek.MondayToFriday);

        Assert.AreEqual(0, service.GetSupportedCalendars().Count);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateService.GetSupportedCalendars" /> deduplicates across providers — when two
    /// providers both contribute rules referencing the same calendar type the calendar appears once.
    /// </summary>
    [TestMethod]
    public void GetSupportedCalendars_WhenMultipleProvidersShareCalendar_ShouldReportOnce()
    {
        NotableDateRule a = new()
        {
            Name = "A",
            Strategy = DateResolutionStrategy.Fixed,
            Category = NotableDateCategory.Holiday,
            Month = 1,
            Day = 1,
            CalendarType = typeof(GregorianCalendar),
        };
        NotableDateRule b = new()
        {
            Name = "B",
            Strategy = DateResolutionStrategy.Fixed,
            Category = NotableDateCategory.Holiday,
            Month = 2,
            Day = 1,
            CalendarType = typeof(GregorianCalendar),
        };

        NotableDateService service = new(
            ruleProviders: new[]
            {
                (INotableDateRuleProvider)new InMemoryRuleProvider(a),
                (INotableDateRuleProvider)new InMemoryRuleProvider(b),
            },
            workingDaysOfWeek: WorkingDaysOfWeek.MondayToFriday);

        IReadOnlyCollection<Type> calendars = service.GetSupportedCalendars();

        Assert.AreEqual(1, calendars.Count);
        Assert.IsTrue(calendars.Contains(typeof(GregorianCalendar)));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateService.GetSupportedCalendars" /> returns a fresh snapshot on each call,
    /// reflecting override additions made between successive reads.
    /// </summary>
    [TestMethod]
    public void GetSupportedCalendars_WhenCalledRepeatedlyAroundReload_ShouldReturnFreshSnapshotEachTime()
    {
        MutableNotableDateRuleOverrideProvider overrides = new();
        NotableDateRule gregorian = new()
        {
            Name = "Gregorian",
            Strategy = DateResolutionStrategy.Fixed,
            Category = NotableDateCategory.Holiday,
            Month = 1,
            Day = 1,
            CalendarType = typeof(GregorianCalendar),
        };

        NotableDateService service = new(
            ruleProviders: new[] { (INotableDateRuleProvider)new InMemoryRuleProvider(gregorian) },
            workingWeek: WeekPattern.MondayToFriday,
            options: new NotableDateServiceOptions { OverrideProviders = new[] { overrides } });

        IReadOnlyCollection<Type> firstSnapshot = service.GetSupportedCalendars();
        Assert.AreEqual(1, firstSnapshot.Count);

        overrides.AddRule(new NotableDateRule
        {
            Name = "Hebrew",
            Strategy = DateResolutionStrategy.Fixed,
            Category = NotableDateCategory.Holiday,
            Month = 1,
            Day = 1,
            CalendarType = typeof(HebrewCalendar),
        });
        service.Reload();

        IReadOnlyCollection<Type> secondSnapshot = service.GetSupportedCalendars();
        Assert.AreEqual(2, secondSnapshot.Count);
        Assert.AreEqual(1, firstSnapshot.Count, "Earlier snapshot must remain a frozen view of its read.");
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateService.GetSupportedCalendars" /> reflects override additions after
    /// <see cref="NotableDateService.Reload" /> has been called.
    /// </summary>
    [TestMethod]
    public void GetSupportedCalendars_WhenOverrideAddsCalendarAndReloadCalled_ShouldIncludeNewCalendar()
    {
        MutableNotableDateRuleOverrideProvider overrides = new();
        NotableDateRule gregorian = new()
        {
            Name = "Gregorian",
            Strategy = DateResolutionStrategy.Fixed,
            Category = NotableDateCategory.Holiday,
            Month = 1,
            Day = 1,
            CalendarType = typeof(GregorianCalendar),
        };
        NotableDateService service = new(
            ruleProviders: new[] { (INotableDateRuleProvider)new InMemoryRuleProvider(gregorian) },
            workingWeek: WeekPattern.MondayToFriday,
            options: new NotableDateServiceOptions { OverrideProviders = new[] { overrides } });

        Assert.IsFalse(service.GetSupportedCalendars().Contains(typeof(HebrewCalendar)));

        overrides.AddRule(new NotableDateRule
        {
            Name = "Hebrew",
            Strategy = DateResolutionStrategy.Fixed,
            Category = NotableDateCategory.Holiday,
            Month = 1,
            Day = 1,
            CalendarType = typeof(HebrewCalendar),
        });
        service.Reload();

        Assert.IsTrue(service.GetSupportedCalendars().Contains(typeof(HebrewCalendar)));
    }
}
