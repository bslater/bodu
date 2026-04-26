// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateAdjusterTests.AdjacentHolidays.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class NotableDateAdjusterTests
{
	/// <summary>
	/// Verifies that when Christmas Day falls on a Saturday the <see cref="AdjustmentTrigger.IfWeekend" /> trigger
	/// fires and <see cref="AdjustmentAction.MoveToNextNonWorkingDay" /> walks past Sunday to land on Monday as the
	/// substitute observance.
	/// </summary>
	[TestMethod]
	public void Apply_WhenChristmasOnSaturday_ShouldPlaceObservanceOnMonday()
	{
		// 25 Dec 2021 = Saturday; walk skips Sunday 26 Dec and stops on Monday 27 Dec.
		var adjuster = CreateAdjuster(
			isNonWorking: (d, t, c) => d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday);
		ObservanceAdjustment adjustment = new()
		{
			Key = "xmas-weekend-roll",
			Trigger = AdjustmentTrigger.IfWeekend,
			Action = AdjustmentAction.MoveToNextNonWorkingDay,
		};

		DateTime christmas = new(2021, 12, 25); // Saturday
		AdjustmentApplyResult result = adjuster.Apply(adjustment, SampleRule("Christmas Day"), christmas);

		Assert.IsTrue(result.Activated);
		Assert.AreEqual(new DateTime(2021, 12, 27), result.AdjustedDate); // Monday
	}

	/// <summary>
	/// Verifies that when Boxing Day falls on a Sunday and Monday is already flagged as non-working by the
	/// Christmas Day substitute observance, <see cref="AdjustmentAction.MoveToNextNonWorkingDay" /> skips Monday
	/// and places the Boxing Day observance on Tuesday.
	/// </summary>
	[TestMethod]
	public void Apply_WhenBoxingDayOnSundayAndChristmasObservanceTakesMonday_ShouldPlaceObservanceOnTuesday()
	{
		// 26 Dec 2021 = Sunday; Monday 27 Dec is already non-working (Christmas substitute); Tuesday 28 Dec is free.
		DateTime christmasObservance = new(2021, 12, 27); // Monday — already a non-working substitute
		var adjuster = CreateAdjuster(
			isNonWorking: (d, t, c) =>
				d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday || d.Date == christmasObservance.Date);
		ObservanceAdjustment adjustment = new()
		{
			Key = "boxing-day-weekend-roll",
			Trigger = AdjustmentTrigger.IfWeekend,
			Action = AdjustmentAction.MoveToNextNonWorkingDay,
		};

		DateTime boxingDay = new(2021, 12, 26); // Sunday
		AdjustmentApplyResult result = adjuster.Apply(adjustment, SampleRule("Boxing Day"), boxingDay);

		Assert.IsTrue(result.Activated);
		Assert.AreEqual(new DateTime(2021, 12, 28), result.AdjustedDate); // Tuesday
	}

	/// <summary>
	/// Verifies that when Boxing Day falls on a Sunday and no other observance has claimed Monday,
	/// <see cref="AdjustmentAction.MoveToNextNonWorkingDay" /> stops on Monday as the substitute
	/// observance, rather than advancing to Tuesday.
	/// </summary>
	[TestMethod]
	public void Apply_WhenBoxingDayOnSundayWithNoConflict_ShouldPlaceObservanceOnMonday()
	{
		// Same calendar day (26 Dec 2021 = Sunday) but Monday is not yet flagged as non-working.
		var adjuster = CreateAdjuster(
			isNonWorking: (d, t, c) => d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday);
		ObservanceAdjustment adjustment = new()
		{
			Key = "boxing-day-weekend-roll",
			Trigger = AdjustmentTrigger.IfWeekend,
			Action = AdjustmentAction.MoveToNextNonWorkingDay,
		};

		DateTime boxingDay = new(2021, 12, 26); // Sunday
		AdjustmentApplyResult result = adjuster.Apply(adjustment, SampleRule("Boxing Day"), boxingDay);

		Assert.IsTrue(result.Activated);
		Assert.AreEqual(new DateTime(2021, 12, 27), result.AdjustedDate); // Monday
	}

	/// <summary>
	/// Verifies that when Christmas Day falls on a weekday the <see cref="AdjustmentTrigger.IfWeekend" /> trigger
	/// does not fire and the adjustment leaves the original date unchanged.
	/// </summary>
	[TestMethod]
	public void Apply_WhenChristmasOnWeekday_ShouldNotActivateWeekendTrigger()
	{
		// 25 Dec 2023 = Monday; IfWeekend must not fire.
		var adjuster = CreateAdjuster(
			isNonWorking: (d, t, c) => d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday);
		ObservanceAdjustment adjustment = new()
		{
			Key = "xmas-weekend-roll",
			Trigger = AdjustmentTrigger.IfWeekend,
			Action = AdjustmentAction.MoveToNextNonWorkingDay,
		};

		DateTime christmas = new(2023, 12, 25); // Monday
		AdjustmentApplyResult result = adjuster.Apply(adjustment, SampleRule("Christmas Day"), christmas);

		Assert.IsFalse(result.Activated);
		Assert.AreEqual(christmas, result.AdjustedDate);
	}
}
