// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensionsTests.NearestDateOfWeek.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class DateTimeExtensionsTests
{
    // =========================================================================
    // NearestDateOfWeek(this DateTime, DayOfWeek)
    // =========================================================================

    /// <summary>
    /// Provides (reference DateTime, target day-of-week, expected nearest DateTime).
    /// Wednesday 17 April 2024 is the reference.
    /// </summary>
    public static IEnumerable<object[]> NearestDateOfWeekDateTimeTestData()
    {
        yield return new object[] { new DateTime(2024, 4, 17), DayOfWeek.Wednesday, new DateTime(2024, 4, 17) };
        yield return new object[] { new DateTime(2024, 4, 17), DayOfWeek.Tuesday, new DateTime(2024, 4, 16) };
        yield return new object[] { new DateTime(2024, 4, 17), DayOfWeek.Thursday, new DateTime(2024, 4, 18) };
        yield return new object[] { new DateTime(2024, 4, 17), DayOfWeek.Sunday, new DateTime(2024, 4, 14) };
        yield return new object[] { new DateTime(2024, 4, 17), DayOfWeek.Saturday, new DateTime(2024, 4, 20) };
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.NearestDateOfWeek(DateTime, DayOfWeek)" /> preserves the input's <see cref="DateTime.Kind" />.
    /// </summary>
    [TestMethod]
    public void NearestDateOfWeek_WhenCalled_ShouldPreserveInputKind()
    {
        var dateTime = new DateTime(2024, 4, 17, 0, 0, 0, DateTimeKind.Local);
        DateTime actual = dateTime.NearestDateOfWeek(DayOfWeek.Monday);
        Assert.AreEqual(DateTimeKind.Local, actual.Kind);
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.NearestDateOfWeek(DateTime, DayOfWeek)" /> returns the closest occurrence of the requested <see cref="DayOfWeek" /> in either direction.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(NearestDateOfWeekDateTimeTestData))]
    public void NearestDateOfWeek_WhenCalled_ShouldReturnExpectedDate(DateTime dateTime, DayOfWeek dayOfWeek, DateTime expected)
    {
        DateTime actual = dateTime.NearestDateOfWeek(dayOfWeek);
        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that an undefined <see cref="DayOfWeek" /> value throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void NearestDateOfWeek_WhenDayOfWeekIsInvalid_ShouldThrowExactly()
    {
        var dateTime = new DateTime(2024, 4, 17);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = dateTime.NearestDateOfWeek((DayOfWeek)999);
        });
    }

    /// <summary>
    /// Verifies that when the previous occurrence is three days back and the next four days ahead, the previous date is
    /// returned.
    /// </summary>
    [TestMethod]
    public void NearestDateOfWeek_WhenPreviousOccurrenceIsCloser_ShouldReturnPreviousDate()
    {
        var dateTime = new DateTime(2024, 4, 17);
        DateTime actual = dateTime.NearestDateOfWeek(DayOfWeek.Sunday);
        Assert.AreEqual(new DateTime(2024, 4, 14), actual);
    }

    // =========================================================================
    // NearestDateOfWeek(int year, int month, int day, DayOfWeek)
    // =========================================================================

    /// <summary>
    /// Verifies that the static <see cref="DateTimeExtensions.GetNearestDateOfWeek(int, int, int, DayOfWeek)" /> overload returns the expected nearest date.
    /// </summary>
    [TestMethod]
    public void NearestDateOfWeek_WhenUsingYearMonthDay_ShouldReturnExpectedDate()
    {
        DateTime actual = DateTimeExtensions.GetNearestDateOfWeek(2024, 4, 17, DayOfWeek.Monday);
        Assert.AreEqual(new DateTime(2024, 4, 15), actual);
    }

    /// <summary>
    /// Verifies that the static <c>(year, month, day)</c> overload always returns a result with <see cref="DateTimeKind.Unspecified" />.
    /// </summary>
    [TestMethod]
    public void NearestDateOfWeek_WhenUsingYearMonthDay_ShouldReturnResultWithUnspecifiedKind()
    {
        DateTime actual = DateTimeExtensions.GetNearestDateOfWeek(2024, 4, 17, DayOfWeek.Monday);
        Assert.AreEqual(DateTimeKind.Unspecified, actual.Kind);
    }

    /// <summary>
    /// Verifies that the static <c>(year, month, day)</c> overload throws <see cref="ArgumentOutOfRangeException" /> for an undefined <see cref="DayOfWeek" />.
    /// </summary>
    [TestMethod]
    public void NearestDateOfWeek_WhenUsingYearMonthDayWithInvalidDayOfWeek_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = DateTimeExtensions.GetNearestDateOfWeek(2024, 4, 17, (DayOfWeek)99);
        });
    }

    /// <summary>
    /// Verifies that the static <see cref="DateTimeExtensions.GetNearestDateOfWeek(int, int, int, DayOfWeek)" /> overload throws
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
                _ = DateTimeExtensions.GetNearestDateOfWeek(year, month, day, DayOfWeek.Monday);
            },
            testName);
        Assert.AreEqual(expectedParamName, ex.ParamName, testName);
    }

    /// <summary>
    /// Verifies that when the nearest occurrence of <paramref name="dayOfWeek" /> to <see cref="DateTime.MaxValue" />
    /// (a Friday) lies after it, <see cref="DateTimeExtensions.NearestDateOfWeek(DateTime, DayOfWeek)" /> returns the
    /// earlier occurrence, which is representable, instead of throwing.
    /// </summary>
    /// <param name="dayOfWeek">The target day of the week.</param>
    /// <param name="daysBack">The number of days from <see cref="DateTime.MaxValue" /> back to the expected result.</param>
    [TestMethod]
    [DataRow(DayOfWeek.Saturday, 6)]
    [DataRow(DayOfWeek.Sunday, 5)]
    [DataRow(DayOfWeek.Monday, 4)]
    public void NearestDateOfWeek_WhenNearestOccurrenceIsAfterMaxValue_ShouldReturnTheEarlierOccurrence(DayOfWeek dayOfWeek, int daysBack)
    {
        DateTime actual = DateTime.MaxValue.NearestDateOfWeek(dayOfWeek);

        Assert.AreEqual(DateTime.MaxValue.AddDays(-daysBack), actual);
    }

    /// <summary>
    /// Verifies that when the nearest occurrence of <paramref name="dayOfWeek" /> to <see cref="DateTime.MinValue" />
    /// (a Monday) lies before it, <see cref="DateTimeExtensions.NearestDateOfWeek(DateTime, DayOfWeek)" /> returns the
    /// later occurrence, which is representable, instead of throwing.
    /// </summary>
    /// <param name="dayOfWeek">The target day of the week.</param>
    /// <param name="daysForward">The number of days from <see cref="DateTime.MinValue" /> to the expected result.</param>
    [TestMethod]
    [DataRow(DayOfWeek.Sunday, 6)]
    [DataRow(DayOfWeek.Saturday, 5)]
    [DataRow(DayOfWeek.Friday, 4)]
    public void NearestDateOfWeek_WhenNearestOccurrenceIsBeforeMinValue_ShouldReturnTheLaterOccurrence(DayOfWeek dayOfWeek, int daysForward)
    {
        DateTime actual = DateTime.MinValue.NearestDateOfWeek(dayOfWeek);

        Assert.AreEqual(DateTime.MinValue.AddDays(daysForward), actual);
    }

    /// <summary>
    /// Verifies that, for every <see cref="DayOfWeek" /> and every reference date in the first and last two weeks of the
    /// <see cref="DateTime" /> range, <see cref="DateTimeExtensions.NearestDateOfWeek(DateTime, DayOfWeek)" /> returns
    /// the nearest occurrence that a <see cref="DateTime" /> can represent, with the time of day kept.
    /// </summary>
    [TestMethod]
    public void NearestDateOfWeek_WhenNearTheEndsOfTheRange_ShouldReturnTheNearestRepresentableOccurrence()
    {
        for (int day = 0; day < 14; day++)
        {
            foreach (DateTime dateTime in new[] { DateTime.MinValue.AddDays(day), DateTime.MaxValue.AddDays(-day) })
            {
                foreach (DayOfWeek dayOfWeek in Enum.GetValues<DayOfWeek>())
                {
                    DateTime expected = NearestRepresentableDateOfWeek(dateTime, dayOfWeek);

                    Assert.AreEqual(expected, dateTime.NearestDateOfWeek(dayOfWeek), $"{dateTime:O} {dayOfWeek}");
                }
            }
        }
    }

    /// <summary>
    /// Verifies that when the nearest occurrence of the requested day to the first or last date of the range lies
    /// outside it, <see cref="DateTimeExtensions.GetNearestDateOfWeek(int, int, int, DayOfWeek)" /> returns the
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
        DateTime actual = DateTimeExtensions.GetNearestDateOfWeek(year, month, day, dayOfWeek);

        Assert.AreEqual(new DateTime(expectedYear, expectedMonth, expectedDay), actual);
    }

    /// <summary>
    /// Returns the nearest occurrence of <paramref name="dayOfWeek" /> to <paramref name="dateTime" /> that a
    /// <see cref="DateTime" /> can represent, by trying every offset of up to six days in either direction.
    /// </summary>
    /// <param name="dateTime">The reference date and time.</param>
    /// <param name="dayOfWeek">The target day of the week.</param>
    /// <returns>The nearest representable occurrence, with the time of day of <paramref name="dateTime" />.</returns>
    private static DateTime NearestRepresentableDateOfWeek(DateTime dateTime, DayOfWeek dayOfWeek)
    {
        DateTime? best = null;
        int bestDistance = int.MaxValue;
        for (int offset = -6; offset <= 6; offset++)
        {
            long ticks = dateTime.Ticks + (offset * TimeSpan.TicksPerDay);
            if (ticks < DateTime.MinValue.Ticks || ticks > DateTime.MaxValue.Ticks)
                continue;

            var candidate = new DateTime(ticks, dateTime.Kind);
            if (candidate.DayOfWeek == dayOfWeek && Math.Abs(offset) < bestDistance)
            {
                best = candidate;
                bestDistance = Math.Abs(offset);
            }
        }

        return best!.Value;
    }
}
