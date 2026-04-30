// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensionsTests.IsLastDateOfMonth.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class DateOnlyExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensionsTests.IsLastDateOfMonth(DateTime)" /> returns <c>true</c> when the date represents the
    /// last day of the month.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(DateTimeExtensionsTests.IsLastDateOfMonthDataTestData), typeof(DateTimeExtensionsTests), DynamicDataSourceType.Method)]
    public void IsLastDateOfMonth_WhenDateIsLastDay_ShouldReturnTrue(DateTime input)
    {
        var actual = input.IsLastDateOfMonth();

        Assert.IsTrue(actual);
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensionsTests.IsLastDateOfMonth(DateTime)" /> returns <c>false</c> when the date does not
    /// represent the last day of the month.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(DateTimeExtensionsTests.IsNotLastDateOfMonthTestData), typeof(DateTimeExtensionsTests), DynamicDataSourceType.Method)]
    public void IsLastDateOfMonth_WhenDateIsNotLastDay_ShouldReturnFalse(DateTime input)
    {
        var actual = input.IsLastDateOfMonth();

        Assert.IsFalse(actual);
    }
}
