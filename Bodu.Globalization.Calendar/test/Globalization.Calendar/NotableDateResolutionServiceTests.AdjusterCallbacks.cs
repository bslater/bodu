// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateResolutionServiceTests.AdjusterCallbacks.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Immutable;
using Bodu.Extensions;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies that <see cref="NotableDateResolutionAdjustmentProcessor.CreateAdjuster" /> wires the resolve-by-name
/// callback to <see cref="NotableDateResolutionWindow.ResolveByName" />: when a <see cref="AdjustmentAction.ReplaceWithNamedDate" />
/// adjustment fires, the substitute date is fetched through that window callback rather than the legacy
/// service cache.
/// </summary>
[TestClass]
public sealed class NotableDateResolutionServiceAdjusterCallbacksTests
{
	/// <summary>
	/// Verifies that a <see cref="AdjustmentAction.ReplaceWithNamedDate" /> adjustment routes through the window's
	/// <see cref="NotableDateResolutionWindow.ResolveByName" /> callback to fetch the substitute date by name.
	/// </summary>
	[TestMethod]
	public void Resolve_WhenReplaceWithNamedDateActivates_ShouldUseWindowResolveByNameCallback()
	{
		NotableDateRule target = new()
		{
			Name = "Substitute Day",
			Strategy = DateResolutionStrategy.Fixed,
			Category = NotableDateCategory.Holiday,
			Month = 6,
			Day = 20,
		};

		NotableDateRule redirect = new()
		{
			Name = "Redirected Day",
			Strategy = DateResolutionStrategy.Fixed,
			Category = NotableDateCategory.Holiday,
			Month = 6,
			Day = 1,
			IsNonWorkingDay = true,
			Adjustments = ImmutableArray.Create(new ObservanceAdjustment
			{
				Key = "replace",
				Trigger = AdjustmentTrigger.Always,
				Action = AdjustmentAction.ReplaceWithNamedDate,
				TargetRuleName = "Substitute Day",
			}),
		};

		NotableDateResolutionService service = new(
			ruleProviders: new[] { (INotableDateRuleProvider)new InMemoryRuleProvider(target, redirect) },
			weekendDefinition: CalendarWeekendDefinition.SaturdaySunday);

		IReadOnlyList<NotableDate> resolved = service.GetNotableDates(
			new DateTime(2026, 6, 1),
			new DateTime(2026, 6, 30));

		// The redirect rule should be adjusted onto Substitute Day's date (20 June 2026), and the AdjustmentReason
		// should reflect the original anchor (1 June 2026).
		NotableDate? adjusted = resolved.FirstOrDefault(n => n.Name == "Redirected Day" && n.WasAdjusted);

		Assert.IsNotNull(adjusted);
		Assert.AreEqual(new DateTime(2026, 6, 20), adjusted!.Date);
		Assert.AreEqual(new DateTime(2026, 6, 1), adjusted.AdjustmentReason!.OriginalDate);
	}

	/// <summary>
	/// Verifies that the window's <see cref="NotableDateResolutionWindow.ResolveByName" /> returns the original anchor
	/// when no target rule with the supplied name is present in the window — exercising the fallback branch of
	/// <see cref="AdjustmentAction.ReplaceWithNamedDate" /> when <c>ResolveByName</c> returns <see langword="null" />.
	/// </summary>
	[TestMethod]
	public void Resolve_WhenReplaceWithNamedDateTargetIsMissing_ShouldFallBackToOriginalDate()
	{
		NotableDateRule redirect = new()
		{
			Name = "Orphan Redirect",
			Strategy = DateResolutionStrategy.Fixed,
			Category = NotableDateCategory.Holiday,
			Month = 6,
			Day = 1,
			IsNonWorkingDay = true,
			Adjustments = ImmutableArray.Create(new ObservanceAdjustment
			{
				Key = "replace",
				Trigger = AdjustmentTrigger.Always,
				Action = AdjustmentAction.ReplaceWithNamedDate,
				TargetRuleName = "Missing Target",
			}),
		};

		NotableDateResolutionService service = new(
			ruleProviders: new[] { (INotableDateRuleProvider)new InMemoryRuleProvider(redirect) },
			weekendDefinition: CalendarWeekendDefinition.SaturdaySunday);

		IReadOnlyList<NotableDate> resolved = service.GetNotableDates(
			new DateTime(2026, 6, 1),
			new DateTime(2026, 6, 30));

		// With no target rule found, the adjustment resolves to the original date (1 June). Because the adjusted
		// date equals the anchor, the adjustment processor suppresses the duplicate and emits only the base entry.
		Assert.AreEqual(1, resolved.Count);
		Assert.IsFalse(resolved[0].WasAdjusted);
	}

	private sealed class InMemoryRuleProvider : INotableDateRuleProvider
	{
		private readonly IReadOnlyList<NotableDateRule> _rules;

		public InMemoryRuleProvider(params NotableDateRule[] rules) => _rules = rules;

		public IEnumerable<NotableDateRule> LoadRules() => _rules;
	}
}
