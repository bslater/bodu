// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensionsTests.IsWeekday.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------


using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bodu.Extensions;
using System.Globalization;

namespace Bodu.Extensions;

public partial class DateTimeExtensionsTests
{

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.IsWeekday" />, when UsingStandardWeekend, returns the expected value.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(WeekendTestData), DynamicDataSourceType.Method)]
    public void IsWeekday_WhenUsingStandardWeekend_ShouldReturnExpected(DateTime input, CalendarWeekendDefinition weekend, Type? providerType, bool expected)
    {
        IWeekendDefinitionProvider? provider = providerType is null ? null : (IWeekendDefinitionProvider)Activator.CreateInstance(providerType)!;

        bool actual = input.IsWeekday(weekend, provider);
        Assert.AreEqual(!expected, actual, $"Failed for {input} with weekend {weekend}");
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.IsWeekday" />, when CustomRuleMissingProvider, throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void IsWeekday_WhenCustomRuleMissingProvider_ShouldThrowExactly()
    {
        DateTime date = new DateTime(2024, 4, 19);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = date.IsWeekday(CalendarWeekendDefinition.Custom, null!);
        });
    }

    /// <summary>
    /// Verifies that the static <see cref="DateTimeExtensions.IsWeekday(DayOfWeek, CalendarWeekendDefinition, IWeekendDefinitionProvider?)" />
    /// overload returns the dual of <see cref="DateTimeExtensions.IsWeekend(DayOfWeek, CalendarWeekendDefinition, IWeekendDefinitionProvider?)" />
    /// without recursing infinitely, regressing the bug fixed in issue #160.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(WeekendTestData), DynamicDataSourceType.Method)]
    public void IsWeekday_WhenCalledOnDayOfWeek_ShouldReturnNegationOfIsWeekend(DateTime input, CalendarWeekendDefinition weekend, Type? providerType, bool expected)
    {
        IWeekendDefinitionProvider? provider = providerType is null ? null : (IWeekendDefinitionProvider)Activator.CreateInstance(providerType)!;

        bool actual = DateTimeExtensions.IsWeekday(input.DayOfWeek, weekend, provider);

        Assert.AreEqual(!expected, actual, $"Failed for {input.DayOfWeek} with weekend {weekend}");
    }

    /// <summary>
    /// Verifies that the static <see cref="DateTimeExtensions.IsWeekday(DayOfWeek, CalendarWeekendDefinition, IWeekendDefinitionProvider?)" />
    /// overload throws <see cref="ArgumentOutOfRangeException" /> when invoked with <see cref="CalendarWeekendDefinition.Custom" /> and no provider.
    /// </summary>
    [TestMethod]
    public void IsWeekday_WhenCalledOnDayOfWeekWithCustomRuleMissingProvider_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = DateTimeExtensions.IsWeekday(DayOfWeek.Monday, CalendarWeekendDefinition.Custom, null);
        });
    }
}
