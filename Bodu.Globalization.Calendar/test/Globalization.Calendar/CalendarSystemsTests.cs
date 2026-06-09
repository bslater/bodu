// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalendarSystemsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the non-Gregorian calendar-system projection helpers in <see cref="CalendarSystems" />, focusing on the
/// boundary and out-of-range paths where a calendar lookup yields nothing.
/// </summary>
[TestClass]
public class CalendarSystemsTests
{
    /// <summary>
    /// Verifies that <see cref="CalendarSystems.Resolve" /> returns <see langword="null" /> for systems with no backing
    /// <see cref="System.Globalization.Calendar" /> (Gregorian and undefined values).
    /// </summary>
    [TestMethod]
    public void Resolve_WhenSystemHasNoBackingCalendar_ShouldReturnNull()
    {
        Assert.IsNull(CalendarSystems.Resolve(CalendarSystem.Gregorian));
        Assert.IsNull(CalendarSystems.Resolve((CalendarSystem)999));
    }

    /// <summary>
    /// Verifies that projecting against the Gregorian system (which has no backing calendar) yields no dates.
    /// </summary>
    [TestMethod]
    public void ResolveFixedAll_WhenGregorian_ShouldReturnEmpty() =>
        Assert.AreEqual(0, CalendarSystems.ResolveFixedAll(CalendarSystem.Gregorian, 1, null, 1, false, false, 2025).Count);

    /// <summary>
    /// Verifies that an unrecognized Hebrew month alias resolves to a sentinel of <c>-1</c>.
    /// </summary>
    [TestMethod]
    public void ResolveHebrewMonthAlias_WhenUnknownAlias_ShouldReturnNegativeOne() =>
        Assert.AreEqual(-1, CalendarSystems.ResolveHebrewMonthAlias("NotAMonth", false));

    /// <summary>
    /// Verifies that a day index longer than any lunar month yields no projected dates.
    /// </summary>
    [TestMethod]
    public void ResolveFixedAll_WhenDayExceedsMonthLength_ShouldReturnEmpty() =>
        Assert.AreEqual(0, CalendarSystems.ResolveFixedAll(CalendarSystem.Hijri, 1, null, 31, true, false, 2025).Count);

    /// <summary>
    /// Verifies that projecting onto a Gregorian year that predates the calendar's supported range yields no dates,
    /// exercising the guarded calendar-lookup paths.
    /// </summary>
    [TestMethod]
    public void ResolveFixedAll_WhenGregorianYearOutOfCalendarRange_ShouldReturnEmpty() =>
        Assert.AreEqual(0, CalendarSystems.ResolveFixedAll(CalendarSystem.Hijri, 1, null, 1, true, false, 1).Count);

    /// <summary>
    /// Verifies that a Hebrew month-alias projection onto an out-of-range Gregorian year yields no dates, exercising
    /// the guarded months-in-year lookup.
    /// </summary>
    [TestMethod]
    public void ResolveFixedAll_WhenHebrewAliasYearOutOfRange_ShouldReturnEmpty() =>
        Assert.AreEqual(0, CalendarSystems.ResolveFixedAll(CalendarSystem.Hebrew, 0, "Elul", 1, true, false, 1).Count);

    /// <summary>
    /// Verifies that a Chinese leap-month-skip projection onto a year outside the calendar's range yields no date.
    /// </summary>
    [TestMethod]
    public void ResolveFixedAll_WhenChineseLeapSkipYearOutOfRange_ShouldReturnEmpty() =>
        Assert.AreEqual(0, CalendarSystems.ResolveFixedAll(CalendarSystem.ChineseLunisolar, 1, null, 1, false, true, 1800).Count);

    /// <summary>
    /// Verifies that a Chinese leap-month-skip projection whose shifted month index exceeds the year's month count
    /// yields no date.
    /// </summary>
    [TestMethod]
    public void ResolveFixedAll_WhenChineseLeapSkipMonthExceedsYear_ShouldReturnEmpty() =>
        Assert.AreEqual(0, CalendarSystems.ResolveFixedAll(CalendarSystem.ChineseLunisolar, 13, null, 1, false, true, 2025).Count);
}
