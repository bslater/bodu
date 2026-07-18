// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensionsTests.QuarterDefinition.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class DateOnlyExtensionsTests
{

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.GetFirstDateOfQuarter(int, int, CalendarQuarterDefinition)" /> throws
    /// <see cref="InvalidOperationException" /> when the supplied <see cref="CalendarQuarterDefinition.Custom" /> definition requires a
    /// provider-based overload.
    /// </summary>
    [TestMethod]
    public void GetFirstDateOfQuarter_DateOnly_WhenDefinitionIsCustom_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = DateOnlyExtensions.GetFirstDateOfQuarter(2024, 1, CalendarQuarterDefinition.Custom);
        });
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.GetLastDateOfQuarter(int, int, CalendarQuarterDefinition)" /> throws
    /// <see cref="InvalidOperationException" /> for the Custom definition.
    /// </summary>
    [TestMethod]
    public void GetLastDateOfQuarter_DateOnly_WhenDefinitionIsCustom_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = DateOnlyExtensions.GetLastDateOfQuarter(2024, 1, CalendarQuarterDefinition.Custom);
        });
    }

    /// <summary>
    /// Provides every non-Custom <see cref="CalendarQuarterDefinition" /> paired with reference dates that straddle
    /// each definition's anchor boundary, for the twin-parity sweep below.
    /// </summary>
    public static IEnumerable<object[]> QuarterEngineParityTestData()
    {
        CalendarQuarterDefinition[] definitions =
        [
            CalendarQuarterDefinition.JanuaryToDecember,
            CalendarQuarterDefinition.JulyToJune,
            CalendarQuarterDefinition.AprilToMarch,
            CalendarQuarterDefinition.April6ToApril5,
            CalendarQuarterDefinition.March25ToMarch24,
            CalendarQuarterDefinition.OctoberToSeptember,
            CalendarQuarterDefinition.FebruaryToJanuary,
        ];

        DateOnly[] dates =
        [
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 4, 5),
            new DateOnly(2024, 4, 6),
            new DateOnly(2024, 3, 24),
            new DateOnly(2024, 3, 25),
            new DateOnly(2024, 6, 30),
            new DateOnly(2024, 10, 1),
            new DateOnly(2024, 12, 31),
            new DateOnly(2025, 2, 28),
        ];

        foreach (CalendarQuarterDefinition definition in definitions)
        {
            foreach (DateOnly date in dates)
            {
                yield return new object[] { date, definition };
            }
        }
    }

    /// <summary>
    /// Verifies that the <see cref="DateOnly" /> quarter members produce exactly the results of the
    /// <see cref="DateTime" /> twin for every definition and boundary date, pinning the shared quarter engine against
    /// future drift between the twins.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(QuarterEngineParityTestData))]
    public void QuarterMembers_WhenComparedWithDateTimeTwin_ShouldReturnIdenticalResults(DateOnly date, CalendarQuarterDefinition definition)
    {
        DateTime dateTime = date.ToDateTime(TimeOnly.MinValue);

        Assert.AreEqual(dateTime.Quarter(definition), date.Quarter(definition));
        Assert.AreEqual(dateTime.FirstDateOfQuarter(definition).ToDateOnly(), date.FirstDateOfQuarter(definition));
        Assert.AreEqual(dateTime.LastDateOfQuarter(definition).ToDateOnly(), date.LastDateOfQuarter(definition));
        Assert.AreEqual(dateTime.FirstDateOfWeekInQuarter(DayOfWeek.Wednesday, definition).ToDateOnly(), date.FirstDateOfWeekInQuarter(DayOfWeek.Wednesday, definition));
        Assert.AreEqual(dateTime.LastDateOfWeekInQuarter(DayOfWeek.Wednesday, definition).ToDateOnly(), date.LastDateOfWeekInQuarter(DayOfWeek.Wednesday, definition));
    }

}
