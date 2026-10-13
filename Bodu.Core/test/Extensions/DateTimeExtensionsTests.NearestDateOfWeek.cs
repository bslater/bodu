// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensionsTests.NearestDateOfWeek.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class DateTimeExtensionsTests
{
    // =========================================================================
    // NearestDateOfWeek(this DateTime, DayOfWeek)
    // =========================================================================

    /// <summary>
    /// Provides (reference DateTime, target day-of-week, expected nearest DateTime).
    /// Wednesday 17 April 2024 is the reference.
    /// </summary>
    public static IEnumerable<object[]> NearestDateOfWeekDateTimeTestData()
    {
        yield return new object[] { new DateTime(2024, 4, 17), DayOfWeek.Wednesday, new DateTime(2024, 4, 17) };
        yield return new object[] { new DateTime(2024, 4, 17), DayOfWeek.Tuesday, new DateTime(2024, 4, 16) };
        yield return new object[] { new DateTime(2024, 4, 17), DayOfWeek.Thursday, new DateTime(2024, 4, 18) };
        yield return new object[] { new DateTime(2024, 4, 17), DayOfWeek.Sunday, new DateTime(2024, 4, 14) };
        yield return new object[] { new DateTime(2024, 4, 17), DayOfWeek.Saturday, new DateTime(2024, 4, 20) };
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.NearestDateOfWeek(DateTime, DayOfWeek)" /> preserves the input's <see cref="DateTime.Kind" />.
    /// </summary>
    [TestMethod]
    public void NearestDateOfWeek_WhenCalled_ShouldPreserveInputKind()
    {
        var dateTime = new DateTime(2024, 4, 17, 0, 0, 0, DateTimeKind.Local);
        DateTime actual = dateTime.NearestDateOfWeek(DayOfWeek.Monday);
        Assert.AreEqual(DateTimeKind.Local, actual.Kind);
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.NearestDateOfWeek(DateTime, DayOfWeek)" /> returns the closest occurrence of the requested <see cref="DayOfWeek" /> in either direction.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(NearestDateOfWeekDateTimeTestData))]
    public void NearestDateOfWeek_WhenCalled_ShouldReturnExpectedDate(DateTime dateTime, DayOfWeek dayOfWeek, DateTime expected)
    {
        DateTime actual = dateTime.NearestDateOfWeek(dayOfWeek);
        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that an undefined <see cref="DayOfWeek" /> value throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void NearestDateOfWeek_WhenDayOfWeekIsInvalid_ShouldThrowExactly()
    {
        var dateTime = new DateTime(2024, 4, 17);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = dateTime.NearestDateOfWeek((DayOfWeek)999);
        });
    }

    /// <summary>
    /// Verifies that when the past and future occurrences are equidistant (3 days each), the earlier date is returned.
    /// </summary>
    [TestMethod]
    public void NearestDateOfWeek_WhenTiedBetweenPastAndFuture_ShouldReturnEarlierDate()
    {
        var dateTime = new DateTime(2024, 4, 17);
        DateTime actual = dateTime.NearestDateOfWeek(DayOfWeek.Sunday);
        Assert.AreEqual(new DateTime(2024, 4, 14), actual);
    }

    // =========================================================================
    // NearestDateOfWeek(int year, int month, int day, DayOfWeek)
    // =========================================================================

    /// <summary>
    /// Verifies that the static <see cref="DateTimeExtensions.GetNearestDateOfWeek(int, int, int, DayOfWeek)" /> overload returns the expected nearest date.
    /// </summary>
    [TestMethod]
    public void NearestDateOfWeek_WhenUsingYearMonthDay_ShouldReturnExpectedDate()
    {
        DateTime actual = DateTimeExtensions.GetNearestDateOfWeek(2024, 4, 17, DayOfWeek.Monday);
        Assert.AreEqual(new DateTime(2024, 4, 15), actual);
    }

    /// <summary>
    /// Verifies that the static <c>(year, month, day)</c> overload always returns a result with <see cref="DateTimeKind.Unspecified" />.
    /// </summary>
    [TestMethod]
    public void NearestDateOfWeek_WhenUsingYearMonthDay_ShouldReturnResultWithUnspecifiedKind()
    {
        DateTime actual = DateTimeExtensions.GetNearestDateOfWeek(2024, 4, 17, DayOfWeek.Monday);
        Assert.AreEqual(DateTimeKind.Unspecified, actual.Kind);
    }

    /// <summary>
    /// Verifies that the static <c>(year, month, day)</c> overload throws <see cref="ArgumentOutOfRangeException" /> for an undefined <see cref="DayOfWeek" />.
    /// </summary>
    [TestMethod]
    public void NearestDateOfWeek_WhenUsingYearMonthDayWithInvalidDayOfWeek_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = DateTimeExtensions.GetNearestDateOfWeek(2024, 4, 17, (DayOfWeek)99);
        });
    }

    /// <summary>
    /// Verifies that the static <see cref="DateTimeExtensions.GetNearestDateOfWeek(int, int, int, DayOfWeek)" /> overload throws
    /// <see cref="ArgumentOutOfRangeException" /> naming the offending component when the year, month, and day do not
    /// form a valid date.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="year">The year component.</param>
    /// <param name="month">The month component.</param>
    /// <param name="day">The day component.</param>
    /// <param name="expectedParamName">The parameter name the exception is expected to report.</param>
    [TestMethod]
    [DataRow("year 0", 0, 1, 1, "year")]
    [DataRow("year 10000", 10_000, 1, 1, "year")]
    [DataRow("month 0", 2024, 0, 1, "month")]
    [DataRow("month 13", 2024, 13, 1, "month")]
    [DataRow("month 14", 2024, 14, 1, "month")]
    [DataRow("day 0", 2024, 1, 0, "day")]
    [DataRow("day 32", 2024, 1, 32, "day")]
    [DataRow("30 February", 2024, 2, 30, "day")]
    [DataRow("29 February in a common year", 2023, 2, 29, "day")]
    public void GetNearestDateOfWeek_WhenDateIsInvalid_ShouldThrowArgumentOutOfRangeException(
        string testName, int year, int month, int day, string expectedParamName)
    {
        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () =>
            {
                _ = DateTimeExtensions.GetNearestDateOfWeek(year, month, day, DayOfWeek.Monday);
            },
            testName);
        Assert.AreEqual(expectedParamName, ex.ParamName, testName);
    }

}
