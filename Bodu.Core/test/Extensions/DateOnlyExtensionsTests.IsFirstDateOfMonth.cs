// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensionsTests.IsFirstDateOfMonth.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class DateOnlyExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.IsFirstDateOfMonth(DateOnly)" /> returns <c>true</c> when the date represents the
    /// first day of the month.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(DateTimeExtensionsTests.IsFirstDateOfMonthTestData), typeof(DateTimeExtensionsTests), DynamicDataSourceType.Method)]
    public void IsFirstDateOfMonth_WhenDateIsFirstDay_ShouldReturnTrue(DateTime inputDateTime)
    {
        var input = DateOnly.FromDateTime(inputDateTime);
        Assert.IsTrue(input.IsFirstDateOfMonth());
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.IsFirstDateOfMonth(DateTime)" /> returns <c>false</c> when the date does not
    /// represent the first day of the month.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(DateTimeExtensionsTests.IsNotFirstDateOfMonthTestData), typeof(DateTimeExtensionsTests), DynamicDataSourceType.Method)]
    public void IsFirstDateOfMonth_WhenDateIsNotFirstDay_ShouldReturnFalse(DateTime inputDateTime)
    {
        var input = DateOnly.FromDateTime(inputDateTime);
        Assert.IsFalse(input.IsFirstDateOfMonth());
    }
}
