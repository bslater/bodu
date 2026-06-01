// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateServiceTests.RuleIdentity.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Pins the composite rule-identity contract used when merging base rules with override additions. Two rules are
/// considered the "same rule" — and therefore one replaces the other on override application — only when their
/// <see cref="NotableDateRule.Name" />, <see cref="NotableDateRule.TerritoryCode" />, AND
/// <see cref="NotableDateRule.CalendarType" /> all match. Regional variants and calendar-system variants of the
/// same named date survive override application as distinct entries.
/// </summary>
public partial class NotableDateServiceTests
{
    /// <summary>
    /// Verifies that two rules sharing a name but differing in territory both survive the override merge.
    /// </summary>
    [TestMethod]
    public void RuleIdentity_WhenSameNameDifferentTerritory_ShouldKeepBoth()
    {
        NotableDateRule auBase = Fixed("Labour Day", 5, 1, territory: "AU-VIC");
        NotableDateRule nzAddition = Fixed("Labour Day", 10, 26, territory: "NZ");

        NotableDateService service = new(
            ruleProviders: [(INotableDateRuleProvider)new InMemoryRuleProvider(auBase)],
            workingDaysOfWeek: WorkingDaysOfWeek.MondayToFriday,
            options: new NotableDateServiceOptions
            {
                OverrideProviders =
                [
                    (INotableDateRuleOverrideProvider)new TestOverrideProvider(
                        removals: [],
                        additions: [nzAddition]),
                ],
            });

        var auResult = service.GetNotableDates(2026, "AU-VIC")
            .Where(d => d.Name == "Labour Day")
            .ToList();
        var nzResult = service.GetNotableDates(2026, "NZ")
            .Where(d => d.Name == "Labour Day")
            .ToList();

        Assert.AreEqual(1, auResult.Count);
        Assert.AreEqual(new DateTime(2026, 5, 1), auResult[0].Date);
        Assert.AreEqual(1, nzResult.Count);
        Assert.AreEqual(new DateTime(2026, 10, 26), nzResult[0].Date);
    }

    /// <summary>
    /// Verifies that two rules sharing a name and territory but differing in <see cref="NotableDateRule.CalendarType" />
    /// both survive the override merge. The composite key includes <see cref="NotableDateRule.CalendarType" /> so a
    /// Gregorian and a Julian variant of the same observance can coexist.
    /// </summary>
    [TestMethod]
    public void RuleIdentity_WhenSameNameSameTerritoryDifferentCalendar_ShouldKeepBoth()
    {
        NotableDateRule gregorianChristmas = Fixed("Christmas Day", 12, 25)
            with
        { CalendarType = typeof(GregorianCalendar) };

        NotableDateRule julianChristmas = Fixed("Christmas Day", 12, 25)
            with
        { CalendarType = typeof(JulianCalendar) };

        NotableDateService service = new(
            ruleProviders: [(INotableDateRuleProvider)new InMemoryRuleProvider(gregorianChristmas)],
            workingDaysOfWeek: WorkingDaysOfWeek.MondayToFriday,
            options: new NotableDateServiceOptions
            {
                OverrideProviders =
                [
                    (INotableDateRuleOverrideProvider)new TestOverrideProvider(
                        removals: [],
                        additions: [julianChristmas]),
                ],
            });

        // Query a Gregorian window wide enough to include both calendars' Dec 25 observances. Julian Dec 25 2026
        // resolves to Gregorian Jan 7 2027, so we span both years.
        var gregorian = service.GetNotableDates(
                new DateTime(2026, 1, 1), new DateTime(2027, 12, 31),
                territoryCode: null,
                calendarType: typeof(GregorianCalendar))
            .Where(d => d.Name == "Christmas Day")
            .ToList();
        var julian = service.GetNotableDates(
                new DateTime(2026, 1, 1), new DateTime(2027, 12, 31),
                territoryCode: null,
                calendarType: typeof(JulianCalendar))
            .Where(d => d.Name == "Christmas Day")
            .ToList();

        Assert.IsTrue(gregorian.Count >= 1, "Gregorian Christmas Day must survive the override merge.");
        Assert.IsTrue(gregorian.All(d => d.CalendarType == typeof(GregorianCalendar)));
        Assert.IsTrue(julian.Count >= 1, "Julian Christmas Day must survive the override merge as a distinct rule.");
        Assert.IsTrue(julian.All(d => d.CalendarType == typeof(JulianCalendar)));
    }

    /// <summary>
    /// Verifies that an override addition with matching name, territory, and calendar replaces the base rule rather
    /// than coexisting with it.
    /// </summary>
    [TestMethod]
    public void RuleIdentity_WhenSameNameSameTerritorySameCalendar_ShouldReplace()
    {
        NotableDateRule baseRule = Fixed("Holiday", 1, 1, territory: "AU")
            with
        { CalendarType = typeof(GregorianCalendar) };

        NotableDateRule replacement = Fixed("Holiday", 7, 4, territory: "AU")
            with
        { CalendarType = typeof(GregorianCalendar) };

        NotableDateService service = new(
            ruleProviders: [(INotableDateRuleProvider)new InMemoryRuleProvider(baseRule)],
            workingDaysOfWeek: WorkingDaysOfWeek.MondayToFriday,
            options: new NotableDateServiceOptions
            {
                OverrideProviders =
                [
                    (INotableDateRuleOverrideProvider)new TestOverrideProvider(
                        removals: [],
                        additions: [replacement]),
                ],
            });

        var result = service.GetNotableDates(2026, "AU", calendarType: typeof(GregorianCalendar))
            .Where(d => d.Name == "Holiday")
            .ToList();

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(new DateTime(2026, 7, 4), result[0].Date,
            "Addition with identical (Name, Territory, CalendarType) must replace the base rule.");
    }

    /// <summary>
    /// Verifies that an override addition replaces only the same-name rule whose composite identity matches it
    /// exactly, leaving other same-name rules (different territory or calendar) intact.
    /// </summary>
    [TestMethod]
    public void RuleIdentity_WhenOverrideMatchesOneOfSeveralSameNameRules_ShouldReplaceOnlyTheMatching()
    {
        NotableDateRule auBase = Fixed("Holiday", 1, 1, territory: "AU");
        NotableDateRule nzBase = Fixed("Holiday", 2, 6, territory: "NZ");
        NotableDateRule auReplacement = Fixed("Holiday", 7, 4, territory: "AU");

        NotableDateService service = new(
            ruleProviders:
            [
                (INotableDateRuleProvider)new InMemoryRuleProvider(auBase, nzBase),
            ],
            workingDaysOfWeek: WorkingDaysOfWeek.MondayToFriday,
            options: new NotableDateServiceOptions
            {
                OverrideProviders =
                [
                    (INotableDateRuleOverrideProvider)new TestOverrideProvider(
                        removals: [],
                        additions: [auReplacement]),
                ],
            });

        var auResult = service.GetNotableDates(2026, "AU")
            .Where(d => d.Name == "Holiday")
            .ToList();
        var nzResult = service.GetNotableDates(2026, "NZ")
            .Where(d => d.Name == "Holiday")
            .ToList();

        Assert.AreEqual(1, auResult.Count);
        Assert.AreEqual(new DateTime(2026, 7, 4), auResult[0].Date, "AU base rule replaced by the AU-scoped addition.");
        Assert.AreEqual(1, nzResult.Count);
        Assert.AreEqual(new DateTime(2026, 2, 6), nzResult[0].Date, "NZ rule untouched by the AU-scoped addition.");
    }
}
