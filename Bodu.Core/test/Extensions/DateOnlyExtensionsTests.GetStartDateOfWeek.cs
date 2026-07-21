// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensionsTests.GetStartDateOfWeek.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Extensions;

public partial class DateOnlyExtensionsTests
{

    /// <summary>
    /// Provides culture-defined ISO week rows (en-GB: FirstFourDayWeek, Monday) for the week-start calculation.
    /// </summary>
    public static IEnumerable<object[]> GetStartDateOfWeekIsoWeekTestCases => new[]
    {
        new object[] { 2024, 1,  new DateOnly(2024, 1, 1)  },  // ISO week 1 starts Mon Jan 1
        new object[] { 2024, 2,  new DateOnly(2024, 1, 8)  },
        new object[] { 2024, 3,  new DateOnly(2024, 1, 15) },
        new object[] { 2024, 52, new DateOnly(2024, 12, 23) }, // last ISO week of 2024
        new object[] { 2015, 53, new DateOnly(2015, 12, 28) }, // 2015 has 53 ISO weeks; Jan 1 is Thursday
        new object[] { 2026, 1,  new DateOnly(2025, 12, 29) }, // ISO week 1 of 2026 starts in Dec 2025
    };

    /// <summary>
    /// Provides FirstDay-rule rows (en-US: FirstDay, Sunday) for the week-start calculation.
    /// </summary>
    public static IEnumerable<object[]> GetStartDateOfWeekFirstDayWeekTestCases => new[]
    {
        new object[] { 2025, 1,  new DateOnly(2025, 1, 1) },   // week 1 begins on Jan 1 (Wednesday) — a partial week under FirstDay
        new object[] { 2025, 2,  new DateOnly(2025, 1, 5) },   // week 2 begins at the first Sunday after Jan 1
        new object[] { 2025, 53, new DateOnly(2025, 12, 28) }, // last week of 2025 begins Sun 28 Dec
        new object[] { 2024, 1,  new DateOnly(2024, 1, 1) },   // Jan 1 2024 is a Monday; week 1 still begins on Jan 1
        new object[] { 2024, 2,  new DateOnly(2024, 1, 7) },   // week 2 begins at the first Sunday after Jan 1 2024
    };

    /// <summary>
    /// Verifies that under a <see cref="CalendarWeekRule.FirstDay" /> culture (en-US), week 1 starts on January 1
    /// itself and later weeks align to the culture's week boundaries.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetStartDateOfWeekFirstDayWeekTestCases))]
    public void GetStartDateOfWeek_WithFirstDayRuleCulture_ShouldReturnExpectedDate(int year, int week, DateOnly expected)
    {
        CultureInfo culture = CultureInfo.GetCultureInfo("en-US"); // FirstDay rule, Sunday week start
        DateOnly actual = DateOnlyExtensions.GetStartDateOfWeek(year, week, culture);

        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that the current culture is used when the culture argument is null.
    /// </summary>
    [TestMethod]
    public void GetStartDateOfWeek_WhenCultureIsNull_ShouldUseCurrentCulture()
    {
        var knownCulture = new CultureInfo("en-GB"); // Week starts on Monday, FirstFourDayWeek
        var year = 2024;
        var week = 1;

        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = knownCulture;
            var resultWithNull = DateOnlyExtensions.GetStartDateOfWeek(year, week, null);
            var resultWithExplicit = DateOnlyExtensions.GetStartDateOfWeek(year, week, knownCulture);

            Assert.AreEqual(resultWithExplicit, resultWithNull, "The method should default to CultureInfo.CurrentCulture when null is passed.");
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.GetStartDateOfWeek" />, with a culture and an invalid year, throws
    /// <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    [DataRow(0)]      // DateOnly.MinValue.Year - 1
    [DataRow(10000)]  // DateOnly.MaxValue.Year + 1
    public void GetStartDateOfWeek_WithCultureAndInvalidYear_ShouldThrowExactly(int year)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = DateOnlyExtensions.GetStartDateOfWeek(year, 1, CultureInfo.CurrentCulture);
        });
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.GetStartDateOfWeek" />, with an ISO-style culture, returns the
    /// expected week-start date.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetStartDateOfWeekIsoWeekTestCases))]
    public void GetStartDateOfWeek_WithCultureInfo_ShouldReturnExpectedDate(int year, int week, DateOnly expected)
    {
        CultureInfo culture = CultureInfo.GetCultureInfo("en-GB"); // ISO-8601 based calendar rule
        DateOnly actual = DateOnlyExtensions.GetStartDateOfWeek(year, week, culture);

        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.GetStartDateOfWeek" />, with an invalid week number, throws
    /// <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void GetStartDateOfWeek_WithInvalidWeekNumber_ShouldThrowExactly()
    {
        // en-US: Week 54 does not exist
        CultureInfo culture = CultureInfo.GetCultureInfo("en-US");

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = DateOnlyExtensions.GetStartDateOfWeek(2024, 54, culture);
        });
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.GetStartDateOfWeek" />, with an invalid year, throws
    /// <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    [DataRow(0)]      // DateOnly.MinValue.Year - 1
    [DataRow(10000)]  // DateOnly.MaxValue.Year + 1
    public void GetStartDateOfWeek_WithInvalidYear_ShouldThrowExactly(int year)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = DateOnlyExtensions.GetStartDateOfWeek(year, 1);
        });
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.GetStartDateOfWeek" /> agrees with the <see cref="DateTime" />
    /// twin for identical inputs under a fixed culture.
    /// </summary>
    [TestMethod]
    [DataRow(2024, 1)]
    [DataRow(2026, 1)]
    [DataRow(2015, 53)]
    public void GetStartDateOfWeek_WhenComparedWithDateTimeTwin_ShouldReturnIdenticalResults(int year, int week)
    {
        CultureInfo culture = CultureInfo.GetCultureInfo("en-GB");
        Assert.AreEqual(DateTimeExtensions.GetStartDateOfWeek(year, week, culture).ToDateOnly(), DateOnlyExtensions.GetStartDateOfWeek(year, week, culture));
    }

}
