// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateAdjusterTests.BoundedWalk.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class NotableDateAdjusterTests
{
	/// <summary>
	/// Verifies that the forward walk in <see cref="AdjustmentAction.MoveToNextNonWorkingDay" /> invokes the non-working-day
	/// predicate at most 366 times. This pins the bound so that a regression which removes the guard or substitutes an unbounded
	/// loop is detected immediately rather than manifesting as a run-time hang.
	/// </summary>
	[TestMethod]
	public void Apply_WhenMoveToNextNonWorkingDayPredicateNeverMatches_ShouldInvokePredicateExactly366Times()
	{
		// isNonWorking always returns true — every day is non-working — so no working day is ever found within the bound.
		int callCount = 0;
		var adjuster = CreateAdjuster(isNonWorking: (d, t, c) =>
		{
			callCount++;
			return true;
		});

		ObservanceAdjustment adjustment = new()
		{
			Key = "bound-regression",
			Trigger = AdjustmentTrigger.Always,
			Action = AdjustmentAction.MoveToNextNonWorkingDay,
		};

		_ = adjuster.Apply(adjustment, SampleRule(), new DateTime(2025, 1, 1));

		Assert.AreEqual(366, callCount, "The bounded walk must invoke the predicate exactly 366 times before falling back to the original date.");
	}

	/// <summary>
	/// Verifies that the forward walk's inclusive upper bound is exactly 366 days after the original date: when the walk finds a
	/// working day only on that last bounded day, it returns that date rather than falling back.
	/// </summary>
	[TestMethod]
	public void Apply_WhenMoveToNextNonWorkingDayMatchesOnLastBoundedDay_ShouldReturnThatDay()
	{
		DateTime original = new(2025, 1, 1);
		DateTime target = original.AddDays(366);
		// All days except target are non-working, so target is the only working day within the bound.
		var adjuster = CreateAdjuster(isNonWorking: (d, t, c) => d.Date != target);

		ObservanceAdjustment adjustment = new()
		{
			Key = "boundary-inclusive",
			Trigger = AdjustmentTrigger.Always,
			Action = AdjustmentAction.MoveToNextNonWorkingDay,
		};

		AdjustmentApplyResult result = adjuster.Apply(adjustment, SampleRule(), original);

		Assert.IsTrue(result.Activated);
		Assert.AreEqual(target, result.AdjustedDate);
	}

	/// <summary>
	/// Verifies that a working day one day beyond the walk's 366-day bound is <em>not</em> reached: the walk terminates at the
	/// bound and the adjuster falls back to the original date. This asserts the bound is exclusive on the far side and prevents a
	/// regression that silently raises the ceiling.
	/// </summary>
	[TestMethod]
	public void Apply_WhenMoveToNextNonWorkingDayMatchesOneDayBeyondBound_ShouldFallBackToOriginal()
	{
		DateTime original = new(2025, 1, 1);
		DateTime beyond = original.AddDays(367);
		// All days except beyond are non-working, so the only working day is one step past the 366-day bound.
		var adjuster = CreateAdjuster(isNonWorking: (d, t, c) => d.Date != beyond);

		ObservanceAdjustment adjustment = new()
		{
			Key = "boundary-exclusive",
			Trigger = AdjustmentTrigger.Always,
			Action = AdjustmentAction.MoveToNextNonWorkingDay,
		};

		AdjustmentApplyResult result = adjuster.Apply(adjustment, SampleRule(), original);

		Assert.IsTrue(result.Activated);
		Assert.AreEqual(original, result.AdjustedDate);
	}
}
