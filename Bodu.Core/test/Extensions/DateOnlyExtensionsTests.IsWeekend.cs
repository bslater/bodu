// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensionsTests.IsWeekend.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------


using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bodu.Extensions;
using System.Globalization;
using System.Data;

namespace Bodu.Extensions;

public partial class DateOnlyExtensionsTests
{

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.IsWeekend" />, when UsingStandardWeekend, returns the expected value.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(DateTimeExtensionsTests.WeekendTestData), typeof(DateTimeExtensionsTests))]
    public void IsWeekend_WhenUsingStandardWeekend_ShouldReturnExpected(DateTime input, CalendarWeekendDefinition weekend, Type? providerType, bool expected)
    {
        IWeekendDefinitionProvider? provider = providerType is null ? null : (IWeekendDefinitionProvider)Activator.CreateInstance(providerType)!;

        var actual = input.IsWeekend(weekend, provider);
        Assert.AreEqual(expected, actual, $"Failed for {input} with weekend {weekend}");
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.IsWeekend" />, when CustomRuleMissingProvider, throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void IsWeekend_WhenCustomRuleMissingProvider_ShouldThrowExactly()
    {
        DateTime date = new DateTime(2024, 4, 19);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = date.IsWeekend(CalendarWeekendDefinition.Custom, null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.IsWeekend" />, when InvalidEnum, throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void IsWeekend_WhenInvalidEnum_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = DateTimeExtensions.IsWeekend((DayOfWeek)99, CalendarWeekendDefinition.SaturdaySunday);
        });
    }

}
