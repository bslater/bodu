// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensionsTests.IsoYear.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Extensions;

public partial class DateOnlyExtensionsTests
{

    /// <summary>
    /// Provides dates that fall at ISO year boundaries to exercise cases where the ISO year differs from the calendar
    /// year.
    /// </summary>
    public static IEnumerable<object[]> GetIsoYearTestData()
    {
        // Mid-year dates: ISO year == calendar year.
        yield return new object[] { new DateOnly(2024, 6, 15), 2024 };
        yield return new object[] { new DateOnly(2023, 3, 1), 2023 };

        // 1 Jan 2023 is a Sunday → belongs to ISO week 52 of 2022.
        yield return new object[] { new DateOnly(2023, 1, 1), 2022 };

        // 31 Dec 2024 is a Tuesday → belongs to ISO week 1 of 2025.
        yield return new object[] { new DateOnly(2024, 12, 31), 2025 };

        // 29 Dec 2024 is a Sunday → still 2024 in ISO calendar.
        yield return new object[] { new DateOnly(2024, 12, 29), 2024 };

        // 3 Jan 2021 is a Sunday → belongs to ISO week 53 of 2020.
        yield return new object[] { new DateOnly(2021, 1, 3), 2020 };
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.IsoYear" />, when called, matches the framework
    /// <see cref="ISOWeek" /> calculator.
    /// </summary>
    [TestMethod]
    public void IsoYear_WhenCalled_ShouldMatchIsoWeekCalculator()
    {
        var input = new DateOnly(2024, 12, 31);
        int expected = ISOWeek.GetYear(input.ToDateTime(TimeOnly.MinValue));
        int actual = input.IsoYear;
        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.IsoYear" />, when called, returns the expected ISO year.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetIsoYearTestData))]
    public void IsoYear_WhenCalled_ShouldReturnExpectedYear(DateOnly input, int expected)
    {
        int actual = input.IsoYear;
        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.IsoYear" /> agrees with the <see cref="DateTime" /> twin for the
    /// same calendar date.
    /// </summary>
    [TestMethod]
    [DataRow(2023, 1, 1)]
    [DataRow(2024, 12, 31)]
    [DataRow(2024, 6, 15)]
    public void IsoYear_WhenComparedWithDateTimeTwin_ShouldReturnIdenticalResults(int year, int month, int day)
    {
        var input = new DateOnly(year, month, day);
        Assert.AreEqual(input.ToDateTime(TimeOnly.MinValue).IsoYear, input.IsoYear);
    }

}
