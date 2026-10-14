// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FixedDateStrategyTests.Ctor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class FixedDateStrategyTests
{
    /// <summary>
    /// Verifies that a month alias for a calendar other than the Hebrew, which the loader never reads, throws
    /// <see cref="ArgumentException" /> naming the <c>monthAlias</c> parameter, rather than constructing a strategy that
    /// resolves the Hebrew name as a position in another calendar.
    /// </summary>
    /// <param name="calendar">The strategy's calendar.</param>
    /// <param name="month">The month passed beside the alias.</param>
    [TestMethod]
    [DataRow(CalendarSystem.Gregorian, 4)]
    [DataRow(CalendarSystem.Hijri, 0)]
    [DataRow(CalendarSystem.UmmAlQura, 0)]
    [DataRow(CalendarSystem.Persian, 0)]
    [DataRow(CalendarSystem.ChineseLunisolar, 0)]
    public void Ctor_WhenMonthAliasIsGivenForCalendarOtherThanHebrew_ShouldThrowArgumentException(CalendarSystem calendar, int month)
    {
        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new FixedDateStrategy(month, 1, calendar, sweepCalendarYears: true, skipLeapMonth: false, monthAlias: "Nisan");
        });

        Assert.AreEqual("monthAlias", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a month alias that names no Hebrew month whose number shifts in a leap year throws
    /// <see cref="ArgumentException" /> naming the <c>monthAlias</c> parameter, rather than constructing a strategy that
    /// never occurs.
    /// </summary>
    /// <param name="monthAlias">The alias.</param>
    [TestMethod]
    [DataRow("")]
    [DataRow("Tishri")]
    [DataRow("AdarI")]
    [DataRow("Smarch")]
    public void Ctor_WhenMonthAliasIsNotAHebrewAlias_ShouldThrowArgumentException(string monthAlias)
    {
        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new FixedDateStrategy(0, 1, CalendarSystem.Hebrew, sweepCalendarYears: true, skipLeapMonth: false, monthAlias);
        });

        Assert.AreEqual("monthAlias", ex.ParamName);
    }

    /// <summary>
    /// Verifies that each Hebrew month alias whose number shifts in a leap year constructs a Hebrew strategy that keeps
    /// the alias.
    /// </summary>
    /// <param name="monthAlias">The alias.</param>
    [TestMethod]
    [DataRow("AdarII")]
    [DataRow("LastAdar")]
    [DataRow("Nisan")]
    [DataRow("Iyar")]
    [DataRow("Sivan")]
    [DataRow("Tammuz")]
    [DataRow("Av")]
    [DataRow("Elul")]
    public void Ctor_WhenMonthAliasIsAHebrewAlias_ShouldKeepTheAlias(string monthAlias)
    {
        var strategy = new FixedDateStrategy(0, 1, CalendarSystem.Hebrew, sweepCalendarYears: true, skipLeapMonth: false, monthAlias);

        Assert.AreEqual(monthAlias, strategy.MonthAlias);
    }

    /// <summary>
    /// Verifies that a month beyond the months the strategy's calendar can name throws
    /// <see cref="ArgumentOutOfRangeException" /> naming the <c>month</c> parameter, as the loader rejects it, rather than
    /// constructing a strategy that never occurs: 13 in the Gregorian and the twelve month calendars, and in the Chinese
    /// lunisolar calendar for a month that skips the leap month, and 14 in the thirteen month calendars.
    /// </summary>
    /// <param name="calendar">The strategy's calendar.</param>
    /// <param name="month">The month.</param>
    /// <param name="skipLeapMonth">A value indicating whether the month skips a Chinese leap month.</param>
    [TestMethod]
    [DataRow(CalendarSystem.Gregorian, 13, false)]
    [DataRow(CalendarSystem.Hijri, 13, false)]
    [DataRow(CalendarSystem.UmmAlQura, 13, false)]
    [DataRow(CalendarSystem.Persian, 13, false)]
    [DataRow(CalendarSystem.ChineseLunisolar, 13, true)]
    [DataRow(CalendarSystem.ChineseLunisolar, 14, false)]
    [DataRow(CalendarSystem.Hebrew, 14, false)]
    [DataRow(CalendarSystem.Hijri, 0, false)]
    public void Ctor_WhenMonthIsBeyondTheCalendarsMonths_ShouldThrowArgumentOutOfRangeException(CalendarSystem calendar, int month, bool skipLeapMonth)
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new FixedDateStrategy(month, 1, calendar, sweepCalendarYears: true, skipLeapMonth, monthAlias: null);
        });

        Assert.AreEqual("month", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the last month a strategy's calendar can name constructs a strategy for that month.
    /// </summary>
    /// <param name="calendar">The strategy's calendar.</param>
    /// <param name="month">The month.</param>
    /// <param name="skipLeapMonth">A value indicating whether the month skips a Chinese leap month.</param>
    [TestMethod]
    [DataRow(CalendarSystem.Gregorian, 12, false)]
    [DataRow(CalendarSystem.Hijri, 12, false)]
    [DataRow(CalendarSystem.UmmAlQura, 12, false)]
    [DataRow(CalendarSystem.Persian, 12, false)]
    [DataRow(CalendarSystem.ChineseLunisolar, 12, true)]
    [DataRow(CalendarSystem.ChineseLunisolar, 13, false)]
    [DataRow(CalendarSystem.Hebrew, 13, false)]
    public void Ctor_WhenMonthIsTheCalendarsLastMonth_ShouldKeepTheMonth(CalendarSystem calendar, int month, bool skipLeapMonth)
    {
        var strategy = new FixedDateStrategy(month, 1, calendar, sweepCalendarYears: true, skipLeapMonth, monthAlias: null);

        Assert.AreEqual(month, strategy.Month);
    }

    /// <summary>
    /// Verifies that a calendar value <see cref="CalendarSystem" /> does not define, which the loader rejects with
    /// <c>BODU-CAL-ENUM</c>, throws <see cref="ArgumentOutOfRangeException" /> naming the <c>calendar</c> parameter.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenCalendarIsUndefined_ShouldThrowArgumentOutOfRangeException()
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new FixedDateStrategy(1, 1, (CalendarSystem)99, sweepCalendarYears: true, skipLeapMonth: false, monthAlias: null);
        });

        Assert.AreEqual("calendar", ex.ParamName);
    }
}
