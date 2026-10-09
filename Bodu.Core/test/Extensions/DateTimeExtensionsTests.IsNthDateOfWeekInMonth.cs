// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensionsTests.IsNthDateOfWeekInMonth.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Bodu.Extensions;

public partial class DateTimeExtensionsTests
{
    /// <summary>Returns dates and expected ordinal weekday membership, including nonexistent fifth occurrences.</summary>
    public static IEnumerable<object[]> IsNthDateOfWeekInMonthTestData()
    {
        yield return new object[] { new DateTime(2024, 1, 1), DayOfWeek.Monday, WeekOrdinal.First, true };
        yield return new object[] { new DateTime(2024, 1, 8), DayOfWeek.Monday, WeekOrdinal.Second, true };
        yield return new object[] { new DateTime(2024, 1, 15), DayOfWeek.Monday, WeekOrdinal.Third, true };
        yield return new object[] { new DateTime(2024, 1, 22), DayOfWeek.Monday, WeekOrdinal.Fourth, true };
        yield return new object[] { new DateTime(2024, 1, 29), DayOfWeek.Monday, WeekOrdinal.Fifth, true };
        yield return new object[] { new DateTime(2024, 1, 29), DayOfWeek.Monday, WeekOrdinal.Last, true };
        yield return new object[] { new DateTime(2024, 1, 22), DayOfWeek.Monday, WeekOrdinal.Last, false };
        yield return new object[] { new DateTime(2023, 2, 22), DayOfWeek.Wednesday, WeekOrdinal.Fifth, false };
        yield return new object[] { new DateTime(2023, 2, 22), DayOfWeek.Wednesday, WeekOrdinal.Last, true };
        yield return new object[] { new DateTime(2024, 2, 29), DayOfWeek.Thursday, WeekOrdinal.Fifth, true };
        yield return new object[] { new DateTime(2024, 2, 29), DayOfWeek.Thursday, WeekOrdinal.Last, true };
        yield return new object[] { new DateTime(2024, 3, 31), DayOfWeek.Sunday, WeekOrdinal.Last, true };
        yield return new object[] { new DateTime(2024, 1, 29), DayOfWeek.Tuesday, WeekOrdinal.Fifth, false };
        yield return new object[] { DateTime.MinValue, DayOfWeek.Monday, WeekOrdinal.First, true };
        yield return new object[] { DateTime.MaxValue, DayOfWeek.Friday, WeekOrdinal.Last, true };
    }

    /// <summary>Verifies that each valid ordinal matches precisely the appropriate weekday occurrence.</summary>
    [TestMethod]
    [DynamicData(nameof(IsNthDateOfWeekInMonthTestData))]
    public void IsNthDateOfWeekInMonth_WhenCalled_ShouldReturnExpected(DateTime input, DayOfWeek dayOfWeek, WeekOrdinal ordinal, bool expected)
    {
        bool actual = input.IsNthDateOfWeekInMonth(dayOfWeek, ordinal);
        Assert.AreEqual(expected, actual);
    }

    /// <summary>Verifies that a valid but nonexistent fifth occurrence returns false rather than throwing.</summary>
    [TestMethod]
    public void IsNthDateOfWeekInMonth_WhenFifthDoesNotExist_ShouldReturnFalse()
    {
        var input = new DateTime(2023, 2, 22);
        Assert.IsFalse(input.IsNthDateOfWeekInMonth(DayOfWeek.Wednesday, WeekOrdinal.Fifth));
    }

    /// <summary>Verifies that undefined weekday enum values are rejected, even when the date would not match.</summary>
    [TestMethod]
    public void IsNthDateOfWeekInMonth_WhenDayOfWeekIsInvalidEnum_ShouldThrowExactly()
    {
        var input = new DateTime(2024, 1, 1);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            _ = input.IsNthDateOfWeekInMonth((DayOfWeek)999, WeekOrdinal.First));
    }

    /// <summary>Verifies that undefined ordinal enum values are rejected.</summary>
    [TestMethod]
    public void IsNthDateOfWeekInMonth_WhenOrdinalIsInvalidEnum_ShouldThrowExactly()
    {
        var input = new DateTime(2024, 1, 1);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            _ = input.IsNthDateOfWeekInMonth(DayOfWeek.Monday, (WeekOrdinal)999));
    }

    /// <summary>Verifies that time of day and DateTime.Kind do not change ordinal membership.</summary>
    [TestMethod]
    [DataRow(DateTimeKind.Unspecified)]
    [DataRow(DateTimeKind.Utc)]
    [DataRow(DateTimeKind.Local)]
    public void IsNthDateOfWeekInMonth_WhenTimeAndKindVary_ShouldReturnSameResult(DateTimeKind kind)
    {
        DateTime input = new DateTime(2024, 1, 29, 23, 59, 59, kind).AddTicks(9999999);
        Assert.IsTrue(input.IsNthDateOfWeekInMonth(DayOfWeek.Monday, WeekOrdinal.Fifth));
        Assert.IsFalse(input.IsNthDateOfWeekInMonth(DayOfWeek.Monday, WeekOrdinal.Fourth));
    }
}
