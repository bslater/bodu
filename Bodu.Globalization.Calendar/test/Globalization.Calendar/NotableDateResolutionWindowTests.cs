// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateResolutionWindowTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Tests the <see cref="NotableDateResolutionWindow" /> class.
/// </summary>
[TestClass]
public sealed class NotableDateResolutionWindowTests
{
    /// <summary>
    /// Verifies that the constructor rejects invalid ranges.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenEndDateIsEarlierThanStartDate_ShouldThrowArgumentException()
    {
        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new NotableDateResolutionWindow(
                new DateTime(2024, 1, 2),
                new DateTime(2024, 1, 1));
        });

        Assert.AreEqual("endDate", ex.ParamName);
    }

    /// <summary>
    /// Verifies that emitted dates are exposed in chronological order.
    /// </summary>
    [TestMethod]
    public void OutputDates_WhenDatesAreAddedOutOfOrder_ShouldReturnDatesInChronologicalOrder()
    {
        NotableDateResolutionWindow window = new(
            new DateTime(2024, 1, 1),
            new DateTime(2024, 12, 31));

        NotableDate later = Date("Later Date", new DateTime(2024, 3, 1));
        NotableDate earlier = Date("Earlier Date", new DateTime(2024, 2, 1));

        window.AddAdjusted(later);
        window.AddAdjusted(earlier);

        CollectionAssert.AreEqual(
            new[] { earlier, later },
            window.OutputDates.ToArray());
    }

    /// <summary>
    /// Verifies that all known dates include blockers that are not emitted.
    /// </summary>
    [TestMethod]
    public void KnownDates_WhenBlockerIsAdded_ShouldIncludeBlockerWithoutAddingItToOutput()
    {
        NotableDateResolutionWindow window = new(
            new DateTime(2024, 1, 1),
            new DateTime(2024, 12, 31));

        NotableDate emitted = Date("Emitted Date", new DateTime(2024, 1, 1));
        NotableDate blocker = Date("Boundary Blocker", new DateTime(2025, 1, 2), nonWorking: true);

        window.AddAdjusted(emitted);
        window.AddBlocker(blocker);

        CollectionAssert.AreEqual(
            new[] { emitted },
            window.OutputDates.ToArray());

        CollectionAssert.AreEqual(
            new[] { emitted, blocker },
            window.KnownDates.ToArray());
    }

    /// <summary>
    /// Verifies that base occurrences are added to the emitted output and base occurrence list.
    /// </summary>
    [TestMethod]
    public void AddBase_WhenOccurrenceIsAdded_ShouldAddBaseDateToOutputAndOccurrenceList()
    {
        NotableDateRule rule = Rule("Base Rule");
        NotableDate notable = Date("Base Rule", new DateTime(2024, 4, 10));

        ResolvedNotableDateOccurrence occurrence = new(
            rule,
            notable.Date,
            TerritoryCode: null,
            notable);

        NotableDateResolutionWindow window = new(
            new DateTime(2024, 1, 1),
            new DateTime(2024, 12, 31));

        window.AddBase(occurrence);

        Assert.AreEqual(1, window.BaseOccurrences.Count);
        Assert.AreEqual(occurrence, window.BaseOccurrences[0]);

        CollectionAssert.AreEqual(
            new[] { notable },
            window.OutputDates.ToArray());
    }

    /// <summary>
    /// Verifies that non-working base dates participate in non-working-day checks.
    /// </summary>
    [TestMethod]
    public void IsNonWorkingDay_WhenBaseDateIsNonWorking_ShouldReturnTrue()
    {
        NotableDateRule rule = Rule("Public Holiday");
        NotableDate notable = Date("Public Holiday", new DateTime(2024, 1, 26), nonWorking: true);

        ResolvedNotableDateOccurrence occurrence = new(
            rule,
            notable.Date,
            TerritoryCode: null,
            notable);

        NotableDateResolutionWindow window = new(
            new DateTime(2024, 1, 1),
            new DateTime(2024, 12, 31));

        window.AddBase(occurrence);

        Assert.IsTrue(window.IsNonWorkingDay(new DateTime(2024, 1, 26), null, null));
    }

    /// <summary>
    /// Verifies that adjusted dates participate in non-working-day checks.
    /// </summary>
    [TestMethod]
    public void IsNonWorkingDay_WhenAdjustedDateIsNonWorking_ShouldReturnTrue()
    {
        NotableDate adjusted = Date("Observed Holiday", new DateTime(2024, 1, 29), nonWorking: true);

        NotableDateResolutionWindow window = new(
            new DateTime(2024, 1, 1),
            new DateTime(2024, 12, 31));

        window.AddAdjusted(adjusted);

        Assert.IsTrue(window.IsNonWorkingDay(new DateTime(2024, 1, 29), null, null));
    }

    /// <summary>
    /// Verifies that blocker dates participate in non-working-day checks without being emitted.
    /// </summary>
    [TestMethod]
    public void IsNonWorkingDay_WhenBlockerIsNonWorking_ShouldReturnTrueWithoutAddingBlockerToOutput()
    {
        NotableDate blocker = Date("Boundary Blocker", new DateTime(2025, 1, 2), nonWorking: true);

        NotableDateResolutionWindow window = new(
            new DateTime(2024, 12, 31),
            new DateTime(2024, 12, 31));

        window.AddBlocker(blocker);

        Assert.IsTrue(window.IsNonWorkingDay(new DateTime(2025, 1, 2), null, null));
        Assert.AreEqual(0, window.OutputDates.Count);
    }

    /// <summary>
    /// Verifies that multi-day non-working dates block every day in their inclusive span.
    /// </summary>
    [TestMethod]
    public void IsNonWorkingDay_WhenDateFallsInsideMultiDaySpan_ShouldReturnTrue()
    {
        NotableDate festival = new()
        {
            Name = "Festival",
            Date = new DateTime(2024, 6, 10),
            Category = NotableDateCategory.Holiday,
            IsNonWorkingDay = true,
            DurationDays = 3,
        };

        NotableDateResolutionWindow window = new(
            new DateTime(2024, 6, 1),
            new DateTime(2024, 6, 30));

        window.AddAdjusted(festival);

        Assert.IsTrue(window.IsNonWorkingDay(new DateTime(2024, 6, 10), null, null));
        Assert.IsTrue(window.IsNonWorkingDay(new DateTime(2024, 6, 11), null, null));
        Assert.IsTrue(window.IsNonWorkingDay(new DateTime(2024, 6, 12), null, null));
        Assert.IsFalse(window.IsNonWorkingDay(new DateTime(2024, 6, 13), null, null));
    }

    /// <summary>
    /// Verifies that the supplied weekend predicate participates in non-working-day checks.
    /// </summary>
    [TestMethod]
    public void IsNonWorkingDay_WhenWeekendPredicateReturnsTrue_ShouldReturnTrue()
    {
        NotableDateResolutionWindow window = new(
            new DateTime(2024, 1, 1),
            new DateTime(2024, 12, 31));

        Assert.IsTrue(window.IsNonWorkingDay(
            new DateTime(2024, 1, 6),
            null,
            null,
            date => date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday));
    }

    /// <summary>
    /// Verifies that territory matching recognises parent and subdivision relationships.
    /// </summary>
    [TestMethod]
    public void IsNonWorkingDay_WhenTerritoryMatchesParentOrSubdivision_ShouldReturnTrue()
    {
        NotableDate nswHoliday = Date(
            "NSW Holiday",
            new DateTime(2024, 8, 5),
            nonWorking: true,
            territoryCode: "AU-NSW");

        NotableDate nationalHoliday = Date(
            "National Holiday",
            new DateTime(2024, 1, 26),
            nonWorking: true,
            territoryCode: "AU");

        NotableDateResolutionWindow window = new(
            new DateTime(2024, 1, 1),
            new DateTime(2024, 12, 31));

        window.AddAdjusted(nswHoliday);
        window.AddAdjusted(nationalHoliday);

        Assert.IsTrue(window.IsNonWorkingDay(new DateTime(2024, 8, 5), "AU", null));
        Assert.IsTrue(window.IsNonWorkingDay(new DateTime(2024, 1, 26), "AU-NSW", null));
    }

    /// <summary>
    /// Verifies that unmatched territories do not block the requested context.
    /// </summary>
    [TestMethod]
    public void IsNonWorkingDay_WhenTerritoryDoesNotMatch_ShouldReturnFalse()
    {
        NotableDate nswHoliday = Date(
            "NSW Holiday",
            new DateTime(2024, 8, 5),
            nonWorking: true,
            territoryCode: "AU-NSW");

        NotableDateResolutionWindow window = new(
            new DateTime(2024, 1, 1),
            new DateTime(2024, 12, 31));

        window.AddAdjusted(nswHoliday);

        Assert.IsFalse(window.IsNonWorkingDay(new DateTime(2024, 8, 5), "NZ", null));
    }

    /// <summary>
    /// Verifies that known dates can be resolved by name.
    /// </summary>
    [TestMethod]
    public void ResolveByName_WhenKnownDateMatchesNameYearAndContext_ShouldReturnDate()
    {
        NotableDate known = Date("Target Date", new DateTime(2024, 9, 1), nonWorking: true);

        NotableDateResolutionWindow window = new(
            new DateTime(2024, 1, 1),
            new DateTime(2024, 12, 31));

        window.AddBlocker(known);

        DateTime? actual = window.ResolveByName("target date", 2024, null, null);

        Assert.AreEqual(new DateTime(2024, 9, 1), actual);
    }

    /// <summary>
    /// Verifies that name resolution returns <see langword="null" /> for a different year.
    /// </summary>
    [TestMethod]
    public void ResolveByName_WhenYearDoesNotMatch_ShouldReturnNull()
    {
        NotableDate known = Date("Target Date", new DateTime(2024, 9, 1), nonWorking: true);

        NotableDateResolutionWindow window = new(
            new DateTime(2024, 1, 1),
            new DateTime(2024, 12, 31));

        window.AddBlocker(known);

        DateTime? actual = window.ResolveByName("Target Date", 2025, null, null);

        Assert.IsNull(actual);
    }

    /// <summary>
    /// Verifies that the known window expands when dates outside the initial range are added.
    /// </summary>
    [TestMethod]
    public void AddBlocker_WhenDateFallsOutsideInitialRange_ShouldExpandKnownWindow()
    {
        NotableDate blocker = Date("Boundary Blocker", new DateTime(2025, 1, 2), nonWorking: true);

        NotableDateResolutionWindow window = new(
            new DateTime(2024, 12, 31),
            new DateTime(2024, 12, 31));

        window.AddBlocker(blocker);

        Assert.AreEqual(new DateTime(2024, 12, 31), window.StartDate);
        Assert.AreEqual(new DateTime(2025, 1, 2), window.EndDate);
        Assert.IsTrue(window.Contains(new DateTime(2025, 1, 2)));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateResolutionWindow.ResolveByName" /> rejects an empty name with
    /// <see cref="ArgumentException" /> exposing the <c>ruleName</c> parameter.
    /// </summary>
    [TestMethod]
    public void ResolveByName_WhenNameIsEmpty_ShouldThrowArgumentException()
    {
        NotableDateResolutionWindow window = new(
            new DateTime(2024, 1, 1),
            new DateTime(2024, 12, 31));

        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = window.ResolveByName(string.Empty, 2024, null, null);
        });

        Assert.AreEqual("ruleName", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateResolutionWindow.ResolveByName" /> returns <see langword="null" /> when the
    /// requested territory does not overlap the stored entry's territory.
    /// </summary>
    [TestMethod]
    public void ResolveByName_WhenRequestedTerritoryDoesNotMatchStored_ShouldReturnNull()
    {
        NotableDate known = Date("Federal Holiday", new DateTime(2024, 6, 10), territoryCode: "AU");

        NotableDateResolutionWindow window = new(
            new DateTime(2024, 1, 1),
            new DateTime(2024, 12, 31));

        window.AddBlocker(known);

        Assert.IsNull(window.ResolveByName("Federal Holiday", 2024, "US", null));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateResolutionWindow.ResolveByName" /> matches via parent / child territory
    /// containment when the stored entry's territory contains the requested subdivision.
    /// </summary>
    [TestMethod]
    public void ResolveByName_WhenStoredTerritoryContainsRequestedSubdivision_ShouldReturnMatch()
    {
        NotableDate known = Date("Federal Holiday", new DateTime(2024, 6, 10), territoryCode: "AU");

        NotableDateResolutionWindow window = new(
            new DateTime(2024, 1, 1),
            new DateTime(2024, 12, 31));

        window.AddBlocker(known);

        Assert.AreEqual(new DateTime(2024, 6, 10), window.ResolveByName("Federal Holiday", 2024, "AU-NSW", null));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateResolutionWindow.ResolveByName" /> matches a stored territory-neutral entry
    /// against any requested territory.
    /// </summary>
    [TestMethod]
    public void ResolveByName_WhenStoredTerritoryIsNull_ShouldMatchAnyRequestedTerritory()
    {
        NotableDate known = Date("Global", new DateTime(2024, 6, 10));

        NotableDateResolutionWindow window = new(
            new DateTime(2024, 1, 1),
            new DateTime(2024, 12, 31));

        window.AddBlocker(known);

        Assert.AreEqual(new DateTime(2024, 6, 10), window.ResolveByName("Global", 2024, "AU", null));
    }

    private static NotableDateRule Rule(string name) =>
        new()
        {
            Name = name,
            Strategy = DateResolutionStrategy.Fixed,
            Category = NotableDateCategory.Holiday,
            Month = 1,
            Day = 1,
            IsNonWorkingDay = true,
        };

    private static NotableDate Date(
        string name,
        DateTime date,
        bool nonWorking = false,
        string? territoryCode = null) =>
        new()
        {
            Name = name,
            Date = date.Date,
            Category = NotableDateCategory.Holiday,
            IsNonWorkingDay = nonWorking,
            TerritoryCode = territoryCode,
        };
}