// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRangePipelineScenarioTests.Boundaries.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Immutable;
using Bodu.Extensions;
using Bodu.Globalization.Calendar;

namespace Bodu.Globalization.Calendar.RangeResolution;

/// <summary>
/// Stress-tests the prototype range-resolution pipeline at the extremes of the supported input space:
/// <see cref="DateTime.MinValue" /> / <see cref="DateTime.MaxValue" /> proximity, very large windows spanning multiple
/// decades, single-day windows, multi-day spans whose duration approaches a full year, offset projections that overflow
/// <see cref="DateTime" />, and adjustments declaring large reach.
/// </summary>
/// <remarks>
/// <para>
/// These tests pin behaviour at boundaries that ordinary scenario tests do not exercise. Where a boundary triggers the
/// pipeline's clamp / catch logic (<see cref="DateTime" /> overflow on offset projection, fringe-day arithmetic clamping near
/// year 1 or year 9999), the assertion describes the expected graceful behaviour.
/// </para>
/// </remarks>
public sealed partial class NotableDateRangePipelineScenarioTests
{
	// =====================================================================================================================
	// Calendar-year boundaries
	// =====================================================================================================================

	/// <summary>
	/// Verifies that a fixed-date rule constrained to civil year 1 emits its anchor when the request window spans year 1.
	/// </summary>
	[TestMethod]
	public void Resolve_WhenWindowIsInYearOne_ShouldEmitYearOneAnchor()
	{
		NotableDateRule rule = new()
		{
			Name = "Year One Anchor",
			Strategy = DateResolutionStrategy.Fixed,
			Category = NotableDateCategory.Holiday,
			Month = 6,
			Day = 15,
			FirstYear = 1,
			LastYear = 1,
			IsNonWorkingDay = true,
		};

		NotableDateService service = BuildService(rule);

		IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
			new DateTime(1, 1, 1),
			new DateTime(1, 12, 31));

		NotableDate match = resolved.Single(n => n.Name == "Year One Anchor");
		Assert.AreEqual(new DateTime(1, 6, 15), match.Date);
	}

	/// <summary>
	/// Verifies that the planner clamps the fringe-pass start date at <see cref="DateTime.MinValue" /> when the request
	/// window touches year 1 — no underflow exception, no spurious year-zero materialisation.
	/// </summary>
	[TestMethod]
	public void Resolve_WhenRequestStartIsExactlyDateTimeMinValue_ShouldNotThrowOnFringeArithmetic()
	{
		NotableDateRule rule = new()
		{
			Name = "First Day",
			Strategy = DateResolutionStrategy.Fixed,
			Category = NotableDateCategory.Holiday,
			Month = 1,
			Day = 1,
			FirstYear = 1,
			LastYear = 1,
			IsNonWorkingDay = true,
		};

		NotableDateService service = BuildService(rule);

		IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
			DateTime.MinValue.Date,
			new DateTime(1, 12, 31));

		NotableDate match = resolved.Single(n => n.Name == "First Day");
		Assert.AreEqual(new DateTime(1, 1, 1), match.Date);
	}

	/// <summary>
	/// Verifies that a fixed-date rule constrained to civil year 9999 emits its anchor when the request window spans year
	/// 9999.
	/// </summary>
	[TestMethod]
	public void Resolve_WhenWindowIsInYear9999_ShouldEmitYear9999Anchor()
	{
		NotableDateRule rule = new()
		{
			Name = "Year 9999 Anchor",
			Strategy = DateResolutionStrategy.Fixed,
			Category = NotableDateCategory.Holiday,
			Month = 6,
			Day = 15,
			FirstYear = 9999,
			LastYear = 9999,
			IsNonWorkingDay = true,
		};

		NotableDateService service = BuildService(rule);

		IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
			new DateTime(9999, 1, 1),
			new DateTime(9999, 12, 31));

		NotableDate match = resolved.Single(n => n.Name == "Year 9999 Anchor");
		Assert.AreEqual(new DateTime(9999, 6, 15), match.Date);
	}

	/// <summary>
	/// Verifies that the planner clamps the fringe-pass end date at <see cref="DateTime.MaxValue" /> when the request window
	/// reaches the last day of year 9999 — no overflow exception, no spurious year-10000 materialisation.
	/// </summary>
	[TestMethod]
	public void Resolve_WhenRequestEndIsExactlyDateTimeMaxValueDate_ShouldNotThrowOnFringeArithmetic()
	{
		NotableDateRule rule = new()
		{
			Name = "Last Day",
			Strategy = DateResolutionStrategy.Fixed,
			Category = NotableDateCategory.Holiday,
			Month = 12,
			Day = 31,
			FirstYear = 9999,
			LastYear = 9999,
			IsNonWorkingDay = true,
		};

		NotableDateService service = BuildService(rule);

		IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
			new DateTime(9999, 1, 1),
			DateTime.MaxValue.Date);

		NotableDate match = resolved.Single(n => n.Name == "Last Day");
		Assert.AreEqual(new DateTime(9999, 12, 31), match.Date);
	}

	// =====================================================================================================================
	// Window-shape extremes
	// =====================================================================================================================

	/// <summary>
	/// Verifies that a request with identical start and end dates is treated as a one-day window — not as an empty
	/// half-open interval.
	/// </summary>
	[TestMethod]
	public void Resolve_WhenWindowStartEqualsEnd_ShouldQuerySingleDay()
	{
		NotableDateRule rule = new()
		{
			Name = "Independence Day",
			Strategy = DateResolutionStrategy.Fixed,
			Category = NotableDateCategory.Holiday,
			Month = 7,
			Day = 4,
			FirstYear = 2026,
			LastYear = 2026,
			IsNonWorkingDay = true,
		};

		NotableDateService service = BuildService(rule);

		IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
			new DateTime(2026, 7, 4),
			new DateTime(2026, 7, 4));

		Assert.AreEqual(1, resolved.Count);
		Assert.AreEqual(new DateTime(2026, 7, 4), resolved[0].Date);
	}

	/// <summary>
	/// Verifies that a request spanning many decades emits an annual rule once per civil year in chronological order.
	/// </summary>
	[DataTestMethod]
	[DataRow(2000, 2009, 10)]
	[DataRow(1990, 2030, 41)]
	[DataRow(2020, 2120, 101)]
	public void Resolve_WhenWindowSpansManyYears_ShouldEmitAnnualRuleOncePerYear(int startYear, int endYear, int expectedCount)
	{
		NotableDateRule rule = new()
		{
			Name = "Annual",
			Strategy = DateResolutionStrategy.Fixed,
			Category = NotableDateCategory.Holiday,
			Month = 1,
			Day = 1,
			IsNonWorkingDay = true,
		};

		NotableDateService service = BuildService(rule);

		IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
			new DateTime(startYear, 1, 1),
			new DateTime(endYear, 12, 31));

		NotableDate[] annualEmissions = resolved.Where(n => n.Name == "Annual").OrderBy(n => n.Date).ToArray();

		Assert.AreEqual(expectedCount, annualEmissions.Length,
			$"Annual rule should emit once per civil year between {startYear} and {endYear} inclusive.");

		for (int i = 0; i < annualEmissions.Length; i++)
		{
			Assert.AreEqual(new DateTime(startYear + i, 1, 1), annualEmissions[i].Date);
		}
	}

	/// <summary>
	/// Verifies that a request whose rule set is entirely empty returns an empty result without throwing.
	/// </summary>
	[TestMethod]
	public void Resolve_WhenRuleSetIsEmpty_ShouldReturnEmpty()
	{
		NotableDateService service = BuildService();

		IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
			new DateTime(2026, 1, 1),
			new DateTime(2026, 12, 31));

		Assert.AreEqual(0, resolved.Count);
	}

	// =====================================================================================================================
	// Long-duration spans
	// =====================================================================================================================

	/// <summary>
	/// Verifies that a 365-day duration rule whose anchor is on 1 January is emitted from a mid-year query because its span
	/// blankets the entire year.
	/// </summary>
	[TestMethod]
	public void Resolve_WhenDurationSpansEntireYearAndQueryIsMidYear_ShouldEmitFromAnchor()
	{
		NotableDateRule rule = new()
		{
			Name = "Year-Long Programme",
			Strategy = DateResolutionStrategy.Fixed,
			Category = NotableDateCategory.Cultural,
			Month = 1,
			Day = 1,
			FirstYear = 2026,
			LastYear = 2026,
			DurationDays = 365,
		};

		NotableDateService service = BuildService(rule);

		IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
			new DateTime(2026, 6, 1),
			new DateTime(2026, 6, 30));

		NotableDate match = resolved.Single(n => n.Name == "Year-Long Programme");
		Assert.AreEqual(new DateTime(2026, 1, 1), match.Date);
		Assert.AreEqual(365, match.DurationDays);
		Assert.AreEqual(new DateTime(2026, 12, 31), match.EndDate);
	}

	/// <summary>
	/// Verifies that a multi-day span whose end date lands on <see cref="DateTime.MaxValue" />.Date is emitted without any
	/// overflow.
	/// </summary>
	[TestMethod]
	public void Resolve_WhenSpanEndDateIsExactlyDateTimeMaxValueDate_ShouldEmitWithFullDuration()
	{
		// Anchor: 25 Dec 9999. DurationDays = 7 → span Dec 25 .. Dec 31 9999 (= MaxValue.Date).
		NotableDateRule rule = new()
		{
			Name = "Year-End Span",
			Strategy = DateResolutionStrategy.Fixed,
			Category = NotableDateCategory.Cultural,
			Month = 12,
			Day = 25,
			FirstYear = 9999,
			LastYear = 9999,
			DurationDays = 7,
		};

		NotableDateService service = BuildService(rule);

		IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
			new DateTime(9999, 12, 1),
			DateTime.MaxValue.Date);

		NotableDate match = resolved.Single(n => n.Name == "Year-End Span");
		Assert.AreEqual(new DateTime(9999, 12, 25), match.Date);
		Assert.AreEqual(new DateTime(9999, 12, 31), match.EndDate);
	}

	// =====================================================================================================================
	// Offset projection overflow / underflow
	// =====================================================================================================================

	/// <summary>
	/// Verifies that an offset rule whose projection lands inside the supported <see cref="DateTime" /> range emits as
	/// expected, including offsets that cross many year boundaries.
	/// </summary>
	[DataTestMethod]
	[DataRow(2026, 7, 1, 1, 2026, 7, 2)]      // +1 day
	[DataRow(2026, 7, 1, 365, 2027, 7, 1)]    // +1 year
	[DataRow(2026, 7, 1, 730, 2028, 7, 1)]    // +2 years (leap year between)
	[DataRow(2026, 7, 1, 3650, 2036, 6, 28)]  // +10 years
	[DataRow(2026, 7, 1, -1, 2026, 6, 30)]    // -1 day
	[DataRow(2026, 7, 1, -365, 2025, 7, 1)]   // -1 year
	[DataRow(2026, 7, 1, -3650, 2016, 7, 4)]  // -10 years
	public void Resolve_WhenOffsetRuleProjectionStaysInRange_ShouldEmitAtProjectedDate(
		int anchorYear, int anchorMonth, int anchorDay,
		int offsetDays,
		int expectedYear, int expectedMonth, int expectedDay)
	{
		NotableDateRule anchor = new()
		{
			Name = "Anchor",
			Strategy = DateResolutionStrategy.Fixed,
			Category = NotableDateCategory.Holiday,
			Month = anchorMonth,
			Day = anchorDay,
			FirstYear = anchorYear,
			LastYear = anchorYear,
			IsNonWorkingDay = true,
		};

		NotableDateRule offset = new()
		{
			Name = "Offset",
			Strategy = DateResolutionStrategy.OffsetFromAnchor,
			Category = NotableDateCategory.Holiday,
			AnchorRuleName = "Anchor",
			OffsetDays = offsetDays,
			IsNonWorkingDay = true,
		};

		NotableDateService service = BuildService(anchor, offset);

		DateTime expected = new(expectedYear, expectedMonth, expectedDay);
		DateTime windowStart = new DateTime(Math.Min(anchorYear, expectedYear), 1, 1);
		DateTime windowEnd = new DateTime(Math.Max(anchorYear, expectedYear), 12, 31);

		IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(windowStart, windowEnd);

		NotableDate offsetEmission = resolved.Single(n => n.Name == "Offset");
		Assert.AreEqual(expected, offsetEmission.Date);
	}

	/// <summary>
	/// Verifies that an offset rule whose projection would overflow <see cref="DateTime.MaxValue" /> is silently skipped — the
	/// pipeline catches the <see cref="ArgumentOutOfRangeException" /> from <see cref="DateTime.AddDays" /> rather than
	/// propagating it.
	/// </summary>
	[TestMethod]
	public void Resolve_WhenOffsetRuleProjectionOverflowsMaxValue_ShouldSilentlySkipOffset()
	{
		NotableDateRule anchor = new()
		{
			Name = "Anchor",
			Strategy = DateResolutionStrategy.Fixed,
			Category = NotableDateCategory.Holiday,
			Month = 12,
			Day = 31,
			FirstYear = 9999,
			LastYear = 9999,
			IsNonWorkingDay = true,
		};

		// Anchor 31 Dec 9999 + 100 days would land in year 10000 — outside DateTime's supported range.
		NotableDateRule offset = new()
		{
			Name = "Offset",
			Strategy = DateResolutionStrategy.OffsetFromAnchor,
			Category = NotableDateCategory.Holiday,
			AnchorRuleName = "Anchor",
			OffsetDays = 100,
			IsNonWorkingDay = true,
		};

		NotableDateService service = BuildService(anchor, offset);

		IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
			new DateTime(9999, 12, 1),
			DateTime.MaxValue.Date);

		Assert.IsTrue(resolved.Any(n => n.Name == "Anchor"),
			"The anchor rule should still emit even though its derived offset rule overflows.");
		Assert.IsFalse(resolved.Any(n => n.Name == "Offset"),
			"The overflowing offset rule should be silently skipped.");
	}

	/// <summary>
	/// Verifies that an offset rule whose projection would underflow <see cref="DateTime.MinValue" /> is silently skipped.
	/// </summary>
	[TestMethod]
	public void Resolve_WhenOffsetRuleProjectionUnderflowsMinValue_ShouldSilentlySkipOffset()
	{
		NotableDateRule anchor = new()
		{
			Name = "Anchor",
			Strategy = DateResolutionStrategy.Fixed,
			Category = NotableDateCategory.Holiday,
			Month = 1,
			Day = 1,
			FirstYear = 1,
			LastYear = 1,
			IsNonWorkingDay = true,
		};

		// Anchor 1 Jan year 1 - 100 days would underflow before year 1.
		NotableDateRule offset = new()
		{
			Name = "Offset",
			Strategy = DateResolutionStrategy.OffsetFromAnchor,
			Category = NotableDateCategory.Holiday,
			AnchorRuleName = "Anchor",
			OffsetDays = -100,
			IsNonWorkingDay = true,
		};

		NotableDateService service = BuildService(anchor, offset);

		IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
			DateTime.MinValue.Date,
			new DateTime(1, 12, 31));

		Assert.IsTrue(resolved.Any(n => n.Name == "Anchor"));
		Assert.IsFalse(resolved.Any(n => n.Name == "Offset"));
	}

	/// <summary>
	/// Verifies that an offset rule whose projection lands exactly on <see cref="DateTime.MaxValue" />.Date emits successfully
	/// — the boundary value itself is supported.
	/// </summary>
	[TestMethod]
	public void Resolve_WhenOffsetRuleProjectionLandsExactlyOnMaxValueDate_ShouldEmit()
	{
		NotableDateRule anchor = new()
		{
			Name = "Anchor",
			Strategy = DateResolutionStrategy.Fixed,
			Category = NotableDateCategory.Holiday,
			Month = 12,
			Day = 25,
			FirstYear = 9999,
			LastYear = 9999,
			IsNonWorkingDay = true,
		};

		// Anchor 25 Dec 9999 + 6 days = 31 Dec 9999 (= MaxValue.Date).
		NotableDateRule offset = new()
		{
			Name = "Offset",
			Strategy = DateResolutionStrategy.OffsetFromAnchor,
			Category = NotableDateCategory.Holiday,
			AnchorRuleName = "Anchor",
			OffsetDays = 6,
			IsNonWorkingDay = true,
		};

		NotableDateService service = BuildService(anchor, offset);

		IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
			new DateTime(9999, 12, 1),
			DateTime.MaxValue.Date);

		NotableDate offsetEmission = resolved.Single(n => n.Name == "Offset");
		Assert.AreEqual(new DateTime(9999, 12, 31), offsetEmission.Date);
	}

	// =====================================================================================================================
	// Adjustment reach pushed wide — large MaxAdjustmentReachDays forces a multi-year fringe scan
	// =====================================================================================================================

	/// <summary>
	/// Verifies that an adjustment declaring a 1000-day reach via <see cref="ObservanceAdjustment.MaxAdjustmentReachDays" />
	/// causes the planner to widen the fringe distance enough that an anchor materialised in a fringe year approximately
	/// three years away is admitted when its projected adjustment date lands inside the request window.
	/// </summary>
	[TestMethod]
	public void Resolve_WhenAdjustmentDeclaresOneThousandDayReach_ShouldAdmitAnchorFromYearsAway()
	{
		NotableDateRule rule = new()
		{
			Name = "Far-Reach Holiday",
			Strategy = DateResolutionStrategy.Fixed,
			Category = NotableDateCategory.Holiday,
			Month = 1,
			Day = 1,
			IsNonWorkingDay = true,
			Adjustments = ImmutableArray.Create(new ObservanceAdjustment
			{
				Key = "shift-1000",
				Trigger = AdjustmentTrigger.Always,
				Action = AdjustmentAction.AddDays,
				OffsetDays = 1000,
				IsNonWorkingDay = true,
				MaxAdjustmentReachDays = 1000,
			}),
		};

		NotableDateService service = BuildService(rule);

		// Anchor 1 Jan 2024 + 1000 days = 27 Sep 2026.
		// Request a single-day window on 27 Sep 2026 — without the wide fringe, year 2024 would not be a fringe year and the
		// adjustment would not surface.
		IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
			new DateTime(2026, 9, 27),
			new DateTime(2026, 9, 27));

		NotableDate match = resolved.Single(n => n.Name == "Far-Reach Holiday");
		Assert.AreEqual(new DateTime(2026, 9, 27), match.Date);
		Assert.IsTrue(match.WasAdjusted);
		Assert.AreEqual(new DateTime(2024, 1, 1), match.AdjustmentReason!.OriginalDate);
	}

	// =====================================================================================================================
	// Multiple-decade reach summary
	// =====================================================================================================================

	/// <summary>
	/// Verifies that <see cref="NotableDateService.ResolvedWindows" /> tracks every previously-resolved window across many
	/// disjoint queries spanning a wide range of years.
	/// </summary>
	[TestMethod]
	public void ResolvedWindows_AfterManyDisjointResolutionsAcrossDecades_ShouldRetainAllAsDisjointIntervals()
	{
		NotableDateRule rule = new()
		{
			Name = "Annual",
			Strategy = DateResolutionStrategy.Fixed,
			Category = NotableDateCategory.Holiday,
			Month = 6,
			Day = 15,
			IsNonWorkingDay = true,
		};

		NotableDateService service = BuildService(rule);

		// Three widely-disjoint windows.
		_ = service.ResolveNotableDatesInRange(new DateTime(1900, 1, 1), new DateTime(1900, 12, 31));
		_ = service.ResolveNotableDatesInRange(new DateTime(2000, 6, 1), new DateTime(2001, 6, 30));
		_ = service.ResolveNotableDatesInRange(new DateTime(2099, 1, 1), new DateTime(2099, 12, 31));

		IReadOnlyList<DateRange> windows = service.ResolvedWindows;

		Assert.AreEqual(3, windows.Count, $"Expected three disjoint resolved windows; got {windows.Count}.");
		Assert.AreEqual(new DateTime(1900, 1, 1), windows[0].StartDate);
		Assert.AreEqual(new DateTime(1900, 12, 31), windows[0].EndDate);
		Assert.AreEqual(new DateTime(2000, 6, 1), windows[1].StartDate);
		Assert.AreEqual(new DateTime(2001, 6, 30), windows[1].EndDate);
		Assert.AreEqual(new DateTime(2099, 1, 1), windows[2].StartDate);
		Assert.AreEqual(new DateTime(2099, 12, 31), windows[2].EndDate);
	}
}
