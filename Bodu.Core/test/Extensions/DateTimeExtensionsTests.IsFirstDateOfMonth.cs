// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensionsTests.IsFirstDateOfMonth.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class DateTimeExtensionsTests
{

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.IsFirstDateOfMonth(DateTime)" /> returns <c>true</c> when the date represents the
    /// first day of the month.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(IsFirstDateOfMonthTestData))]
    public void IsFirstDateOfMonth_WhenDateIsFirstDay_ShouldReturnTrue(DateTime input)
    {
        var actual = input.IsFirstDateOfMonth();

        Assert.IsTrue(actual);
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.IsFirstDateOfMonth(DateTime)" /> returns <c>false</c> when the date does not
    /// represent the first day of the month.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(IsNotFirstDateOfMonthTestData))]
    public void IsFirstDateOfMonth_WhenDateIsNotFirstDay_ShouldReturnFalse(DateTime input)
    {
        var actual = input.IsFirstDateOfMonth();

        Assert.IsFalse(actual);
    }

}
