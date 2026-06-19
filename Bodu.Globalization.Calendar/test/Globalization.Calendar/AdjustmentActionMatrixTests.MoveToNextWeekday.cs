// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdjustmentActionMatrixTests.MoveToNextWeekday.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class AdjustmentActionMatrixTests
{
    /// <summary>
    /// Verifies that <see cref="AdjustmentAction.MoveToNextWeekday" /> targeting Monday returns the anchor unchanged when
    /// it is already a Monday and otherwise rolls forward to the first following Monday, crossing the year boundary for
    /// anchors after the last Monday of the year.
    /// </summary>
    /// <param name="strategyDay">The day of December 2026 the holiday resolves to.</param>
    /// <param name="expectedYear">The expected emitted year.</param>
    /// <param name="expectedMonth">The expected emitted month.</param>
    /// <param name="expectedDay">The expected emitted day.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(25, 2026, 12, 28)] // Fri → Mon 28 Dec
    [DataRow(26, 2026, 12, 28)] // Sat → Mon 28 Dec
    [DataRow(27, 2026, 12, 28)] // Sun → Mon 28 Dec
    [DataRow(28, 2026, 12, 28)] // Mon → unchanged (inclusive)
    [DataRow(29, 2027, 1, 4)]   // Tue → next Mon 4 Jan 2027
    [DataRow(31, 2027, 1, 4)]   // Thu → next Mon 4 Jan 2027
    public void MoveToNextWeekday_WhenTargetingMonday_ShouldRollForwardInclusively(int strategyDay, int expectedYear, int expectedMonth, int expectedDay)
    {
        INotableDateService service = WeekdayMoveService("MoveToNextWeekday", "Monday", strategyDay);
        DateOnly expected = new(expectedYear, expectedMonth, expectedDay);

        NotableDate match = Single(service, "probe", new DateOnly(2026, 12, 1), expected);

        Assert.AreEqual(expected, match.Date);
        Assert.AreEqual(new DateOnly(2026, 12, strategyDay), match.ActualDate);
    }
}
