// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensionsTests.IsWeekend.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class DateOnlyExtensionsTests
{

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.IsWeekend" />, when CustomRuleMissingProvider, throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void IsWeekend_WhenCustomRuleMissingProvider_ShouldThrowExactly()
    {
        var date = new DateTime(2024, 4, 19);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = date.IsWeekend(WorkingDaysOfWeek.Custom, null!);
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
            _ = DateTimeExtensions.IsWeekend((DayOfWeek)99, WorkingDaysOfWeek.MondayToFriday);
        });
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.IsWeekend" />, when UsingStandardWeekend, returns the expected value.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(DateTimeExtensionsTests.WeekendTestData), typeof(DateTimeExtensionsTests))]
    public void IsWeekend_WhenUsingStandardWeekend_ShouldReturnExpected(DateTime input, WorkingDaysOfWeek weekend, Type? providerType, bool expected)
    {
        IWeekendDefinitionProvider? provider = providerType is null ? null : (IWeekendDefinitionProvider)Activator.CreateInstance(providerType)!;

        var actual = input.IsWeekend(weekend, provider);
        Assert.AreEqual(expected, actual, $"Failed for {input} with weekend {weekend}");
    }

}
