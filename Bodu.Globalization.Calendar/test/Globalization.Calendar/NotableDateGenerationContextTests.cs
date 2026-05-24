// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateGenerationContextTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Immutable;
using SysGlobal = System.Globalization;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the territory and calendar context matching performed by <see cref="NotableDateGenerationContext" />, as
/// observed through the public <see cref="NotableDateGenerationContext.ResolveByName" /> and
/// <see cref="NotableDateGenerationContext.IsNonWorkingDay" /> entry points.
/// </summary>
[TestClass]
public sealed class NotableDateGenerationContextTests
{
    /// <summary>
    /// Verifies that <see cref="NotableDateGenerationContext.ResolveByName" /> returns <see langword="null" /> when the
    /// context is empty.
    /// </summary>
    [TestMethod]
    public void ResolveByName_WhenContextIsEmpty_ShouldReturnNull()
    {
        NotableDateGenerationContext context = new();

        Assert.IsNull(context.ResolveByName("Anything", 2026, null, null));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateGenerationContext.ResolveByName" /> returns the previously added notable date
    /// when the rule name matches.
    /// </summary>
    [TestMethod]
    public void ResolveByName_WhenAddedNotableMatchesName_ShouldReturnNotableDate()
    {
        NotableDateGenerationContext context = new();
        context.Add(BuildNotable("New Year's Day", new DateTime(2026, 1, 1)));

        Assert.AreEqual(new DateTime(2026, 1, 1), context.ResolveByName("New Year's Day", 2026, null, null));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateGenerationContext.ResolveByName" /> ignores stored entries whose year does not
    /// match the requested year.
    /// </summary>
    [TestMethod]
    public void ResolveByName_WhenStoredYearDiffersFromRequest_ShouldReturnNull()
    {
        NotableDateGenerationContext context = new();
        context.Add(BuildNotable("Holiday", new DateTime(2026, 5, 1)));

        Assert.IsNull(context.ResolveByName("Holiday", 2025, null, null));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateGenerationContext.ResolveByName" /> matches a stored territory-neutral entry
    /// against any requested territory.
    /// </summary>
    [TestMethod]
    public void ResolveByName_WhenStoredTerritoryIsNull_ShouldMatchAnyRequestedTerritory()
    {
        NotableDateGenerationContext context = new();
        context.Add(BuildNotable("Global", new DateTime(2026, 5, 1)));

        Assert.AreEqual(new DateTime(2026, 5, 1), context.ResolveByName("Global", 2026, "AU", null));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateGenerationContext.ResolveByName" /> excludes entries whose stored territory
    /// does not overlap the requested territory.
    /// </summary>
    [TestMethod]
    public void ResolveByName_WhenStoredTerritoryDoesNotMatchRequest_ShouldReturnNull()
    {
        NotableDateGenerationContext context = new();
        context.Add(BuildNotable("Holiday", new DateTime(2026, 5, 1), territory: "AU"));

        Assert.IsNull(context.ResolveByName("Holiday", 2026, "US", null));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateGenerationContext.ResolveByName" /> matches via parent / child territory
    /// containment — a stored <c>AU</c> entry matches a requested <c>AU-NSW</c> subdivision.
    /// </summary>
    [TestMethod]
    public void ResolveByName_WhenStoredTerritoryContainsRequestedSubdivision_ShouldReturnMatch()
    {
        NotableDateGenerationContext context = new();
        context.Add(BuildNotable("Federal Holiday", new DateTime(2026, 5, 1), territory: "AU"));

        Assert.AreEqual(new DateTime(2026, 5, 1), context.ResolveByName("Federal Holiday", 2026, "AU-NSW", null));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateGenerationContext.ResolveByName" /> excludes entries whose stored calendar
    /// type differs from the requested calendar context.
    /// </summary>
    [TestMethod]
    public void ResolveByName_WhenStoredCalendarTypeDiffersFromRequest_ShouldReturnNull()
    {
        NotableDateGenerationContext context = new();
        context.Add(BuildNotable(
            "Rosh Hashanah",
            new DateTime(2026, 9, 12),
            calendarType: typeof(SysGlobal.HebrewCalendar)));

        Assert.IsNull(context.ResolveByName("Rosh Hashanah", 2026, null, typeof(SysGlobal.GregorianCalendar)));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateGenerationContext.ResolveByName" /> matches a stored calendar-neutral entry
    /// against any requested calendar context.
    /// </summary>
    [TestMethod]
    public void ResolveByName_WhenStoredCalendarTypeIsNull_ShouldMatchAnyRequestedCalendar()
    {
        NotableDateGenerationContext context = new();
        context.Add(BuildNotable("Universal", new DateTime(2026, 1, 1)));

        Assert.AreEqual(new DateTime(2026, 1, 1), context.ResolveByName("Universal", 2026, null, typeof(SysGlobal.HebrewCalendar)));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateGenerationContext.IsNonWorkingDay" /> returns <see langword="true" /> when the
    /// requested day is a weekend.
    /// </summary>
    [TestMethod]
    public void IsNonWorkingDay_WhenWeekendPredicateReturnsTrue_ShouldReturnTrue()
    {
        NotableDateGenerationContext context = new();

        Assert.IsTrue(context.IsNonWorkingDay(new DateTime(2026, 1, 3), null, null, _ => true));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateGenerationContext.IsNonWorkingDay" /> returns <see langword="true" /> when an
    /// added non-working date covers the requested day.
    /// </summary>
    [TestMethod]
    public void IsNonWorkingDay_WhenContextCoversDay_ShouldReturnTrue()
    {
        NotableDateGenerationContext context = new();
        context.Add(BuildNotable("Christmas Day", new DateTime(2026, 12, 25), isNonWorking: true));

        Assert.IsTrue(context.IsNonWorkingDay(new DateTime(2026, 12, 25), null, null, _ => false));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateGenerationContext.IsNonWorkingDay" /> ignores entries flagged as working days.
    /// </summary>
    [TestMethod]
    public void IsNonWorkingDay_WhenCoveringEntryIsWorkingDay_ShouldReturnFalse()
    {
        NotableDateGenerationContext context = new();
        context.Add(BuildNotable("Casual Observance", new DateTime(2026, 5, 1), isNonWorking: false));

        Assert.IsFalse(context.IsNonWorkingDay(new DateTime(2026, 5, 1), null, null, _ => false));
    }

    private static NotableDate BuildNotable(
        string name,
        DateTime date,
        string? territory = null,
        Type? calendarType = null,
        bool isNonWorking = false) =>
        new()
        {
            Name = name,
            Date = date,
            Category = NotableDateCategory.Holiday,
            IsNonWorkingDay = isNonWorking,
            TerritoryCode = territory,
            CalendarType = calendarType,
            Tags = ImmutableHashSet<string>.Empty,
        };
}
