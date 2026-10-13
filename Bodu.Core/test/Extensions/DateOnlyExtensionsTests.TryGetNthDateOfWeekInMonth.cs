// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensionsTests.TryGetNthDateOfWeekInMonth.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class DateOnlyExtensionsTests
{
    /// <summary>
    /// The years the nth-weekday grid covers: both ends of the range, a common year, a leap year, a century that is not
    /// a leap year and one that is.
    /// </summary>
    private static readonly int[] NthWeekdayGridYears = [1, 1900, 2000, 2023, 2024, 9999];

    /// <summary>
    /// Verifies that the internal non-throwing nth-weekday lookup finds every occurrence a month has, for every
    /// weekday, ordinal and month of the grid years, matching a scan of the month day by day.
    /// </summary>
    [TestMethod]
    public void TryGetNthDateOfWeekInMonth_WhenTheMonthHasTheOccurrence_ShouldReturnIt()
    {
        foreach ((int year, int month, DayOfWeek dayOfWeek, WeekOrdinal ordinal, DateOnly? expected) in NthWeekdayGrid())
        {
            if (expected is not DateOnly occurrence)
                continue;

            bool found = DateOnlyExtensions.TryGetNthDateOfWeekInMonth(year, month, dayOfWeek, ordinal, out DateOnly actual);

            Assert.IsTrue(found, $"{ordinal} {dayOfWeek} of {year}-{month:D2}");
            Assert.AreEqual(occurrence, actual, $"{ordinal} {dayOfWeek} of {year}-{month:D2}");
        }
    }

    /// <summary>
    /// Verifies that the internal non-throwing nth-weekday lookup reports a fifth occurrence a month lacks as missing,
    /// without throwing, December 9999 included.
    /// </summary>
    [TestMethod]
    public void TryGetNthDateOfWeekInMonth_WhenTheMonthLacksTheFifthOccurrence_ShouldReturnFalse()
    {
        int missing = 0;
        foreach ((int year, int month, DayOfWeek dayOfWeek, WeekOrdinal ordinal, DateOnly? expected) in NthWeekdayGrid())
        {
            if (expected is not null)
                continue;

            missing++;
            bool found = DateOnlyExtensions.TryGetNthDateOfWeekInMonth(year, month, dayOfWeek, ordinal, out DateOnly actual);

            Assert.IsFalse(found, $"{ordinal} {dayOfWeek} of {year}-{month:D2}");
            Assert.AreEqual(default, actual, $"{ordinal} {dayOfWeek} of {year}-{month:D2}");
        }

        Assert.IsGreaterThan(0, missing);
    }

    /// <summary>
    /// Verifies that the internal non-throwing nth-weekday lookup reports an ordinal <see cref="WeekOrdinal" /> does not
    /// define as naming no occurrence, without throwing, at either end of the date range and between them.
    /// </summary>
    /// <param name="ordinal">The undefined ordinal.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    [DataRow(7)]
    [DataRow(53)]
    [DataRow(int.MinValue)]
    [DataRow(int.MaxValue)]
    public void TryGetNthDateOfWeekInMonth_WhenOrdinalIsUndefined_ShouldReturnFalse(int ordinal)
    {
        foreach ((int year, int month) in new[] { (1, 1), (2023, 1), (9999, 12) })
        {
            for (DayOfWeek dayOfWeek = DayOfWeek.Sunday; dayOfWeek <= DayOfWeek.Saturday; dayOfWeek++)
            {
                bool found = DateOnlyExtensions.TryGetNthDateOfWeekInMonth(year, month, dayOfWeek, (WeekOrdinal)ordinal, out DateOnly actual);

                Assert.IsFalse(found, $"{dayOfWeek} of {year}-{month:D2}");
                Assert.AreEqual(default, actual);
            }
        }
    }

    /// <summary>
    /// Enumerates every weekday and ordinal of every month of the grid years, with the occurrence a day-by-day scan of
    /// the month finds, or <see langword="null" /> when the month has no such occurrence.
    /// </summary>
    /// <returns>The grid rows.</returns>
    private static IEnumerable<(int Year, int Month, DayOfWeek DayOfWeek, WeekOrdinal Ordinal, DateOnly? Expected)> NthWeekdayGrid()
    {
        foreach (int year in NthWeekdayGridYears)
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

                        yield return (year, month, dayOfWeek, ordinal, expected);
                    }
                }
            }
        }
    }
}
