// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRangePipelineScenarioTests.AdjustmentMatrix.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Immutable;
using Bodu.Extensions;
using Bodu.Globalization.Calendar;

namespace Bodu.Globalization.Calendar.RangeResolution;

/// <summary>
/// Data-driven matrix of <see cref="AdjustmentTrigger" /> activation conditions and <see cref="AdjustmentAction" /> shift outputs
/// exercised end-to-end through <see cref="NotableDateService.ResolveNotableDatesInRange" />. Each row pins the observable
/// emission of a <see cref="NotableDate" /> for a specific <c>(anchor, trigger, action)</c> combination so future refactors
/// cannot silently change semantics.
/// </summary>
public sealed partial class NotableDateRangePipelineScenarioTests
{
	// =====================================================================================================================
	// Trigger activation matrix — one DataTestMethod per AdjustmentTrigger value.
	//
	// Each test pairs the trigger with a deterministic AddDays(+1) action so "rule fired" is observable as
	// `WasAdjusted == true && Date == anchor.AddDays(1)`, and "rule did not fire" as `WasAdjusted == false && Date == anchor`.
	// =====================================================================================================================

	/// <summary>
	/// Verifies that <see cref="AdjustmentTrigger.Always" /> fires unconditionally regardless of the anchor's weekday.
	/// </summary>
	[DataTestMethod]
	[DataRow(2026, 1, 1)]   // Thursday
	[DataRow(2026, 6, 13)]  // Saturday
	[DataRow(2026, 6, 14)]  // Sunday
	[DataRow(2026, 12, 25)] // Friday
	public void Trigger_Always_ShouldFireForAnyAnchor(int year, int month, int day)
	{
		AssertTriggerActivation(
			AdjustmentTrigger.Always,
			anchor: new DateTime(year, month, day),
			expectedActivation: true);
	}

	/// <summary>
	/// Verifies that <see cref="AdjustmentTrigger.IfWeekend" /> fires only when the anchor lands on Saturday or Sunday.
	/// </summary>
	[DataTestMethod]
	[DataRow(2026, 12, 25, false)] // Friday
	[DataRow(2026, 12, 26, true)]  // Saturday
	[DataRow(2026, 12, 27, true)]  // Sunday
	[DataRow(2026, 12, 28, false)] // Monday
	[DataRow(2026, 12, 29, false)] // Tuesday
	public void Trigger_IfWeekend_ShouldFireOnlyOnSaturdayOrSunday(int year, int month, int day, bool expectedActivation)
	{
		AssertTriggerActivation(
			AdjustmentTrigger.IfWeekend,
			anchor: new DateTime(year, month, day),
			expectedActivation: expectedActivation);
	}

	/// <summary>
	/// Verifies that <see cref="AdjustmentTrigger.IfWeekday" /> fires only on weekdays under the configured weekend definition.
	/// </summary>
	[DataTestMethod]
	[DataRow(2026, 12, 25, true)]  // Friday
	[DataRow(2026, 12, 26, false)] // Saturday
	[DataRow(2026, 12, 27, false)] // Sunday
	[DataRow(2026, 12, 28, true)]  // Monday
	[DataRow(2026, 12, 29, true)]  // Tuesday
	public void Trigger_IfWeekday_ShouldFireOnlyOnMondayThroughFriday(int year, int month, int day, bool expectedActivation)
	{
		AssertTriggerActivation(
			AdjustmentTrigger.IfWeekday,
			anchor: new DateTime(year, month, day),
			expectedActivation: expectedActivation);
	}

	/// <summary>
	/// Verifies that <see cref="AdjustmentTrigger.IfLeapYear" /> fires for years divisible by four (with the standard Gregorian
	/// leap-year exceptions handled by <see cref="DateTime.IsLeapYear" />).
	/// </summary>
	[DataTestMethod]
	[DataRow(2024, 7, 1, true)]   // Leap
	[DataRow(2025, 7, 1, false)]  // Non-leap
	[DataRow(2026, 7, 1, false)]  // Non-leap
	[DataRow(2027, 7, 1, false)]  // Non-leap
	[DataRow(2028, 7, 1, true)]   // Leap
	[DataRow(2100, 7, 1, false)]  // Century, not divisible by 400 → non-leap
	[DataRow(2400, 7, 1, true)]   // Century, divisible by 400 → leap
	public void Trigger_IfLeapYear_ShouldFireOnlyInLeapYears(int year, int month, int day, bool expectedActivation)
	{
		AssertTriggerActivation(
			AdjustmentTrigger.IfLeapYear,
			anchor: new DateTime(year, month, day),
			expectedActivation: expectedActivation);
	}

	/// <summary>
	/// Verifies that <see cref="AdjustmentTrigger.IfDayOfWeek" /> with <c>DayOfWeek.Sunday</c> fires only on Sundays.
	/// </summary>
	[DataTestMethod]
	[DataRow(2026, 7, 1, false)]  // Wednesday
	[DataRow(2026, 7, 2, false)]  // Thursday
	[DataRow(2026, 7, 5, true)]   // Sunday
	[DataRow(2026, 7, 6, false)]  // Monday
	[DataRow(2026, 7, 12, true)]  // Sunday
	public void Trigger_IfDayOfWeekSunday_ShouldFireOnlyOnSundays(int year, int month, int day, bool expectedActivation)
	{
		ObservanceAdjustment adjustment = MakeAddOneDayAdjustment() with
		{
			Trigger = AdjustmentTrigger.IfDayOfWeek,
			DayOfWeek = DayOfWeek.Sunday,
		};

		AssertCustomAdjustmentActivation(
			adjustment,
			anchor: new DateTime(year, month, day),
			expectedActivation: expectedActivation);
	}

	/// <summary>
	/// Verifies that <see cref="AdjustmentTrigger.IfBeforeFixedDate" /> fires when the anchor is strictly earlier than the
	/// year-projected comparison date.
	/// </summary>
	[DataTestMethod]
	[DataRow(2026, 1, 1, true)]   // 1 Jan < 4 Apr → fire
	[DataRow(2026, 4, 3, true)]   // 3 Apr < 4 Apr → fire
	[DataRow(2026, 4, 4, false)]  // 4 Apr is NOT strictly less than 4 Apr → no fire
	[DataRow(2026, 4, 5, false)]  // 5 Apr > 4 Apr → no fire
	[DataRow(2026, 12, 31, false)]
	public void Trigger_IfBeforeFixedDate_ShouldFireOnlyForAnchorsBeforeComparison(int year, int month, int day, bool expectedActivation)
	{
		ObservanceAdjustment adjustment = MakeAddOneDayAdjustment() with
		{
			Trigger = AdjustmentTrigger.IfBeforeFixedDate,
			ComparisonDate = new DateTime(year, 4, 4), // The year component is replaced with the anchor's year at evaluation.
		};

		AssertCustomAdjustmentActivation(
			adjustment,
			anchor: new DateTime(year, month, day),
			expectedActivation: expectedActivation);
	}

	/// <summary>
	/// Verifies that <see cref="AdjustmentTrigger.IfAfterFixedDate" /> fires when the anchor is strictly later than the
	/// year-projected comparison date.
	/// </summary>
	[DataTestMethod]
	[DataRow(2026, 1, 1, false)]
	[DataRow(2026, 4, 4, false)]  // Not strictly greater
	[DataRow(2026, 4, 5, true)]   // 5 Apr > 4 Apr → fire
	[DataRow(2026, 12, 31, true)] // late in year → fire
	public void Trigger_IfAfterFixedDate_ShouldFireOnlyForAnchorsAfterComparison(int year, int month, int day, bool expectedActivation)
	{
		ObservanceAdjustment adjustment = MakeAddOneDayAdjustment() with
		{
			Trigger = AdjustmentTrigger.IfAfterFixedDate,
			ComparisonDate = new DateTime(year, 4, 4),
		};

		AssertCustomAdjustmentActivation(
			adjustment,
			anchor: new DateTime(year, month, day),
			expectedActivation: expectedActivation);
	}

	/// <summary>
	/// Verifies that <see cref="AdjustmentTrigger.IfNthOccurrenceInMonth" /> fires only when the anchor's day-of-month falls in
	/// the seven-day block that <see cref="WeekOfMonthOrdinal" /> identifies (days 1-7 = First, 8-14 = Second, …).
	/// </summary>
	[DataTestMethod]
	[DataRow(2026, 6, 1, (int)WeekOfMonthOrdinal.First, true)]    // Day 1 → First
	[DataRow(2026, 6, 7, (int)WeekOfMonthOrdinal.First, true)]    // Day 7 → First
	[DataRow(2026, 6, 8, (int)WeekOfMonthOrdinal.First, false)]   // Day 8 → Second
	[DataRow(2026, 6, 8, (int)WeekOfMonthOrdinal.Second, true)]
	[DataRow(2026, 6, 15, (int)WeekOfMonthOrdinal.Third, true)]
	[DataRow(2026, 6, 22, (int)WeekOfMonthOrdinal.Fourth, true)]
	[DataRow(2026, 6, 29, (int)WeekOfMonthOrdinal.Fifth, true)]
	[DataRow(2026, 6, 1, (int)WeekOfMonthOrdinal.Fifth, false)]
	public void Trigger_IfNthOccurrenceInMonth_ShouldFireOnlyForMatchingOrdinalBlock(
		int year,
		int month,
		int day,
		int ordinalAsInt,
		bool expectedActivation)
	{
		ObservanceAdjustment adjustment = MakeAddOneDayAdjustment() with
		{
			Trigger = AdjustmentTrigger.IfNthOccurrenceInMonth,
			WeekOrdinal = (WeekOfMonthOrdinal)ordinalAsInt,
		};

		AssertCustomAdjustmentActivation(
			adjustment,
			anchor: new DateTime(year, month, day),
			expectedActivation: expectedActivation);
	}

	/// <summary>
	/// Verifies that <see cref="AdjustmentTrigger.IfNonWorkingDay" /> fires when the cache reports the anchor as a non-working
	/// day — either because it is a weekend or because another non-working rule covers it.
	/// </summary>
	[TestMethod]
	public void Trigger_IfNonWorkingDay_ShouldFireWhenAnotherRuleMakesAnchorNonWorking()
	{
		// Probe rule on 7 Jul 2026 (Tuesday, weekday). Adjustment fires only if some other rule marks 7 Jul non-working.
		NotableDateRule blocker = new()
		{
			Name = "Blocker",
			Strategy = DateResolutionStrategy.Fixed,
			Category = NotableDateCategory.Holiday,
			Month = 7,
			Day = 7,
			IsNonWorkingDay = true,
		};

		NotableDateRule probe = new()
		{
			Name = "Probe",
			Strategy = DateResolutionStrategy.Fixed,
			Category = NotableDateCategory.Holiday,
			Month = 7,
			Day = 7,
			IsNonWorkingDay = false,
			Adjustments = ImmutableArray.Create(MakeAddOneDayAdjustment() with
			{
				Trigger = AdjustmentTrigger.IfNonWorkingDay,
			}),
		};

		NotableDateService service = BuildService(blocker, probe);

		IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
			new DateTime(2026, 7, 1),
			new DateTime(2026, 7, 31));

		NotableDate probeMatch = resolved.Single(n => n.Name == "Probe");
		Assert.IsTrue(probeMatch.WasAdjusted, "IfNonWorkingDay should have fired because Blocker marks 7 Jul as non-working.");
		Assert.AreEqual(new DateTime(2026, 7, 8), probeMatch.Date);
	}

	/// <summary>
	/// Verifies that <see cref="AdjustmentTrigger.IfNonWorkingDay" /> does not fire when the anchor is a working day with no
	/// covering blocker rule and no weekend overlap.
	/// </summary>
	[TestMethod]
	public void Trigger_IfNonWorkingDay_ShouldNotFireWhenAnchorIsAWorkingDay()
	{
		NotableDateRule probe = new()
		{
			Name = "Probe",
			Strategy = DateResolutionStrategy.Fixed,
			Category = NotableDateCategory.Holiday,
			Month = 7,
			Day = 7, // Tue 7 Jul 2026 — a regular working day.
			IsNonWorkingDay = false,
			Adjustments = ImmutableArray.Create(MakeAddOneDayAdjustment() with
			{
				Trigger = AdjustmentTrigger.IfNonWorkingDay,
			}),
		};

		NotableDateService service = BuildService(probe);

		IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
			new DateTime(2026, 7, 1),
			new DateTime(2026, 7, 31));

		NotableDate probeMatch = resolved.Single(n => n.Name == "Probe");
		Assert.IsFalse(probeMatch.WasAdjusted);
		Assert.AreEqual(new DateTime(2026, 7, 7), probeMatch.Date);
	}

	// =====================================================================================================================
	// Action shift matrix — one DataTestMethod per AdjustmentAction value (using AdjustmentTrigger.Always so the action's
	// shift is always observable).
	// =====================================================================================================================

	/// <summary>
	/// Verifies that <see cref="AdjustmentAction.AddDays" /> shifts the anchor by the configured offset, including negative
	/// offsets that cross month and year boundaries.
	/// </summary>
	[DataTestMethod]
	[DataRow(1, 2026, 7, 1, 2026, 7, 2)]
	[DataRow(7, 2026, 7, 1, 2026, 7, 8)]
	[DataRow(31, 2026, 7, 1, 2026, 8, 1)]      // Cross month forward
	[DataRow(180, 2026, 7, 1, 2026, 12, 28)]   // Half-year shift
	[DataRow(-1, 2026, 7, 1, 2026, 6, 30)]     // Cross month backward
	[DataRow(-7, 2026, 7, 1, 2026, 6, 24)]
	[DataRow(-180, 2026, 7, 1, 2026, 1, 2)]    // Half-year backward
	[DataRow(365, 2026, 7, 1, 2027, 7, 1)]     // Forward to next year
	[DataRow(-365, 2026, 7, 1, 2025, 7, 1)]    // Backward to prior year
	public void Action_AddDays_ShouldShiftAnchorByConfiguredOffset(
		int offsetDays,
		int anchorYear, int anchorMonth, int anchorDay,
		int expectedYear, int expectedMonth, int expectedDay)
	{
		ObservanceAdjustment adjustment = MakeAddOneDayAdjustment() with
		{
			Trigger = AdjustmentTrigger.Always,
			Action = AdjustmentAction.AddDays,
			OffsetDays = offsetDays,
		};

		AssertActionShiftDate(
			adjustment,
			anchor: new DateTime(anchorYear, anchorMonth, anchorDay),
			expectedAdjusted: new DateTime(expectedYear, expectedMonth, expectedDay));
	}

	/// <summary>
	/// Verifies that <see cref="AdjustmentAction.MoveToNextWeekday" /> always advances at least one day and skips Saturday and
	/// Sunday under the configured Saturday/Sunday weekend definition.
	/// </summary>
	[DataTestMethod]
	[DataRow(2026, 12, 25, 2026, 12, 28)] // Fri → Mon (skip Sat/Sun)
	[DataRow(2026, 12, 26, 2026, 12, 28)] // Sat → Mon
	[DataRow(2026, 12, 27, 2026, 12, 28)] // Sun → Mon
	[DataRow(2026, 12, 28, 2026, 12, 29)] // Mon → Tue (always advances at least one day)
	public void Action_MoveToNextWeekday_ShouldAdvanceToFirstFollowingWeekday(
		int anchorYear, int anchorMonth, int anchorDay,
		int expectedYear, int expectedMonth, int expectedDay)
	{
		ObservanceAdjustment adjustment = MakeAddOneDayAdjustment() with
		{
			Trigger = AdjustmentTrigger.Always,
			Action = AdjustmentAction.MoveToNextWeekday,
		};

		AssertActionShiftDate(
			adjustment,
			anchor: new DateTime(anchorYear, anchorMonth, anchorDay),
			expectedAdjusted: new DateTime(expectedYear, expectedMonth, expectedDay));
	}

	/// <summary>
	/// Verifies that <see cref="AdjustmentAction.MoveToPreviousWeekday" /> always retreats at least one day and skips Saturday
	/// and Sunday under the configured weekend definition.
	/// </summary>
	[DataTestMethod]
	[DataRow(2026, 12, 28, 2026, 12, 25)] // Mon → Fri (skip Sun/Sat)
	[DataRow(2026, 12, 27, 2026, 12, 25)] // Sun → Fri
	[DataRow(2026, 12, 26, 2026, 12, 25)] // Sat → Fri
	[DataRow(2026, 12, 25, 2026, 12, 24)] // Fri → Thu (always retreats at least one day)
	public void Action_MoveToPreviousWeekday_ShouldRetreatToFirstPrecedingWeekday(
		int anchorYear, int anchorMonth, int anchorDay,
		int expectedYear, int expectedMonth, int expectedDay)
	{
		ObservanceAdjustment adjustment = MakeAddOneDayAdjustment() with
		{
			Trigger = AdjustmentTrigger.Always,
			Action = AdjustmentAction.MoveToPreviousWeekday,
		};

		AssertActionShiftDate(
			adjustment,
			anchor: new DateTime(anchorYear, anchorMonth, anchorDay),
			expectedAdjusted: new DateTime(expectedYear, expectedMonth, expectedDay));
	}

	/// <summary>
	/// Verifies that <see cref="AdjustmentAction.None" /> leaves the date unchanged even though the trigger fires; the rule
	/// emits the base anchor with no <see cref="AdjustmentReason" /> attached because the adjuster reports the activated
	/// result equals the original date.
	/// </summary>
	[TestMethod]
	public void Action_None_ShouldEmitBaseAnchorWithoutAdjustmentReason()
	{
		ObservanceAdjustment adjustment = MakeAddOneDayAdjustment() with
		{
			Trigger = AdjustmentTrigger.Always,
			Action = AdjustmentAction.None,
			OffsetDays = 0,
		};

		NotableDateRule probe = MakeProbeRule(2026, 7, 1, ImmutableArray.Create(adjustment));
		NotableDateService service = BuildService(probe);

		IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
			new DateTime(2026, 7, 1),
			new DateTime(2026, 7, 5));

		NotableDate match = resolved.Single(n => n.Name == "Probe");
		Assert.AreEqual(new DateTime(2026, 7, 1), match.Date);
		Assert.IsFalse(match.WasAdjusted, "AdjustmentAction.None produces no shift; the emitted date is the unchanged base anchor.");
	}

	/// <summary>
	/// Verifies that <see cref="AdjustmentAction.MoveToNextNonWorkingDay" /> walks past every cached non-working blocker before
	/// landing on the next clean working day.
	/// </summary>
	[TestMethod]
	public void Action_MoveToNextNonWorkingDay_ShouldWalkPastCachedBlockers()
	{
		// Anchor: Wed 1 Jul 2026 (working day). Adjustment Always + MoveToNextNonWorkingDay.
		// Wed → walk Thu 2 Jul (clean) → return Thu 2 Jul. Without blockers, the walk stops on the very next weekday.
		NotableDateRule withoutBlockers = MakeProbeRule(
			year: 2026,
			month: 7,
			day: 1,
			adjustments: ImmutableArray.Create(MakeAddOneDayAdjustment() with
			{
				Trigger = AdjustmentTrigger.Always,
				Action = AdjustmentAction.MoveToNextNonWorkingDay,
			}));

		NotableDateService noBlockerService = BuildService(withoutBlockers);
		NotableDate noBlockerEmission = noBlockerService.ResolveNotableDatesInRange(
			new DateTime(2026, 7, 1),
			new DateTime(2026, 7, 10)).Single(n => n.Name == "Probe");

		Assert.AreEqual(new DateTime(2026, 7, 2), noBlockerEmission.Date);

		// Now add a blocker on 2 Jul 2026; the walk should skip Thu 2 Jul and land on Fri 3 Jul.
		NotableDateRule blocker = new()
		{
			Name = "Blocker",
			Strategy = DateResolutionStrategy.Fixed,
			Category = NotableDateCategory.Holiday,
			Month = 7,
			Day = 2,
			IsNonWorkingDay = true,
		};

		NotableDateService withBlockerService = BuildService(withoutBlockers, blocker);
		NotableDate withBlockerEmission = withBlockerService.ResolveNotableDatesInRange(
			new DateTime(2026, 7, 1),
			new DateTime(2026, 7, 10)).Single(n => n.Name == "Probe");

		Assert.AreEqual(new DateTime(2026, 7, 3), withBlockerEmission.Date);
	}

	/// <summary>
	/// Verifies that <see cref="AdjustmentAction.ReplaceWithNamedDate" /> replaces the anchor with the cached observed date of
	/// another rule resolved earlier in the pipeline.
	/// </summary>
	[TestMethod]
	public void Action_ReplaceWithNamedDate_ShouldUseTargetRulesObservedDate()
	{
		NotableDateRule target = new()
		{
			Name = "Target",
			Strategy = DateResolutionStrategy.Fixed,
			Category = NotableDateCategory.Holiday,
			Month = 7,
			Day = 15,
			IsNonWorkingDay = true,
		};

		NotableDateRule replacement = new()
		{
			Name = "Replacement",
			Strategy = DateResolutionStrategy.Fixed,
			Category = NotableDateCategory.Holiday,
			Month = 7,
			Day = 1,
			IsNonWorkingDay = true,
			Adjustments = ImmutableArray.Create(MakeAddOneDayAdjustment() with
			{
				Trigger = AdjustmentTrigger.Always,
				Action = AdjustmentAction.ReplaceWithNamedDate,
				TargetRuleName = "Target",
			}),
		};

		NotableDateService service = BuildService(target, replacement);

		IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
			new DateTime(2026, 7, 1),
			new DateTime(2026, 7, 31));

		NotableDate replacementEmission = resolved.Single(n => n.Name == "Replacement");
		Assert.AreEqual(new DateTime(2026, 7, 15), replacementEmission.Date,
			"ReplaceWithNamedDate should pull the target's observed date from the cache.");
		Assert.IsTrue(replacementEmission.WasAdjusted);
	}

	// =====================================================================================================================
	// Multiple adjustments per rule
	// =====================================================================================================================

	/// <summary>
	/// Verifies that when a rule has multiple <see cref="ObservanceAdjustment" /> entries, only those whose triggers activate
	/// contribute to the emitted observed date. The pipeline iterates adjustments in <see cref="ObservanceAdjustment.Priority" />
	/// order (lower first) and the last activating adjustment wins on the entry's <c>Adjusted</c> field — a documented
	/// limitation of the prototype pipeline.
	/// </summary>
	[TestMethod]
	public void MultipleAdjustments_WhenSecondActivatingAdjustmentRunsLast_ShouldOverwriteFirst()
	{
		// Anchor: Fri 25 Dec 2026.
		// Adjustment A (priority 10): IfWeekday + AddDays(+1) → activates → Sat 26 Dec.
		// Adjustment B (priority 20): IfWeekday + AddDays(+3) → activates → Mon 28 Dec.
		// Both trigger on the original Friday anchor; B has lower priority precedence (higher number) and therefore runs last.
		// Last-wins on entry.Adjusted, so the emitted observed date is Mon 28 Dec.
		NotableDateRule probe = new()
		{
			Name = "Probe",
			Strategy = DateResolutionStrategy.Fixed,
			Category = NotableDateCategory.Holiday,
			Month = 12,
			Day = 25,
			IsNonWorkingDay = true,
			Adjustments = ImmutableArray.Create(
				MakeAddOneDayAdjustment() with
				{
					Key = "first",
					Priority = 10,
					Trigger = AdjustmentTrigger.IfWeekday,
					Action = AdjustmentAction.AddDays,
					OffsetDays = 1,
				},
				MakeAddOneDayAdjustment() with
				{
					Key = "second",
					Priority = 20,
					Trigger = AdjustmentTrigger.IfWeekday,
					Action = AdjustmentAction.AddDays,
					OffsetDays = 3,
				}),
		};

		NotableDateService service = BuildService(probe);

		IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
			new DateTime(2026, 12, 25),
			new DateTime(2026, 12, 31));

		NotableDate match = resolved.Single(n => n.Name == "Probe");
		Assert.AreEqual(new DateTime(2026, 12, 28), match.Date,
			"Last-applied activating adjustment should win on the entry's Adjusted field (documented prototype limitation).");
		Assert.IsTrue(match.WasAdjusted);
	}

	/// <summary>
	/// Verifies that when only one of two adjustments activates, that single activation is reflected in the emission and the
	/// non-activating adjustment is invisible.
	/// </summary>
	[TestMethod]
	public void MultipleAdjustments_WhenOnlyOneTriggerActivates_ShouldEmitThatActivation()
	{
		// Anchor: Sat 26 Dec 2026.
		// Adjustment A: IfWeekday → does not activate (Saturday is not a weekday).
		// Adjustment B: IfWeekend + AddDays(+2) → activates → Mon 28 Dec.
		NotableDateRule probe = new()
		{
			Name = "Probe",
			Strategy = DateResolutionStrategy.Fixed,
			Category = NotableDateCategory.Holiday,
			Month = 12,
			Day = 26,
			IsNonWorkingDay = true,
			Adjustments = ImmutableArray.Create(
				MakeAddOneDayAdjustment() with
				{
					Key = "weekday-only",
					Priority = 10,
					Trigger = AdjustmentTrigger.IfWeekday,
					Action = AdjustmentAction.AddDays,
					OffsetDays = 1,
				},
				MakeAddOneDayAdjustment() with
				{
					Key = "weekend-only",
					Priority = 20,
					Trigger = AdjustmentTrigger.IfWeekend,
					Action = AdjustmentAction.AddDays,
					OffsetDays = 2,
				}),
		};

		NotableDateService service = BuildService(probe);

		IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
			new DateTime(2026, 12, 26),
			new DateTime(2026, 12, 31));

		NotableDate match = resolved.Single(n => n.Name == "Probe");
		Assert.AreEqual(new DateTime(2026, 12, 28), match.Date);
		Assert.IsTrue(match.WasAdjusted);
	}

	// =====================================================================================================================
	// Helpers
	// =====================================================================================================================

	/// <summary>
	/// Builds a deterministic <c>AddDays(+1)</c> adjustment template used by the trigger-activation tests so a fired adjustment
	/// is observable as <c>WasAdjusted == true</c> and <c>Date == anchor.AddDays(1)</c>.
	/// </summary>
	/// <returns>The adjustment template.</returns>
	private static ObservanceAdjustment MakeAddOneDayAdjustment() => new()
	{
		Key = "matrix-probe",
		Trigger = AdjustmentTrigger.Always,
		Action = AdjustmentAction.AddDays,
		OffsetDays = 1,
		IsNonWorkingDay = true,
	};

	/// <summary>
	/// Builds a fixed-date probe rule constrained to a single civil year so an adjustment whose shift crosses a year boundary
	/// does not cause the rule to be materialised twice (once per neighbouring candidate year). The
	/// <see cref="NotableDateRule.FirstYear" /> / <see cref="NotableDateRule.LastYear" /> bounds make the rule applicable only in
	/// the supplied <paramref name="year" />.
	/// </summary>
	/// <param name="year">The single civil year the rule applies to.</param>
	/// <param name="month">The fixed month.</param>
	/// <param name="day">The fixed day.</param>
	/// <param name="adjustments">The adjustments attached to the rule.</param>
	/// <returns>The probe rule.</returns>
	private static NotableDateRule MakeProbeRule(int year, int month, int day, ImmutableArray<ObservanceAdjustment> adjustments) => new()
	{
		Name = "Probe",
		Strategy = DateResolutionStrategy.Fixed,
		Category = NotableDateCategory.Holiday,
		Month = month,
		Day = day,
		FirstYear = year,
		LastYear = year,
		IsNonWorkingDay = true,
		Adjustments = adjustments,
	};

	/// <summary>
	/// Asserts that a probe rule with <c>(trigger, AddDays(+1))</c> emits the expected adjusted or unchanged date for the
	/// supplied anchor.
	/// </summary>
	/// <param name="trigger">The trigger to test.</param>
	/// <param name="anchor">The anchor date the rule resolves to.</param>
	/// <param name="expectedActivation">Whether the trigger is expected to fire.</param>
	private static void AssertTriggerActivation(AdjustmentTrigger trigger, DateTime anchor, bool expectedActivation)
	{
		ObservanceAdjustment adjustment = MakeAddOneDayAdjustment() with { Trigger = trigger };
		AssertCustomAdjustmentActivation(adjustment, anchor, expectedActivation);
	}

	/// <summary>
	/// Asserts that a probe rule using the supplied custom adjustment emits the expected adjusted or unchanged date.
	/// </summary>
	/// <param name="adjustment">The adjustment to attach to the probe rule.</param>
	/// <param name="anchor">The anchor date the rule resolves to.</param>
	/// <param name="expectedActivation">Whether the adjustment is expected to fire.</param>
	private static void AssertCustomAdjustmentActivation(ObservanceAdjustment adjustment, DateTime anchor, bool expectedActivation)
	{
		NotableDateRule probe = MakeProbeRule(anchor.Year, anchor.Month, anchor.Day, ImmutableArray.Create(adjustment));
		NotableDateService service = BuildService(probe);

		// Window covers the anchor and any potential AddDays(+1) shift. Always extend at least one day past the anchor so the
		// shifted date is visible in the output regardless of whether the trigger fires.
		DateTime windowStart = SafeAddDays(anchor, -1);
		DateTime windowEnd = SafeAddDays(anchor, 7);

		IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(windowStart, windowEnd);

		NotableDate match = resolved.Single(n => n.Name == "Probe");

		if (expectedActivation)
		{
			Assert.IsTrue(match.WasAdjusted, $"Expected the trigger to fire for anchor {anchor:yyyy-MM-dd}.");
			Assert.AreEqual(anchor.AddDays(1), match.Date,
				$"Expected adjusted date to be anchor + 1 ({anchor.AddDays(1):yyyy-MM-dd}); got {match.Date:yyyy-MM-dd}.");
		}
		else
		{
			Assert.IsFalse(match.WasAdjusted, $"Expected the trigger to not fire for anchor {anchor:yyyy-MM-dd}.");
			Assert.AreEqual(anchor, match.Date,
				$"Expected unchanged base anchor {anchor:yyyy-MM-dd}; got {match.Date:yyyy-MM-dd}.");
		}
	}

	/// <summary>
	/// Asserts that a probe rule using the supplied action (with an always-firing trigger) emits the expected shifted date.
	/// </summary>
	/// <param name="adjustment">The adjustment to attach to the probe rule.</param>
	/// <param name="anchor">The anchor date the rule resolves to.</param>
	/// <param name="expectedAdjusted">The expected adjusted date.</param>
	private static void AssertActionShiftDate(ObservanceAdjustment adjustment, DateTime anchor, DateTime expectedAdjusted)
	{
		NotableDateRule probe = MakeProbeRule(anchor.Year, anchor.Month, anchor.Day, ImmutableArray.Create(adjustment));
		NotableDateService service = BuildService(probe);

		DateTime windowStart = anchor < expectedAdjusted ? anchor : expectedAdjusted;
		DateTime windowEnd = anchor > expectedAdjusted ? anchor : expectedAdjusted;
		windowStart = SafeAddDays(windowStart, -1);
		windowEnd = SafeAddDays(windowEnd, 1);

		IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(windowStart, windowEnd);

		NotableDate match = resolved.Single(n => n.Name == "Probe");
		Assert.AreEqual(expectedAdjusted, match.Date,
			$"For anchor {anchor:yyyy-MM-dd} with action {adjustment.Action}, expected adjusted {expectedAdjusted:yyyy-MM-dd}; got {match.Date:yyyy-MM-dd}.");
	}

	/// <summary>
	/// Adds days to a date with clamp-on-overflow semantics so test parameters cannot push the window outside the supported
	/// <see cref="DateTime" /> range.
	/// </summary>
	/// <param name="date">The source date.</param>
	/// <param name="days">The day delta.</param>
	/// <returns>The clamped result.</returns>
	private static DateTime SafeAddDays(DateTime date, int days)
	{
		if (days < 0 && date <= DateTime.MinValue.AddDays(-days)) return DateTime.MinValue.Date;
		if (days > 0 && date >= DateTime.MaxValue.AddDays(-days)) return DateTime.MaxValue.Date;
		return date.AddDays(days);
	}
}
