// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensionsTests.IsWeekend.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------


using System;
using System.Data;

namespace Bodu.Extensions;

public partial class DateTimeExtensionsTests
{

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.IsWeekend" />, when CustomRuleMissingProvider, throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void IsWeekend_WhenCustomRuleMissingProvider_ShouldThrowExactly()
    {
        var date = new DateTime(2024, 4, 19);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = date.IsWeekend(CalendarWeekendDefinition.Custom, null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.IsWeekend" />, when InvalidEnum, throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void IsWeekend_WhenInvalidEnum_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = DateTimeExtensions.IsWeekend((DayOfWeek)99, CalendarWeekendDefinition.SaturdaySunday);
        });
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.IsWeekend(DayOfWeek, CalendarWeekendDefinition, IWeekendDefinitionProvider?)" />
    /// returns <see langword="false" /> for every day of the week when called with
    /// <see cref="CalendarWeekendDefinition.None" />, matching the enum's documented contract that no standard weekend is defined.
    /// </summary>
    [TestMethod]
    [DataRow(DayOfWeek.Sunday)]
    [DataRow(DayOfWeek.Monday)]
    [DataRow(DayOfWeek.Tuesday)]
    [DataRow(DayOfWeek.Wednesday)]
    [DataRow(DayOfWeek.Thursday)]
    [DataRow(DayOfWeek.Friday)]
    [DataRow(DayOfWeek.Saturday)]
    public void IsWeekend_WhenNoneAndAnyDay_ShouldReturnFalse(DayOfWeek day)
    {
        var actual = DateTimeExtensions.IsWeekend(day, CalendarWeekendDefinition.None);

        Assert.IsFalse(actual, $"CalendarWeekendDefinition.None must classify every day as a weekday, but {day} was classified as weekend.");
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.IsWeekend" />, when UsingStandardWeekend, returns the expected value.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(WeekendTestData))]
    public void IsWeekend_WhenUsingStandardWeekend_ShouldReturnExpected(DateTime input, CalendarWeekendDefinition weekend, Type? providerType, bool expected)
    {
        IWeekendDefinitionProvider? provider = providerType is null ? null : (IWeekendDefinitionProvider)Activator.CreateInstance(providerType)!;

        var actual = input.IsWeekend(weekend, provider);
        Assert.AreEqual(expected, actual, $"Failed for {input} with weekend {weekend}");
    }

}
