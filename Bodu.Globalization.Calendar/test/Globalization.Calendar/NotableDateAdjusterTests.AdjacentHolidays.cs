// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateAdjusterTests.AdjacentHolidays.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;

namespace Bodu.Globalization.Calendar;

public sealed partial class NotableDateAdjusterTests
{
    /// <summary>
    /// Returns the display name for an adjacent-holiday test case by reading the first element of the data row.
    /// </summary>
    public static string GetAdjacentHolidayDisplayName(MethodInfo _, object[] data) =>
        (string)data[0];

    /// <summary>
    /// Gets the test cases that drive <see cref="Apply_WhenHolidayOnWeekend_ShouldProduceCorrectObservanceDate" />.
    /// Each row contains a display name, the original holiday date, a set of dates already claimed as non-working
    /// substitutes, a flag indicating whether the <see cref="AdjustmentTrigger.IfWeekend" /> trigger is expected to
    /// fire, and the expected observance date.
    /// </summary>
    public static IEnumerable<object[]> AdjacentHolidayCases =>
    [
		// ── Christmas + Boxing Day both on weekend (2021) ─────────────────────────────
		[
            "Christmas Saturday: walks past Sunday to Monday",
            new DateTime(2021, 12, 25), // Saturday
			Array.Empty<DateTime>(),
            true,
            new DateTime(2021, 12, 27), // Monday
		],
        [
            "Boxing Day Sunday: Monday free, stops on Monday",
            new DateTime(2021, 12, 26), // Sunday
			Array.Empty<DateTime>(),
            true,
            new DateTime(2021, 12, 27), // Monday
		],
        [
            "Boxing Day Sunday: Monday claimed by Christmas observance, advances to Tuesday",
            new DateTime(2021, 12, 26),                      // Sunday
			new[] { new DateTime(2021, 12, 27) },            // Monday already taken
			true,
            new DateTime(2021, 12, 28),                      // Tuesday
		],
        [
            "Boxing Day Sunday: Monday and Tuesday both claimed, advances to Wednesday",
            new DateTime(2021, 12, 26),                                              // Sunday
			new[] { new DateTime(2021, 12, 27), new DateTime(2021, 12, 28) },        // Mon + Tue taken
			true,
            new DateTime(2021, 12, 29),                                              // Wednesday
		],

		// ── IfWeekend does not fire on a weekday ──────────────────────────────────────
		[
            "Christmas Monday: IfWeekend trigger does not fire",
            new DateTime(2023, 12, 25), // Monday
			Array.Empty<DateTime>(),
            false,
            new DateTime(2023, 12, 25), // unchanged
		],
        [
            "Boxing Day Monday: IfWeekend trigger does not fire",
            new DateTime(2022, 12, 26), // Monday
			Array.Empty<DateTime>(),
            false,
            new DateTime(2022, 12, 26), // unchanged
		],

		// ── Other holidays ────────────────────────────────────────────────────────────
		[
            "New Year's Day Sunday 2023: walks to Monday",
            new DateTime(2023, 1, 1),   // Sunday
			Array.Empty<DateTime>(),
            true,
            new DateTime(2023, 1, 2),   // Monday
		],
        [
            "ANZAC Day Saturday 2020: walks past Sunday to Monday",
            new DateTime(2020, 4, 25),  // Saturday
			Array.Empty<DateTime>(),
            true,
            new DateTime(2020, 4, 27),  // Monday
		],
    ];

    /// <summary>
    /// Verifies that <see cref="AdjustmentTrigger.IfWeekend" /> combined with
    /// <see cref="AdjustmentAction.MoveToNextNonWorkingDay" /> produces the correct substitute observance date for
    /// each scenario supplied by <see cref="AdjacentHolidayCases" />, including cases where an earlier holiday's
    /// observance has already claimed the immediately following weekday.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(AdjacentHolidayCases), DynamicDataDisplayName = nameof(GetAdjacentHolidayDisplayName))]
    public void Apply_WhenHolidayOnWeekend_ShouldProduceCorrectObservanceDate(
        string displayName,
        DateTime holidayDate,
        DateTime[] claimedObservances,
        bool expectedActivated,
        DateTime expectedDate)
    {
        NotableDateAdjuster adjuster = CreateAdjuster(
            isNonWorking: (d, t, c) =>
                d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday
                || Array.Exists(claimedObservances, obs => obs.Date == d.Date));
        ObservanceAdjustment adjustment = new()
        {
            Key = "weekend-roll",
            Trigger = AdjustmentTrigger.IfWeekend,
            Action = AdjustmentAction.MoveToNextNonWorkingDay,
        };

        AdjustmentApplyResult result = adjuster.Apply(adjustment, SampleRule(), holidayDate);

        Assert.AreEqual(expectedActivated, result.Activated);
        Assert.AreEqual(expectedDate, result.AdjustedDate);
    }
}
