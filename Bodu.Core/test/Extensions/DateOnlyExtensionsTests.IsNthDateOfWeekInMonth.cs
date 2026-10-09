// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensionsTests.IsNthDateOfWeekInMonth.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Bodu.Extensions;

public partial class DateOnlyExtensionsTests
{
    /// <summary>Verifies the DateOnly overload against the same ordinal data used by DateTime.</summary>
    [TestMethod]
    [DynamicData(nameof(DateTimeExtensionsTests.IsNthDateOfWeekInMonthTestData), typeof(DateTimeExtensionsTests))]
    public void IsNthDateOfWeekInMonth_WhenCalled_ShouldReturnExpected(DateTime inputDateTime, DayOfWeek dayOfWeek, WeekOrdinal ordinal, bool expected)
    {
        DateOnly input = DateOnly.FromDateTime(inputDateTime);
        bool actual = input.IsNthDateOfWeekInMonth(dayOfWeek, ordinal);
        Assert.AreEqual(expected, actual);
    }

    /// <summary>Verifies that the fifth occurrence is a non-match when the month contains only four weekdays.</summary>
    [TestMethod]
    public void IsNthDateOfWeekInMonth_WhenFifthDoesNotExist_ShouldReturnFalse()
    {
        var input = new DateOnly(2023, 2, 22);
        Assert.IsFalse(input.IsNthDateOfWeekInMonth(DayOfWeek.Wednesday, WeekOrdinal.Fifth));
    }

    /// <summary>Verifies that an undefined weekday throws exactly ArgumentOutOfRangeException.</summary>
    [TestMethod]
    public void IsNthDateOfWeekInMonth_WhenDayOfWeekIsInvalidEnum_ShouldThrowExactly()
    {
        var input = new DateOnly(2024, 1, 1);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            _ = input.IsNthDateOfWeekInMonth((DayOfWeek)999, WeekOrdinal.First));
    }

    /// <summary>Verifies that an undefined ordinal throws exactly ArgumentOutOfRangeException.</summary>
    [TestMethod]
    public void IsNthDateOfWeekInMonth_WhenOrdinalIsInvalidEnum_ShouldThrowExactly()
    {
        var input = new DateOnly(2024, 1, 1);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            _ = input.IsNthDateOfWeekInMonth(DayOfWeek.Monday, (WeekOrdinal)999));
    }
}
