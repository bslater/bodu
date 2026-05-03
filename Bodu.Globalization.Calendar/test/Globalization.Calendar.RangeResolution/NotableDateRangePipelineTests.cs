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
	/// Verifies that a request whose window spans both the original Dec 31 anchor and its rolled-forward Jan 3 observance returns
	/// both occurrences — the actual calendar holiday and the observed substitute — and does not duplicate or lose either one.
	/// </summary>
	[TestMethod]
	public void Resolve_WhenWindowSpansBothAnchorAndAdjustedObservance_ShouldEmitBaseAndAdjustedDates()
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
		Assert.AreEqual(2, yearEndOccurrences.Length,
			"Spanning [25 Dec 2022, 5 Jan 2023] should return both the actual 31 Dec anchor and the 3 Jan observance.");

		NotableDate baseDate = yearEndOccurrences.Single(n => n.Date == new DateTime(2022, 12, 31));
		NotableDate observed = yearEndOccurrences.Single(n => n.Date == new DateTime(2023, 1, 3));

		Assert.IsFalse(baseDate.WasAdjusted, "The Dec 31 base occurrence must not carry an AdjustmentReason.");
		Assert.IsTrue(observed.WasAdjusted, "The Jan 3 observance must carry an AdjustmentReason.");
		Assert.IsNotNull(observed.AdjustmentReason);
		Assert.AreEqual(new DateTime(2022, 12, 31), observed.AdjustmentReason.OriginalDate);
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
}
