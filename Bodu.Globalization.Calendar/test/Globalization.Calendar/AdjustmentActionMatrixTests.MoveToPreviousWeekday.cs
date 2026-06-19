// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdjustmentActionMatrixTests.MoveToPreviousWeekday.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class AdjustmentActionMatrixTests
{
    /// <summary>
    /// Verifies that <see cref="AdjustmentAction.MoveToPreviousWeekday" /> targeting Friday returns the anchor unchanged
    /// when it is already a Friday and otherwise rolls back to the preceding Friday.
    /// </summary>
    /// <param name="strategyDay">The day of December 2026 the holiday resolves to.</param>
    /// <param name="expectedDay">The expected emitted day of December 2026.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(25, 25)] // Fri → unchanged (inclusive)
    [DataRow(26, 25)] // Sat → Fri 25 Dec
    [DataRow(27, 25)] // Sun → Fri 25 Dec
    [DataRow(28, 25)] // Mon → Fri 25 Dec
    [DataRow(31, 25)] // Thu → Fri 25 Dec
    public void MoveToPreviousWeekday_WhenTargetingFriday_ShouldRollBackInclusively(int strategyDay, int expectedDay)
    {
        INotableDateService service = WeekdayMoveService("MoveToPreviousWeekday", "Friday", strategyDay);

        NotableDate match = Single(service, "probe", new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 31));

        Assert.AreEqual(new DateOnly(2026, 12, expectedDay), match.Date);
        Assert.AreEqual(new DateOnly(2026, 12, strategyDay), match.ActualDate);
    }
}
