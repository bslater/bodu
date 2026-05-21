// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensionsTests.IsWeekday.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class DateTimeExtensionsTests
{

    /// <summary>
    /// Verifies that the static <see cref="DateTimeExtensions.IsWeekday(DayOfWeek, WorkingDaysOfWeek, IWeekendDefinitionProvider?)" />
    /// overload returns the dual of <see cref="DateTimeExtensions.IsWeekend(DayOfWeek, WorkingDaysOfWeek, IWeekendDefinitionProvider?)" />
    /// without recursing infinitely, regressing the bug fixed in issue #160.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(WeekendTestData))]
    public void IsWeekday_WhenCalledOnDayOfWeek_ShouldReturnNegationOfIsWeekend(DateTime input, WorkingDaysOfWeek weekend, Type? providerType, bool expected)
    {
        IWeekendDefinitionProvider? provider = providerType is null ? null : (IWeekendDefinitionProvider)Activator.CreateInstance(providerType)!;

        var actual = DateTimeExtensions.IsWeekday(input.DayOfWeek, weekend, provider);

        Assert.AreEqual(!expected, actual, $"Failed for {input.DayOfWeek} with weekend {weekend}");
    }

    /// <summary>
    /// Verifies that the static <see cref="DateTimeExtensions.IsWeekday(DayOfWeek, WorkingDaysOfWeek, IWeekendDefinitionProvider?)" />
    /// overload throws <see cref="ArgumentOutOfRangeException" /> when invoked with <see cref="WorkingDaysOfWeek.Custom" /> and no provider.
    /// </summary>
    [TestMethod]
    public void IsWeekday_WhenCalledOnDayOfWeekWithCustomRuleMissingProvider_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = DateTimeExtensions.IsWeekday(DayOfWeek.Monday, WorkingDaysOfWeek.Custom, null);
        });
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.IsWeekday" />, when CustomRuleMissingProvider, throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void IsWeekday_WhenCustomRuleMissingProvider_ShouldThrowExactly()
    {
        var date = new DateTime(2024, 4, 19);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = date.IsWeekday(WorkingDaysOfWeek.Custom, null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.IsWeekday" />, when UsingStandardWeekend, returns the expected value.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(WeekendTestData))]
    public void IsWeekday_WhenUsingStandardWeekend_ShouldReturnExpected(DateTime input, WorkingDaysOfWeek weekend, Type? providerType, bool expected)
    {
        IWeekendDefinitionProvider? provider = providerType is null ? null : (IWeekendDefinitionProvider)Activator.CreateInstance(providerType)!;

        var actual = input.IsWeekday(weekend, provider);
        Assert.AreEqual(!expected, actual, $"Failed for {input} with weekend {weekend}");
    }

}
