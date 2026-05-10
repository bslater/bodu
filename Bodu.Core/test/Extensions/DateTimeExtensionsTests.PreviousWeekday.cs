// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensionsTests.PreviousWeekday.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Extensions;

public partial class DateTimeExtensionsTests
{
    public static IEnumerable<object[]> PreviousWeekdaySaturdaySundayDateTimeTestData()
    {
        yield return new object[] { new DateTime(2024, 4, 22), new DateTime(2024, 4, 19) }; // Mon → Fri
        yield return new object[] { new DateTime(2024, 4, 21), new DateTime(2024, 4, 19) }; // Sun → Fri
        yield return new object[] { new DateTime(2024, 4, 20), new DateTime(2024, 4, 19) }; // Sat → Fri
        yield return new object[] { new DateTime(2024, 4, 17), new DateTime(2024, 4, 16) }; // Wed → Tue
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.PreviousWeekday(DateTime, CalendarWeekendDefinition)" /> returns the prior non-weekend date when Saturday and Sunday are defined as the weekend.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(PreviousWeekdaySaturdaySundayDateTimeTestData))]
    public void PreviousWeekday_WhenWeekendIsSaturdaySunday_ShouldReturnExpectedDate(DateTime input, DateTime expected)
    {
        DateTime actual = input.PreviousWeekday(CalendarWeekendDefinition.SaturdaySunday);
        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that with a Friday/Saturday weekend, <see cref="DateTimeExtensions.PreviousWeekday(DateTime, CalendarWeekendDefinition)" /> skips both weekend days and returns the preceding Thursday.
    /// </summary>
    [TestMethod]
    public void PreviousWeekday_WhenWeekendIsFridaySaturday_ShouldSkipSaturdayAndFriday()
    {
        DateTime input = new DateTime(2024, 4, 21); // Sunday
        DateTime actual = input.PreviousWeekday(CalendarWeekendDefinition.FridaySaturday);
        Assert.AreEqual(new DateTime(2024, 4, 18), actual); // Thursday
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.PreviousWeekday(DateTime, CalendarWeekendDefinition)" />, when called with
    /// <see cref="CalendarWeekendDefinition.None" />, retreats by exactly one day. Because <see cref="DateTimeExtensions.IsWeekend(DayOfWeek, CalendarWeekendDefinition, IWeekendDefinitionProvider?)" />
    /// classifies every day as a weekday under <c>None</c>, the reverse search loop's first iteration moves the cursor back one day and exits.
    /// </summary>
    [TestMethod]
    public void PreviousWeekday_WhenWeekendIsNone_ShouldRetreatOneDay()
    {
        DateTime input = new DateTime(2024, 4, 21);

        DateTime actual = input.PreviousWeekday(CalendarWeekendDefinition.None);

        Assert.AreEqual(input.AddDays(-1), actual);
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.PreviousWeekday(DateTime, CalendarWeekendDefinition)" /> preserves the input's <see cref="DateTime.Kind" />.
    /// </summary>
    [TestMethod]
    public void PreviousWeekday_WhenCalled_ShouldPreserveInputKind()
    {
        DateTime input = new DateTime(2024, 4, 22, 0, 0, 0, DateTimeKind.Utc);
        DateTime actual = input.PreviousWeekday(CalendarWeekendDefinition.SaturdaySunday);
        Assert.AreEqual(DateTimeKind.Utc, actual.Kind);
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.PreviousWeekday(DateTime, CalendarWeekendDefinition)" /> preserves the input's <see cref="DateTime.TimeOfDay" />.
    /// </summary>
    [TestMethod]
    public void PreviousWeekday_WhenCalled_ShouldPreserveTimeOfDay()
    {
        DateTime input = new DateTime(2024, 4, 22, 9, 15, 0);
        DateTime actual = input.PreviousWeekday(CalendarWeekendDefinition.SaturdaySunday);
        Assert.AreEqual(new TimeSpan(9, 15, 0), actual.TimeOfDay);
    }

    /// <summary>
    /// Verifies that an undefined <see cref="CalendarWeekendDefinition" /> value throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void PreviousWeekday_WhenWeekendIsInvalid_ShouldThrowArgumentOutOfRangeException()
    {
        DateTime input = new DateTime(2024, 4, 22);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = input.PreviousWeekday((CalendarWeekendDefinition)999);
        });
    }

    // =========================================================================
    // Provider overload
    // =========================================================================

    /// <summary>
    /// Verifies that the provider overload falls back to the <see cref="CalendarWeekendDefinition" /> enum value when the provider is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void PreviousWeekday_WhenProviderIsNull_ShouldUseWeekendEnum()
    {
        DateTime input = new DateTime(2024, 4, 22);
        DateTime actual = input.PreviousWeekday(CalendarWeekendDefinition.SaturdaySunday, provider: null);
        Assert.AreEqual(new DateTime(2024, 4, 19), actual);
    }

    /// <summary>
    /// Verifies that <see cref="CalendarWeekendDefinition.Custom" /> with a user-supplied <c>IWeekendDefinitionProvider</c> applies the provider's rule when determining the previous weekday.
    /// </summary>
    [TestMethod]
    public void PreviousWeekday_WhenUsingCustomProvider_ShouldApplyProviderRule()
    {
        DateTime input = new DateTime(2024, 4, 20); // Saturday
        IWeekendDefinitionProvider provider = new FridayOnlyWeekendProvider();

        // Only Friday is weekend per provider. Previous calendar day is Fri 19 → weekend, so skip to Thu 18.
        DateTime actual = input.PreviousWeekday(CalendarWeekendDefinition.Custom, provider);
        Assert.AreEqual(new DateTime(2024, 4, 18), actual);
    }

    /// <summary>
    /// Verifies that the provider overload still throws <see cref="ArgumentOutOfRangeException" /> for an undefined <see cref="CalendarWeekendDefinition" /> even when a provider is supplied.
    /// </summary>
    [TestMethod]
    public void PreviousWeekday_WhenProviderOverload_WeekendIsInvalid_ShouldThrowArgumentOutOfRangeException()
    {
        DateTime input = new DateTime(2024, 4, 22);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = input.PreviousWeekday((CalendarWeekendDefinition)999, provider: null);
        });
    }
}
