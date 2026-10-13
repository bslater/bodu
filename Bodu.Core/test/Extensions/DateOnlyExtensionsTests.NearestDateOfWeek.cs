// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensionsTests.NearestDateOfWeek.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class DateOnlyExtensionsTests
{
    // =========================================================================
    // NearestDateOfWeek(this DateOnly, DayOfWeek)
    // =========================================================================

    /// <summary>
    /// Provides (reference date, target day-of-week, expected nearest date) tuples.
    /// April 17 2024 is a Wednesday.
    /// </summary>
    public static IEnumerable<object[]> NearestDateOfWeekTestData()
    {
        // Wednesday 17 April 2024, target Wednesday → same day.
        yield return new object[] { new DateOnly(2024, 4, 17), DayOfWeek.Wednesday, new DateOnly(2024, 4, 17) };
        // Wednesday target Tuesday → 16 April (1 day back).
        yield return new object[] { new DateOnly(2024, 4, 17), DayOfWeek.Tuesday, new DateOnly(2024, 4, 16) };
        // Wednesday target Thursday → 18 April (1 day forward).
        yield return new object[] { new DateOnly(2024, 4, 17), DayOfWeek.Thursday, new DateOnly(2024, 4, 18) };
        // Wednesday target Sunday → Sunday 14 is 3 days back, Sunday 21 is 4 forward → earlier wins.
        yield return new object[] { new DateOnly(2024, 4, 17), DayOfWeek.Sunday, new DateOnly(2024, 4, 14) };
        // Wednesday target Saturday → Sat 20 is 3 forward, Sat 13 is 4 back → forward wins.
        yield return new object[] { new DateOnly(2024, 4, 17), DayOfWeek.Saturday, new DateOnly(2024, 4, 20) };
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.NearestDateOfWeek(DateOnly, DayOfWeek)" /> returns the closest occurrence of the requested <see cref="DayOfWeek" /> in either direction.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(NearestDateOfWeekTestData))]
    public void NearestDateOfWeek_WhenCalled_ShouldReturnExpectedDate(DateOnly date, DayOfWeek dayOfWeek, DateOnly expected)
    {
        DateOnly actual = date.NearestDateOfWeek(dayOfWeek);
        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that an undefined <see cref="DayOfWeek" /> value throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void NearestDateOfWeek_WhenDayOfWeekIsInvalid_ShouldThrowExactly()
    {
        var date = new DateOnly(2024, 4, 17);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = date.NearestDateOfWeek((DayOfWeek)999);
        });
    }

    /// <summary>
    /// Verifies that when the previous occurrence is three days back and the next four days ahead, the previous date is
    /// returned.
    /// </summary>
    [TestMethod]
    public void NearestDateOfWeek_WhenPreviousOccurrenceIsCloser_ShouldReturnPreviousDate()
    {
        // Wednesday 17 April 2024, target Sunday: Sun 14 (3 days back) vs Sun 21 (4 days fwd) - back wins.
        var date = new DateOnly(2024, 4, 17);
        DateOnly actual = date.NearestDateOfWeek(DayOfWeek.Sunday);
        Assert.AreEqual(new DateOnly(2024, 4, 14), actual);
    }

    // =========================================================================
    // NearestDateOfWeek(int year, int month, int day, DayOfWeek)
    // =========================================================================

    /// <summary>
    /// Verifies that the static <see cref="DateOnlyExtensions.GetNearestDateOfWeek(int, int, int, DayOfWeek)" /> overload returns the expected nearest date.
    /// </summary>
    [TestMethod]
    public void NearestDateOfWeek_WhenUsingYearMonthDay_ShouldReturnExpectedDate()
    {
        // Wed 17 Apr 2024 → Monday 15 Apr (2 days back) is nearer than Mon 22 (5 fwd).
        DateOnly actual = DateOnlyExtensions.GetNearestDateOfWeek(2024, 4, 17, DayOfWeek.Monday);
        Assert.AreEqual(new DateOnly(2024, 4, 15), actual);
    }

    /// <summary>
    /// Verifies that the static <c>(year, month, day)</c> overload throws <see cref="ArgumentOutOfRangeException" /> for an undefined <see cref="DayOfWeek" />.
    /// </summary>
    [TestMethod]
    public void NearestDateOfWeek_WhenUsingYearMonthDayWithInvalidDayOfWeek_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = DateOnlyExtensions.GetNearestDateOfWeek(2024, 4, 17, (DayOfWeek)99);
        });
    }

    /// <summary>
    /// Verifies that the static <see cref="DateOnlyExtensions.GetNearestDateOfWeek(int, int, int, DayOfWeek)" /> overload throws
    /// <see cref="ArgumentOutOfRangeException" /> naming the offending component when the year, month, and day do not
    /// form a valid date.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="year">The year component.</param>
    /// <param name="month">The month component.</param>
    /// <param name="day">The day component.</param>
    /// <param name="expectedParamName">The parameter name the exception is expected to report.</param>
    [TestMethod]
    [DataRow("year 0", 0, 1, 1, "year")]
    [DataRow("year 10000", 10_000, 1, 1, "year")]
    [DataRow("month 0", 2024, 0, 1, "month")]
    [DataRow("month 13", 2024, 13, 1, "month")]
    [DataRow("month 14", 2024, 14, 1, "month")]
    [DataRow("day 0", 2024, 1, 0, "day")]
    [DataRow("day 32", 2024, 1, 32, "day")]
    [DataRow("30 February", 2024, 2, 30, "day")]
    [DataRow("29 February in a common year", 2023, 2, 29, "day")]
    public void GetNearestDateOfWeek_WhenDateIsInvalid_ShouldThrowArgumentOutOfRangeException(
        string testName, int year, int month, int day, string expectedParamName)
    {
        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () =>
            {
                _ = DateOnlyExtensions.GetNearestDateOfWeek(year, month, day, DayOfWeek.Monday);
            },
            testName);
        Assert.AreEqual(expectedParamName, ex.ParamName, testName);
    }

    /// <summary>
    /// Verifies that when the nearest occurrence of <paramref name="dayOfWeek" /> to <see cref="DateOnly.MaxValue" />
    /// (a Friday) lies after it, <see cref="DateOnlyExtensions.NearestDateOfWeek(DateOnly, DayOfWeek)" /> returns the
    /// earlier occurrence, which is representable, instead of throwing.
    /// </summary>
    /// <param name="dayOfWeek">The target day of the week.</param>
    /// <param name="daysBack">The number of days from <see cref="DateOnly.MaxValue" /> back to the expected result.</param>
    [TestMethod]
    [DataRow(DayOfWeek.Saturday, 6)]
    [DataRow(DayOfWeek.Sunday, 5)]
    [DataRow(DayOfWeek.Monday, 4)]
    public void NearestDateOfWeek_WhenNearestOccurrenceIsAfterMaxValue_ShouldReturnTheEarlierOccurrence(DayOfWeek dayOfWeek, int daysBack)
    {
        DateOnly actual = DateOnly.MaxValue.NearestDateOfWeek(dayOfWeek);

        Assert.AreEqual(DateOnly.MaxValue.AddDays(-daysBack), actual);
    }

    /// <summary>
    /// Verifies that when the nearest occurrence of <paramref name="dayOfWeek" /> to <see cref="DateOnly.MinValue" />
    /// (a Monday) lies before it, <see cref="DateOnlyExtensions.NearestDateOfWeek(DateOnly, DayOfWeek)" /> returns the
    /// later occurrence, which is representable, instead of throwing.
    /// </summary>
    /// <param name="dayOfWeek">The target day of the week.</param>
    /// <param name="daysForward">The number of days from <see cref="DateOnly.MinValue" /> to the expected result.</param>
    [TestMethod]
    [DataRow(DayOfWeek.Sunday, 6)]
    [DataRow(DayOfWeek.Saturday, 5)]
    [DataRow(DayOfWeek.Friday, 4)]
    public void NearestDateOfWeek_WhenNearestOccurrenceIsBeforeMinValue_ShouldReturnTheLaterOccurrence(DayOfWeek dayOfWeek, int daysForward)
    {
        DateOnly actual = DateOnly.MinValue.NearestDateOfWeek(dayOfWeek);

        Assert.AreEqual(DateOnly.MinValue.AddDays(daysForward), actual);
    }

    /// <summary>
    /// Verifies that, for every <see cref="DayOfWeek" /> and every reference date in the first and last two weeks of the
    /// <see cref="DateOnly" /> range, <see cref="DateOnlyExtensions.NearestDateOfWeek(DateOnly, DayOfWeek)" /> returns
    /// the nearest occurrence that a <see cref="DateOnly" /> can represent.
    /// </summary>
    [TestMethod]
    public void NearestDateOfWeek_WhenNearTheEndsOfTheRange_ShouldReturnTheNearestRepresentableOccurrence()
    {
        for (int day = 0; day < 14; day++)
        {
            foreach (DateOnly date in new[] { DateOnly.MinValue.AddDays(day), DateOnly.MaxValue.AddDays(-day) })
            {
                foreach (DayOfWeek dayOfWeek in Enum.GetValues<DayOfWeek>())
                {
                    DateOnly expected = NearestRepresentableDateOfWeek(date, dayOfWeek);

                    Assert.AreEqual(expected, date.NearestDateOfWeek(dayOfWeek), $"{date:O} {dayOfWeek}");
                }
            }
        }
    }

    /// <summary>
    /// Verifies that when the nearest occurrence of the requested day to the first or last date of the range lies
    /// outside it, <see cref="DateOnlyExtensions.GetNearestDateOfWeek(int, int, int, DayOfWeek)" /> returns the
    /// occurrence in the other direction, which is representable, instead of throwing.
    /// </summary>
    /// <param name="year">The year of the reference date.</param>
    /// <param name="month">The month of the reference date.</param>
    /// <param name="day">The day of the reference date.</param>
    /// <param name="dayOfWeek">The target day of the week.</param>
    /// <param name="expectedYear">The year of the expected result.</param>
    /// <param name="expectedMonth">The month of the expected result.</param>
    /// <param name="expectedDay">The day of the expected result.</param>
    [TestMethod]
    [DataRow(9999, 12, 31, DayOfWeek.Saturday, 9999, 12, 25)]
    [DataRow(9999, 12, 29, DayOfWeek.Saturday, 9999, 12, 25)]
    [DataRow(1, 1, 1, DayOfWeek.Sunday, 1, 1, 7)]
    [DataRow(1, 1, 3, DayOfWeek.Sunday, 1, 1, 7)]
    public void GetNearestDateOfWeek_WhenNearestOccurrenceIsOutsideTheRange_ShouldReturnTheOtherOccurrence(
        int year, int month, int day, DayOfWeek dayOfWeek, int expectedYear, int expectedMonth, int expectedDay)
    {
        DateOnly actual = DateOnlyExtensions.GetNearestDateOfWeek(year, month, day, dayOfWeek);

        Assert.AreEqual(new DateOnly(expectedYear, expectedMonth, expectedDay), actual);
    }

    /// <summary>
    /// Returns the nearest occurrence of <paramref name="dayOfWeek" /> to <paramref name="date" /> that a
    /// <see cref="DateOnly" /> can represent, by trying every offset of up to six days in either direction.
    /// </summary>
    /// <param name="date">The reference date.</param>
    /// <param name="dayOfWeek">The target day of the week.</param>
    /// <returns>The nearest representable occurrence.</returns>
    private static DateOnly NearestRepresentableDateOfWeek(DateOnly date, DayOfWeek dayOfWeek)
    {
        DateOnly? best = null;
        int bestDistance = int.MaxValue;
        for (int offset = -6; offset <= 6; offset++)
        {
            int dayNumber = date.DayNumber + offset;
            if (dayNumber < DateOnly.MinValue.DayNumber || dayNumber > DateOnly.MaxValue.DayNumber)
                continue;

            DateOnly candidate = DateOnly.FromDayNumber(dayNumber);
            if (candidate.DayOfWeek == dayOfWeek && Math.Abs(offset) < bestDistance)
            {
                best = candidate;
                bestDistance = Math.Abs(offset);
            }
        }

        return best!.Value;
    }
}
