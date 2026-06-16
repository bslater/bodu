// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensionsTests.IsoWeekOfYear.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Extensions;

public partial class DateTimeExtensionsTests
{

    /// <summary>
    /// Provides representative dates paired with their expected ISO 8601 week numbers.
    /// </summary>
    public static IEnumerable<object[]> GetIsoWeekOfYearTestData()
    {
        // Thu 4 Jan 2024 is always in ISO week 1 by definition (first Thursday of year).
        yield return new object[] { new DateTime(2024, 1, 4), 1 };
        // Mon 1 Jan 2024 is also in week 1 since week containing 4 Jan starts Mon 1 Jan.
        yield return new object[] { new DateTime(2024, 1, 1), 1 };
        // Sun 31 Dec 2023 is in ISO week 52 of 2023.
        yield return new object[] { new DateTime(2023, 12, 31), 52 };
        // Fri 1 Jan 2021 is in ISO week 53 of 2020 (long year).
        yield return new object[] { new DateTime(2021, 1, 1), 53 };
        // Mid-year sanity check.
        yield return new object[] { new DateTime(2024, 6, 17), 25 };
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.IsoWeekOfYear" />, when Called, returns the expected value.
    /// </summary>
    [TestMethod]
    [DataRow(2024, 12, 30, DisplayName = "Late December Monday belonging to week 1 of next year")]
    [DataRow(2024, 12, 31, DisplayName = "Late December Tuesday belonging to week 1 of next year")]
    [DataRow(2025, 1, 1, DisplayName = "New Year's Day belonging to week 1 of same year")]
    [DataRow(2021, 1, 1, DisplayName = "New Year's Day belonging to week 53 of previous year")]
    [DataRow(2021, 1, 3, DisplayName = "Early January Sunday belonging to week 53 of previous year")]
    [DataRow(2021, 1, 4, DisplayName = "Early January Monday starting week 1 of current year")]
    [DataRow(2020, 12, 28, DisplayName = "Late December Monday in last week of same year")]
    [DataRow(2015, 12, 31, DisplayName = "Late December Thursday belonging to week 53 of same year")]
    [DataRow(2009, 12, 31, DisplayName = "Late December Thursday belonging to week 53 of same year")]
    [DataRow(2010, 1, 1, DisplayName = "New Year's Friday belonging to week 53 of previous year")]
    [DataRow(2024, 6, 15, DisplayName = "Mid-year date in unambiguous week")]
    [DataRow(2024, 1, 1, DisplayName = "New Year's Day in week 1 of same year")]
    public void IsoWeekOfYear_WhenCalled_ShouldMatchIsoWeekCalculator(int year, int month, int day)
    {
        var input = new DateTime(year, month, day);
        int expected = ISOWeek.GetWeekOfYear(input);
        int actual = input.IsoWeekOfYear();
        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.IsoWeekOfYear" />, when Called, returns the expected value.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetIsoWeekOfYearTestData))]
    public void IsoWeekOfYear_WhenCalled_ShouldReturnExpectedWeekNumber(DateTime input, int expected)
    {
        int actual = input.IsoWeekOfYear();
        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.IsoWeekOfYear" />, when Called, returns <see langword="true" />.
    /// </summary>
    [TestMethod]
    public void IsoWeekOfYear_WhenCalled_ShouldReturnValueInRange1To53()
    {
        var input = new DateTime(2024, 8, 15);
        int actual = input.IsoWeekOfYear();
        Assert.IsTrue(actual is >= 1 and <= 53);
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.IsoWeekOfYear" />, when TimeOfDayIsNonZero, returns the expected value.
    /// </summary>
    [TestMethod]
    public void IsoWeekOfYear_WhenTimeOfDayIsNonZero_ShouldReturnSameResultAsMidnight()
    {
        var morning = new DateTime(2024, 1, 4, 0, 0, 0);
        var evening = new DateTime(2024, 1, 4, 23, 59, 59);
        Assert.AreEqual(morning.IsoWeekOfYear(), evening.IsoWeekOfYear());
    }

}
