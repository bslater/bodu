// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensionsTests.DaysInMonth.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bodu.Extensions;

namespace Bodu.Extensions;

public partial class DateOnlyExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.DaysInMonth(DateOnly)" /> returns the Gregorian day count for the month of the
    /// supplied <see cref="DateOnly" /> value across leap and non-leap years.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(DateTimeExtensionsTests.DaysInMonthTestData), typeof(DateTimeExtensionsTests), DynamicDataSourceType.Method)]
    public void DaysInMonth_WhenCalled_ShouldReturnDaysInMonth(DateTime inputDateTime, int expected)
    {
        var input = DateOnly.FromDateTime(inputDateTime);

        var actual = input.DaysInMonth();

        Assert.AreEqual(expected, actual);
    }
}
