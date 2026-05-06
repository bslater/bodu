// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensionsTests.IsLastDateOfMonth.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class DateTimeExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.IsLastDateOfMonth(DateTime)" /> returns <c>true</c> when the date represents the last
    /// day of the month.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(DateTimeExtensionsTests.IsLastDateOfMonthDataTestData), DynamicDataSourceType.Method)]
    public void IsLastDateOfMonth_WhenDateIsLastDay_ShouldReturnTrue(DateTime input)
    {
        var actual = input.IsLastDateOfMonth();

        Assert.IsTrue(actual);
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.IsLastDateOfMonth(DateTime)" /> returns <c>false</c> when the date does not represent
    /// the last day of the month.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(DateTimeExtensionsTests.IsNotLastDateOfMonthTestData), DynamicDataSourceType.Method)]
    public void IsLastDateOfMonth_WhenDateIsNotLastDay_ShouldReturnFalse(DateTime input)
    {
        var actual = input.IsLastDateOfMonth();

        Assert.IsFalse(actual);
    }
}
