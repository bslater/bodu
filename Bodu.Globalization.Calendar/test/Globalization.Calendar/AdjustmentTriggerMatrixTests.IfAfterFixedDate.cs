// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdjustmentTriggerMatrixTests.IfAfterFixedDate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class AdjustmentTriggerMatrixTests
{
    /// <summary>
    /// Verifies that <see cref="AdjustmentTrigger.IfAfterFixedDate" /> fires only when the occurrence falls strictly
    /// after the comparison month and day projected onto the occurrence year. The comparison anchor is 4 April.
    /// </summary>
    /// <param name="strategyMonth">The English month of the holiday's fixed strategy.</param>
    /// <param name="strategyDay">The day of the holiday's fixed strategy.</param>
    /// <param name="expectedFire">Whether the trigger is expected to fire.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("January", 1, false)]  // before comparison → no fire
    [DataRow("April", 3, false)]    // 3 Apr < 4 Apr → no fire
    [DataRow("April", 4, false)]    // 4 Apr is not strictly after 4 Apr → no fire
    [DataRow("April", 5, true)]     // 5 Apr > 4 Apr → fire
    [DataRow("December", 31, true)] // late in year → fire
    public void IfAfterFixedDate_WhenOccurrenceVsComparison_ShouldFireOnlyWhenStrictlyLater(string strategyMonth, int strategyDay, bool expectedFire)
    {
        INotableDateService service = FixedDateService("IfAfterFixedDate", "April", 4, strategyMonth, strategyDay);

        // The window extends into early 2027 so a late-December occurrence shifted across the year boundary is captured;
        // the 2026 applicability bound guarantees a single occurrence regardless of window width.
        NotableDate match = service
            .Resolve(new DateRange(new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 31)), Territory)
            .Single(r => r.NotableDateId == "probe");

        Assert.AreEqual(expectedFire, match.IsObserved, $"{strategyMonth} {strategyDay}");
        Assert.AreEqual(expectedFire ? "fixed" : null, match.AdjustmentPolicyId);
    }
}
