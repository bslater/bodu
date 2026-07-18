// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensionsTests.LastDateOfWeekInQuarter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class DateOnlyExtensionsTests
{
    // The implementation searches backward from the quarter end, matching the documented "last
    // occurrence within the same quarter" contract. (An earlier implementation stepped forward from
    // the quarter end, walking into the following quarter for non-matching targets; these tests pin
    // the corrected behaviour, including targets that do not match the quarter-end day-of-week.)

    // =========================================================================
    // LastDateOfWeekInQuarter(this DateOnly, DayOfWeek, CalendarQuarterDefinition)
    // =========================================================================

    public static IEnumerable<object[]> LastDateOfWeekInQuarterJanuaryDecemberTestData()
    {
        // Q1 2024 ends Sun 31 Mar. Target Sun → 31 Mar (quarter end matches target).
        yield return new object[] { new DateOnly(2024, 2, 15), DayOfWeek.Sunday, new DateOnly(2024, 3, 31) };
        // Q1 2024 ends Sun 31 Mar. Target Fri → last Friday inside the quarter = 29 Mar.
        yield return new object[] { new DateOnly(2024, 2, 15), DayOfWeek.Friday, new DateOnly(2024, 3, 29) };
        // Q2 2024 ends Sun 30 Jun. Target Mon → last Monday inside the quarter = 24 Jun.
        yield return new object[] { new DateOnly(2024, 5, 10), DayOfWeek.Monday, new DateOnly(2024, 6, 24) };
        // Q3 2024 ends Mon 30 Sep. Target Mon → 30 Sep.
        yield return new object[] { new DateOnly(2024, 8, 20), DayOfWeek.Monday, new DateOnly(2024, 9, 30) };
        // Q4 2024 ends Tue 31 Dec. Target Tue → 31 Dec.
        yield return new object[] { new DateOnly(2024, 11, 5), DayOfWeek.Tuesday, new DateOnly(2024, 12, 31) };
    }

    /// <summary>
    /// Verifies that an undefined <see cref="DayOfWeek" /> value throws <see cref="ArgumentOutOfRangeException" /> on the instance overload.
    /// </summary>
    [TestMethod]
    public void LastDateOfWeekInQuarter_WhenDayOfWeekIsInvalid_ShouldThrowExactly()
    {
        var input = new DateOnly(2024, 4, 20);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = input.LastDateOfWeekInQuarter((DayOfWeek)999, CalendarQuarterDefinition.JanuaryToDecember);
        });
    }

    /// <summary>
    /// Verifies that an undefined <see cref="CalendarQuarterDefinition" /> value throws <see cref="ArgumentOutOfRangeException" /> on the instance overload.
    /// </summary>
    [TestMethod]
    public void LastDateOfWeekInQuarter_WhenDefinitionIsInvalid_ShouldThrowExactly()
    {
        var input = new DateOnly(2024, 4, 20);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = input.LastDateOfWeekInQuarter(DayOfWeek.Monday, (CalendarQuarterDefinition)999);
        });
    }

    /// <summary>
    /// Verifies that the static overload throws <see cref="ArgumentOutOfRangeException" /> for quarter values outside <c>1..4</c>.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(5)]
    [DataRow(-1)]
    public void LastDateOfWeekInQuarter_WhenQuarterIsOutOfRange_ShouldThrowExactly(int quarter)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = DateOnlyExtensions.GetLastDateOfWeekInQuarter(2024, quarter, DayOfWeek.Monday, CalendarQuarterDefinition.JanuaryToDecember);
        });
    }

    /// <summary>
    /// Verifies that the instance overload returns the last occurrence of the requested <see cref="DayOfWeek" /> within the quarter, whether or not the quarter end falls on that day.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(LastDateOfWeekInQuarterJanuaryDecemberTestData))]
    public void LastDateOfWeekInQuarter_WhenUsingJanuaryToDecemberDefinition_ShouldReturnLastOccurrenceInQuarter(DateOnly input, DayOfWeek dayOfWeek, DateOnly expected)
    {
        DateOnly actual = input.LastDateOfWeekInQuarter(dayOfWeek, CalendarQuarterDefinition.JanuaryToDecember);
        Assert.AreEqual(expected, actual);
    }

    // =========================================================================
    // LastDateOfWeekInQuarter(int year, int quarter, DayOfWeek, CalendarQuarterDefinition)
    // =========================================================================

    /// <summary>
    /// Verifies that the static <see cref="DateOnlyExtensions.GetLastDateOfWeekInQuarter(int, int, DayOfWeek, CalendarQuarterDefinition)" /> overload returns the expected last occurrence for each <c>(year, quarter, dayOfWeek)</c> tuple.
    /// </summary>
    [TestMethod]
    [DataRow(2024, 1, DayOfWeek.Sunday, 2024, 3, 31)]  // Q1 ends Sun; target Sun → 31 Mar
    [DataRow(2024, 1, DayOfWeek.Friday, 2024, 3, 29)]  // Q1 ends Sun; target Fri → 29 Mar (backward search)
    [DataRow(2024, 2, DayOfWeek.Monday, 2024, 6, 24)]  // Q2 ends Sun; target Mon → 24 Jun (backward search)
    [DataRow(2024, 3, DayOfWeek.Monday, 2024, 9, 30)]  // Q3 ends Mon; target Mon → 30 Sep
    [DataRow(2024, 4, DayOfWeek.Tuesday, 2024, 12, 31)] // Q4 ends Tue; target Tue → 31 Dec
    public void LastDateOfWeekInQuarter_WhenUsingYearAndQuarter_ShouldReturnExpectedDate(
        int year, int quarter, DayOfWeek dayOfWeek, int expectedYear, int expectedMonth, int expectedDay)
    {
        DateOnly actual = DateOnlyExtensions.GetLastDateOfWeekInQuarter(year, quarter, dayOfWeek, CalendarQuarterDefinition.JanuaryToDecember);
        Assert.AreEqual(new DateOnly(expectedYear, expectedMonth, expectedDay), actual);
    }

    /// <summary>
    /// Verifies that the static overload throws <see cref="ArgumentOutOfRangeException" /> for an undefined <see cref="DayOfWeek" /> value.
    /// </summary>
    [TestMethod]
    public void LastDateOfWeekInQuarter_WhenYearAndQuarterOverloadDayOfWeekIsInvalid_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = DateOnlyExtensions.GetLastDateOfWeekInQuarter(2024, 1, (DayOfWeek)999, CalendarQuarterDefinition.JanuaryToDecember);
        });
    }

    /// <summary>
    /// Verifies that the static overload throws <see cref="ArgumentOutOfRangeException" /> for an undefined <see cref="CalendarQuarterDefinition" /> value.
    /// </summary>
    [TestMethod]
    public void LastDateOfWeekInQuarter_WhenYearAndQuarterOverloadDefinitionIsInvalid_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = DateOnlyExtensions.GetLastDateOfWeekInQuarter(2024, 1, DayOfWeek.Monday, (CalendarQuarterDefinition)999);
        });
    }

    /// <summary>
    /// Verifies that passing <see cref="CalendarQuarterDefinition.Custom" /> without supplying a provider throws
    /// <see cref="InvalidOperationException" /> on the instance overload, matching the <see cref="DateTime" /> twin.
    /// </summary>
    [TestMethod]
    public void LastDateOfWeekInQuarter_WhenUsingCustomQuarterDefinitionWithoutProvider_ShouldThrowExactly()
    {
        var input = new DateOnly(2024, 4, 20);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = input.LastDateOfWeekInQuarter(DayOfWeek.Monday, CalendarQuarterDefinition.Custom);
        });
    }

    /// <summary>
    /// Verifies that the static overload throws <see cref="InvalidOperationException" /> for
    /// <see cref="CalendarQuarterDefinition.Custom" />, matching the <see cref="DateTime" /> twin.
    /// </summary>
    [TestMethod]
    public void GetLastDateOfWeekInQuarter_WhenDefinitionIsCustom_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = DateOnlyExtensions.GetLastDateOfWeekInQuarter(2024, 1, DayOfWeek.Monday, CalendarQuarterDefinition.Custom);
        });
    }

    /// <summary>
    /// Verifies that the static overload with a definition throws <see cref="ArgumentOutOfRangeException" /> for year
    /// values outside <c>1..9999</c>, matching the <see cref="DateTime" /> twin.
    /// </summary>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(0)]
    [DataRow(10_000)]
    public void GetLastDateOfWeekInQuarter_WhenYearIsInvalid_WithDefinition_ShouldThrowExactly(int year)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = DateOnlyExtensions.GetLastDateOfWeekInQuarter(year, 1, DayOfWeek.Monday, CalendarQuarterDefinition.JanuaryToDecember);
        });
    }

    // =========================================================================
    // LastDateOfWeekInQuarter(this DateOnly, DayOfWeek)
    // =========================================================================

    /// <summary>
    /// Verifies that the parameterless-definition overload returns the last occurrence of the requested
    /// <see cref="DayOfWeek" /> within the January-to-December quarter.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(LastDateOfWeekInQuarterJanuaryDecemberTestData))]
    public void LastDateOfWeekInQuarter_WhenUsingDefaultDefinition_ShouldReturnExpectedDate(DateOnly input, DayOfWeek dayOfWeek, DateOnly expected)
    {
        DateOnly actual = input.LastDateOfWeekInQuarter(dayOfWeek);
        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that an undefined <see cref="DayOfWeek" /> value throws <see cref="ArgumentOutOfRangeException" /> on
    /// the parameterless-definition overload.
    /// </summary>
    [TestMethod]
    public void LastDateOfWeekInQuarter_WhenDayOfWeekIsInvalid_ForDefaultDefinition_ShouldThrowExactly()
    {
        var input = new DateOnly(2024, 4, 20);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = input.LastDateOfWeekInQuarter((DayOfWeek)999);
        });
    }

    // =========================================================================
    // LastDateOfWeekInQuarter(this DateOnly, DayOfWeek, IQuarterDefinitionProvider)
    // =========================================================================

    /// <summary>
    /// Provides <see cref="DateOnly" /> rows for the provider-based overload, mirroring the
    /// <see cref="DateTimeExtensionsTests.ValidQuarterProvider" /> quarter grid (Q1 = Dec–Feb, Q2 = Mar–May,
    /// Q3 = Jun–Aug, Q4 = Sep–Nov).
    /// </summary>
    public static IEnumerable<object[]> LastDateOfWeekInQuarterProviderTestData()
    {
        // Q1 (Dec 2023 – Feb 2024) ends Thu 29 Feb 2024.
        yield return new object[] { new DateOnly(2024, 1, 15), DayOfWeek.Thursday, new DateOnly(2024, 2, 29) };
        yield return new object[] { new DateOnly(2024, 1, 15), DayOfWeek.Sunday, new DateOnly(2024, 2, 25) };
        // Q3 (Jun – Aug 2024) ends Sat 31 Aug 2024.
        yield return new object[] { new DateOnly(2024, 7, 1), DayOfWeek.Saturday, new DateOnly(2024, 8, 31) };
        yield return new object[] { new DateOnly(2024, 7, 1), DayOfWeek.Monday, new DateOnly(2024, 8, 26) };
    }

    /// <summary>
    /// Verifies that the provider-based overload returns the last occurrence of the requested
    /// <see cref="DayOfWeek" /> on or before the provider's quarter end.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(LastDateOfWeekInQuarterProviderTestData))]
    public void LastDateOfWeekInQuarter_WhenUsingValidQuarterProvider_ShouldReturnExpectedDate(DateOnly input, DayOfWeek dayOfWeek, DateOnly expected)
    {
        var provider = new DateTimeExtensionsTests.ValidQuarterProvider();
        DateOnly actual = input.LastDateOfWeekInQuarter(dayOfWeek, provider);
        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> provider throws <see cref="ArgumentNullException" /> on the
    /// provider-based overload.
    /// </summary>
    [TestMethod]
    public void LastDateOfWeekInQuarter_WhenProviderIsNull_ShouldThrowExactly()
    {
        var input = new DateOnly(2024, 4, 20);

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = input.LastDateOfWeekInQuarter(DayOfWeek.Monday, (IQuarterDefinitionProvider)null!);
        });
    }

    /// <summary>
    /// Verifies that a provider whose quarter-end mapping throws causes the provider-based overload to surface
    /// <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void LastDateOfWeekInQuarter_WhenUsingInvalidProvider_ShouldThrowExactly()
    {
        var input = new DateOnly(2024, 4, 20);
        var provider = new DateTimeExtensionsTests.InValidQuarterProvider();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = input.LastDateOfWeekInQuarter(DayOfWeek.Monday, provider);
        });
    }

    // =========================================================================
    // GetLastDateOfWeekInQuarter(int year, int quarter, DayOfWeek)
    // =========================================================================

    /// <summary>
    /// Verifies that the three-argument static overload with the default January-to-December definition returns the
    /// expected last occurrence.
    /// </summary>
    [TestMethod]
    [DataRow(2024, 1, DayOfWeek.Sunday, 2024, 3, 31)]
    [DataRow(2024, 1, DayOfWeek.Friday, 2024, 3, 29)]
    [DataRow(2024, 2, DayOfWeek.Monday, 2024, 6, 24)]
    [DataRow(2024, 4, DayOfWeek.Tuesday, 2024, 12, 31)]
    public void GetLastDateOfWeekInQuarter_WhenUsingDefaultDefinition_ShouldReturnExpectedDate(
        int year, int quarter, DayOfWeek dayOfWeek, int expectedYear, int expectedMonth, int expectedDay)
    {
        DateOnly actual = DateOnlyExtensions.GetLastDateOfWeekInQuarter(year, quarter, dayOfWeek);
        Assert.AreEqual(new DateOnly(expectedYear, expectedMonth, expectedDay), actual);
    }

    /// <summary>
    /// Verifies that the three-argument static overload throws <see cref="ArgumentOutOfRangeException" /> for year
    /// values outside <c>1..9999</c>.
    /// </summary>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(0)]
    [DataRow(10_000)]
    public void GetLastDateOfWeekInQuarter_WhenYearIsInvalid_ShouldThrowExactly(int year)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = DateOnlyExtensions.GetLastDateOfWeekInQuarter(year, 1, DayOfWeek.Monday);
        });
    }

    /// <summary>
    /// Verifies that the three-argument static overload throws <see cref="ArgumentOutOfRangeException" /> for quarter
    /// values outside <c>1..4</c>.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(5)]
    [DataRow(-1)]
    public void GetLastDateOfWeekInQuarter_WhenQuarterIsOutOfRange_ForDefaultDefinition_ShouldThrowExactly(int quarter)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = DateOnlyExtensions.GetLastDateOfWeekInQuarter(2024, quarter, DayOfWeek.Monday);
        });
    }

    /// <summary>
    /// Verifies that the three-argument static overload throws <see cref="ArgumentOutOfRangeException" /> for an
    /// undefined <see cref="DayOfWeek" /> value.
    /// </summary>
    [TestMethod]
    public void GetLastDateOfWeekInQuarter_WhenDayOfWeekIsInvalid_ForDefaultDefinition_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = DateOnlyExtensions.GetLastDateOfWeekInQuarter(2024, 1, (DayOfWeek)999);
        });
    }

}
