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
