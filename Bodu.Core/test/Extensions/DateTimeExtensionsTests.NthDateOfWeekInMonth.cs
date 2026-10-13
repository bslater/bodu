// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensionsTests.NthDateOfWeekInMonth.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Globalization;

namespace Bodu.Extensions;

public partial class DateTimeExtensionsTests
{

    /// <summary>
    /// Verifies that the instance overload returns the expected nth-occurrence date for each <c>(input, dayOfWeek, ordinal)</c> tuple.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(NthDateOfWeekInMonthTestData))]
    public void NthDateOfWeekInMonth_WhenCalled_ShouldReturnExpected(DateTime input, DayOfWeek dayOfWeek, WeekOrdinal ordinal, DateTime expected)
    {
        var actual = input.NthDateOfWeekInMonth(dayOfWeek, ordinal);

        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that an undefined <see cref="DayOfWeek" /> value throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void NthDateOfWeekInMonth_WhenDayOfWeekIsInvalidEnum_ShouldThrowExactly()
    {
        var input = new DateTime(2024, 1, 1);
        DayOfWeek invalidDay = (DayOfWeek)999;

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            input.NthDateOfWeekInMonth(invalidDay, WeekOrdinal.First);
        });
    }

    /// <summary>
    /// Verifies that requesting <see cref="WeekOrdinal.Fifth" /> in a month that has only four occurrences throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void NthDateOfWeekInMonth_WhenFifthDoesNotExist_ShouldThrowExactly()
    {
        DateTime input = new DateTime(2023, 2, 1); // February 2023 has only 4 Wednesdays

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            input.NthDateOfWeekInMonth(DayOfWeek.Wednesday, WeekOrdinal.Fifth);
        });
    }

    /// <summary>
    /// Verifies that requesting a fifth occurrence December 9999 lacks throws <see cref="ArgumentOutOfRangeException" />
    /// naming <c>ordinal</c>, with the resource message, as in any other month.
    /// </summary>
    /// <param name="dayOfWeek">A weekday that occurs only four times in December 9999.</param>
    [TestMethod]
    [DataRow(DayOfWeek.Saturday)]
    [DataRow(DayOfWeek.Sunday)]
    [DataRow(DayOfWeek.Monday)]
    [DataRow(DayOfWeek.Tuesday)]
    public void NthDateOfWeekInMonth_WhenFifthDoesNotExistInDecember9999_ShouldThrowForOrdinal(DayOfWeek dayOfWeek)
    {
        var input = new DateTime(9999, 12, 31, 23, 59, 59, DateTimeKind.Utc);

        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = input.NthDateOfWeekInMonth(dayOfWeek, WeekOrdinal.Fifth);
        });

        string expected = string.Format(CultureInfo.CurrentCulture, ResourceStrings.Arg_Invalid_OrdinalDoesNotExistForMonth, WeekOrdinal.Fifth, dayOfWeek, input.ToString("MMMM yyyy", CultureInfo.CurrentCulture));
        Assert.AreEqual("ordinal", ex.ParamName);
        Assert.StartsWith(expected, ex.Message);
    }

    /// <summary>
    /// Verifies that the result is computed within the input's month, even when the resulting date is earlier than the input.
    /// </summary>
    [TestMethod]
    public void NthDateOfWeekInMonth_WhenGivenDateIsInFuture_ShouldReturnExpectedDateInGivenMonth()
    {
        var input = new DateTime(2024, 1, 30);

        var actual = input.NthDateOfWeekInMonth(DayOfWeek.Sunday, WeekOrdinal.First);

        Assert.IsTrue(actual < input);
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.NthDateOfWeekInMonth(DateTime, DayOfWeek, WeekOrdinal)" /> preserves the input's <see cref="DateTime.Kind" /> across all <see cref="DateTimeKind" /> values.
    /// </summary>
    [TestMethod]
    [DataRow(DateTimeKind.Unspecified)]
    [DataRow(DateTimeKind.Utc)]
    [DataRow(DateTimeKind.Local)]
    public void NthDateOfWeekInMonth_WhenKindIsSet_ShouldPreserveKind(DateTimeKind kind)
    {
        var input = new DateTime(2024, 4, 18, 10, 0, 0, kind);
        var actual = input.NthDateOfWeekInMonth(DayOfWeek.Wednesday, WeekOrdinal.Second);

        Assert.AreEqual(kind, actual.Kind);
    }

    /// <summary>
    /// Verifies that an undefined <see cref="WeekOrdinal" /> value throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void NthDateOfWeekInMonth_WhenOrdinalIsInvalidEnum_ShouldThrowExactly()
    {
        var input = new DateTime(2024, 1, 1);
        WeekOrdinal invalidOrdinal = (WeekOrdinal)999;

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            input.NthDateOfWeekInMonth(DayOfWeek.Monday, invalidOrdinal);
        });
    }

    /// <summary>
    /// Verifies that the static <see cref="DateTimeExtensions.GetNthDateOfWeekInMonth(int, int, DayOfWeek, WeekOrdinal)" /> overload returns the same nth-occurrence date as the instance overload.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(NthDateOfWeekInMonthTestData))]
    public void NthDateOfWeekInMonth_WhenUsingYearMonth_ShouldReturnExpected(DateTime input, DayOfWeek dayOfWeek, WeekOrdinal ordinal, DateTime expected)
    {
        int year = input.Year, month = input.Month;

        var actual = DateTimeExtensions.GetNthDateOfWeekInMonth(year, month, dayOfWeek, ordinal);

        Assert.AreEqual(expected, actual);
    }

}
