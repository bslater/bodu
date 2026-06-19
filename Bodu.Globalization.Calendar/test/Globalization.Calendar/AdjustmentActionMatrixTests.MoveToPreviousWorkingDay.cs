// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdjustmentActionMatrixTests.MoveToPreviousWorkingDay.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class AdjustmentActionMatrixTests
{
    /// <summary>
    /// Verifies that <see cref="AdjustmentAction.MoveToPreviousWorkingDay" /> retreats strictly past the anchor and skips
    /// Saturday and Sunday, so weekend anchors and the following Monday all land on the preceding Friday.
    /// </summary>
    /// <param name="strategyDay">The day of December 2026 the holiday resolves to.</param>
    /// <param name="expectedDay">The expected emitted day of December 2026.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(24, 23)] // Thu → Wed 23 Dec
    [DataRow(25, 24)] // Fri → Thu 24 Dec
    [DataRow(26, 25)] // Sat → Fri 25 Dec
    [DataRow(27, 25)] // Sun → Fri 25 Dec
    [DataRow(28, 25)] // Mon → Fri 25 Dec (skip Sun/Sat)
    [DataRow(29, 28)] // Tue → Mon 28 Dec
    public void MoveToPreviousWorkingDay_WhenAlwaysTrigger_ShouldRetreatPastWeekends(int strategyDay, int expectedDay)
    {
        INotableDateService service = WorkingDayService("MoveToPreviousWorkingDay", strategyDay, 7);

        NotableDate match = Single(service, "probe", new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 31));

        Assert.AreEqual(new DateOnly(2026, 12, expectedDay), match.Date);
        Assert.AreEqual(new DateOnly(2026, 12, strategyDay), match.ActualDate);
    }
}
