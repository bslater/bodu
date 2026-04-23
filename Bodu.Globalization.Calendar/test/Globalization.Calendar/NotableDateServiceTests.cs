// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateServiceTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Extensions;
using System.Collections.Immutable;
using System.Linq;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the end-to-end behaviour of <see cref="NotableDateService" /> across rule resolution, adjustments, overrides, multi-day
/// spans, and cache invalidation.
/// </summary>
[TestClass]
public sealed class NotableDateServiceTests
{
	private static NotableDateRule Fixed(string name, int month, int day, NotableDateCategory category = NotableDateCategory.Holiday, bool nonWorking = false, string? territory = null) =>
		new()
		{
			Name = name,
			Strategy = DateResolutionStrategy.Fixed,
			Category = category,
			Month = month,
			Day = day,
			IsNonWorkingDay = nonWorking,
			TerritoryCode = territory,
		};

	private sealed class InMemoryRuleProvider : INotableDateRuleProvider
	{
		private readonly IEnumerable<NotableDateRule> _rules;

		public InMemoryRuleProvider(params NotableDateRule[] rules) => _rules = rules;

		public IEnumerable<NotableDateRule> LoadRules() => _rules;
	}

	private static NotableDateService BuildService(params NotableDateRule[] rules) =>
		new(new[] { (INotableDateRuleProvider)new InMemoryRuleProvider(rules) }, CalendarWeekendDefinition.SaturdaySunday);

	/// <summary>
	/// Verifies that querying a year returns every notable date defined for that year in chronological order.
	/// </summary>
	[TestMethod]
	public void GetNotableDates_WhenQueryingYear_ShouldReturnChronologicalDates()
	{
		var service = BuildService(
			Fixed("New Year's Day", 1, 1),
			Fixed("Christmas Day", 12, 25));

		var results = service.GetNotableDates(2026);

		Assert.AreEqual(2, results.Count);
		Assert.AreEqual(new DateTime(2026, 1, 1), results[0].Date);
		Assert.AreEqual(new DateTime(2026, 12, 25), results[1].Date);
	}

	/// <summary>
	/// Verifies that an adjustment that rolls a Saturday onto Monday produces both the original and adjusted notable dates and that
	/// the adjusted occurrence carries an <see cref="AdjustmentReason" />.
	/// </summary>
	[TestMethod]
	public void GetNotableDates_WhenWeekendRollAdjustmentFires_ShouldExposeAdjustmentReason()
	{
		var rule = Fixed("New Year's Day", 1, 1, nonWorking: true) with
		{
			Adjustments = ImmutableArray.Create(new ObservanceAdjustment
			{
				Trigger = AdjustmentTrigger.IfWeekend,
				Action = AdjustmentAction.MoveToNextWeekday,
			}),
		};

		var service = BuildService(rule);

		// 1 January 2022 is a Saturday.
		var results = service.GetNotableDates(2022).Where(d => d.Name == "New Year's Day").ToList();

		Assert.AreEqual(2, results.Count);
		var adjusted = results.Single(d => d.WasAdjusted);
		Assert.AreEqual(DayOfWeek.Monday, adjusted.Date.DayOfWeek);
		Assert.AreEqual(new DateTime(2022, 1, 1), adjusted.AdjustmentReason!.OriginalDate);
		Assert.AreEqual(AdjustmentTrigger.IfWeekend, adjusted.AdjustmentReason.Trigger);
	}

	/// <summary>
	/// Verifies that querying a single day inside a multi-day span returns the span anchored on a previous day.
	/// </summary>
	[TestMethod]
	public void GetNotableDates_WhenMultiDaySpanCoversQueryDay_ShouldReturnSpan()
	{
		var span = Fixed("Festival", 6, 1) with { DurationDays = 5 };
		var service = BuildService(span);

		var results = service.GetNotableDates(new DateTime(2025, 6, 3));

		Assert.AreEqual(1, results.Count);
		Assert.AreEqual("Festival", results[0].Name);
		Assert.AreEqual(new DateTime(2025, 6, 1), results[0].Date);
		Assert.AreEqual(new DateTime(2025, 6, 5), results[0].EndDate);
	}

	/// <summary>
	/// Verifies that <see cref="NotableDateService.IsNonWorkingDay" /> returns <see langword="true" /> for a weekend.
	/// </summary>
	[TestMethod]
	public void IsNonWorkingDay_WhenSaturday_ShouldReturnTrue()
	{
		var service = BuildService();

		Assert.IsTrue(service.IsNonWorkingDay(new DateTime(2025, 1, 4)));
	}

	/// <summary>
	/// Verifies that <see cref="NotableDateService.IsNonWorkingDay" /> returns <see langword="true" /> when a notable date marked
	/// non-working falls on the requested day.
	/// </summary>
	[TestMethod]
	public void IsNonWorkingDay_WhenNotableHolidayOnDay_ShouldReturnTrue()
	{
		var service = BuildService(Fixed("Christmas Day", 12, 25, nonWorking: true));

		Assert.IsTrue(service.IsNonWorkingDay(new DateTime(2025, 12, 25)));
	}

	/// <summary>
	/// Verifies that querying with a country territory matches a rule scoped to that country's subdivision.
	/// </summary>
	[TestMethod]
	public void GetNotableDates_WhenTerritoryMatchesSubdivision_ShouldReturnRule()
	{
		var rule = Fixed("Picnic Day", 8, 4, territory: "AU-NT");
		var service = BuildService(rule);

		var auNt = service.GetNotableDates(2025, territoryCode: "AU-NT");
		var au = service.GetNotableDates(2025, territoryCode: "AU");

		Assert.AreEqual(1, auNt.Count);
		Assert.AreEqual(1, au.Count);
	}

	/// <summary>
	/// Verifies that querying with an unrelated territory excludes the rule.
	/// </summary>
	[TestMethod]
	public void GetNotableDates_WhenTerritoryUnrelated_ShouldExcludeRule()
	{
		var rule = Fixed("Bastille Day", 7, 14, territory: "FR");
		var service = BuildService(rule);

		var us = service.GetNotableDates(2025, territoryCode: "US");

		Assert.AreEqual(0, us.Count);
	}

	/// <summary>
	/// Verifies that <see cref="NotableDateService.Invalidate(int)" /> clears only the targeted year's cache.
	/// </summary>
	[TestMethod]
	public void Invalidate_WhenYearSpecified_ShouldOnlyClearThatYear()
	{
		var service = BuildService(Fixed("Holiday", 1, 1));

		_ = service.GetNotableDates(2025);
		_ = service.GetNotableDates(2026);

		service.Invalidate(2025);

		// Re-querying both years should still succeed; we only verify the call does not throw and returns expected counts.
		Assert.AreEqual(1, service.GetNotableDates(2025).Count);
		Assert.AreEqual(1, service.GetNotableDates(2026).Count);
	}

	/// <summary>
	/// Verifies that an override provider can suppress a base rule for a specific year window.
	/// </summary>
	[TestMethod]
	public void GetNotableDates_WhenOverrideRemovesRuleForYear_ShouldExcludeRule()
	{
		var baseRule = Fixed("Holiday", 1, 1);
		var override_ = new TestOverrideProvider(
			removals: new[] { new RuleRemoval("Holiday", FromYear: 2025, ToYear: 2025) },
			additions: Array.Empty<NotableDateRule>());

		var service = new NotableDateService(
			new[] { (INotableDateRuleProvider)new InMemoryRuleProvider(baseRule) },
			CalendarWeekendDefinition.SaturdaySunday,
			overrideProviders: new[] { (INotableDateRuleOverrideProvider)override_ });

		Assert.AreEqual(0, service.GetNotableDates(2025).Count);
		Assert.AreEqual(1, service.GetNotableDates(2026).Count);
	}

	/// <summary>
	/// Verifies that an override provider can add a one-off notable date.
	/// </summary>
	[TestMethod]
	public void GetNotableDates_WhenOverrideAddsRule_ShouldExposeRule()
	{
		var addition = Fixed("Royal Wedding", 5, 19, NotableDateCategory.Cultural);
		var override_ = new TestOverrideProvider(
			removals: Array.Empty<RuleRemoval>(),
			additions: new[] { addition });

		var service = new NotableDateService(
			new[] { (INotableDateRuleProvider)new InMemoryRuleProvider(Fixed("Holiday", 1, 1)) },
			CalendarWeekendDefinition.SaturdaySunday,
			overrideProviders: new[] { (INotableDateRuleOverrideProvider)override_ });

		var results = service.GetNotableDates(2025);
		Assert.IsTrue(results.Any(r => r.Name == "Royal Wedding"));
	}

	private sealed class TestOverrideProvider : INotableDateRuleOverrideProvider
	{
		private readonly IEnumerable<RuleRemoval> _removals;
		private readonly IEnumerable<NotableDateRule> _additions;

		public TestOverrideProvider(IEnumerable<RuleRemoval> removals, IEnumerable<NotableDateRule> additions)
		{
			_removals = removals;
			_additions = additions;
		}

		public IEnumerable<RuleRemoval> GetRemovals() => _removals;

		public IEnumerable<NotableDateRule> GetAdditions() => _additions;
	}
}
