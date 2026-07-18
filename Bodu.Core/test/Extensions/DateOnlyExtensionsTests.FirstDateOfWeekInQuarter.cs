// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensionsTests.FirstDateOfWeekInQuarter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class DateOnlyExtensionsTests
{
    // =========================================================================
    // FirstDateOfWeekInQuarter(this DateOnly, DayOfWeek, CalendarQuarterDefinition)
    // =========================================================================

    public static IEnumerable<object[]> FirstDateOfWeekInQuarterJanuaryDecemberTestData()
    {
        // (input date, target DayOfWeek, expected first occurrence)
        // Q1 2024 starts on Mon 1 Jan. First Monday = 1 Jan; first Friday = 5 Jan.
        yield return new object[] { new DateOnly(2024, 2, 15), DayOfWeek.Monday, new DateOnly(2024, 1, 1) };
        yield return new object[] { new DateOnly(2024, 2, 15), DayOfWeek.Friday, new DateOnly(2024, 1, 5) };
        // Q2 2024 starts Mon 1 Apr. First Monday = 1 Apr; first Sunday = 7 Apr.
        yield return new object[] { new DateOnly(2024, 5, 10), DayOfWeek.Monday, new DateOnly(2024, 4, 1) };
        yield return new object[] { new DateOnly(2024, 5, 10), DayOfWeek.Sunday, new DateOnly(2024, 4, 7) };
        // Q3 2024 starts Mon 1 Jul.
        yield return new object[] { new DateOnly(2024, 8, 20), DayOfWeek.Monday, new DateOnly(2024, 7, 1) };
        yield return new object[] { new DateOnly(2024, 8, 20), DayOfWeek.Wednesday, new DateOnly(2024, 7, 3) };
        // Q4 2024 starts Tue 1 Oct. First Monday = 7 Oct.
        yield return new object[] { new DateOnly(2024, 11, 5), DayOfWeek.Monday, new DateOnly(2024, 10, 7) };
        yield return new object[] { new DateOnly(2024, 11, 5), DayOfWeek.Tuesday, new DateOnly(2024, 10, 1) };
    }

    /// <summary>
    /// Verifies that when the quarter starts on the requested <see cref="DayOfWeek" />, the result is the quarter-start date itself.
    /// </summary>
    [TestMethod]
    public void FirstDateOfWeekInQuarter_WhenDateFallsOnTargetDayOfWeekFirstInQuarter_ShouldReturnSameDate()
    {
        var input = new DateOnly(2024, 1, 1); // Monday, Q1 start
        DateOnly actual = input.FirstDateOfWeekInQuarter(DayOfWeek.Monday, CalendarQuarterDefinition.JanuaryToDecember);
        Assert.AreEqual(new DateOnly(2024, 1, 1), actual);
    }

    /// <summary>
    /// Verifies that an undefined <see cref="DayOfWeek" /> value throws <see cref="ArgumentOutOfRangeException" /> on the instance overload.
    /// </summary>
    [TestMethod]
    public void FirstDateOfWeekInQuarter_WhenDayOfWeekIsInvalid_ShouldThrowExactly()
    {
        var input = new DateOnly(2024, 4, 20);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = input.FirstDateOfWeekInQuarter((DayOfWeek)999, CalendarQuarterDefinition.JanuaryToDecember);
        });
    }

    /// <summary>
    /// Verifies that an undefined <see cref="CalendarQuarterDefinition" /> value throws <see cref="ArgumentOutOfRangeException" /> on the instance overload.
    /// </summary>
    [TestMethod]
    public void FirstDateOfWeekInQuarter_WhenDefinitionIsInvalid_ShouldThrowExactly()
    {
        var input = new DateOnly(2024, 4, 20);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = input.FirstDateOfWeekInQuarter(DayOfWeek.Monday, (CalendarQuarterDefinition)999);
        });
    }

    /// <summary>
    /// Verifies that the static overload throws <see cref="ArgumentOutOfRangeException" /> for quarter values outside <c>1..4</c>.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(5)]
    [DataRow(-1)]
    public void FirstDateOfWeekInQuarter_WhenQuarterIsOutOfRange_ShouldThrowExactly(int quarter)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = DateOnlyExtensions.GetFirstDateOfWeekInQuarter(2024, quarter, DayOfWeek.Monday, CalendarQuarterDefinition.JanuaryToDecember);
        });
    }

    /// <summary>
    /// Verifies that the instance overload returns the expected first occurrence of the requested <see cref="DayOfWeek" /> within the January-to-December quarter for the given input.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(FirstDateOfWeekInQuarterJanuaryDecemberTestData))]
    public void FirstDateOfWeekInQuarter_WhenUsingJanuaryToDecember_ShouldReturnExpectedDate(DateOnly input, DayOfWeek dayOfWeek, DateOnly expected)
    {
        DateOnly actual = input.FirstDateOfWeekInQuarter(dayOfWeek, CalendarQuarterDefinition.JanuaryToDecember);
        Assert.AreEqual(expected, actual);
    }

    // =========================================================================
    // FirstDateOfWeekInQuarter(int year, int quarter, DayOfWeek, CalendarQuarterDefinition)
    // =========================================================================

    /// <summary>
    /// Verifies that the static <see cref="DateOnlyExtensions.GetFirstDateOfWeekInQuarter(int, int, DayOfWeek, CalendarQuarterDefinition)" /> overload returns the expected first occurrence for each <c>(year, quarter, dayOfWeek)</c> tuple.
    /// </summary>
    [TestMethod]
    [DataRow(2024, 1, DayOfWeek.Monday, 2024, 1, 1)]
    [DataRow(2024, 1, DayOfWeek.Friday, 2024, 1, 5)]
    [DataRow(2024, 4, DayOfWeek.Monday, 2024, 10, 7)]
    [DataRow(2023, 2, DayOfWeek.Sunday, 2023, 4, 2)]
    public void FirstDateOfWeekInQuarter_WhenUsingYearAndQuarter_ShouldReturnExpectedDate(
        int year, int quarter, DayOfWeek dayOfWeek, int expectedYear, int expectedMonth, int expectedDay)
    {
        DateOnly actual = DateOnlyExtensions.GetFirstDateOfWeekInQuarter(year, quarter, dayOfWeek, CalendarQuarterDefinition.JanuaryToDecember);
        Assert.AreEqual(new DateOnly(expectedYear, expectedMonth, expectedDay), actual);
    }

    /// <summary>
    /// Verifies that the static overload throws <see cref="ArgumentOutOfRangeException" /> for an undefined <see cref="DayOfWeek" /> value.
    /// </summary>
    [TestMethod]
    public void FirstDateOfWeekInQuarter_WhenYearAndQuarterOverloadDayOfWeekIsInvalid_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = DateOnlyExtensions.GetFirstDateOfWeekInQuarter(2024, 1, (DayOfWeek)999, CalendarQuarterDefinition.JanuaryToDecember);
        });
    }

    /// <summary>
    /// Verifies that the static overload throws <see cref="ArgumentOutOfRangeException" /> for an undefined <see cref="CalendarQuarterDefinition" /> value.
    /// </summary>
    [TestMethod]
    public void FirstDateOfWeekInQuarter_WhenYearAndQuarterOverloadDefinitionIsInvalid_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = DateOnlyExtensions.GetFirstDateOfWeekInQuarter(2024, 1, DayOfWeek.Monday, (CalendarQuarterDefinition)999);
        });
    }

    /// <summary>
    /// Verifies that passing <see cref="CalendarQuarterDefinition.Custom" /> without supplying a provider throws
    /// <see cref="InvalidOperationException" /> on the instance overload, matching the <see cref="DateTime" /> twin.
    /// </summary>
    [TestMethod]
    public void FirstDateOfWeekInQuarter_WhenUsingCustomQuarterDefinitionWithoutProvider_ShouldThrowExactly()
    {
        var input = new DateOnly(2024, 4, 20);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = input.FirstDateOfWeekInQuarter(DayOfWeek.Monday, CalendarQuarterDefinition.Custom);
        });
    }

    /// <summary>
    /// Verifies that the static overload throws <see cref="InvalidOperationException" /> for
    /// <see cref="CalendarQuarterDefinition.Custom" />, matching the <see cref="DateTime" /> twin.
    /// </summary>
    [TestMethod]
    public void GetFirstDateOfWeekInQuarter_WhenDefinitionIsCustom_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = DateOnlyExtensions.GetFirstDateOfWeekInQuarter(2024, 1, DayOfWeek.Monday, CalendarQuarterDefinition.Custom);
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
    public void GetFirstDateOfWeekInQuarter_WhenYearIsInvalid_WithDefinition_ShouldThrowExactly(int year)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = DateOnlyExtensions.GetFirstDateOfWeekInQuarter(year, 1, DayOfWeek.Monday, CalendarQuarterDefinition.JanuaryToDecember);
        });
    }

    // =========================================================================
    // FirstDateOfWeekInQuarter(this DateOnly, DayOfWeek)
    // =========================================================================

    /// <summary>
    /// Verifies that the parameterless-definition overload returns the expected first-weekday occurrence using the
    /// January-to-December quarter boundaries.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(FirstDateOfWeekInQuarterJanuaryDecemberTestData))]
    public void FirstDateOfWeekInQuarter_WhenUsingDefaultDefinition_ShouldReturnExpectedDate(DateOnly input, DayOfWeek dayOfWeek, DateOnly expected)
    {
        DateOnly actual = input.FirstDateOfWeekInQuarter(dayOfWeek);
        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that an undefined <see cref="DayOfWeek" /> value throws <see cref="ArgumentOutOfRangeException" /> on
    /// the parameterless-definition overload.
    /// </summary>
    [TestMethod]
    public void FirstDateOfWeekInQuarter_WhenDayOfWeekIsInvalid_ForDefaultDefinition_ShouldThrowExactly()
    {
        var input = new DateOnly(2024, 4, 20);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = input.FirstDateOfWeekInQuarter((DayOfWeek)999);
        });
    }

    // =========================================================================
    // FirstDateOfWeekInQuarter(this DateOnly, DayOfWeek, IQuarterDefinitionProvider)
    // =========================================================================

    /// <summary>
    /// Provides <see cref="DateOnly" /> rows for the provider-based overload, mirroring the
    /// <see cref="DateTimeExtensionsTests.ValidQuarterProvider" /> quarter grid (Q1 = Dec–Feb, Q2 = Mar–May,
    /// Q3 = Jun–Aug, Q4 = Sep–Nov).
    /// </summary>
    public static IEnumerable<object[]> FirstDateOfWeekInQuarterProviderTestData()
    {
        yield return new object[] { new DateOnly(2024, 1, 1), DayOfWeek.Monday, new DateOnly(2023, 12, 4) };
        yield return new object[] { new DateOnly(2024, 5, 1), DayOfWeek.Friday, new DateOnly(2024, 3, 1) };
        yield return new object[] { new DateOnly(2024, 6, 1), DayOfWeek.Saturday, new DateOnly(2024, 6, 1) };
        yield return new object[] { new DateOnly(2024, 7, 1), DayOfWeek.Sunday, new DateOnly(2024, 6, 2) };
        yield return new object[] { new DateOnly(2024, 10, 1), DayOfWeek.Wednesday, new DateOnly(2024, 9, 4) };
    }

    /// <summary>
    /// Verifies that the provider-based overload returns the first occurrence of the requested
    /// <see cref="DayOfWeek" /> on or after the provider's quarter start.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(FirstDateOfWeekInQuarterProviderTestData))]
    public void FirstDateOfWeekInQuarter_WhenUsingValidQuarterProvider_ShouldReturnExpectedDate(DateOnly input, DayOfWeek dayOfWeek, DateOnly expected)
    {
        var provider = new DateTimeExtensionsTests.ValidQuarterProvider();
        DateOnly actual = input.FirstDateOfWeekInQuarter(dayOfWeek, provider);
        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> provider throws <see cref="ArgumentNullException" /> on the
    /// provider-based overload.
    /// </summary>
    [TestMethod]
    public void FirstDateOfWeekInQuarter_WhenProviderIsNull_ShouldThrowExactly()
    {
        var input = new DateOnly(2024, 4, 20);

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = input.FirstDateOfWeekInQuarter(DayOfWeek.Monday, (IQuarterDefinitionProvider)null!);
        });
    }

    /// <summary>
    /// Verifies that a provider whose quarter-start mapping throws causes the provider-based overload to surface
    /// <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void FirstDateOfWeekInQuarter_WhenUsingInvalidProvider_ShouldThrowExactly()
    {
        var input = new DateOnly(2024, 4, 20);
        var provider = new DateTimeExtensionsTests.InValidQuarterProvider();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = input.FirstDateOfWeekInQuarter(DayOfWeek.Monday, provider);
        });
    }

    // =========================================================================
    // GetFirstDateOfWeekInQuarter(int year, int quarter, DayOfWeek)
    // =========================================================================

    /// <summary>
    /// Verifies that the three-argument static overload with the default January-to-December definition returns the
    /// expected first-weekday occurrence.
    /// </summary>
    [TestMethod]
    [DataRow(2024, 1, DayOfWeek.Monday, 2024, 1, 1)]
    [DataRow(2024, 1, DayOfWeek.Friday, 2024, 1, 5)]
    [DataRow(2024, 4, DayOfWeek.Monday, 2024, 10, 7)]
    [DataRow(2023, 2, DayOfWeek.Sunday, 2023, 4, 2)]
    public void GetFirstDateOfWeekInQuarter_WhenUsingDefaultDefinition_ShouldReturnExpectedDate(
        int year, int quarter, DayOfWeek dayOfWeek, int expectedYear, int expectedMonth, int expectedDay)
    {
        DateOnly actual = DateOnlyExtensions.GetFirstDateOfWeekInQuarter(year, quarter, dayOfWeek);
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
    public void GetFirstDateOfWeekInQuarter_WhenYearIsInvalid_ShouldThrowExactly(int year)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = DateOnlyExtensions.GetFirstDateOfWeekInQuarter(year, 1, DayOfWeek.Monday);
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
    public void GetFirstDateOfWeekInQuarter_WhenQuarterIsOutOfRange_ForDefaultDefinition_ShouldThrowExactly(int quarter)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = DateOnlyExtensions.GetFirstDateOfWeekInQuarter(2024, quarter, DayOfWeek.Monday);
        });
    }

    /// <summary>
    /// Verifies that the three-argument static overload throws <see cref="ArgumentOutOfRangeException" /> for an
    /// undefined <see cref="DayOfWeek" /> value.
    /// </summary>
    [TestMethod]
    public void GetFirstDateOfWeekInQuarter_WhenDayOfWeekIsInvalid_ForDefaultDefinition_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = DateOnlyExtensions.GetFirstDateOfWeekInQuarter(2024, 1, (DayOfWeek)999);
        });
    }

}
