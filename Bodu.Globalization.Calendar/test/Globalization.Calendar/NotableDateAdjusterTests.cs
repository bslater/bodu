// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateAdjusterTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Extensions;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies that <see cref="NotableDateAdjuster" /> evaluates every <see cref="AdjustmentTrigger" /> and applies every
/// <see cref="AdjustmentAction" />.
/// </summary>
[TestClass]
public sealed class NotableDateAdjusterTests
{
	private static NotableDateRule SampleRule(string name = "Test") => new()
	{
		Name = name,
		Strategy = DateResolutionStrategy.Fixed,
		Category = NotableDateCategory.Holiday,
	};

	private static NotableDateAdjuster CreateAdjuster(
		Func<DateTime, bool>? isWeekend = null,
		Func<DateTime, string?, Type?, bool>? isNonWorking = null,
		IAdjustmentHandlerRegistry? handlers = null,
		Func<string, int, string?, Type?, DateTime?>? resolveByName = null)
	{
		return new NotableDateAdjuster(
			isWeekend ?? (d => d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday),
			isNonWorking ?? ((d, t, c) => false),
			CalendarWeekendDefinition.SaturdaySunday,
			weekendProvider: null,
			handlerRegistry: handlers,
			resolveByName: resolveByName);
	}

	/// <summary>
	/// Verifies that the <see cref="AdjustmentTrigger.Always" /> trigger always activates and that
	/// <see cref="AdjustmentAction.AddDays" /> shifts the date by the configured offset.
	/// </summary>
	[TestMethod]
	public void Apply_WhenAlwaysAddDays_ShouldShiftDate()
	{
		var adjuster = CreateAdjuster();
		var adjustment = new ObservanceAdjustment
		{
			Key = "test",
			Trigger = AdjustmentTrigger.Always,
			Action = AdjustmentAction.AddDays,
			OffsetDays = 3,
		};

		var result = adjuster.Apply(adjustment, SampleRule(), new DateTime(2025, 1, 1));

		Assert.IsTrue(result.Activated);
		Assert.AreEqual(new DateTime(2025, 1, 4), result.AdjustedDate);
	}

	/// <summary>
	/// Verifies that <see cref="AdjustmentTrigger.IfWeekend" /> with <see cref="AdjustmentAction.MoveToNextWeekday" /> rolls a Saturday
	/// holiday onto Monday.
	/// </summary>
	[TestMethod]
	public void Apply_WhenSaturdayAndIfWeekend_ShouldMoveToMonday()
	{
		var adjuster = CreateAdjuster();
		var adjustment = new ObservanceAdjustment
		{
			Key = "test",
			Trigger = AdjustmentTrigger.IfWeekend,
			Action = AdjustmentAction.MoveToNextWeekday,
		};

		var saturday = new DateTime(2026, 1, 3);
		var result = adjuster.Apply(adjustment, SampleRule(), saturday);

		Assert.IsTrue(result.Activated);
		Assert.AreEqual(DayOfWeek.Monday, result.AdjustedDate.DayOfWeek);
	}

	/// <summary>
	/// Verifies that <see cref="AdjustmentTrigger.IfWeekend" /> does not fire on a Tuesday.
	/// </summary>
	[TestMethod]
	public void Apply_WhenTuesdayAndIfWeekend_ShouldNotActivate()
	{
		var adjuster = CreateAdjuster();
		var adjustment = new ObservanceAdjustment
		{
			Key = "test",
			Trigger = AdjustmentTrigger.IfWeekend,
			Action = AdjustmentAction.MoveToNextWeekday,
		};

		var result = adjuster.Apply(adjustment, SampleRule(), new DateTime(2025, 1, 7));

		Assert.IsFalse(result.Activated);
	}

	/// <summary>
	/// Verifies that <see cref="AdjustmentTrigger.IfDayOfWeek" /> only activates when the date matches the configured weekday.
	/// </summary>
	[TestMethod]
	public void Apply_WhenIfDayOfWeekMatches_ShouldActivate()
	{
		var adjuster = CreateAdjuster();
		var adjustment = new ObservanceAdjustment
		{
			Key = "test",
			Trigger = AdjustmentTrigger.IfDayOfWeek,
			DayOfWeek = DayOfWeek.Friday,
			Action = AdjustmentAction.AddDays,
			OffsetDays = 1,
		};

		var friday = new DateTime(2025, 1, 3);
		var result = adjuster.Apply(adjustment, SampleRule(), friday);

		Assert.IsTrue(result.Activated);
		Assert.AreEqual(new DateTime(2025, 1, 4), result.AdjustedDate);
	}

	/// <summary>
	/// Verifies that <see cref="AdjustmentTrigger.IfNonWorkingDay" /> consults the supplied predicate.
	/// </summary>
	[TestMethod]
	public void Apply_WhenIfNonWorkingDayPredicateReturnsTrue_ShouldActivate()
	{
		var adjuster = CreateAdjuster(isNonWorking: (d, t, c) => true);
		var adjustment = new ObservanceAdjustment
		{
			Key = "test",
			Trigger = AdjustmentTrigger.IfNonWorkingDay,
			Action = AdjustmentAction.AddDays,
			OffsetDays = 2,
		};

		var result = adjuster.Apply(adjustment, SampleRule(), new DateTime(2025, 1, 1));

		Assert.IsTrue(result.Activated);
		Assert.AreEqual(new DateTime(2025, 1, 3), result.AdjustedDate);
	}

	/// <summary>
	/// Verifies that <see cref="AdjustmentTrigger.IfLeapYear" /> only activates in leap years.
	/// </summary>
	[TestMethod]
	public void Apply_WhenIfLeapYearAndYearIsLeap_ShouldActivate()
	{
		var adjuster = CreateAdjuster();
		var adjustment = new ObservanceAdjustment
		{
			Key = "test",
			Trigger = AdjustmentTrigger.IfLeapYear,
			Action = AdjustmentAction.None,
		};

		var leap = adjuster.Apply(adjustment, SampleRule(), new DateTime(2024, 2, 29));
		var common = adjuster.Apply(adjustment, SampleRule(), new DateTime(2025, 2, 28));

		Assert.IsTrue(leap.Activated);
		Assert.IsFalse(common.Activated);
	}

	/// <summary>
	/// Verifies that <see cref="AdjustmentTrigger.IfBeforeFixedDate" /> reprojects the comparison month/day onto the active year.
	/// </summary>
	[TestMethod]
	public void Apply_WhenIfBeforeFixedDate_ShouldUseProjectedYear()
	{
		var adjuster = CreateAdjuster();
		var adjustment = new ObservanceAdjustment
		{
			Key = "test",
			Trigger = AdjustmentTrigger.IfBeforeFixedDate,
			Action = AdjustmentAction.AddDays,
			OffsetDays = 7,
			ComparisonDate = new DateTime(2000, 6, 1),
		};

		var early = adjuster.Apply(adjustment, SampleRule(), new DateTime(2030, 5, 10));
		var late = adjuster.Apply(adjustment, SampleRule(), new DateTime(2030, 6, 10));

		Assert.IsTrue(early.Activated);
		Assert.IsFalse(late.Activated);
	}

	/// <summary>
	/// Verifies that <see cref="AdjustmentTrigger.IfNthOccurrenceInMonth" /> activates only when the date is the nth weekday of its
	/// month.
	/// </summary>
	[TestMethod]
	public void Apply_WhenIfNthOccurrenceInMonth_ShouldRespectOrdinal()
	{
		var adjuster = CreateAdjuster();
		var adjustment = new ObservanceAdjustment
		{
			Key = "test",
			Trigger = AdjustmentTrigger.IfNthOccurrenceInMonth,
			Action = AdjustmentAction.None,
			WeekOrdinal = WeekOfMonthOrdinal.Second,
		};

		// 8 January 2025 is the second day-of-week-ordinal slot.
		var second = adjuster.Apply(adjustment, SampleRule(), new DateTime(2025, 1, 8));
		var first = adjuster.Apply(adjustment, SampleRule(), new DateTime(2025, 1, 1));

		Assert.IsTrue(second.Activated);
		Assert.IsFalse(first.Activated);
	}

	/// <summary>
	/// Verifies that <see cref="AdjustmentAction.MoveToNextNonWorkingDay" /> walks forward until the predicate returns true.
	/// </summary>
	[TestMethod]
	public void Apply_WhenMoveToNextNonWorkingDay_ShouldStopOnFirstMatchingDay()
	{
		// First non-working day in this scenario is the first Saturday after the original date.
		var adjuster = CreateAdjuster(
			isNonWorking: (d, t, c) => d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday);

		var adjustment = new ObservanceAdjustment
		{
			Key = "test",
			Trigger = AdjustmentTrigger.Always,
			Action = AdjustmentAction.MoveToNextNonWorkingDay,
		};

		var wednesday = new DateTime(2025, 1, 1); // Wed
		var result = adjuster.Apply(adjustment, SampleRule(), wednesday);

		Assert.IsTrue(result.Activated);
		Assert.AreEqual(DayOfWeek.Saturday, result.AdjustedDate.DayOfWeek);
	}

	/// <summary>
	/// Verifies that <see cref="AdjustmentAction.ReplaceWithNamedDate" /> uses the supplied name resolver callback.
	/// </summary>
	[TestMethod]
	public void Apply_WhenReplaceWithNamedDate_ShouldUseResolverCallback()
	{
		var target = new DateTime(2025, 12, 26);
		var adjuster = CreateAdjuster(resolveByName: (name, year, t, c) =>
			string.Equals(name, "Boxing Day", StringComparison.OrdinalIgnoreCase) ? target : null);

		var adjustment = new ObservanceAdjustment
		{
			Key = "test",
			Trigger = AdjustmentTrigger.Always,
			Action = AdjustmentAction.ReplaceWithNamedDate,
			TargetRuleName = "Boxing Day",
		};

		var result = adjuster.Apply(adjustment, SampleRule(), new DateTime(2025, 12, 25));

		Assert.IsTrue(result.Activated);
		Assert.AreEqual(target, result.AdjustedDate);
	}

	/// <summary>
	/// Verifies that <see cref="AdjustmentTrigger.Custom" /> dispatches to the registered handler and uses its activation result.
	/// </summary>
	[TestMethod]
	public void Apply_WhenCustomTrigger_ShouldUseHandlerRegistry()
	{
		var registry = new AdjustmentHandlerRegistry()
			.Register("shift-by-five", new ShiftByFiveDaysHandler());

		var adjuster = CreateAdjuster(handlers: registry);
		var adjustment = new ObservanceAdjustment
		{
			Key = "test",
			Trigger = AdjustmentTrigger.Custom,
			Action = AdjustmentAction.Custom,
			HandlerKey = "shift-by-five",
		};

		var result = adjuster.Apply(adjustment, SampleRule(), new DateTime(2025, 6, 1));

		Assert.IsTrue(result.Activated);
		Assert.AreEqual(new DateTime(2025, 6, 6), result.AdjustedDate);
	}

	/// <summary>
	/// Verifies that scope evaluation excludes adjustments whose effective year window does not contain the active year.
	/// </summary>
	[TestMethod]
	public void IsInScope_WhenYearOutsideEffectiveWindow_ShouldReturnFalse()
	{
		var adjustment = new ObservanceAdjustment
		{
			Key = "test",
			Trigger = AdjustmentTrigger.Always,
			Action = AdjustmentAction.None,
			EffectiveFromYear = 2030,
		};

		Assert.IsFalse(NotableDateAdjuster.IsInScope(adjustment, 2025, null, null));
		Assert.IsTrue(NotableDateAdjuster.IsInScope(adjustment, 2031, null, null));
	}

	/// <summary>
	/// Verifies that scope evaluation respects parent-territory containment when the rule scopes to a country and the query is for a
	/// subdivision.
	/// </summary>
	[TestMethod]
	public void IsInScope_WhenAdjustmentTerritoryParentOfRequested_ShouldReturnTrue()
	{
		var adjustment = new ObservanceAdjustment
		{
			Key = "test",
			Trigger = AdjustmentTrigger.Always,
			Action = AdjustmentAction.None,
			TerritoryCode = "AU",
		};

		Assert.IsTrue(NotableDateAdjuster.IsInScope(adjustment, 2025, "AU-NSW", null));
	}

	/// <summary>
	/// Verifies that scope evaluation is bidirectional: an adjustment scoped to a subdivision (e.g. <c>AU-WA</c>) is also in scope
	/// when the call site is generating for the parent country (<c>AU</c>). This lets subdivision-specific substitutes fire while
	/// generating country-level rules; the generator tags the emitted occurrence with the narrower territory.
	/// </summary>
	[TestMethod]
	public void IsInScope_WhenAdjustmentTerritoryChildOfRequested_ShouldReturnTrue()
	{
		var adjustment = new ObservanceAdjustment
		{
			Key = "test",
			Trigger = AdjustmentTrigger.Always,
			Action = AdjustmentAction.None,
			TerritoryCode = "AU-WA",
		};

		Assert.IsTrue(NotableDateAdjuster.IsInScope(adjustment, 2025, "AU", null));
	}

	/// <summary>
	/// Verifies that a peer-subdivision adjustment (e.g. <c>AU-WA</c>) does not activate when generating for a different,
	/// unrelated subdivision (<c>AU-NSW</c>).
	/// </summary>
	[TestMethod]
	public void IsInScope_WhenAdjustmentTerritoryIsUnrelatedSibling_ShouldReturnFalse()
	{
		var adjustment = new ObservanceAdjustment
		{
			Key = "test",
			Trigger = AdjustmentTrigger.Always,
			Action = AdjustmentAction.None,
			TerritoryCode = "AU-WA",
		};

		Assert.IsFalse(NotableDateAdjuster.IsInScope(adjustment, 2025, "AU-NSW", null));
	}

	private sealed class ShiftByFiveDaysHandler : IAdjustmentHandler
	{
		public AdjustmentHandlerResult Apply(AdjustmentHandlerContext context) =>
			new(true, context.Date.AddDays(5));
	}
}
