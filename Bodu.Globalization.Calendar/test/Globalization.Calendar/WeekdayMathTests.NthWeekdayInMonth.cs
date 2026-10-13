// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WeekdayMathTests.NthWeekdayInMonth.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public partial class WeekdayMathTests
{
    /// <summary>
    /// Verifies that the last-occurrence ordinal returns the final occurrence of the weekday in the month.
    /// </summary>
    [TestMethod]
    public void NthWeekdayInMonth_WhenLast_ShouldReturnFinalOccurrence()
    {
        // The last Monday of January 2025 is 2025-01-27.
        DateOnly? result = WeekdayMath.NthWeekdayInMonth(2025, 1, DayOfWeek.Monday, WeekOrdinal.Last);

        Assert.AreEqual(new DateOnly(2025, 1, 27), result);
    }

    /// <summary>
    /// Verifies that an ordinal <see cref="WeekOrdinal" /> does not define yields no date, rather than the date of the
    /// same month in another year that adding whole weeks to the month's first match reaches: ordinal 53 lands on
    /// 1 January 2024 and ordinal -51 on 3 January 2022 for the Mondays of January 2023.
    /// </summary>
    /// <param name="ordinal">The undefined ordinal.</param>
    [TestMethod]
    [DataRow(53)]
    [DataRow(-51)]
    public void NthWeekdayInMonth_WhenOrdinalIsUndefinedAndReachesAnotherYear_ShouldReturnNull(int ordinal)
    {
        DateOnly? result = WeekdayMath.NthWeekdayInMonth(2023, 1, DayOfWeek.Monday, (WeekOrdinal)ordinal);

        Assert.IsNull(result);
    }

    /// <summary>
    /// Verifies that an ordinal <see cref="WeekOrdinal" /> does not define yields no date at the ends of the date range,
    /// rather than throwing from the date arithmetic: ordinal 0 steps back before 1 January 0001, and ordinal 7 forward
    /// past 31 December 9999.
    /// </summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month.</param>
    /// <param name="ordinal">The undefined ordinal.</param>
    [TestMethod]
    [DataRow(1, 1, 0)]
    [DataRow(9999, 12, 7)]
    public void NthWeekdayInMonth_WhenOrdinalIsUndefinedAtTheEndsOfTheRange_ShouldReturnNull(int year, int month, int ordinal)
    {
        DateOnly? result = WeekdayMath.NthWeekdayInMonth(year, month, DayOfWeek.Monday, (WeekOrdinal)ordinal);

        Assert.IsNull(result);
    }

    /// <summary>
    /// Verifies that a fifth occurrence December 9999 does not have yields no date, rather than throwing from the date
    /// arithmetic: December 9999 begins on a Wednesday, so a fifth Saturday, Sunday, Monday or Tuesday would fall in
    /// year 10000.
    /// </summary>
    /// <param name="dayOfWeek">The weekday with only four occurrences in December 9999.</param>
    [TestMethod]
    [DataRow(DayOfWeek.Saturday)]
    [DataRow(DayOfWeek.Sunday)]
    [DataRow(DayOfWeek.Monday)]
    [DataRow(DayOfWeek.Tuesday)]
    public void NthWeekdayInMonth_WhenAMissingFifthWouldFallAfterDecember9999_ShouldReturnNull(DayOfWeek dayOfWeek)
    {
        DateOnly? result = WeekdayMath.NthWeekdayInMonth(9999, 12, dayOfWeek, WeekOrdinal.Fifth);

        Assert.IsNull(result);
    }

    /// <summary>
    /// Verifies that the nth weekday of every month of the grid years, for every weekday and ordinal, is the occurrence
    /// a day-by-day scan of the month finds, or <see langword="null" /> when the month has no such occurrence; the
    /// grid covers both ends of the date range, common and leap years, and both kinds of century.
    /// </summary>
    [TestMethod]
    public void NthWeekdayInMonth_WhenComparedWithAScanOfTheMonth_ShouldAgreeForEveryWeekdayAndOrdinal()
    {
        foreach (int year in new[] { 1, 1900, 2000, 2023, 2024, 9999 })
        {
            for (int month = 1; month <= 12; month++)
            {
                for (DayOfWeek dayOfWeek = DayOfWeek.Sunday; dayOfWeek <= DayOfWeek.Saturday; dayOfWeek++)
                {
                    var matches = new List<DateOnly>();
                    for (int day = 1; day <= DateTime.DaysInMonth(year, month); day++)
                    {
                        var date = new DateOnly(year, month, day);
                        if (date.DayOfWeek == dayOfWeek)
                            matches.Add(date);
                    }

                    for (WeekOrdinal ordinal = WeekOrdinal.First; ordinal <= WeekOrdinal.Last; ordinal++)
                    {
                        DateOnly? expected = ordinal == WeekOrdinal.Last
                            ? matches[^1]
                            : (int)ordinal <= matches.Count ? matches[(int)ordinal - 1] : null;

                        Assert.AreEqual(expected, WeekdayMath.NthWeekdayInMonth(year, month, dayOfWeek, ordinal), $"{ordinal} {dayOfWeek} of {year}-{month:D2}");
                    }
                }
            }
        }
    }

    /// <summary>
    /// Verifies that a year or month outside the calendar yields no date rather than throwing.
    /// </summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month.</param>
    [TestMethod]
    [DataRow(0, 1)]
    [DataRow(10000, 1)]
    [DataRow(2023, 0)]
    [DataRow(2023, 13)]
    public void NthWeekdayInMonth_WhenYearOrMonthIsOutOfRange_ShouldReturnNull(int year, int month)
    {
        Assert.IsNull(WeekdayMath.NthWeekdayInMonth(year, month, DayOfWeek.Monday, WeekOrdinal.First));
    }

    /// <summary>
    /// Verifies that a weekday <see cref="DayOfWeek" /> does not define, with a defined ordinal, throws
    /// <see cref="ArgumentOutOfRangeException" /> naming <c>dayOfWeek</c>.
    /// </summary>
    /// <param name="ordinal">The defined ordinal.</param>
    [TestMethod]
    [DataRow(WeekOrdinal.First)]
    [DataRow(WeekOrdinal.Fifth)]
    [DataRow(WeekOrdinal.Last)]
    public void NthWeekdayInMonth_WhenDayOfWeekIsUndefined_ShouldThrowArgumentOutOfRangeException(WeekOrdinal ordinal)
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = WeekdayMath.NthWeekdayInMonth(2023, 1, (DayOfWeek)7, ordinal);
        });

        Assert.AreEqual("dayOfWeek", ex.ParamName);
    }

    /// <summary>
    /// Verifies that when both the weekday and the ordinal are undefined the ordinal decides first, so the call yields
    /// no date rather than throwing for the weekday.
    /// </summary>
    [TestMethod]
    public void NthWeekdayInMonth_WhenOrdinalAndDayOfWeekAreBothUndefined_ShouldReturnNull()
    {
        Assert.IsNull(WeekdayMath.NthWeekdayInMonth(2023, 1, (DayOfWeek)7, (WeekOrdinal)0));
    }
}
