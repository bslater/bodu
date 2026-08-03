// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AmericasCalendarDataTests.UnitedStatesFederalHolidays.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Globalization.Calendar;

public sealed partial class AmericasCalendarDataTests
{
    /// <summary>The statutory nominal dates of the fixed federal holidays, for observed-shift verification.</summary>
    private static readonly Dictionary<string, (int Month, int Day)> s_federalNominalDates = new(StringComparer.Ordinal)
    {
        ["new-years-day"] = (1, 1),
        ["juneteenth"] = (6, 19),
        ["independence-day"] = (7, 4),
        ["veterans-day"] = (11, 11),
        ["christmas-day"] = (12, 25),
    };

    /// <summary>The shared US service, built once for the twenty-year federal sweep.</summary>
    private static readonly Lazy<INotableDateService> s_usService = new(() => AmericasCalendarData.CreateService("US"));

    /// <summary>
    /// Verifies that every United States federal holiday resolves to the observed date the Office of Personnel
    /// Management published across schedule years 2011-2030, including the statutory Saturday-to-Friday and
    /// Sunday-to-Monday substitutions and the three cross-year cases where an observed New Year's Day falls on
    /// 31 December of the previous Gregorian year. For a substituted occurrence the originally calculated date must
    /// also equal the statutory nominal date.
    /// </summary>
    /// <param name="kat">The vector row carrying the (schedule year, holiday) input and the observed date and flag.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(UsFederalHolidayVectors.Rows),
        typeof(UsFederalHolidayVectors),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Resolve_WhenSweptAcrossOpmSchedule_ShouldMatchObservedFederalDate(ValidKat<(int ScheduleYear, string ObservanceId), (DateOnly Observed, bool IsObserved)> kat)
    {
        (DateOnly observed, bool isObserved) = kat.Expected;

        // Resolve the observed date's own year so a 31 December in-lieu day is inside the queried range.
        List<NotableDate> matches = s_usService.Value
            .Resolve(new DateRange(new DateOnly(observed.Year, 1, 1), new DateOnly(observed.Year, 12, 31)), "US")
            .Where(r => r.NotableDateId == kat.Input.ObservanceId && r.Date == observed)
            .ToList();

        Assert.HasCount(1, matches, $"{kat.Name}: expected one occurrence on {observed:yyyy-MM-dd}");
        Assert.AreEqual(isObserved, matches[0].IsObserved, $"{kat.Name}: observed flag");

        if (isObserved)
        {
            (int month, int day) = s_federalNominalDates[kat.Input.ObservanceId];
            Assert.AreEqual(new DateOnly(kat.Input.ScheduleYear, month, day), matches[0].ActualDate, $"{kat.Name}: nominal date");
        }
    }
}
