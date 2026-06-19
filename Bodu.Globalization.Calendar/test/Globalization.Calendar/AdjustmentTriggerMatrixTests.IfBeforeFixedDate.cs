// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdjustmentTriggerMatrixTests.IfBeforeFixedDate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class AdjustmentTriggerMatrixTests
{
    /// <summary>
    /// Verifies that <see cref="AdjustmentTrigger.IfBeforeFixedDate" /> fires only when the occurrence falls strictly
    /// before the comparison month and day projected onto the occurrence year. The comparison anchor is 4 April.
    /// </summary>
    /// <param name="strategyMonth">The English month of the holiday's fixed strategy.</param>
    /// <param name="strategyDay">The day of the holiday's fixed strategy.</param>
    /// <param name="expectedFire">Whether the trigger is expected to fire.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("January", 1, true)]    // 1 Jan < 4 Apr → fire
    [DataRow("April", 3, true)]      // 3 Apr < 4 Apr → fire
    [DataRow("April", 4, false)]     // 4 Apr is not strictly before 4 Apr → no fire
    [DataRow("April", 5, false)]     // 5 Apr > 4 Apr → no fire
    [DataRow("December", 31, false)] // late in year → no fire
    public void IfBeforeFixedDate_WhenOccurrenceVsComparison_ShouldFireOnlyWhenStrictlyEarlier(string strategyMonth, int strategyDay, bool expectedFire)
    {
        INotableDateService service = FixedDateService("IfBeforeFixedDate", "April", 4, strategyMonth, strategyDay);

        // The window extends into early 2027 so a late-December occurrence shifted across the year boundary is captured;
        // the 2026 applicability bound guarantees a single occurrence regardless of window width.
        NotableDate match = service
            .Resolve(new DateRange(new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 31)), Territory)
            .Single(r => r.NotableDateId == "probe");

        Assert.AreEqual(expectedFire, match.IsObserved, $"{strategyMonth} {strategyDay}");
        Assert.AreEqual(expectedFire ? "fixed" : null, match.AdjustmentPolicyId);
    }

    /// <summary>
    /// Verifies that a 29 February comparison day is clamped to the last valid day of February when projected onto a
    /// non-leap occurrence year, so <see cref="AdjustmentTrigger.IfBeforeFixedDate" /> evaluates without overflow: an
    /// occurrence on 28 February 2026 is not strictly before the clamped 28 February pivot. 2026 is not a leap year.
    /// </summary>
    /// <param name="strategyDay">The day of February 2026 the holiday's fixed strategy resolves to.</param>
    /// <param name="expectedFire">Whether the trigger is expected to fire against the clamped 28 February pivot.</param>
    [TestMethod]
    [DataRow(15, true)]   // 15 Feb is strictly before the clamped 28 Feb pivot → fire
    [DataRow(28, false)]  // 28 Feb equals the clamped pivot, not strictly before → no fire
    public void IfBeforeFixedDate_WhenComparisonIsFeb29InNonLeapYear_ShouldClampToFeb28(int strategyDay, bool expectedFire)
    {
        NotableDate match = FixedDateService("IfBeforeFixedDate", "February", 29, "February", strategyDay)
            .Resolve(new DateRange(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)), Territory)
            .Single(r => r.NotableDateId == "probe");

        Assert.AreEqual(expectedFire, match.IsObserved);
    }
}
