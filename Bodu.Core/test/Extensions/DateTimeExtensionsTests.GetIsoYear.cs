// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensionsTests.GetIsoYear.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Extensions;

public partial class DateTimeExtensionsTests
{
    /// <summary>
    /// Provides dates that fall at ISO year boundaries to exercise cases where the ISO year
    /// differs from the calendar year.
    /// </summary>
    public static IEnumerable<object[]> GetIsoYearTestData()
    {
        // Mid-year dates: ISO year == calendar year.
        yield return new object[] { new DateTime(2024, 6, 15), 2024 };
        yield return new object[] { new DateTime(2023, 3, 1), 2023 };

        // 1 Jan 2023 is a Sunday → belongs to ISO week 52 of 2022.
        yield return new object[] { new DateTime(2023, 1, 1), 2022 };

        // 31 Dec 2024 is a Tuesday → belongs to ISO week 1 of 2025.
        yield return new object[] { new DateTime(2024, 12, 31), 2025 };

        // 29 Dec 2024 is a Sunday → still 2024 in ISO calendar.
        yield return new object[] { new DateTime(2024, 12, 29), 2024 };

        // 3 Jan 2021 is a Sunday → belongs to ISO week 53 of 2020.
        yield return new object[] { new DateTime(2021, 1, 3), 2020 };
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.GetIsoYear" />, when Called, returns the expected value.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetIsoYearTestData), DynamicDataSourceType.Method)]
    public void GetIsoYear_WhenCalled_ShouldReturnExpectedYear(DateTime input, int expected)
    {
        int actual = input.GetIsoYear();
        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.GetIsoYear" />, when Called, returns the expected value.
    /// </summary>
    [TestMethod]
    public void GetIsoYear_WhenCalled_ShouldMatchIsoWeekCalculator()
    {
        DateTime input = new DateTime(2024, 12, 31);
        int expected = ISOWeek.GetYear(input);
        int actual = input.GetIsoYear();
        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.GetIsoYear" />, when TimeOfDayIsNonZero, returns the expected value.
    /// </summary>
    [TestMethod]
    public void GetIsoYear_WhenTimeOfDayIsNonZero_ShouldReturnSameResultAsMidnight()
    {
        DateTime morning = new DateTime(2024, 12, 31, 0, 0, 0);
        DateTime evening = new DateTime(2024, 12, 31, 23, 59, 59);
        Assert.AreEqual(morning.GetIsoYear(), evening.GetIsoYear());
    }
}
