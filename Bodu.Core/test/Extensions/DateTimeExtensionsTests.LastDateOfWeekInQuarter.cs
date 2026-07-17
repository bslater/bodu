// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensionsTests.LastDateOfWeekInQuarter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class DateTimeExtensionsTests
{
    // The definition-based overloads search backward from the quarter end, matching the documented
    // "last occurrence within the quarter" contract. (An earlier implementation stepped forward from
    // the quarter start, returning the FIRST occurrence; these tests pin the corrected behaviour.)

    // =========================================================================
    // Instance: LastDateOfWeekInQuarter(this DateTime, DayOfWeek) — calendar default
    // =========================================================================

    public static IEnumerable<object[]> LastDateOfWeekInQuarterCalendarTestData()
    {
        // Q1 2024 ends Sun 31 Mar. Target Sun → 31 Mar (quarter end matches target).
        yield return new object[] { new DateTime(2024, 2, 15), DayOfWeek.Sunday, new DateTime(2024, 3, 31) };
        // Target Friday → last Friday before 31 Mar = 29 Mar.
        yield return new object[] { new DateTime(2024, 2, 15), DayOfWeek.Friday, new DateTime(2024, 3, 29) };
        // Q2 2024 ends Sun 30 Jun. Target Mon → last Monday = 24 Jun.
        yield return new object[] { new DateTime(2024, 5, 10), DayOfWeek.Monday, new DateTime(2024, 6, 24) };
        // Q4 2024 ends Tue 31 Dec. Target Tue → 31 Dec.
        yield return new object[] { new DateTime(2024, 11, 5), DayOfWeek.Tuesday, new DateTime(2024, 12, 31) };
    }

    /// <summary>
    /// Verifies that the static year/quarter overload throws <see cref="ArgumentOutOfRangeException" /> for an undefined <see cref="DayOfWeek" />.
    /// </summary>
    [TestMethod]
    public void GetLastDateOfWeekInQuarter_WhenDayOfWeekIsInvalid_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = DateTimeExtensions.GetLastDateOfWeekInQuarter(2024, 1, (DayOfWeek)999);
        });
    }

    /// <summary>
    /// Verifies that the static definition overload throws <see cref="InvalidOperationException" /> when given <see cref="CalendarQuarterDefinition.Custom" />.
    /// </summary>
    [TestMethod]
    public void GetLastDateOfWeekInQuarter_WhenDefinitionIsCustom_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = DateTimeExtensions.GetLastDateOfWeekInQuarter(2024, 1, DayOfWeek.Monday, CalendarQuarterDefinition.Custom);
        });
    }

    /// <summary>
    /// Verifies that the static definition overload throws <see cref="ArgumentOutOfRangeException" /> for an undefined <see cref="CalendarQuarterDefinition" /> value.
    /// </summary>
    [TestMethod]
    public void GetLastDateOfWeekInQuarter_WhenDefinitionIsInvalid_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = DateTimeExtensions.GetLastDateOfWeekInQuarter(2024, 1, DayOfWeek.Monday, (CalendarQuarterDefinition)999);
        });
    }

    /// <summary>
    /// Verifies that the static year/quarter overload throws <see cref="ArgumentOutOfRangeException" /> for quarter values outside <c>1..4</c>.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(5)]
    [DataRow(-1)]
    public void GetLastDateOfWeekInQuarter_WhenQuarterIsOutOfRange_ShouldThrowExactly(int quarter)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = DateTimeExtensions.GetLastDateOfWeekInQuarter(2024, quarter, DayOfWeek.Monday);
        });
    }

    // =========================================================================
    // Static: GetLastDateOfWeekInQuarter(int, int, DayOfWeek, CalendarQuarterDefinition)
    // =========================================================================

    /// <summary>
    /// Verifies that the static definition overload returns the last occurrence of the target day within the quarter.
    /// </summary>
    [TestMethod]
    public void GetLastDateOfWeekInQuarter_WhenUsingDefinitionOverload_ShouldReturnExpectedDate()
    {
        DateTime actual = DateTimeExtensions.GetLastDateOfWeekInQuarter(2024, 1, DayOfWeek.Sunday, CalendarQuarterDefinition.JanuaryToDecember);
        Assert.AreEqual(new DateTime(2024, 3, 31), actual);
    }

    // =========================================================================
    // Static: GetLastDateOfWeekInQuarter(int, int, DayOfWeek)
    // =========================================================================

    /// <summary>
    /// Verifies that the static year/quarter overload returns the last occurrence of the target day within the quarter.
    /// </summary>
    [TestMethod]
    public void GetLastDateOfWeekInQuarter_WhenUsingYearAndQuarter_ShouldReturnExpectedDate()
    {
        // Q1 2024 ends Sun 31 Mar. Target Sunday → 31 Mar.
        DateTime actual = DateTimeExtensions.GetLastDateOfWeekInQuarter(2024, 1, DayOfWeek.Sunday);
        Assert.AreEqual(new DateTime(2024, 3, 31), actual);
    }

    /// <summary>
    /// Verifies that the static year/quarter overload throws <see cref="ArgumentOutOfRangeException" /> for year values outside <c>1..9999</c>.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(10000)]
    public void GetLastDateOfWeekInQuarter_WhenYearIsOutOfRange_ShouldThrowExactly(int year)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = DateTimeExtensions.GetLastDateOfWeekInQuarter(year, 1, DayOfWeek.Monday);
        });
    }

    /// <summary>
    /// Verifies that passing <see cref="CalendarQuarterDefinition.Custom" /> without supplying a provider throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void LastDateOfWeekInQuarter_WhenCustomDefinitionWithoutProvider_ShouldThrowExactly()
    {
        var input = new DateTime(2024, 5, 10);
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = input.LastDateOfWeekInQuarter(DayOfWeek.Monday, CalendarQuarterDefinition.Custom);
        });
    }

    /// <summary>
    /// Verifies that the instance calendar-default overload throws <see cref="ArgumentOutOfRangeException" /> for an undefined <see cref="DayOfWeek" />.
    /// </summary>
    [TestMethod]
    public void LastDateOfWeekInQuarter_WhenInstanceCalendarOverload_DayOfWeekInvalid_ShouldThrowExactly()
    {
        var input = new DateTime(2024, 2, 15);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = input.LastDateOfWeekInQuarter((DayOfWeek)999);
        });
    }

    /// <summary>
    /// Verifies that the instance calendar-default overload preserves the input's <see cref="DateTime.Kind" />.
    /// </summary>
    [TestMethod]
    public void LastDateOfWeekInQuarter_WhenInstanceCalendarOverload_PreservesInputKind()
    {
        var input = new DateTime(2024, 2, 15, 0, 0, 0, DateTimeKind.Utc);
        DateTime actual = input.LastDateOfWeekInQuarter(DayOfWeek.Monday);
        Assert.AreEqual(DateTimeKind.Utc, actual.Kind);
    }

    /// <summary>
    /// Verifies that the instance definition overload throws <see cref="ArgumentOutOfRangeException" /> for an undefined <see cref="CalendarQuarterDefinition" /> value.
    /// </summary>
    [TestMethod]
    public void LastDateOfWeekInQuarter_WhenInstanceDefinitionOverload_DefinitionInvalid_ShouldThrowExactly()
    {
        var input = new DateTime(2024, 5, 10);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = input.LastDateOfWeekInQuarter(DayOfWeek.Monday, (CalendarQuarterDefinition)999);
        });
    }

    // =========================================================================
    // Instance: LastDateOfWeekInQuarter(this DateTime, DayOfWeek, IQuarterDefinitionProvider)
    // This overload uses GetTicksToPreviousDayOfWeek on the provider's quarter end and DOES
    // return the most recent occurrence of the target day on or before that end.
    // =========================================================================

    /// <summary>
    /// Verifies that the instance provider overload throws <see cref="ArgumentNullException" /> when the provider is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void LastDateOfWeekInQuarter_WhenProviderIsNull_ShouldThrowExactly()
    {
        var input = new DateTime(2024, 5, 10);
        IQuarterDefinitionProvider? provider = null;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = input.LastDateOfWeekInQuarter(DayOfWeek.Monday, provider!);
        });
    }

    /// <summary>
    /// Verifies that the instance provider overload throws <see cref="ArgumentOutOfRangeException" /> for an undefined <see cref="DayOfWeek" />.
    /// </summary>
    [TestMethod]
    public void LastDateOfWeekInQuarter_WhenProviderOverload_DayOfWeekInvalid_ShouldThrowExactly()
    {
        var input = new DateTime(2024, 5, 10);
        IQuarterDefinitionProvider provider = new ValidQuarterProvider();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = input.LastDateOfWeekInQuarter((DayOfWeek)999, provider);
        });
    }

    /// <summary>
    /// Verifies that the instance calendar-default overload returns the last occurrence of the target day within the quarter.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(LastDateOfWeekInQuarterCalendarTestData))]
    public void LastDateOfWeekInQuarter_WhenUsingCalendarDefault_ShouldReturnExpectedDate(DateTime input, DayOfWeek dayOfWeek, DateTime expected)
    {
        DateTime actual = input.LastDateOfWeekInQuarter(dayOfWeek);
        Assert.AreEqual(expected, actual);
    }

    // =========================================================================
    // Instance: LastDateOfWeekInQuarter(this DateTime, DayOfWeek, CalendarQuarterDefinition)
    // =========================================================================

    /// <summary>
    /// Verifies that passing <see cref="CalendarQuarterDefinition.JanuaryToDecember" /> produces the same result as the default calendar overload.
    /// </summary>
    [TestMethod]
    public void LastDateOfWeekInQuarter_WhenUsingJanuaryToDecemberDefinition_ShouldReturnSameResultAsCalendarOverload()
    {
        var input = new DateTime(2024, 2, 15);
        DateTime fromDefaultOverload = input.LastDateOfWeekInQuarter(DayOfWeek.Monday);
        DateTime fromExplicitDefinition = input.LastDateOfWeekInQuarter(DayOfWeek.Monday, CalendarQuarterDefinition.JanuaryToDecember);
        Assert.AreEqual(fromDefaultOverload, fromExplicitDefinition);
    }

    /// <summary>
    /// Verifies that the provider overload returns a date whose <see cref="DateTime.DayOfWeek" /> matches the requested day.
    /// </summary>
    [TestMethod]
    public void LastDateOfWeekInQuarter_WhenUsingValidQuarterProvider_ShouldReturnTargetDayOfWeek()
    {
        var input = new DateTime(2024, 5, 10);
        IQuarterDefinitionProvider provider = new ValidQuarterProvider();
        DateTime actual = input.LastDateOfWeekInQuarter(DayOfWeek.Monday, provider);
        Assert.AreEqual(DayOfWeek.Monday, actual.DayOfWeek);
    }

}
