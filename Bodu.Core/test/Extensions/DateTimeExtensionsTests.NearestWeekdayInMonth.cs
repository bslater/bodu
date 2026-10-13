// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensionsTests.NearestWeekdayInMonth.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Bodu.Extensions;

public partial class DateTimeExtensionsTests
{
    /// <summary>Returns test cases for month-boundary weekend adjustments and unchanged weekdays.</summary>
    public static IEnumerable<object[]> NearestWeekdayInMonthTestData()
    {
        yield return new object[] { new DateTime(2024, 6, 1), new DateTime(2024, 6, 3) };   // First Saturday => Monday
        yield return new object[] { new DateTime(2024, 6, 8), new DateTime(2024, 6, 7) };   // Saturday => Friday
        yield return new object[] { new DateTime(2024, 6, 2), new DateTime(2024, 6, 3) };   // Sunday => Monday
        yield return new object[] { new DateTime(2024, 6, 30), new DateTime(2024, 6, 28) }; // Final Sunday => Friday
        yield return new object[] { new DateTime(2024, 3, 31), new DateTime(2024, 3, 29) }; // Final Sunday in March
        yield return new object[] { new DateTime(2024, 9, 1), new DateTime(2024, 9, 2) };   // First Sunday
        yield return new object[] { new DateTime(2022, 10, 1), new DateTime(2022, 10, 3) }; // First Saturday
        yield return new object[] { new DateTime(2024, 6, 17), new DateTime(2024, 6, 17) };// Weekday unchanged
        yield return new object[] { new DateTime(2024, 6, 21), new DateTime(2024, 6, 21) };// Friday unchanged
        yield return new object[] { new DateTime(2024, 2, 29), new DateTime(2024, 2, 29) };// Leap day
        yield return new object[] { DateTime.MinValue, DateTime.MinValue };
        yield return new object[] { DateTime.MaxValue, DateTime.MaxValue };
    }

    /// <summary>Verifies the nearest weekday is chosen without crossing the input month.</summary>
    [TestMethod]
    [DynamicData(nameof(NearestWeekdayInMonthTestData))]
    public void NearestWeekdayInMonth_WhenCalled_ShouldReturnExpectedDate(DateTime input, DateTime expected)
    {
        DateTime actual = input.NearestWeekdayInMonth();
        Assert.AreEqual(expected, actual);
        Assert.AreEqual(input.Month, actual.Month);
        Assert.AreEqual(input.Year, actual.Year);
    }

    /// <summary>Verifies that weekend adjustment retains time, fractional ticks, and DateTime.Kind.</summary>
    [TestMethod]
    [DataRow(DateTimeKind.Unspecified)]
    [DataRow(DateTimeKind.Utc)]
    [DataRow(DateTimeKind.Local)]
    public void NearestWeekdayInMonth_WhenKindAndTimeAreSet_ShouldPreserveBoth(DateTimeKind kind)
    {
        DateTime input = new DateTime(2024, 6, 1, 14, 37, 56, kind).AddTicks(12345);
        DateTime expected = new DateTime(2024, 6, 3, 14, 37, 56, kind).AddTicks(12345);
        DateTime actual = input.NearestWeekdayInMonth();
        Assert.AreEqual(expected.Ticks, actual.Ticks);
        Assert.AreEqual(kind, actual.Kind);
    }

    /// <summary>Verifies that the operation is idempotent because its result is already a weekday.</summary>
    [TestMethod]
    public void NearestWeekdayInMonth_WhenAppliedTwice_ShouldReturnSameResult()
    {
        var input = new DateTime(2024, 6, 30);
        DateTime once = input.NearestWeekdayInMonth();
        Assert.AreEqual(once, once.NearestWeekdayInMonth());
    }

    /// <summary>
    /// Verifies, for every day of the years 1, 1900, 2000, 2023, 2024 and 9999 and each <see cref="DateTimeKind" />,
    /// that the nearest weekday in the month follows the documented rule: a Saturday moves to the Friday before it, or
    /// to the Monday after it on the first of the month; a Sunday moves to the Monday after it, or to the Friday before
    /// it on the last of the month; every other day is unchanged. The time of day and kind are kept.
    /// </summary>
    [TestMethod]
    public void NearestWeekdayInMonth_WhenComparedWithTheRuleForEveryDay_ShouldAgreeAndKeepTimeAndKind()
    {
        TimeSpan timeOfDay = new TimeSpan(13, 45, 30) + TimeSpan.FromTicks(1234567);
        DateTimeKind[] kinds = [DateTimeKind.Unspecified, DateTimeKind.Utc, DateTimeKind.Local];

        foreach (int year in new[] { 1, 1900, 2000, 2023, 2024, 9999 })
        {
            for (int dayNumber = new DateOnly(year, 1, 1).DayNumber; dayNumber <= new DateOnly(year, 12, 31).DayNumber; dayNumber++)
            {
                DateOnly date = DateOnly.FromDayNumber(dayNumber);
                int daysInMonth = DateTime.DaysInMonth(date.Year, date.Month);
                int shift = date.DayOfWeek switch
                {
                    DayOfWeek.Saturday => date.Day == 1 ? 2 : -1,
                    DayOfWeek.Sunday => date.Day == daysInMonth ? -2 : 1,
                    _ => 0,
                };

                DateTimeKind kind = kinds[date.DayNumber % kinds.Length];
                DateTime input = DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue) + timeOfDay, kind);
                DateTime expected = DateTime.SpecifyKind(date.AddDays(shift).ToDateTime(TimeOnly.MinValue) + timeOfDay, kind);

                DateTime actual = input.NearestWeekdayInMonth();

                Assert.AreEqual(expected, actual, $"{input:O}");
                Assert.AreEqual(kind, actual.Kind, $"{input:O}");
            }
        }
    }

    /// <summary>
    /// Verifies that the first and last values a <see cref="DateTime" /> can hold, a Monday and a Friday, are their own
    /// nearest weekdays, to the tick.
    /// </summary>
    [TestMethod]
    public void NearestWeekdayInMonth_WhenAtTheEndsOfTheRange_ShouldReturnTheSameValue()
    {
        Assert.AreEqual(DateTime.MinValue, DateTime.MinValue.NearestWeekdayInMonth());
        Assert.AreEqual(DateTime.MaxValue, DateTime.MaxValue.NearestWeekdayInMonth());
    }
}
