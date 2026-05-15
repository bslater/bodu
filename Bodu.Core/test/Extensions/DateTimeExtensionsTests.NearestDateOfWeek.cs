// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensionsTests.NearestDateOfWeek.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

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
    public void NearestDateOfWeek_WhenDayOfWeekIsInvalid_ShouldThrowArgumentOutOfRangeException()
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
    public void NearestDateOfWeek_WhenUsingYearMonthDayWithInvalidDayOfWeek_ShouldThrowArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = DateTimeExtensions.GetNearestDateOfWeek(2024, 4, 17, (DayOfWeek)99);
        });
    }

}
