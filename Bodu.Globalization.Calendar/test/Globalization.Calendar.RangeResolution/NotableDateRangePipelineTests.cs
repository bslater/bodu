// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRangePipelineTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Immutable;
using Bodu.Extensions;
using Bodu.Globalization.Calendar;

namespace Bodu.Globalization.Calendar.RangeResolution;

/// <summary>
/// Integration tests for the prototype <see cref="NotableDateRangePipeline" /> covering the two motivating scenarios:
/// successor offset (<em>Start of Lent</em> from <em>Easter Sunday</em> in a Q1 window) and prior-year roll-forward
/// (<em>31 December</em> rolling into <em>3 January</em> past blockers).
/// </summary>
[TestClass]
public sealed class NotableDateRangePipelineTests
{
	/// <summary>
	/// Verifies that a Q1 window resolves <em>Start of Lent</em> via its <em>Easter Sunday</em> algorithmic anchor without
	/// emitting Easter itself when Easter falls outside the window.
	/// </summary>
	[TestMethod]
	public void Resolve_WhenWindowContainsLentButNotEaster_ShouldEmitLentOnly()
	{
		// Easter Sunday 2026 = 5 April 2026. Lent = Easter − 46 = 18 February 2026.
		NotableDateRule easter = new()
		{
			Name = "Easter Sunday",
			Strategy = DateResolutionStrategy.Algorithm,
			Category = NotableDateCategory.Religious,
			AlgorithmKey = "easter-sunday",
		};

		NotableDateRule lent = new()
		{
			Name = "Start of Lent",
			Strategy = DateResolutionStrategy.OffsetFromAnchor,
			Category = NotableDateCategory.Religious,
			AnchorRuleName = "Easter Sunday",
			OffsetDays = -46,
		};

		NotableDateRule palmSunday = new()
		{
			Name = "Palm Sunday",
			Strategy = DateResolutionStrategy.OffsetFromAnchor,
			Category = NotableDateCategory.Religious,
			AnchorRuleName = "Easter Sunday",
			OffsetDays = -7,
		};

		NotableDateService service = new(
			ruleProviders: new[] { (INotableDateRuleProvider)new InMemoryRuleProvider(easter, lent, palmSunday) },
			weekendDefinition: CalendarWeekendDefinition.SaturdaySunday,
			algorithmRegistry: new NotableDateAlgorithmRegistry()
				.Register("easter-sunday", new GregorianEasterSundayAlgorithm()));

		IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
			new DateTime(2026, 1, 1),
			new DateTime(2026, 3, 31));

		Assert.IsTrue(resolved.Any(n => n.Name == "Start of Lent" && n.Date == new DateTime(2026, 2, 18)),
			"Expected Start of Lent on 18 Feb 2026 to be emitted.");

		Assert.IsTrue(resolved.Any(n => n.Name == "Palm Sunday" && n.Date == new DateTime(2026, 3, 29)),
			"Expected Palm Sunday on 29 Mar 2026 to be emitted.");

		Assert.IsFalse(resolved.Any(n => n.Name == "Easter Sunday"),
			"Easter Sunday is on 5 Apr 2026 and must not be emitted for a window ending 31 Mar 2026.");
	}

	/// <summary>
	/// Verifies that a request limited to <em>3 January</em> resolves <em>Year-End Holiday</em> (Dec 31, prior year) when its
	/// roll-forward chain skips <em>1 January</em> (weekend) and <em>2 January</em> (non-working blocker) to land on Jan 3.
	/// </summary>
	[TestMethod]
	public void Resolve_WhenPriorYearHolidayRollsForwardPastBlocker_ShouldEmitObservedDateOnJanuaryThird()
	{
		// 31 Dec 2022 was a Saturday. With "2 January" also flagged as non-working, the snap chain should land on Mon 3 Jan 2023.
		NotableDateRule yearEnd = new()
		{
			Name = "Year-End Holiday",
			Strategy = DateResolutionStrategy.Fixed,
			Category = NotableDateCategory.Holiday,
			Month = 12,
			Day = 31,
			IsNonWorkingDay = true,
			Adjustments = ImmutableArray.Create(new ObservanceAdjustment
			{
				Key = "weekend-substitute",
				Trigger = AdjustmentTrigger.IfWeekend,
				Action = AdjustmentAction.MoveToNextNonWorkingDay,
				IsNonWorkingDay = true,
			}),
		};

		NotableDateRule secondJan = new()
		{
			Name = "New Year Second Day",
			Strategy = DateResolutionStrategy.Fixed,
			Category = NotableDateCategory.Holiday,
			Month = 1,
			Day = 2,
			IsNonWorkingDay = true,
		};

		NotableDateService service = new(
			ruleProviders: new[] { (INotableDateRuleProvider)new InMemoryRuleProvider(yearEnd, secondJan) },
			weekendDefinition: CalendarWeekendDefinition.SaturdaySunday);

		IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
			new DateTime(2023, 1, 3),
			new DateTime(2023, 1, 3));

		NotableDate adjusted = resolved.SingleOrDefault(n => n.Name == "Year-End Holiday" && n.Date == new DateTime(2023, 1, 3))
			?? throw new AssertFailedException("Expected adjusted Year-End Holiday on 3 Jan 2023.");

		Assert.IsTrue(adjusted.WasAdjusted, "Adjusted occurrence should carry an AdjustmentReason.");
		Assert.IsNotNull(adjusted.AdjustmentReason);
		Assert.AreEqual(new DateTime(2022, 12, 31), adjusted.AdjustmentReason.OriginalDate);

		Assert.IsFalse(resolved.Any(n => n.Name == "New Year Second Day"),
			"The 2 Jan blocker is outside the requested window and must not be emitted.");

		Assert.IsFalse(resolved.Any(n => n.Date == new DateTime(2022, 12, 31)),
			"The original 31 Dec anchor lies outside the requested window and must not be emitted as a base date.");
	}

	/// <summary>
	/// Verifies that a request whose window is entirely outside any rule's projection produces no results.
	/// </summary>
	[TestMethod]
	public void Resolve_WhenNoRuleProjectsIntoWindow_ShouldReturnEmpty()
	{
		NotableDateRule christmas = new()
		{
			Name = "Christmas Day",
			Strategy = DateResolutionStrategy.Fixed,
			Category = NotableDateCategory.Holiday,
			Month = 12,
			Day = 25,
			IsNonWorkingDay = true,
		};

		NotableDateService service = new(
			ruleProviders: new[] { (INotableDateRuleProvider)new InMemoryRuleProvider(christmas) },
			weekendDefinition: CalendarWeekendDefinition.SaturdaySunday);

		IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
			new DateTime(2026, 6, 1),
			new DateTime(2026, 6, 30));

		Assert.AreEqual(0, resolved.Count);
	}

	/// <summary>
	/// Verifies that a fixed-date rule whose anchor lies inside the requested window is emitted with its base date intact.
	/// </summary>
	[TestMethod]
	public void Resolve_WhenFixedRuleAnchorIsInsideWindow_ShouldEmitBaseDate()
	{
		NotableDateRule christmas = new()
		{
			Name = "Christmas Day",
			Strategy = DateResolutionStrategy.Fixed,
			Category = NotableDateCategory.Holiday,
			Month = 12,
			Day = 25,
			IsNonWorkingDay = true,
		};

		NotableDateService service = new(
			ruleProviders: new[] { (INotableDateRuleProvider)new InMemoryRuleProvider(christmas) },
			weekendDefinition: CalendarWeekendDefinition.SaturdaySunday);

		IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
			new DateTime(2026, 12, 1),
			new DateTime(2026, 12, 31));

		NotableDate match = resolved.Single(n => n.Name == "Christmas Day");
		Assert.AreEqual(new DateTime(2026, 12, 25), match.Date);
		Assert.IsFalse(match.WasAdjusted);
	}

	/// <summary>
	/// Verifies that a request whose window covers both the original Dec 31 anchor and its rolled-forward Jan 3 observance
	/// returns only the adjusted observance — adjusted dates supersede the base anchor so the emitted count equals the
	/// notable-date count.
	/// </summary>
	[TestMethod]
	public void Resolve_WhenWindowSpansBothAnchorAndAdjustedObservance_ShouldEmitOnlyAdjustedDate()
	{
		NotableDateRule yearEnd = new()
		{
			Name = "Year-End Holiday",
			Strategy = DateResolutionStrategy.Fixed,
			Category = NotableDateCategory.Holiday,
			Month = 12,
			Day = 31,
			IsNonWorkingDay = true,
			Adjustments = ImmutableArray.Create(new ObservanceAdjustment
			{
				Key = "weekend-substitute",
				Trigger = AdjustmentTrigger.IfWeekend,
				Action = AdjustmentAction.MoveToNextNonWorkingDay,
				IsNonWorkingDay = true,
			}),
		};

		NotableDateRule secondJan = new()
		{
			Name = "New Year Second Day",
			Strategy = DateResolutionStrategy.Fixed,
			Category = NotableDateCategory.Holiday,
			Month = 1,
			Day = 2,
			IsNonWorkingDay = true,
		};

		NotableDateService service = new(
			ruleProviders: new[] { (INotableDateRuleProvider)new InMemoryRuleProvider(yearEnd, secondJan) },
			weekendDefinition: CalendarWeekendDefinition.SaturdaySunday);

		IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
			new DateTime(2022, 12, 25),
			new DateTime(2023, 1, 5));

		NotableDate[] yearEndOccurrences = resolved.Where(n => n.Name == "Year-End Holiday").ToArray();
		Assert.AreEqual(1, yearEndOccurrences.Length,
			$"An adjusted observance must replace the base anchor; got: {string.Join(", ", yearEndOccurrences.Select(n => $"{n.Date:yyyy-MM-dd}"))}.");

		NotableDate observed = yearEndOccurrences.Single();
		Assert.AreEqual(new DateTime(2023, 1, 3), observed.Date, "The single emitted occurrence must be the adjusted Jan 3 form.");
		Assert.IsTrue(observed.WasAdjusted, "The emitted occurrence must carry an AdjustmentReason.");
		Assert.IsNotNull(observed.AdjustmentReason);
		Assert.AreEqual(new DateTime(2022, 12, 31), observed.AdjustmentReason.OriginalDate,
			"The AdjustmentReason must record the suppressed Dec 31 anchor as the original date.");

		Assert.IsFalse(resolved.Any(n => n.Date == new DateTime(2022, 12, 31)),
			"The Dec 31 anchor is suppressed by its adjustment and must not appear in the output.");
	}

	/// <summary>
	/// Verifies that a single-day query for the rolled-forward Jan 3 observance does not leak the prior-year Dec 31 anchor into
	/// the output even though the anchor was materialised internally to support the adjustment chain.
	/// </summary>
	[TestMethod]
	public void Resolve_WhenSingleDayWindowOnAdjustedDate_ShouldNotLeakAnchorOutsideWindow()
	{
		NotableDateRule yearEnd = new()
		{
			Name = "Year-End Holiday",
			Strategy = DateResolutionStrategy.Fixed,
			Category = NotableDateCategory.Holiday,
			Month = 12,
			Day = 31,
			IsNonWorkingDay = true,
			Adjustments = ImmutableArray.Create(new ObservanceAdjustment
			{
				Key = "weekend-substitute",
				Trigger = AdjustmentTrigger.IfWeekend,
				Action = AdjustmentAction.MoveToNextNonWorkingDay,
				IsNonWorkingDay = true,
			}),
		};

		NotableDateRule secondJan = new()
		{
			Name = "New Year Second Day",
			Strategy = DateResolutionStrategy.Fixed,
			Category = NotableDateCategory.Holiday,
			Month = 1,
			Day = 2,
			IsNonWorkingDay = true,
		};

		NotableDateService service = new(
			ruleProviders: new[] { (INotableDateRuleProvider)new InMemoryRuleProvider(yearEnd, secondJan) },
			weekendDefinition: CalendarWeekendDefinition.SaturdaySunday);

		IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
			new DateTime(2023, 1, 3),
			new DateTime(2023, 1, 3));

		Assert.AreEqual(1, resolved.Count,
			$"Expected exactly one notable date for [3 Jan, 3 Jan]; got: {string.Join(", ", resolved.Select(n => $"{n.Name}@{n.Date:yyyy-MM-dd}"))}.");

		Assert.IsFalse(resolved.Any(n => n.Date.Year != 2023 || n.Date.Month != 1 || n.Date.Day != 3),
			$"Every emitted date must intersect the requested window [3 Jan 2023, 3 Jan 2023].");
	}

	/// <summary>
	/// Verifies that a Tier 2 (OffsetFromFixed) rule with an observance adjustment in a fringe year is materialised by the fringe
	/// pass and that its root anchor is fetched on-demand when the main pass did not load it for that year.
	/// </summary>
	[TestMethod]
	public void Resolve_WhenOffsetFromFixedRuleWithAdjustmentRollsForwardFromFringeYear_ShouldEmitObservedDate()
	{
		// Tier 1 fixed anchor with no adjustment of its own. The fringe pass skips it directly (no adjustment, no multi-day) so
		// reaching the offset rule below requires the on-demand anchor materialisation path.
		NotableDateRule yearEndAnchor = new()
		{
			Name = "Year-End Anchor",
			Strategy = DateResolutionStrategy.Fixed,
			Category = NotableDateCategory.Holiday,
			Month = 12,
			Day = 30,
			IsNonWorkingDay = true,
		};

		// Tier 2 offset-from-fixed: Year-End Anchor + 1, with an unconditional +5 day shift. (anchor 30 Dec) + 1 + 5 = 5 Jan.
		NotableDateRule yearEndBonus = new()
		{
			Name = "Year-End Bonus",
			Strategy = DateResolutionStrategy.OffsetFromAnchor,
			Category = NotableDateCategory.Holiday,
			AnchorRuleName = "Year-End Anchor",
			OffsetDays = 1,
			IsNonWorkingDay = true,
			Adjustments = ImmutableArray.Create(new ObservanceAdjustment
			{
				Key = "shift-5",
				Trigger = AdjustmentTrigger.Always,
				Action = AdjustmentAction.AddDays,
				OffsetDays = 5,
				IsNonWorkingDay = true,
			}),
		};

		NotableDateService service = new(
			ruleProviders: new[] { (INotableDateRuleProvider)new InMemoryRuleProvider(yearEndAnchor, yearEndBonus) },
			weekendDefinition: CalendarWeekendDefinition.SaturdaySunday);

		// Request entirely in 2027 so 2026 is a fringe year. The Tier 2 Year-End Bonus 2026 anchors on Year-End Anchor 2026
		// (Dec 30 2026) which Pass 1 does NOT materialise. The fringe pass must on-demand materialise the anchor before computing
		// the offset and applying the +5 day shift to Jan 5 2027.
		IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
			new DateTime(2027, 1, 4),
			new DateTime(2027, 1, 10));

		NotableDate observed = resolved.SingleOrDefault(n => n.Name == "Year-End Bonus")
			?? throw new AssertFailedException("Expected Year-End Bonus to be emitted via the fringe pass with on-demand anchor materialisation.");

		Assert.AreEqual(new DateTime(2027, 1, 5), observed.Date);
		Assert.IsTrue(observed.WasAdjusted);
		Assert.IsNotNull(observed.AdjustmentReason);
		Assert.AreEqual(new DateTime(2026, 12, 31), observed.AdjustmentReason.OriginalDate);
	}

	/// <summary>
	/// Verifies that a multi-day rule whose anchor sits in an adjacent year and whose span reaches into the requested window is
	/// materialised by the fringe pass and emitted with the correct duration metadata.
	/// </summary>
	[TestMethod]
	public void Resolve_WhenMultiDayRuleSpansAcrossYearBoundaryIntoRequest_ShouldEmitFromAdjacentYear()
	{
		// A 7-day festival starting 30 December — the span runs Dec 30 .. Jan 5 (inclusive).
		NotableDateRule festival = new()
		{
			Name = "Year-End Festival",
			Strategy = DateResolutionStrategy.Fixed,
			Category = NotableDateCategory.Cultural,
			Month = 12,
			Day = 30,
			DurationDays = 7,
			IsNonWorkingDay = false,
		};

		NotableDateService service = new(
			ruleProviders: new[] { (INotableDateRuleProvider)new InMemoryRuleProvider(festival) },
			weekendDefinition: CalendarWeekendDefinition.SaturdaySunday);

		IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
			new DateTime(2026, 1, 1),
			new DateTime(2026, 1, 31));

		NotableDate festivalDate = resolved.SingleOrDefault(n => n.Name == "Year-End Festival")
			?? throw new AssertFailedException("Expected the prior-year festival span to surface inside the January window.");

		Assert.AreEqual(new DateTime(2025, 12, 30), festivalDate.Date,
			"Anchor must remain at Dec 30 of the prior year so consumers can identify the originating day.");
		Assert.AreEqual(7, festivalDate.DurationDays);
		Assert.AreEqual(new DateTime(2026, 1, 5), festivalDate.EndDate);
	}

	/// <summary>
	/// Verifies that the planner's fringe distance widens to accommodate rules with unusually large
	/// <see cref="AdjustmentAction.AddDays" /> shifts so a cross-year adjustment whose result lands inside the requested window is
	/// admitted by the fringe pass even though it sits outside the planner's default 7-day envelope.
	/// </summary>
	[TestMethod]
	public void Resolve_WhenRuleHasLargeCrossYearAddDaysAdjustment_ShouldExtendFringeDistanceToCoverIt()
	{
		// Rule anchored on 1 Dec each year, unconditionally shifts +60 days. 1 Dec + 60 = 30 Jan of the following year. With
		// the planner's default 7-day fringe this would not be admitted; the rule set's GlobalFringeReach must widen the fringe
		// distance to 60 days so the prior-year anchor (1 Dec 2025) becomes a fringe-year candidate for a January 2026 request.
		NotableDateRule shifted = new()
		{
			Name = "Sixty-Day Shifted Holiday",
			Strategy = DateResolutionStrategy.Fixed,
			Category = NotableDateCategory.Holiday,
			Month = 12,
			Day = 1,
			IsNonWorkingDay = true,
			Adjustments = ImmutableArray.Create(new ObservanceAdjustment
			{
				Key = "shift-60",
				Trigger = AdjustmentTrigger.Always,
				Action = AdjustmentAction.AddDays,
				OffsetDays = 60,
				IsNonWorkingDay = true,
			}),
		};

		NotableDateService service = new(
			ruleProviders: new[] { (INotableDateRuleProvider)new InMemoryRuleProvider(shifted) },
			weekendDefinition: CalendarWeekendDefinition.SaturdaySunday);

		// Anchor 1 Dec 2025 + 60 days = 30 Jan 2026. The request ends 5 Feb 2026 so the adjusted date lands inside.
		IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
			new DateTime(2026, 1, 25),
			new DateTime(2026, 2, 5));

		NotableDate match = resolved.SingleOrDefault(n => n.Name == "Sixty-Day Shifted Holiday")
			?? throw new AssertFailedException("A +60 day adjustment should be admitted by the fringe pass when its envelope reaches the request.");

		Assert.AreEqual(new DateTime(2026, 1, 30), match.Date);
		Assert.IsTrue(match.WasAdjusted);
		Assert.AreEqual(new DateTime(2025, 12, 1), match.AdjustmentReason!.OriginalDate);
	}

	/// <summary>
	/// Verifies that a custom <see cref="AdjustmentAction.Custom" /> handler that shifts further than the default ±31 day envelope
	/// is admitted by the fringe pass when the rule declares
	/// <see cref="ObservanceAdjustment.MaxAdjustmentReachDays" /> explicitly.
	/// </summary>
	[TestMethod]
	public void Resolve_WhenCustomHandlerExceedsDefaultEnvelopeAndDeclaresExplicitReach_ShouldEmitFromAdjacentYear()
	{
		// Custom handler that always shifts +90 days. Declared via MaxAdjustmentReachDays = 90 so the planner widens the fringe.
		const int shiftDays = 90;

		AdjustmentHandlerRegistry handlers = new();
		handlers.Register("ninety-day-shift", new ShiftByDaysHandler(shiftDays));

		NotableDateRule rule = new()
		{
			Name = "Long-Reach Holiday",
			Strategy = DateResolutionStrategy.Fixed,
			Category = NotableDateCategory.Holiday,
			Month = 11,
			Day = 1,
			IsNonWorkingDay = true,
			Adjustments = ImmutableArray.Create(new ObservanceAdjustment
			{
				Key = "ninety-day-shift",
				Trigger = AdjustmentTrigger.Custom,
				Action = AdjustmentAction.Custom,
				HandlerKey = "ninety-day-shift",
				MaxAdjustmentReachDays = shiftDays,
				IsNonWorkingDay = true,
			}),
		};

		NotableDateService service = new(
			ruleProviders: new[] { (INotableDateRuleProvider)new InMemoryRuleProvider(rule) },
			weekendDefinition: CalendarWeekendDefinition.SaturdaySunday,
			adjustmentHandlers: handlers);

		// Anchor 1 Nov 2025 + 90 days = 30 Jan 2026. Default ±31 fringe would not admit this; the explicit declaration must.
		IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
			new DateTime(2026, 1, 25),
			new DateTime(2026, 2, 5));

		NotableDate observed = resolved.SingleOrDefault(n => n.Name == "Long-Reach Holiday")
			?? throw new AssertFailedException("Expected the custom-handler-shifted occurrence to be emitted via the widened fringe.");

		Assert.AreEqual(new DateTime(2026, 1, 30), observed.Date);
		Assert.IsTrue(observed.WasAdjusted);
		Assert.AreEqual(new DateTime(2025, 11, 1), observed.AdjustmentReason!.OriginalDate);
	}

	/// <summary>
	/// Verifies that a request whose window does not touch a year boundary inside the planner's fringe distance does not
	/// materialise rules from adjacent years — the fringe pass is skipped entirely when no fringe years are needed.
	/// </summary>
	[TestMethod]
	public void Resolve_WhenWindowIsNotNearYearBoundary_ShouldNotMaterialiseAdjacentYearRules()
	{
		NotableDateRule christmas = new()
		{
			Name = "Christmas Day",
			Strategy = DateResolutionStrategy.Fixed,
			Category = NotableDateCategory.Holiday,
			Month = 12,
			Day = 25,
			IsNonWorkingDay = true,
		};

		NotableDateRule probe = new()
		{
			Name = "Mid-Year Holiday",
			Strategy = DateResolutionStrategy.Fixed,
			Category = NotableDateCategory.Holiday,
			Month = 7,
			Day = 1,
			IsNonWorkingDay = true,
		};

		NotableDateService service = new(
			ruleProviders: new[] { (INotableDateRuleProvider)new InMemoryRuleProvider(christmas, probe) },
			weekendDefinition: CalendarWeekendDefinition.SaturdaySunday);

		IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
			new DateTime(2026, 6, 15),
			new DateTime(2026, 7, 31));

		// July request — no fringe year crossing required. Mid-Year Holiday emitted; Christmas not.
		Assert.IsTrue(resolved.Any(n => n.Name == "Mid-Year Holiday" && n.Date == new DateTime(2026, 7, 1)));
		Assert.IsFalse(resolved.Any(n => n.Name == "Christmas Day"));
	}

	/// <summary>
	/// Verifies that <see cref="NotableDateService.ResolvedWindows" /> exposes the union of every range that has been resolved,
	/// merging overlapping or adjacent windows into the minimum number of disjoint intervals.
	/// </summary>
	[TestMethod]
	public void ResolvedWindows_WhenMultipleRangesResolved_ShouldExposeMergedDisjointIntervals()
	{
		NotableDateService service = new(
			ruleProviders: new[] { (INotableDateRuleProvider)new InMemoryRuleProvider() },
			weekendDefinition: CalendarWeekendDefinition.SaturdaySunday);

		Assert.AreEqual(0, service.ResolvedWindows.Count, "Service should report no resolved windows on construction.");

		_ = service.ResolveNotableDatesInRange(new DateTime(2026, 1, 1), new DateTime(2026, 3, 31));
		_ = service.ResolveNotableDatesInRange(new DateTime(2026, 4, 1), new DateTime(2026, 6, 30));
		_ = service.ResolveNotableDatesInRange(new DateTime(2026, 9, 1), new DateTime(2026, 12, 31));

		IReadOnlyList<DateRange> windows = service.ResolvedWindows;

		Assert.AreEqual(2, windows.Count, "Adjacent Q1+Q2 windows should merge; the disjoint Q4 window stays separate.");
		Assert.AreEqual(new DateTime(2026, 1, 1), windows[0].StartDate);
		Assert.AreEqual(new DateTime(2026, 6, 30), windows[0].EndDate);
		Assert.AreEqual(new DateTime(2026, 9, 1), windows[1].StartDate);
		Assert.AreEqual(new DateTime(2026, 12, 31), windows[1].EndDate);
	}

	/// <summary>
	/// Verifies that <see cref="NotableDateService.IsRangeResolved" /> returns true for a range fully inside a previously resolved
	/// window and false for one that spans a gap between two windows.
	/// </summary>
	[TestMethod]
	public void IsRangeResolved_WhenProbeFitsAndStraddlesResolvedWindows_ShouldReturnExpectedResults()
	{
		NotableDateService service = new(
			ruleProviders: new[] { (INotableDateRuleProvider)new InMemoryRuleProvider() },
			weekendDefinition: CalendarWeekendDefinition.SaturdaySunday);

		_ = service.ResolveNotableDatesInRange(new DateTime(2026, 1, 1), new DateTime(2026, 3, 31));
		_ = service.ResolveNotableDatesInRange(new DateTime(2026, 9, 1), new DateTime(2026, 12, 31));

		Assert.IsTrue(service.IsRangeResolved(new DateTime(2026, 2, 1), new DateTime(2026, 2, 28)),
			"February 2026 lies inside the resolved Q1 window.");

		Assert.IsFalse(service.IsRangeResolved(new DateTime(2026, 2, 1), new DateTime(2026, 10, 31)),
			"A range that bridges the unresolved Q2/Q3 gap is not fully resolved.");

		Assert.IsFalse(service.IsRangeResolved(new DateTime(2026, 5, 1), new DateTime(2026, 5, 31)),
			"May 2026 has not been resolved.");
	}

	/// <summary>
	/// Verifies that <see cref="NotableDateService.Invalidate()" /> clears the resolved-windows tracker so consumers see an empty
	/// list after invalidation.
	/// </summary>
	[TestMethod]
	public void Invalidate_AfterRangesResolved_ShouldEmptyResolvedWindows()
	{
		NotableDateService service = new(
			ruleProviders: new[] { (INotableDateRuleProvider)new InMemoryRuleProvider() },
			weekendDefinition: CalendarWeekendDefinition.SaturdaySunday);

		_ = service.ResolveNotableDatesInRange(new DateTime(2026, 1, 1), new DateTime(2026, 3, 31));
		Assert.AreEqual(1, service.ResolvedWindows.Count);

		service.Invalidate();

		Assert.AreEqual(0, service.ResolvedWindows.Count);
		Assert.IsFalse(service.IsRangeResolved(new DateTime(2026, 2, 1), new DateTime(2026, 2, 28)));
	}

	/// <summary>
	/// Verifies that <see cref="NotableDateService.ResolveNotableDatesInRange" /> throws
	/// <see cref="ArgumentException" /> when the end date precedes the start date.
	/// </summary>
	[TestMethod]
	public void ResolveNotableDatesInRange_WhenEndDateIsBeforeStartDate_ShouldThrowArgumentException()
	{
		NotableDateService service = new(
			ruleProviders: new[] { (INotableDateRuleProvider)new InMemoryRuleProvider() },
			weekendDefinition: CalendarWeekendDefinition.SaturdaySunday);

		ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
		{
			_ = service.ResolveNotableDatesInRange(
				new DateTime(2026, 6, 30),
				new DateTime(2026, 6, 1));
		});

		Assert.AreEqual("endDate", ex.ParamName);
	}

	/// <summary>
	/// In-memory rule provider used by the integration tests.
	/// </summary>
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
	/// Minimal Gregorian Easter Sunday algorithm (Anonymous Gregorian / Meeus / Jones / Butcher) used by the Lent integration test.
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

	/// <summary>
	/// Custom <see cref="IAdjustmentHandler" /> used by the explicit-reach integration test. Shifts the supplied date by a fixed
	/// number of days every time it is consulted.
	/// </summary>
	private sealed class ShiftByDaysHandler : IAdjustmentHandler
	{
		private readonly int _shiftDays;

		public ShiftByDaysHandler(int shiftDays)
		{
			_shiftDays = shiftDays;
		}

		public AdjustmentHandlerResult Apply(AdjustmentHandlerContext context) =>
			new(true, context.Date.AddDays(_shiftDays), IsNonWorkingOverride: null);
	}
}
