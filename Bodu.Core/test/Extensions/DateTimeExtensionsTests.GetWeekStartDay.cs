// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensionsTests.GetWeekStartDay.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class DateTimeExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.GetWeekStartDay" /> returns the correct start-of-week day for each known
    /// <see cref="CalendarWeekendDefinition" /> value.
    /// </summary>
    [TestMethod]
    [DataRow(CalendarWeekendDefinition.SaturdaySunday, DayOfWeek.Monday)]
    [DataRow(CalendarWeekendDefinition.ThursdayFriday, DayOfWeek.Saturday)]
    [DataRow(CalendarWeekendDefinition.FridaySaturday, DayOfWeek.Sunday)]
    [DataRow(CalendarWeekendDefinition.SundayOnly, DayOfWeek.Monday)]
    [DataRow(CalendarWeekendDefinition.FridayOnly, DayOfWeek.Saturday)]
    [DataRow(CalendarWeekendDefinition.None, DayOfWeek.Monday)]
    public void GetWeekStartDay_ForDefinedDefinition_ShouldReturnExpectedDay(CalendarWeekendDefinition weekend, DayOfWeek expected)
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
            _ = DateTimeExtensions.GetWeekStartDay((CalendarWeekendDefinition)int.MaxValue);
        });
    }
}
