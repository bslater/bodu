// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRangePipelineScenarioTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Immutable;
using Bodu.Extensions;
using Bodu.Globalization.Calendar;

namespace Bodu.Globalization.Calendar.RangeResolution;

/// <summary>
/// Validates <see cref="NotableDateService.ResolveNotableDatesInRange" /> against a curated catalogue of realistic
/// notable-date rules across the scenario families the prototype range-resolution pipeline must support: window boundaries,
/// fixed-date emission, weekend roll-forward / roll-backward adjustments, consecutive-holiday distinct-substitute allocation,
/// algorithmic anchors with offset descendants, multi-day spans, calendar-year applicability bounds and recurrence cadence,
/// territory parent / child containment, and category-level filtering.
/// </summary>
/// <remarks>
/// <para>
/// Each scenario sets up a minimal subset of the catalogue rules so the test asserts only the behaviour under examination.
/// Day-of-week pairings used in the assertions are real Gregorian calendar dates so reviewers can verify them with any almanac.
/// </para>
/// <para>
/// All tests target the public <see cref="NotableDateService.ResolveNotableDatesInRange" /> overload — the prototype pipeline's
/// public entry point — to exercise the full request lifecycle (planning, main pass, fringe pass, adjustment, emission,
/// collision resolution, localisation).
/// </para>
/// </remarks>
[TestClass]
public sealed partial class NotableDateRangePipelineScenarioTests
{
	// =====================================================================================================================
	// Catalogue rule definitions — realistic notable-date specifications used across the scenario suite.
	// =====================================================================================================================

	private static NotableDateRule ChristmasDay() => new()
	{
		Name = "Christmas Day",
		Strategy = DateResolutionStrategy.Fixed,
		Category = NotableDateCategory.Holiday,
		Month = 12,
		Day = 25,
		IsNonWorkingDay = true,
	};

	private static NotableDateRule ChristmasDayWithWeekendSubstitute() => ChristmasDay() with
	{
		Adjustments = ImmutableArray.Create(new ObservanceAdjustment
		{
			Key = "christmas-substitute",
			Trigger = AdjustmentTrigger.IfWeekend,
			Action = AdjustmentAction.MoveToNextNonWorkingDay,
			IsNonWorkingDay = true,
		}),
	};

	private static NotableDateRule BoxingDayFixed() => new()
	{
		Name = "Boxing Day",
		Strategy = DateResolutionStrategy.Fixed,
		Category = NotableDateCategory.Holiday,
		Month = 12,
		Day = 26,
		IsNonWorkingDay = true,
	};

	private static NotableDateRule BoxingDayFixedWithWeekendSubstitute() => BoxingDayFixed() with
	{
		Adjustments = ImmutableArray.Create(new ObservanceAdjustment
		{
			Key = "boxing-day-substitute",
			Trigger = AdjustmentTrigger.IfWeekend,
			Action = AdjustmentAction.MoveToNextNonWorkingDay,
			IsNonWorkingDay = true,
		}),
	};

	private static NotableDateRule BoxingDayOffsetFromChristmas() => new()
	{
		Name = "Boxing Day",
		Strategy = DateResolutionStrategy.OffsetFromAnchor,
		Category = NotableDateCategory.Holiday,
		AnchorRuleName = "Christmas Day",
		OffsetDays = 1,
		IsNonWorkingDay = true,
	};

	private static NotableDateRule NewYearsDayWithWeekendSubstitute() => new()
	{
		Name = "New Year's Day",
		Strategy = DateResolutionStrategy.Fixed,
		Category = NotableDateCategory.Holiday,
		Month = 1,
		Day = 1,
		IsNonWorkingDay = true,
		Adjustments = ImmutableArray.Create(new ObservanceAdjustment
		{
			Key = "new-year-substitute",
			Trigger = AdjustmentTrigger.IfWeekend,
			Action = AdjustmentAction.MoveToNextNonWorkingDay,
			IsNonWorkingDay = true,
		}),
	};

	private static NotableDateRule EasterSunday() => new()
	{
		Name = "Easter Sunday",
		Strategy = DateResolutionStrategy.Algorithm,
		Category = NotableDateCategory.Religious,
		AlgorithmKey = EasterAlgorithmKey,
		IsNonWorkingDay = false,
	};

	private static NotableDateRule GoodFriday() => new()
	{
		Name = "Good Friday",
		Strategy = DateResolutionStrategy.OffsetFromAnchor,
		Category = NotableDateCategory.Religious,
		AnchorRuleName = "Easter Sunday",
		OffsetDays = -2,
		IsNonWorkingDay = true,
	};

	private static NotableDateRule EasterMonday() => new()
	{
		Name = "Easter Monday",
		Strategy = DateResolutionStrategy.OffsetFromAnchor,
		Category = NotableDateCategory.Religious,
		AnchorRuleName = "Easter Sunday",
		OffsetDays = 1,
		IsNonWorkingDay = true,
	};

	private static NotableDateRule StartOfLent() => new()
	{
		Name = "Start of Lent",
		Strategy = DateResolutionStrategy.OffsetFromAnchor,
		Category = NotableDateCategory.Religious,
		AnchorRuleName = "Easter Sunday",
		OffsetDays = -46,
		IsNonWorkingDay = false,
	};

	private static NotableDateRule PalmSunday() => new()
	{
		Name = "Palm Sunday",
		Strategy = DateResolutionStrategy.OffsetFromAnchor,
		Category = NotableDateCategory.Religious,
		AnchorRuleName = "Easter Sunday",
		OffsetDays = -7,
		IsNonWorkingDay = false,
	};

	private static NotableDateRule SevenDayFestival() => new()
	{
		Name = "Year-End Festival",
		Strategy = DateResolutionStrategy.Fixed,
		Category = NotableDateCategory.Cultural,
		Month = 12,
		Day = 30,
		DurationDays = 7,
		IsNonWorkingDay = false,
	};

	private static NotableDateRule AustraliaDayFederal() => new()
	{
		Name = "Australia Day",
		Strategy = DateResolutionStrategy.Fixed,
		Category = NotableDateCategory.Holiday,
		Month = 1,
		Day = 26,
		TerritoryCode = "AU",
		IsNonWorkingDay = true,
	};

	private static NotableDateRule QueensBirthdayWA() => new()
	{
		Name = "Queen's Birthday",
		Strategy = DateResolutionStrategy.DayOfWeekInMonth,
		Category = NotableDateCategory.Holiday,
		Month = 9,
		DayOfWeek = DayOfWeek.Monday,
		WeekOrdinal = WeekOfMonthOrdinal.Last,
		TerritoryCode = "AU-WA",
		IsNonWorkingDay = true,
	};

	private static NotableDateRule LeapYearOnlyEvent() => new()
	{
		Name = "Leap Year Special",
		Strategy = DateResolutionStrategy.Fixed,
		Category = NotableDateCategory.Observance,
		Month = 2,
		Day = 29,
	};

	private static NotableDateRule QuadrennialEvent() => new()
	{
		Name = "Olympic Year",
		Strategy = DateResolutionStrategy.Fixed,
		Category = NotableDateCategory.Observance,
		Month = 7,
		Day = 1,
		FirstYear = 2024,
		OccurrenceYears = 4,
	};

	private static NotableDateRule BoundedRule() => new()
	{
		Name = "Centennial Programme",
		Strategy = DateResolutionStrategy.Fixed,
		Category = NotableDateCategory.Civic,
		Month = 5,
		Day = 1,
		FirstYear = 2020,
		LastYear = 2025,
	};

	// =====================================================================================================================
	// Fixed-date emission and window boundaries
	// =====================================================================================================================

	/// <summary>
	/// Verifies that a fixed-date rule whose anchor lies in the requested window is emitted with its base date intact and no
	/// adjustment reason.
	/// </summary>
	[TestMethod]
	public void Resolve_WhenFixedDateAnchorIsInsideWindow_ShouldEmitBaseDate()
	{
		NotableDateService service = BuildService(ChristmasDay());

		IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
			new DateTime(2026, 12, 1),
			new DateTime(2026, 12, 31));

		NotableDate match = resolved.Single(n => n.Name == "Christmas Day");
		Assert.AreEqual(new DateTime(2026, 12, 25), match.Date);
		Assert.IsFalse(match.WasAdjusted);
	}

	/// <summary>
	/// Verifies that a fixed-date rule whose anchor lies outside the requested window is not emitted.
	/// </summary>
	[TestMethod]
	public void Resolve_WhenFixedDateAnchorIsOutsideWindow_ShouldNotEmit()
	{
		NotableDateService service = BuildService(ChristmasDay());

		IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
			new DateTime(2026, 6, 1),
			new DateTime(2026, 6, 30));

		Assert.IsFalse(resolved.Any(n => n.Name == "Christmas Day"));
	}

	/// <summary>
	/// Verifies that a fixed-date rule whose anchor is exactly the requested start date is emitted (inclusive boundary).
	/// </summary>
	[TestMethod]
	public void Resolve_WhenFixedDateAnchorIsExactlyTheWindowStart_ShouldEmit()
	{
		NotableDateService service = BuildService(ChristmasDay());

		IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
			new DateTime(2026, 12, 25),
			new DateTime(2026, 12, 31));

		Assert.IsTrue(resolved.Any(n => n.Name == "Christmas Day" && n.Date == new DateTime(2026, 12, 25)));
	}

	/// <summary>
	/// Verifies that a fixed-date rule whose anchor is exactly the requested end date is emitted (inclusive boundary).
	/// </summary>
	[TestMethod]
	public void Resolve_WhenFixedDateAnchorIsExactlyTheWindowEnd_ShouldEmit()
	{
		NotableDateService service = BuildService(ChristmasDay());

		IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
			new DateTime(2026, 12, 1),
			new DateTime(2026, 12, 25));

		Assert.IsTrue(resolved.Any(n => n.Name == "Christmas Day" && n.Date == new DateTime(2026, 12, 25)));
	}

	/// <summary>
	/// Verifies that a fixed-date rule whose anchor falls on a weekday and whose adjustment trigger does not activate is
	/// emitted with no <see cref="NotableDate.AdjustmentReason" />.
	/// </summary>
	[TestMethod]
	public void Resolve_WhenAdjustmentTriggerDoesNotActivate_ShouldEmitBaseDateWithoutAdjustmentReason()
	{
		// 25 Dec 2024 = Wednesday. The IfWeekend trigger does not fire, so the rule emits its plain anchor date.
		NotableDateService service = BuildService(ChristmasDayWithWeekendSubstitute());

		IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
			new DateTime(2024, 12, 25),
			new DateTime(2024, 12, 25));

		NotableDate match = resolved.Single();
		Assert.AreEqual(new DateTime(2024, 12, 25), match.Date);
		Assert.IsFalse(match.WasAdjusted);
		Assert.IsNull(match.AdjustmentReason);
	}

	// =====================================================================================================================
	// Weekend roll-forward / roll-backward adjustments
	// =====================================================================================================================

	/// <summary>
	/// Verifies that a Sunday Christmas Day rolls to the next non-working day (Monday), the adjusted occurrence supersedes the
	/// base, and the <see cref="AdjustmentReason.OriginalDate" /> records the suppressed Sunday anchor.
	/// </summary>
	[TestMethod]
	public void Resolve_WhenChristmasFallsOnSunday_ShouldEmitMondaySubstituteOnly()
	{
		// 25 Dec 2022 = Sunday → roll forward to Mon 26 Dec 2022.
		NotableDateService service = BuildService(ChristmasDayWithWeekendSubstitute());

		IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
			new DateTime(2022, 12, 24),
			new DateTime(2022, 12, 28));

		NotableDate substitute = resolved.Single(n => n.Name == "Christmas Day");
		Assert.AreEqual(new DateTime(2022, 12, 26), substitute.Date);
		Assert.IsTrue(substitute.WasAdjusted);
		Assert.AreEqual(new DateTime(2022, 12, 25), substitute.AdjustmentReason!.OriginalDate);

		// The original Sunday anchor is suppressed by the adjustment.
		Assert.IsFalse(resolved.Any(n => n.Name == "Christmas Day" && n.Date == new DateTime(2022, 12, 25)));
	}

	/// <summary>
	/// Verifies that a Saturday Christmas Day rolls forward past the weekend to the following Monday.
	/// </summary>
	[TestMethod]
	public void Resolve_WhenChristmasFallsOnSaturday_ShouldEmitMondaySubstituteOnly()
	{
		// 25 Dec 2021 = Saturday → walk Sun → Mon 27 Dec 2021.
		NotableDateService service = BuildService(ChristmasDayWithWeekendSubstitute());

		IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
			new DateTime(2021, 12, 25),
			new DateTime(2021, 12, 31));

		NotableDate substitute = resolved.Single(n => n.Name == "Christmas Day");
		Assert.AreEqual(new DateTime(2021, 12, 27), substitute.Date);
		Assert.IsTrue(substitute.WasAdjusted);
		Assert.AreEqual(new DateTime(2021, 12, 25), substitute.AdjustmentReason!.OriginalDate);
	}

	/// <summary>
	/// Verifies that two adjacent fixed-date holidays whose weekend substitutes would otherwise collide are allocated distinct
	/// observed days when each rule's <see cref="AdjustmentAction.MoveToNextNonWorkingDay" /> walk skips the other holiday
	/// already in the cache.
	/// </summary>
	[TestMethod]
	public void Resolve_WhenChristmasIsSaturdayAndBoxingDayIsSunday_ShouldAllocateMondayAndTuesdaySubstitutes()
	{
		// 25 Dec 2021 = Saturday, 26 Dec 2021 = Sunday. Christmas walks Sun → Mon 27 Dec. Boxing Day walks
		// (already-blocker Sun) → (already-blocker Mon, taken by Christmas substitute) → Tue 28 Dec.
		NotableDateService service = BuildService(
			ChristmasDayWithWeekendSubstitute(),
			BoxingDayFixedWithWeekendSubstitute());

		IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
			new DateTime(2021, 12, 25),
			new DateTime(2021, 12, 31));

		NotableDate christmasSubstitute = resolved.Single(n => n.Name == "Christmas Day");
		Assert.AreEqual(new DateTime(2021, 12, 27), christmasSubstitute.Date);
		Assert.IsTrue(christmasSubstitute.WasAdjusted);

		NotableDate boxingSubstitute = resolved.Single(n => n.Name == "Boxing Day");
		Assert.AreEqual(new DateTime(2021, 12, 28), boxingSubstitute.Date);
		Assert.IsTrue(boxingSubstitute.WasAdjusted);
	}

	/// <summary>
	/// Verifies that a custom <see cref="AdjustmentAction.AddDays" /> adjustment with a negative offset rolls the date backward
	/// when its trigger fires.
	/// </summary>
	[TestMethod]
	public void Resolve_WhenAdjustmentRollsDateBackward_ShouldEmitEarlierObservedDate()
	{
		NotableDateRule rule = new()
		{
			Name = "Backward Rolling Holiday",
			Strategy = DateResolutionStrategy.Fixed,
			Category = NotableDateCategory.Holiday,
			Month = 7,
			Day = 5,
			IsNonWorkingDay = true,
			Adjustments = ImmutableArray.Create(new ObservanceAdjustment
			{
				Key = "back-2",
				Trigger = AdjustmentTrigger.IfDayOfWeek,
				DayOfWeek = DayOfWeek.Sunday,
				Action = AdjustmentAction.AddDays,
				OffsetDays = -2,
				IsNonWorkingDay = true,
			}),
		};

		// 5 Jul 2026 = Sunday → IfDayOfWeek trigger fires, AddDays(-2) lands on Fri 3 Jul 2026.
		NotableDateService service = BuildService(rule);

		IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
			new DateTime(2026, 7, 1),
			new DateTime(2026, 7, 10));

		NotableDate match = resolved.Single(n => n.Name == "Backward Rolling Holiday");
		Assert.AreEqual(new DateTime(2026, 7, 3), match.Date);
		Assert.IsTrue(match.WasAdjusted);
		Assert.AreEqual(new DateTime(2026, 7, 5), match.AdjustmentReason!.OriginalDate);
	}

	// =====================================================================================================================
	// Algorithmic anchors and offset-from-algorithmic descendants
	// =====================================================================================================================

	/// <summary>
	/// Verifies that <see cref="DateResolutionStrategy.Algorithm" /> rules whose computed date intersects the request window are
	/// emitted with their algorithm-computed anchor date.
	/// </summary>
	[TestMethod]
	public void Resolve_WhenAlgorithmicAnchorIsInsideWindow_ShouldEmitAtComputedDate()
	{
		// Easter Sunday 2026 = 5 April 2026.
		NotableDateService service = BuildService(EasterSunday());

		IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
			new DateTime(2026, 4, 1),
			new DateTime(2026, 4, 30));

		NotableDate easter = resolved.Single(n => n.Name == "Easter Sunday");
		Assert.AreEqual(new DateTime(2026, 4, 5), easter.Date);
	}

	/// <summary>
	/// Verifies that a request that contains every Easter-derived offset rule emits all of them with their correct projected
	/// dates while the algorithmic anchor itself is invoked exactly once for the year.
	/// </summary>
	[TestMethod]
	public void Resolve_WhenWindowContainsEntireEasterCycle_ShouldEmitAllDerivativesAndAnchor()
	{
		// 2026 Easter cycle:
		//   Lent             Easter − 46 = 18 Feb 2026
		//   Palm Sunday      Easter − 7  = 29 Mar 2026
		//   Good Friday      Easter − 2  = 3 Apr 2026
		//   Easter Sunday    algorithm   = 5 Apr 2026
		//   Easter Monday    Easter + 1  = 6 Apr 2026
		NotableDateService service = BuildService(
			EasterSunday(),
			StartOfLent(),
			PalmSunday(),
			GoodFriday(),
			EasterMonday());

		IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
			new DateTime(2026, 2, 1),
			new DateTime(2026, 4, 30));

		AssertSingleEmittedOn(resolved, "Start of Lent", new DateTime(2026, 2, 18));
		AssertSingleEmittedOn(resolved, "Palm Sunday", new DateTime(2026, 3, 29));
		AssertSingleEmittedOn(resolved, "Good Friday", new DateTime(2026, 4, 3));
		AssertSingleEmittedOn(resolved, "Easter Sunday", new DateTime(2026, 4, 5));
		AssertSingleEmittedOn(resolved, "Easter Monday", new DateTime(2026, 4, 6));
	}

	/// <summary>
	/// Verifies that a request that intersects only one Easter derivative (Palm Sunday) emits only that derivative even when the
	/// other derivatives and the algorithmic anchor are computed internally for the same year.
	/// </summary>
	[TestMethod]
	public void Resolve_WhenWindowOnlyContainsPalmSunday_ShouldEmitPalmSundayOnly()
	{
		// Palm Sunday 2026 = 29 Mar 2026. Lent (18 Feb), Good Friday (3 Apr), Easter (5 Apr) and Easter Monday (6 Apr) all sit
		// outside the late-March window.
		NotableDateService service = BuildService(
			EasterSunday(),
			StartOfLent(),
			PalmSunday(),
			GoodFriday(),
			EasterMonday());

		IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
			new DateTime(2026, 3, 25),
			new DateTime(2026, 3, 31));

		Assert.AreEqual(1, resolved.Count);
		AssertSingleEmittedOn(resolved, "Palm Sunday", new DateTime(2026, 3, 29));
	}

	// =====================================================================================================================
	// Offset from fixed
	// =====================================================================================================================

	/// <summary>
	/// Verifies that an offset-from-fixed rule (Boxing Day = Christmas Day + 1) is emitted alongside its anchor when both fall
	/// in the requested window.
	/// </summary>
	[TestMethod]
	public void Resolve_WhenOffsetFromFixedAndAnchorAreBothInWindow_ShouldEmitBoth()
	{
		NotableDateService service = BuildService(ChristmasDay(), BoxingDayOffsetFromChristmas());

		IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
			new DateTime(2026, 12, 25),
			new DateTime(2026, 12, 27));

		AssertSingleEmittedOn(resolved, "Christmas Day", new DateTime(2026, 12, 25));
		AssertSingleEmittedOn(resolved, "Boxing Day", new DateTime(2026, 12, 26));
	}

	// =====================================================================================================================
	// Multi-day spans
	// =====================================================================================================================

	/// <summary>
	/// Verifies that a multi-day span entirely contained within the request is emitted once with the correct duration metadata.
	/// </summary>
	[TestMethod]
	public void Resolve_WhenMultiDaySpanFullyInsideWindow_ShouldEmitWithSpanIntact()
	{
		NotableDateService service = BuildService(SevenDayFestival());

		IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
			new DateTime(2026, 12, 28),
			new DateTime(2027, 1, 10));

		NotableDate festival = resolved.Single(n => n.Name == "Year-End Festival");
		Assert.AreEqual(new DateTime(2026, 12, 30), festival.Date);
		Assert.AreEqual(7, festival.DurationDays);
		Assert.AreEqual(new DateTime(2027, 1, 5), festival.EndDate);
	}

	/// <summary>
	/// Verifies that a multi-day span whose anchor lies before the requested window but whose tail extends into it is still
	/// emitted, with the anchor preserved on <see cref="NotableDate.Date" />.
	/// </summary>
	[TestMethod]
	public void Resolve_WhenMultiDaySpanStartsBeforeWindowAndExtendsIntoIt_ShouldEmitFromAnchorYear()
	{
		// 7-day festival starting 30 Dec 2025 ends 5 Jan 2026. Request a January 2026 window — only the tail intersects.
		NotableDateService service = BuildService(SevenDayFestival());

		IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
			new DateTime(2026, 1, 1),
			new DateTime(2026, 1, 31));

		NotableDate festival = resolved.Single(n => n.Name == "Year-End Festival");
		Assert.AreEqual(new DateTime(2025, 12, 30), festival.Date);
		Assert.AreEqual(new DateTime(2026, 1, 5), festival.EndDate);
	}

	// =====================================================================================================================
	// Calendar applicability — FirstYear / LastYear / OccurrenceYears
	// =====================================================================================================================

	/// <summary>
	/// Verifies that a rule constrained by <see cref="NotableDateRule.FirstYear" /> and <see cref="NotableDateRule.LastYear" />
	/// is not emitted for a year outside the bound.
	/// </summary>
	[TestMethod]
	public void Resolve_WhenRuleIsBoundedAndQueryYearIsAfterLastYear_ShouldNotEmit()
	{
		NotableDateService service = BuildService(BoundedRule());

		IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
			new DateTime(2030, 5, 1),
			new DateTime(2030, 5, 31));

		Assert.IsFalse(resolved.Any(n => n.Name == "Centennial Programme"));
	}

	/// <summary>
	/// Verifies that the same bounded rule emits for a year inside its valid range.
	/// </summary>
	[TestMethod]
	public void Resolve_WhenRuleIsBoundedAndQueryYearIsInsideRange_ShouldEmit()
	{
		NotableDateService service = BuildService(BoundedRule());

		IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
			new DateTime(2024, 5, 1),
			new DateTime(2024, 5, 31));

		AssertSingleEmittedOn(resolved, "Centennial Programme", new DateTime(2024, 5, 1));
	}

	/// <summary>
	/// Verifies that <see cref="NotableDateRule.OccurrenceYears" /> is honoured — a quadrennial rule does not emit on years
	/// that do not match the cadence offset from <see cref="NotableDateRule.FirstYear" />.
	/// </summary>
	[TestMethod]
	public void Resolve_WhenRuleHasQuadrennialCadenceAndQueryYearIsOffCadence_ShouldNotEmit()
	{
		// Olympic Year cadence anchored at 2024 with OccurrenceYears = 4 → emits in 2024, 2028, 2032 …
		NotableDateService service = BuildService(QuadrennialEvent());

		IReadOnlyList<NotableDate> resolvedOnCadence = service.ResolveNotableDatesInRange(
			new DateTime(2024, 7, 1),
			new DateTime(2024, 7, 31));
		Assert.IsTrue(resolvedOnCadence.Any(n => n.Name == "Olympic Year"));

		IReadOnlyList<NotableDate> resolvedOffCadence = service.ResolveNotableDatesInRange(
			new DateTime(2025, 7, 1),
			new DateTime(2025, 7, 31));
		Assert.IsFalse(resolvedOffCadence.Any(n => n.Name == "Olympic Year"));
	}

	/// <summary>
	/// Verifies that a fixed-date rule on 29 February resolves only in leap years.
	/// </summary>
	[TestMethod]
	public void Resolve_WhenFixedDateIsLeapDayAndYearIsNotALeapYear_ShouldNotEmit()
	{
		NotableDateService service = BuildService(LeapYearOnlyEvent());

		IReadOnlyList<NotableDate> leap = service.ResolveNotableDatesInRange(
			new DateTime(2024, 2, 1),
			new DateTime(2024, 3, 1));
		Assert.IsTrue(leap.Any(n => n.Name == "Leap Year Special" && n.Date == new DateTime(2024, 2, 29)));

		IReadOnlyList<NotableDate> nonLeap = service.ResolveNotableDatesInRange(
			new DateTime(2025, 2, 1),
			new DateTime(2025, 3, 1));
		Assert.IsFalse(nonLeap.Any(n => n.Name == "Leap Year Special"));
	}

	// =====================================================================================================================
	// DayOfWeekInMonth
	// =====================================================================================================================

	/// <summary>
	/// Verifies that a <see cref="DateResolutionStrategy.DayOfWeekInMonth" /> rule (last Monday of September) is resolved
	/// against the actual calendar layout.
	/// </summary>
	[TestMethod]
	public void Resolve_WhenDayOfWeekInMonthRuleIsRequested_ShouldEmitOnTheCorrectCalendarDate()
	{
		// Last Monday of September 2026 = 28 Sep 2026.
		NotableDateService service = BuildService(QueensBirthdayWA());

		IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
			new DateTime(2026, 9, 1),
			new DateTime(2026, 9, 30),
			territoryCode: "AU-WA");

		AssertSingleEmittedOn(resolved, "Queen's Birthday", new DateTime(2026, 9, 28));
	}

	// =====================================================================================================================
	// Territory containment
	// =====================================================================================================================

	/// <summary>
	/// Verifies that a query for the parent territory <c>AU</c> includes a rule scoped to the parent territory.
	/// </summary>
	[TestMethod]
	public void Resolve_WhenRequestTerritoryMatchesAuthoredTerritory_ShouldEmit()
	{
		NotableDateService service = BuildService(AustraliaDayFederal());

		IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
			new DateTime(2026, 1, 1),
			new DateTime(2026, 1, 31),
			territoryCode: "AU");

		AssertSingleEmittedOn(resolved, "Australia Day", new DateTime(2026, 1, 26));
	}

	/// <summary>
	/// Verifies that a child-territory query (<c>AU-NSW</c>) includes a rule scoped to the parent country (<c>AU</c>) under
	/// parent / child containment semantics.
	/// </summary>
	[TestMethod]
	public void Resolve_WhenRequestTerritoryIsChildOfAuthoredTerritory_ShouldEmit()
	{
		NotableDateService service = BuildService(AustraliaDayFederal());

		IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
			new DateTime(2026, 1, 1),
			new DateTime(2026, 1, 31),
			territoryCode: "AU-NSW");

		AssertSingleEmittedOn(resolved, "Australia Day", new DateTime(2026, 1, 26));
	}

	/// <summary>
	/// Verifies that a parent-territory query (<c>AU</c>) includes a rule scoped to a child subdivision (<c>AU-WA</c>) under
	/// parent / child containment semantics.
	/// </summary>
	[TestMethod]
	public void Resolve_WhenRequestTerritoryIsParentOfAuthoredTerritory_ShouldEmit()
	{
		NotableDateService service = BuildService(QueensBirthdayWA());

		IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
			new DateTime(2026, 9, 1),
			new DateTime(2026, 9, 30),
			territoryCode: "AU");

		Assert.IsTrue(resolved.Any(n => n.Name == "Queen's Birthday"));
	}

	/// <summary>
	/// Verifies that a query for an unrelated territory does not match a rule scoped to a different country.
	/// </summary>
	[TestMethod]
	public void Resolve_WhenRequestTerritoryIsUnrelatedToAuthoredTerritory_ShouldNotEmit()
	{
		NotableDateService service = BuildService(AustraliaDayFederal());

		IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
			new DateTime(2026, 1, 1),
			new DateTime(2026, 1, 31),
			territoryCode: "GB");

		Assert.IsFalse(resolved.Any(n => n.Name == "Australia Day"));
	}

	// =====================================================================================================================
	// Filter
	// =====================================================================================================================

	/// <summary>
	/// Verifies that a category filter restricts the emitted set to rules whose category matches the predicate.
	/// </summary>
	[TestMethod]
	public void Resolve_WhenFilterRestrictsByCategory_ShouldOnlyEmitMatchingCategory()
	{
		NotableDateService service = BuildService(
			ChristmasDay(),
			EasterSunday(),
			PalmSunday(),
			GoodFriday());

		IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
			new DateTime(2026, 1, 1),
			new DateTime(2026, 12, 31),
			filter: NotableDateFilter.ForCategory(NotableDateCategory.Religious));

		Assert.IsFalse(resolved.Any(n => n.Category == NotableDateCategory.Holiday),
			"Holiday-category Christmas Day must be excluded by the religious-only filter.");
		Assert.IsTrue(resolved.All(n => n.Category == NotableDateCategory.Religious));
		Assert.IsTrue(resolved.Any(n => n.Name == "Easter Sunday"));
		Assert.IsTrue(resolved.Any(n => n.Name == "Palm Sunday"));
		Assert.IsTrue(resolved.Any(n => n.Name == "Good Friday"));
	}

	// =====================================================================================================================
	// New Year and full calendar coverage smoke test
	// =====================================================================================================================

	/// <summary>
	/// Verifies that a full-year request returns every emission once and only once when the rule set contains a mix of fixed,
	/// algorithmic, offset, and territory-scoped rules.
	/// </summary>
	[TestMethod]
	public void Resolve_WhenWholeYearRequestedWithMixedRuleSet_ShouldEmitEverythingExactlyOnce()
	{
		NotableDateService service = BuildService(
			NewYearsDayWithWeekendSubstitute(),
			AustraliaDayFederal(),
			GoodFriday(),
			EasterSunday(),
			EasterMonday(),
			QueensBirthdayWA(),
			ChristmasDay(),
			BoxingDayOffsetFromChristmas());

		IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
			new DateTime(2026, 1, 1),
			new DateTime(2026, 12, 31),
			territoryCode: "AU-WA");

		// Each rule should appear at most once for AU-WA across a single year.
		string[] names = resolved.Select(n => n.Name).ToArray();
		CollectionAssert.AllItemsAreUnique(names);

		Assert.IsTrue(resolved.Any(n => n.Name == "New Year's Day"));
		Assert.IsTrue(resolved.Any(n => n.Name == "Australia Day"));
		Assert.IsTrue(resolved.Any(n => n.Name == "Good Friday"));
		Assert.IsTrue(resolved.Any(n => n.Name == "Easter Sunday"));
		Assert.IsTrue(resolved.Any(n => n.Name == "Easter Monday"));
		Assert.IsTrue(resolved.Any(n => n.Name == "Queen's Birthday"));
		Assert.IsTrue(resolved.Any(n => n.Name == "Christmas Day"));
		Assert.IsTrue(resolved.Any(n => n.Name == "Boxing Day"));
	}

	// =====================================================================================================================
	// Helpers
	// =====================================================================================================================

	private const string EasterAlgorithmKey = "easter-sunday";

	private static NotableDateService BuildService(params NotableDateRule[] rules)
	{
		bool needsAlgorithm = rules.Any(r => r.Strategy == DateResolutionStrategy.Algorithm);
		NotableDateAlgorithmRegistry? registry = needsAlgorithm
			? new NotableDateAlgorithmRegistry().Register(EasterAlgorithmKey, new GregorianEasterSundayAlgorithm())
			: null;

		return new NotableDateService(
			ruleProviders: new[] { (INotableDateRuleProvider)new InMemoryRuleProvider(rules) },
			weekendDefinition: CalendarWeekendDefinition.SaturdaySunday,
			options: new NotableDateServiceOptions { AlgorithmRegistry = registry });
	}

	private static void AssertSingleEmittedOn(IReadOnlyList<NotableDate> resolved, string name, DateTime expectedDate)
	{
		NotableDate match = resolved.SingleOrDefault(n => n.Name == name)
			?? throw new AssertFailedException(
				$"Expected exactly one emission of {name}; got: {string.Join(", ", resolved.Where(n => n.Name == name).Select(n => n.Date.ToString("yyyy-MM-dd")))}.");

		Assert.AreEqual(expectedDate, match.Date,
			$"{name} should be emitted on {expectedDate:yyyy-MM-dd} but was emitted on {match.Date:yyyy-MM-dd}.");
	}

	private sealed class InMemoryRuleProvider : INotableDateRuleProvider
	{
		private readonly IReadOnlyList<NotableDateRule> _rules;

		public InMemoryRuleProvider(params NotableDateRule[] rules)
		{
			_rules = rules;
		}

		public IEnumerable<NotableDateRule> LoadRules() => _rules;
	}

	/// <summary>
	/// Anonymous Gregorian Easter Sunday algorithm (Meeus / Jones / Butcher).
	/// </summary>
	private sealed class GregorianEasterSundayAlgorithm : INotableDateAlgorithm
	{
		public DateTime? GetDate(int year, System.Globalization.Calendar? calendar = null)
		{
			int a = year % 19;
			int b = year / 100;
			int c = year % 100;
			int d = b / 4;
			int e = b % 4;
			int f = (b + 8) / 25;
			int g = (b - f + 1) / 3;
			int h = ((19 * a) + b - d - g + 15) % 30;
			int i = c / 4;
			int k = c % 4;
			int l = (32 + (2 * e) + (2 * i) - h - k) % 7;
			int m = (a + (11 * h) + (22 * l)) / 451;
			int month = (h + l - (7 * m) + 114) / 31;
			int day = ((h + l - (7 * m) + 114) % 31) + 1;

			return new DateTime(year, month, day);
		}
	}
}
