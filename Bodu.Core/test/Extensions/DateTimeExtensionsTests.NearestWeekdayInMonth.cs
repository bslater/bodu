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
}
