// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensionsTests.NearestWeekdayInMonth.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Bodu.Extensions;

public partial class DateOnlyExtensionsTests
{
    /// <summary>Verifies DateOnly adjustments using the DateTime test cases for the same calendar operation.</summary>
    [TestMethod]
    [DynamicData(nameof(DateTimeExtensionsTests.NearestWeekdayInMonthTestData), typeof(DateTimeExtensionsTests))]
    public void NearestWeekdayInMonth_WhenCalled_ShouldReturnExpectedDate(DateTime inputDateTime, DateTime expectedDateTime)
    {
        DateOnly input = DateOnly.FromDateTime(inputDateTime);
        DateOnly expected = DateOnly.FromDateTime(expectedDateTime);
        DateOnly actual = input.NearestWeekdayInMonth();
        Assert.AreEqual(expected, actual);
        Assert.AreEqual(input.Month, actual.Month);
        Assert.AreEqual(input.Year, actual.Year);
    }

    /// <summary>Verifies idempotence of a month-contained nearest-weekday adjustment.</summary>
    [TestMethod]
    public void NearestWeekdayInMonth_WhenAppliedTwice_ShouldReturnSameResult()
    {
        var input = new DateOnly(2024, 6, 1);
        DateOnly once = input.NearestWeekdayInMonth();
        Assert.AreEqual(once, once.NearestWeekdayInMonth());
    }
}
