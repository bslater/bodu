// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensionsTests.GetWeekStartDay.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class DateTimeExtensionsTests
{

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.GetWeekStartDay" /> returns the correct start-of-week day for each known
    /// <see cref="WorkingDaysOfWeek" /> value.
    /// </summary>
    [TestMethod]
    [DataRow(WorkingDaysOfWeek.MondayToFriday, DayOfWeek.Monday)]
    [DataRow(WorkingDaysOfWeek.SaturdayToWednesday, DayOfWeek.Saturday)]
    [DataRow(WorkingDaysOfWeek.SundayToThursday, DayOfWeek.Sunday)]
    [DataRow(WorkingDaysOfWeek.MondayToSaturday, DayOfWeek.Monday)]
    [DataRow(WorkingDaysOfWeek.SaturdayToThursday, DayOfWeek.Saturday)]
    [DataRow(WorkingDaysOfWeek.AllDays, DayOfWeek.Monday)]
    public void GetWeekStartDay_ForDefinedDefinition_ShouldReturnExpectedDay(WorkingDaysOfWeek weekend, DayOfWeek expected)
    {
        Assert.AreEqual(expected, DateTimeExtensions.GetWeekStartDay(weekend));
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.GetWeekStartDay" /> throws <see cref="ArgumentOutOfRangeException" /> when the
    /// supplied weekend definition is not a recognised enumeration value.
    /// </summary>
    [TestMethod]
    public void GetWeekStartDay_WhenDefinitionIsUndefined_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = DateTimeExtensions.GetWeekStartDay((WorkingDaysOfWeek)int.MaxValue);
        });
    }

}
