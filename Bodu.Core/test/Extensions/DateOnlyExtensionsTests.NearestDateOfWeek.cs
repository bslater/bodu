// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensionsTests.NearestDateOfWeek.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class DateOnlyExtensionsTests
{
    // =========================================================================
    // NearestDateOfWeek(this DateOnly, DayOfWeek)
    // =========================================================================

    /// <summary>
    /// Provides (reference date, target day-of-week, expected nearest date) tuples.
    /// April 17 2024 is a Wednesday.
    /// </summary>
    public static IEnumerable<object[]> NearestDateOfWeekTestData()
    {
        // Wednesday 17 April 2024, target Wednesday → same day.
        yield return new object[] { new DateOnly(2024, 4, 17), DayOfWeek.Wednesday, new DateOnly(2024, 4, 17) };
        // Wednesday target Tuesday → 16 April (1 day back).
        yield return new object[] { new DateOnly(2024, 4, 17), DayOfWeek.Tuesday, new DateOnly(2024, 4, 16) };
        // Wednesday target Thursday → 18 April (1 day forward).
        yield return new object[] { new DateOnly(2024, 4, 17), DayOfWeek.Thursday, new DateOnly(2024, 4, 18) };
        // Wednesday target Sunday → Sunday 14 is 3 days back, Sunday 21 is 4 forward → earlier wins.
        yield return new object[] { new DateOnly(2024, 4, 17), DayOfWeek.Sunday, new DateOnly(2024, 4, 14) };
        // Wednesday target Saturday → Sat 20 is 3 forward, Sat 13 is 4 back → forward wins.
        yield return new object[] { new DateOnly(2024, 4, 17), DayOfWeek.Saturday, new DateOnly(2024, 4, 20) };
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.NearestDateOfWeek(DateOnly, DayOfWeek)" /> returns the closest occurrence of the requested <see cref="DayOfWeek" /> in either direction.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(NearestDateOfWeekTestData))]
    public void NearestDateOfWeek_WhenCalled_ShouldReturnExpectedDate(DateOnly date, DayOfWeek dayOfWeek, DateOnly expected)
    {
        DateOnly actual = date.NearestDateOfWeek(dayOfWeek);
        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that an undefined <see cref="DayOfWeek" /> value throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void NearestDateOfWeek_WhenDayOfWeekIsInvalid_ShouldThrowExactly()
    {
        var date = new DateOnly(2024, 4, 17);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = date.NearestDateOfWeek((DayOfWeek)999);
        });
    }

    /// <summary>
    /// Verifies that when the past and future occurrences are equidistant (3 days each), the earlier date is returned.
    /// </summary>
    [TestMethod]
    public void NearestDateOfWeek_WhenTiedBetweenPastAndFuture_ShouldReturnEarlierDate()
    {
        // Wednesday 17 April 2024, target Sunday: Sun 14 (3 days back) vs Sun 21 (4 days fwd) — back wins.
        var date = new DateOnly(2024, 4, 17);
        DateOnly actual = date.NearestDateOfWeek(DayOfWeek.Sunday);
        Assert.AreEqual(new DateOnly(2024, 4, 14), actual);
    }

    // =========================================================================
    // NearestDateOfWeek(int year, int month, int day, DayOfWeek)
    // =========================================================================

    /// <summary>
    /// Verifies that the static <see cref="DateOnlyExtensions.GetNearestDateOfWeek(int, int, int, DayOfWeek)" /> overload returns the expected nearest date.
    /// </summary>
    [TestMethod]
    public void NearestDateOfWeek_WhenUsingYearMonthDay_ShouldReturnExpectedDate()
    {
        // Wed 17 Apr 2024 → Monday 15 Apr (2 days back) is nearer than Mon 22 (5 fwd).
        DateOnly actual = DateOnlyExtensions.GetNearestDateOfWeek(2024, 4, 17, DayOfWeek.Monday);
        Assert.AreEqual(new DateOnly(2024, 4, 15), actual);
    }

    /// <summary>
    /// Verifies that the static <c>(year, month, day)</c> overload throws <see cref="ArgumentOutOfRangeException" /> for an undefined <see cref="DayOfWeek" />.
    /// </summary>
    [TestMethod]
    public void NearestDateOfWeek_WhenUsingYearMonthDayWithInvalidDayOfWeek_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = DateOnlyExtensions.GetNearestDateOfWeek(2024, 4, 17, (DayOfWeek)99);
        });
    }

}
