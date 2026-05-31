// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensionsTests.LastDateOfWeek.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Globalization;

namespace Bodu.Extensions;

public partial class DateOnlyExtensionsTests
{

    /// <summary>
    /// Verifies that passing a <see langword="null" /> <see cref="CultureInfo" /> falls back to <see cref="CultureInfo.CurrentCulture" />.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(DateTimeExtensionsTests.LastDateOfWeekCultureInfoTestData), typeof(DateTimeExtensionsTests))]
    public void LastDateOfWeek_WhenCultureIsNull_ShouldUseCurrentCulture(DateTime inputDateTime, CultureInfo culture, DateTime expectedDateTime)
    {
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = culture;
            var input = DateOnly.FromDateTime(inputDateTime);
            var expected = DateOnly.FromDateTime(expectedDateTime);

            DateOnly actual = input.LastDateOfWeek((CultureInfo?)null);

            Assert.AreEqual(expected, actual, $"Failed for culture: {culture.Name}");
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture; // Always restore
        }
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.LastDateOfWeek(DateOnly, CultureInfo)" /> returns the expected week end for the supplied culture's first-day-of-week.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(DateTimeExtensionsTests.LastDateOfWeekCultureInfoTestData), typeof(DateTimeExtensionsTests))]
    public void LastDateOfWeek_WhenCurrentCultureSet_ShouldReturnExpectedStart(DateTime inputDateTime, CultureInfo culture, DateTime expectedDateTime)
    {
        var input = DateOnly.FromDateTime(inputDateTime);
        var expected = DateOnly.FromDateTime(expectedDateTime);

        DateOnly actual = input.LastDateOfWeek(culture);

        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that <see cref="DateOnly.MaxValue" /> with a Sunday-start (en-US) culture throws <see cref="ArgumentOutOfRangeException" /> because the computed week end would overflow.
    /// </summary>
    [TestMethod]
    public void LastDateOfWeek_WhenMinValueAndCultureIsUS_ShouldReturnThrowArgumentOutOfRangeException()
    {
        DateOnly min = DateOnly.MaxValue;
        var culture = new CultureInfo("en-US");// Sunday is first day

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = min.LastDateOfWeek(culture); // is outside the range for a DateOnly value
        });
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.LastDateOfWeek"/> works near <see cref="DateOnly.MaxValue"/> without throwing.
    /// </summary>
    [TestMethod]
    public void LastDateOfWeek_WhenNearMaxValue_ShouldReturnValidResult()
    {
        DateOnly date = DateOnly.MaxValue.AddDays(-6); // 9999-12-25
        DateOnly actual = date.LastDateOfWeek(WorkingDaysOfWeek.MondayToFriday);

        Assert.IsTrue(actual <= DateOnly.MaxValue);
        Assert.AreEqual(DayOfWeek.Sunday, actual.DayOfWeek);
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.LastDateOfWeek"/> throws if the calculated actual exceeds <see cref="DateOnly.MaxValue"/>.
    /// </summary>
    [TestMethod]
    public void LastDateOfWeek_WhenResultExceedsMaxValue_ShouldThrowExactly()
    {
        DateOnly nearMax = DateOnly.MaxValue.AddDays(-1); // e.g., Dec 30, 9999
        WorkingDaysOfWeek weekend = WorkingDaysOfWeek.MondayToFriday; // Start of week = Monday → end = Sunday

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = nearMax.LastDateOfWeek(weekend);
        });
    }

    /// <summary>
    /// Verifies that <see cref="DateOnly.MaxValue" /> with a Friday-end culture returns <see cref="DateOnly.MaxValue" /> (which is itself the configured last day of the week).
    /// </summary>
    [TestMethod]
    public void LastDateOfWeek_WhenUsingMaxValue_ShouldSucceed()
    {
        DateOnly max = DateOnly.MaxValue;
        DateOnly actual = max.LastDateOfWeek(new CultureInfo("fa-IR")); // Friday is last day of week

        Assert.AreEqual(max, actual);
    }

    /// <summary>
    /// Verifies that <see cref="DateOnly.MinValue" /> with the invariant culture returns a date that is on or after <see cref="DateOnly.MinValue" />.
    /// </summary>
    [TestMethod]
    public void LastDateOfWeek_WhenUsingMinValue_ShouldSucceed()
    {
        DateOnly input = DateOnly.MinValue;
        DateOnly actual = input.LastDateOfWeek(CultureInfo.InvariantCulture);

        Assert.IsTrue(actual >= DateOnly.MinValue);
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.LastDateOfWeek"/> returns the expected actual based on the specified weekend definition.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(DateTimeExtensionsTests.GetLastDateOfWeekWithDefinitionTestData), typeof(DateTimeExtensionsTests))]
    public void LastDateOfWeek_WhenUsingWeekendDefinition_ShouldReturnExpectedEnd(DateTime inputDateTime, WorkingDaysOfWeek weekend, DateTime expectedDateTime)
    {
        var input = DateOnly.FromDateTime(inputDateTime);
        var expected = DateOnly.FromDateTime(expectedDateTime);
        DateOnly actual = input.LastDateOfWeek(weekend);
        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.LastDateOfWeek"/> throws when given an undefined <see cref="WorkingDaysOfWeek"/>.
    /// </summary>
    [TestMethod]
    public void LastDateOfWeek_WhenWeekendIsUndefined_ShouldThrowExactly()
    {
        var date = new DateOnly(2024, 1, 1);
        var invalidWeekend = (WorkingDaysOfWeek)(-5);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = date.LastDateOfWeek(invalidWeekend);
        });
    }

}
