// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensionsTests.IsoYear.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

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
    /// Verifies that <see cref="DateTimeExtensions.IsoYear" />, when Called, returns the expected value.
    /// </summary>
    [TestMethod]
    public void IsoYear_WhenCalled_ShouldMatchIsoWeekCalculator()
    {
        var input = new DateTime(2024, 12, 31);
        var expected = ISOWeek.GetYear(input);
        var actual = input.IsoYear;
        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.IsoYear" />, when Called, returns the expected value.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetIsoYearTestData))]
    public void IsoYear_WhenCalled_ShouldReturnExpectedYear(DateTime input, int expected)
    {
        var actual = input.IsoYear;
        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.IsoYear" />, when TimeOfDayIsNonZero, returns the expected value.
    /// </summary>
    [TestMethod]
    public void IsoYear_WhenTimeOfDayIsNonZero_ShouldReturnSameResultAsMidnight()
    {
        var morning = new DateTime(2024, 12, 31, 0, 0, 0);
        var evening = new DateTime(2024, 12, 31, 23, 59, 59);
        Assert.AreEqual(morning.IsoYear, evening.IsoYear);
    }

}
