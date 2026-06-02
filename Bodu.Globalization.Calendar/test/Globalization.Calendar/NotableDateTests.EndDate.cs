// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateTests.EndDate.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies that <see cref="NotableDate.EndDate" /> is overflow-safe for durations near the end of the supported date
/// range (F-016).
/// </summary>
public sealed partial class NotableDateTests
{
    /// <summary>
    /// Verifies that reading <see cref="NotableDate.EndDate" /> for a multi-day span anchored at the maximum date
    /// clamps to <see cref="DateTime.MaxValue" /> instead of throwing.
    /// </summary>
    [TestMethod]
    public void EndDate_WhenSpanWouldOverflowMaxValue_ShouldClampWithoutThrowing()
    {
        NotableDate notable = new()
        {
            Date = DateTime.MaxValue.Date,
            Name = "End Of Time",
            Category = NotableDateCategory.Observance,
            DurationDays = 10,
        };

        Assert.AreEqual(DateTime.MaxValue.Date, notable.EndDate);
    }

    /// <summary>
    /// Verifies that a normal multi-day span computes its inclusive end date correctly.
    /// </summary>
    [TestMethod]
    public void EndDate_WhenMultiDaySpan_ShouldReturnInclusiveLastDay()
    {
        NotableDate notable = new()
        {
            Date = new DateTime(2026, 6, 1),
            Name = "Festival",
            Category = NotableDateCategory.Cultural,
            DurationDays = 5,
        };

        Assert.AreEqual(new DateTime(2026, 6, 5), notable.EndDate);
    }
}
