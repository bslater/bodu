// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensionsTests.IsWeekday.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------


using System;

namespace Bodu.Extensions;

public partial class DateOnlyExtensionsTests
{

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.IsWeekday" />, when CustomRuleMissingProvider, throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void IsWeekday_WhenCustomRuleMissingProvider_ShouldThrowExactly()
    {
        var date = new DateTime(2024, 4, 19);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = date.IsWeekday(CalendarWeekendDefinition.Custom, null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.IsWeekday" />, when UsingStandardWeekend, returns the expected value.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(DateTimeExtensionsTests.WeekendTestData), typeof(DateTimeExtensionsTests))]
    public void IsWeekday_WhenUsingStandardWeekend_ShouldReturnExpected(DateTime input, CalendarWeekendDefinition weekend, Type? providerType, bool expected)
    {
        IWeekendDefinitionProvider? provider = providerType is null ? null : (IWeekendDefinitionProvider)Activator.CreateInstance(providerType)!;

        var actual = input.IsWeekday(weekend, provider);
        Assert.AreEqual(!expected, actual, $"Failed for {input} with weekend {weekend}");
    }

}
