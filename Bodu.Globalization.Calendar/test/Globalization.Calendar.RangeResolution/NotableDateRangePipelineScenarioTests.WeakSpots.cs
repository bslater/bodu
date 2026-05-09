// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRangePipelineScenarioTests.WeakSpots.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Immutable;
using Bodu.Extensions;
using Bodu.Globalization.Calendar;

namespace Bodu.Globalization.Calendar.RangeResolution;

/// <summary>
/// Probes the public surface of <see cref="NotableDateService.ResolveNotableDatesInRange" /> at points that look like they
/// might break: malformed offset chains, algorithms that misbehave, anchors that disappear at runtime, contradictory
/// territory / duration / handler configurations, and silently-surprising defaults. Each scenario either pins an expected
/// graceful behaviour the pipeline already provides or surfaces a documented gap as an <c>[Ignore]</c>'d test that future
/// pipeline work should remove.
/// </summary>
public sealed partial class NotableDateRangePipelineScenarioTests
{
	// =====================================================================================================================
	// Offset-chain integrity
	// =====================================================================================================================

	/// <summary>
	/// Verifies that an <see cref="DateResolutionStrategy.OffsetFromAnchor" /> rule whose <see cref="NotableDateRule.AnchorRuleName" />
	/// references a rule that was never authored is silently skipped instead of throwing.
	/// </summary>
	[TestMethod]
	public void Resolve_WhenOffsetRuleAnchorNameIsMissing_ShouldSilentlySkip()
	{
		NotableDateRule orphan = new()
		{
			Name = "Orphan",
			Strategy = DateResolutionStrategy.OffsetFromAnchor,
			Category = NotableDateCategory.Holiday,
			AnchorRuleName = "Does Not Exist",
			OffsetDays = 1,
		};

		NotableDateService service = BuildService(orphan);

		IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
			new DateTime(2026, 1, 1),
			new DateTime(2026, 12, 31));

		Assert.IsFalse(resolved.Any(n => n.Name == "Orphan"));
	}

	/// <summary>
	/// Verifies that an <see cref="DateResolutionStrategy.OffsetFromAnchor" /> rule whose
	/// <see cref="NotableDateRule.AnchorRuleName" /> is <see langword="null" /> or whitespace is silently skipped.
	/// </summary>
	[DataTestMethod]
	[DataRow(null!)]
	[DataRow("")]
	[DataRow("   ")]
	public void Resolve_WhenOffsetRuleAnchorNameIsNullOrWhitespace_ShouldSilentlySkip(string? anchorName)
	{
		NotableDateRule rule = new()
		{
			Name = "No Anchor Specified",
			Strategy = DateResolutionStrategy.OffsetFromAnchor,
			Category = NotableDateCategory.Holiday,
			AnchorRuleName = anchorName,
			OffsetDays = 1,
		};

		NotableDateService service = BuildService(rule);

		IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
			new DateTime(2026, 1, 1),
			new DateTime(2026, 12, 31));

		Assert.IsFalse(resolved.Any(n => n.Name == "No Anchor Specified"));
	}

	/// <summary>
	/// Verifies that a self-referencing offset rule (<c>A → A</c>) is silently skipped — the resolver detects the cycle and
	/// the pipeline does not propagate the resulting <see cref="InvalidOperationException" />.
	/// </summary>
	[TestMethod]
	public void Resolve_WhenOffsetRuleReferencesItself_ShouldSilentlySkip()
	{
		NotableDateRule selfRef = new()
		{
			Name = "Loop",
			Strategy = DateResolutionStrategy.OffsetFromAnchor,
			Category = NotableDateCategory.Holiday,
			AnchorRuleName = "Loop",
			OffsetDays = 1,
		};

		NotableDateService service = BuildService(selfRef);

		IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
			new DateTime(2026, 1, 1),
			new DateTime(2026, 12, 31));

		Assert.IsFalse(resolved.Any(n => n.Name == "Loop"));
	}

	/// <summary>
	/// Verifies that a circular offset chain (<c>A → B → A</c>) is silently skipped.
	/// </summary>
	[TestMethod]
	public void Resolve_WhenOffsetChainIsCircular_ShouldSilentlySkipBoth()
	{
		NotableDateRule a = new()
		{
			Name = "Cycle A",
			Strategy = DateResolutionStrategy.OffsetFromAnchor,
			Category = NotableDateCategory.Holiday,
			AnchorRuleName = "Cycle B",
			OffsetDays = 1,
		};

		NotableDateRule b = new()
		{
			Name = "Cycle B",
			Strategy = DateResolutionStrategy.OffsetFromAnchor,
			Category = NotableDateCategory.Holiday,
			AnchorRuleName = "Cycle A",
			OffsetDays = -1,
		};

		NotableDateService service = BuildService(a, b);

		IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
			new DateTime(2026, 1, 1),
			new DateTime(2026, 12, 31));

		Assert.IsFalse(resolved.Any(n => n.Name == "Cycle A"));
		Assert.IsFalse(resolved.Any(n => n.Name == "Cycle B"));
	}

	// =====================================================================================================================
	// Duration clamping
	// =====================================================================================================================

	/// <summary>
	/// Verifies that <see cref="NotableDateRule.DurationDays" /> values of zero or below are clamped to a single-day span
	/// when emitted — the pipeline guarantees a minimum span of one day even when authors specify nonsensical inputs.
	/// </summary>
	[DataTestMethod]
	[DataRow(0)]
	[DataRow(-1)]
	[DataRow(-365)]
	[DataRow(int.MinValue)]
	public void Resolve_WhenDurationDaysIsZeroOrNegative_ShouldClampToOneDay(int durationDays)
	{
		NotableDateRule rule = new()
		{
			Name = "Clamped",
			Strategy = DateResolutionStrategy.Fixed,
			Category = NotableDateCategory.Holiday,
			Month = 7,
			Day = 4,
			FirstYear = 2026,
			LastYear = 2026,
			DurationDays = durationDays,
		};

		NotableDateService service = BuildService(rule);

		IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
			new DateTime(2026, 7, 4),
			new DateTime(2026, 7, 4));

		NotableDate match = resolved.Single(n => n.Name == "Clamped");
		Assert.AreEqual(1, match.DurationDays, $"Expected DurationDays clamp to 1 for input {durationDays}.");
		Assert.AreEqual(new DateTime(2026, 7, 4), match.Date);
		Assert.AreEqual(new DateTime(2026, 7, 4), match.EndDate);
	}

	// =====================================================================================================================
	// Territory normalisation — null vs empty string vs comma-separated
	// =====================================================================================================================

	/// <summary>
	/// Verifies that a rule with no authored territory (<see langword="null" />) and a rule with an empty-string territory
	/// behave identically — both are treated as global.
	/// </summary>
	[DataTestMethod]
	[DataRow(null!)]
	[DataRow("")]
	public void Resolve_WhenRuleTerritoryIsNullOrEmpty_ShouldBeTreatedAsGlobal(string? territoryCode)
	{
		NotableDateRule rule = new()
		{
			Name = "Global",
			Strategy = DateResolutionStrategy.Fixed,
			Category = NotableDateCategory.Holiday,
			Month = 7,
			Day = 4,
			TerritoryCode = territoryCode,
		};

		NotableDateService service = BuildService(rule);

		IReadOnlyList<NotableDate> withoutContext = service.ResolveNotableDatesInRange(
			new DateTime(2026, 7, 1),
			new DateTime(2026, 7, 31));
		Assert.IsTrue(withoutContext.Any(n => n.Name == "Global"));

		IReadOnlyList<NotableDate> withAuContext = service.ResolveNotableDatesInRange(
			new DateTime(2026, 7, 1),
			new DateTime(2026, 7, 31),
			territoryCode: "AU");
		Assert.IsTrue(withAuContext.Any(n => n.Name == "Global"));
	}

	/// <summary>
	/// Verifies that a comma-separated <see cref="NotableDateRule.TerritoryCode" /> is expanded into a separate emission per
	/// territory in the request scope when no specific territory is requested.
	/// </summary>
	[TestMethod]
	public void Resolve_WhenRuleHasCommaSeparatedTerritory_AndRequestIsContextless_ShouldEmitPerTerritory()
	{
		NotableDateRule rule = new()
		{
			Name = "Multi-Territory Holiday",
			Strategy = DateResolutionStrategy.Fixed,
			Category = NotableDateCategory.Holiday,
			Month = 1,
			Day = 26,
			TerritoryCode = "AU,NZ",
			IsNonWorkingDay = true,
		};

		NotableDateService service = BuildService(rule);

		IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
			new DateTime(2026, 1, 1),
			new DateTime(2026, 1, 31));

		string[] emittedTerritories = resolved
			.Where(n => n.Name == "Multi-Territory Holiday")
			.Select(n => n.TerritoryCode ?? "<null>")
			.OrderBy(s => s, StringComparer.Ordinal)
			.ToArray();

		CollectionAssert.AreEqual(new[] { "AU", "NZ" }, emittedTerritories,
			$"Expected one emission per authored territory; got: {string.Join(", ", emittedTerritories)}.");
	}

	/// <summary>
	/// Verifies that requesting territory <c>AU</c> against a rule scoped to <c>AU,NZ</c> emits a single occurrence —
	/// the pipeline filters the comma-list to just the matching territory.
	/// </summary>
	[TestMethod]
	public void Resolve_WhenRuleHasCommaSeparatedTerritory_AndRequestNarrowsToOne_ShouldOnlyEmitMatchingTerritory()
	{
		NotableDateRule rule = new()
		{
			Name = "Multi-Territory Holiday",
			Strategy = DateResolutionStrategy.Fixed,
			Category = NotableDateCategory.Holiday,
			Month = 1,
			Day = 26,
			TerritoryCode = "AU,NZ",
			IsNonWorkingDay = true,
		};

		NotableDateService service = BuildService(rule);

		IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
			new DateTime(2026, 1, 1),
			new DateTime(2026, 1, 31),
			territoryCode: "AU");

		NotableDate match = resolved.Single(n => n.Name == "Multi-Territory Holiday");
		Assert.AreEqual("AU", match.TerritoryCode);
	}

	// =====================================================================================================================
	// Algorithm misbehaviour
	// =====================================================================================================================

	/// <summary>
	/// Verifies that an algorithm returning <see langword="null" /> for the requested year causes both the algorithmic rule
	/// and any offset rules dependent on it to be silently skipped — null is the contracted "I don't apply this year" signal.
	/// </summary>
	[TestMethod]
	public void Resolve_WhenAlgorithmReturnsNullForRequestedYear_ShouldSilentlySkipAnchorAndDependents()
	{
		NotableDateRule anchor = new()
		{
			Name = "Probe Anchor",
			Strategy = DateResolutionStrategy.Algorithm,
			Category = NotableDateCategory.Religious,
			AlgorithmKey = "always-null",
		};

		NotableDateRule dependent = new()
		{
			Name = "Probe Dependent",
			Strategy = DateResolutionStrategy.OffsetFromAnchor,
			Category = NotableDateCategory.Religious,
			AnchorRuleName = "Probe Anchor",
			OffsetDays = 1,
		};

		NotableDateAlgorithmRegistry registry = new();
		registry.Register("always-null", new AlwaysNullAlgorithm());

		NotableDateService service = new(
			ruleProviders: new[] { (INotableDateRuleProvider)new InMemoryRuleProvider(anchor, dependent) },
			weekendDefinition: CalendarWeekendDefinition.SaturdaySunday,
			algorithmRegistry: registry);

		IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
			new DateTime(2026, 1, 1),
			new DateTime(2026, 12, 31));

		Assert.IsFalse(resolved.Any(n => n.Name == "Probe Anchor"));
		Assert.IsFalse(resolved.Any(n => n.Name == "Probe Dependent"));
	}

	/// <summary>
	/// Verifies that an algorithmic rule whose <see cref="NotableDateRule.AlgorithmKey" /> is not registered with the algorithm
	/// registry resolves to <see langword="null" /> and is silently skipped.
	/// </summary>
	[TestMethod]
	public void Resolve_WhenAlgorithmKeyIsNotRegistered_ShouldSilentlySkip()
	{
		NotableDateRule rule = new()
		{
			Name = "Unregistered",
			Strategy = DateResolutionStrategy.Algorithm,
			Category = NotableDateCategory.Religious,
			AlgorithmKey = "never-registered",
		};

		NotableDateService service = new(
			ruleProviders: new[] { (INotableDateRuleProvider)new InMemoryRuleProvider(rule) },
			weekendDefinition: CalendarWeekendDefinition.SaturdaySunday,
			algorithmRegistry: new NotableDateAlgorithmRegistry());

		IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
			new DateTime(2026, 1, 1),
			new DateTime(2026, 12, 31));

		Assert.IsFalse(resolved.Any(n => n.Name == "Unregistered"));
	}

	// =====================================================================================================================
	// ReplaceWithNamedDate edge cases
	// =====================================================================================================================

	/// <summary>
	/// Verifies that <see cref="AdjustmentAction.ReplaceWithNamedDate" /> with a target rule name that does not exist falls
	/// back to the original anchor — no shift, no exception.
	/// </summary>
	[TestMethod]
	public void Resolve_WhenReplaceWithNamedDateTargetIsMissing_ShouldFallBackToOriginalAnchor()
	{
		NotableDateRule rule = new()
		{
			Name = "Probe",
			Strategy = DateResolutionStrategy.Fixed,
			Category = NotableDateCategory.Holiday,
			Month = 7,
			Day = 4,
			FirstYear = 2026,
			LastYear = 2026,
			IsNonWorkingDay = true,
			Adjustments = ImmutableArray.Create(new ObservanceAdjustment
			{
				Key = "missing-target",
				Trigger = AdjustmentTrigger.Always,
				Action = AdjustmentAction.ReplaceWithNamedDate,
				TargetRuleName = "Does Not Exist",
				IsNonWorkingDay = true,
			}),
		};

		NotableDateService service = BuildService(rule);

		IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
			new DateTime(2026, 7, 1),
			new DateTime(2026, 7, 31));

		NotableDate match = resolved.Single(n => n.Name == "Probe");
		Assert.AreEqual(new DateTime(2026, 7, 4), match.Date);
		Assert.IsFalse(match.WasAdjusted,
			"ReplaceWithNamedDate falling back to original should not record an AdjustmentReason because the resolved date equals the base anchor.");
	}

	/// <summary>
	/// Verifies that <see cref="AdjustmentAction.ReplaceWithNamedDate" /> with a <see langword="null" /> or whitespace
	/// <see cref="ObservanceAdjustment.TargetRuleName" /> falls back to the original anchor.
	/// </summary>
	[DataTestMethod]
	[DataRow(null!)]
	[DataRow("")]
	[DataRow("   ")]
	public void Resolve_WhenReplaceWithNamedDateTargetIsNullOrWhitespace_ShouldFallBackToOriginalAnchor(string? targetRuleName)
	{
		NotableDateRule rule = new()
		{
			Name = "Probe",
			Strategy = DateResolutionStrategy.Fixed,
			Category = NotableDateCategory.Holiday,
			Month = 7,
			Day = 4,
			FirstYear = 2026,
			LastYear = 2026,
			IsNonWorkingDay = true,
			Adjustments = ImmutableArray.Create(new ObservanceAdjustment
			{
				Key = "missing-target",
				Trigger = AdjustmentTrigger.Always,
				Action = AdjustmentAction.ReplaceWithNamedDate,
				TargetRuleName = targetRuleName,
				IsNonWorkingDay = true,
			}),
		};

		NotableDateService service = BuildService(rule);

		IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
			new DateTime(2026, 7, 1),
			new DateTime(2026, 7, 31));

		NotableDate match = resolved.Single(n => n.Name == "Probe");
		Assert.AreEqual(new DateTime(2026, 7, 4), match.Date);
	}

	// =====================================================================================================================
	// Custom trigger / handler degenerate paths
	// =====================================================================================================================

	/// <summary>
	/// Verifies that an adjustment with <see cref="AdjustmentTrigger.Custom" /> and no <see cref="ObservanceAdjustment.HandlerKey" />
	/// does not activate — the adjuster has no handler to delegate to and falls back to a not-activated outcome.
	/// </summary>
	[TestMethod]
	public void Resolve_WhenCustomTriggerHasNoHandlerKey_ShouldNotActivate()
	{
		NotableDateRule rule = new()
		{
			Name = "Probe",
			Strategy = DateResolutionStrategy.Fixed,
			Category = NotableDateCategory.Holiday,
			Month = 7,
			Day = 4,
			FirstYear = 2026,
			LastYear = 2026,
			IsNonWorkingDay = true,
			Adjustments = ImmutableArray.Create(new ObservanceAdjustment
			{
				Key = "no-handler-key",
				Trigger = AdjustmentTrigger.Custom,
				Action = AdjustmentAction.AddDays,
				OffsetDays = 5,
				IsNonWorkingDay = true,
			}),
		};

		NotableDateService service = BuildService(rule);

		IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
			new DateTime(2026, 7, 1),
			new DateTime(2026, 7, 31));

		NotableDate match = resolved.Single(n => n.Name == "Probe");
		Assert.AreEqual(new DateTime(2026, 7, 4), match.Date);
		Assert.IsFalse(match.WasAdjusted);
	}

	/// <summary>
	/// Verifies that an adjustment with <see cref="AdjustmentTrigger.Custom" /> whose handler key is not registered does
	/// not activate.
	/// </summary>
	[TestMethod]
	public void Resolve_WhenCustomTriggerHandlerIsNotRegistered_ShouldNotActivate()
	{
		NotableDateRule rule = new()
		{
			Name = "Probe",
			Strategy = DateResolutionStrategy.Fixed,
			Category = NotableDateCategory.Holiday,
			Month = 7,
			Day = 4,
			FirstYear = 2026,
			LastYear = 2026,
			IsNonWorkingDay = true,
			Adjustments = ImmutableArray.Create(new ObservanceAdjustment
			{
				Key = "missing-handler",
				Trigger = AdjustmentTrigger.Custom,
				Action = AdjustmentAction.Custom,
				HandlerKey = "never-registered",
				OffsetDays = 5,
				IsNonWorkingDay = true,
			}),
		};

		NotableDateService service = new(
			ruleProviders: new[] { (INotableDateRuleProvider)new InMemoryRuleProvider(rule) },
			weekendDefinition: CalendarWeekendDefinition.SaturdaySunday,
			adjustmentHandlers: new AdjustmentHandlerRegistry());

		IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
			new DateTime(2026, 7, 1),
			new DateTime(2026, 7, 31));

		NotableDate match = resolved.Single(n => n.Name == "Probe");
		Assert.AreEqual(new DateTime(2026, 7, 4), match.Date);
		Assert.IsFalse(match.WasAdjusted);
	}

	/// <summary>
	/// Verifies that a custom <see cref="IAdjustmentHandler" /> that throws is treated as not-activated — the adjuster
	/// catches the exception so a single misbehaving handler does not abort the entire resolution.
	/// </summary>
	[TestMethod]
	public void Resolve_WhenCustomHandlerThrows_ShouldNotActivate()
	{
		NotableDateRule rule = new()
		{
			Name = "Probe",
			Strategy = DateResolutionStrategy.Fixed,
			Category = NotableDateCategory.Holiday,
			Month = 7,
			Day = 4,
			FirstYear = 2026,
			LastYear = 2026,
			IsNonWorkingDay = true,
			Adjustments = ImmutableArray.Create(new ObservanceAdjustment
			{
				Key = "throwing",
				Trigger = AdjustmentTrigger.Custom,
				Action = AdjustmentAction.Custom,
				HandlerKey = "thrower",
				IsNonWorkingDay = true,
			}),
		};

		AdjustmentHandlerRegistry handlers = new();
		handlers.Register("thrower", new ThrowingAdjustmentHandler());

		NotableDateService service = new(
			ruleProviders: new[] { (INotableDateRuleProvider)new InMemoryRuleProvider(rule) },
			weekendDefinition: CalendarWeekendDefinition.SaturdaySunday,
			adjustmentHandlers: handlers);

		IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
			new DateTime(2026, 7, 1),
			new DateTime(2026, 7, 31));

		NotableDate match = resolved.Single(n => n.Name == "Probe");
		Assert.AreEqual(new DateTime(2026, 7, 4), match.Date);
		Assert.IsFalse(match.WasAdjusted);
	}

	// =====================================================================================================================
	// DateTimeKind insensitivity — request boundaries should normalise regardless of Kind
	// =====================================================================================================================

	/// <summary>
	/// Verifies that the same calendar day expressed with different <see cref="DateTimeKind" /> values produces identical
	/// emission results — the pipeline normalises via <c>.Date</c> and the kind component does not influence resolution.
	/// </summary>
	[DataTestMethod]
	[DataRow((int)DateTimeKind.Unspecified)]
	[DataRow((int)DateTimeKind.Utc)]
	[DataRow((int)DateTimeKind.Local)]
	public void Resolve_WhenRequestBoundaryHasNonDefaultDateTimeKind_ShouldProduceSameResults(int kindAsInt)
	{
		NotableDateRule rule = new()
		{
			Name = "Probe",
			Strategy = DateResolutionStrategy.Fixed,
			Category = NotableDateCategory.Holiday,
			Month = 7,
			Day = 4,
			FirstYear = 2026,
			LastYear = 2026,
		};

		NotableDateService service = BuildService(rule);

		DateTimeKind kind = (DateTimeKind)kindAsInt;
		DateTime start = DateTime.SpecifyKind(new DateTime(2026, 7, 1), kind);
		DateTime end = DateTime.SpecifyKind(new DateTime(2026, 7, 31), kind);

		IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(start, end);

		NotableDate match = resolved.Single(n => n.Name == "Probe");
		Assert.AreEqual(new DateTime(2026, 7, 4), match.Date);
	}

	// =====================================================================================================================
	// Documented limitations — currently throw / lose data; ignored until fixed
	// =====================================================================================================================

	/// <summary>
	/// Documents a current pipeline gap: <see cref="AdjustmentAction.AddDays" /> with an offset that pushes the result past
	/// <see cref="DateTime.MaxValue" /> propagates the underlying <see cref="ArgumentOutOfRangeException" /> instead of
	/// silently skipping the adjustment. Re-enable this test when the pipeline catches the overflow during adjustment
	/// evaluation in the same way it already does for offset-rule projection.
	/// </summary>
	[Ignore("Pending: pipeline does not currently catch ArgumentOutOfRangeException from DateTime.AddDays during adjustment evaluation.")]
	[TestMethod]
	public void Resolve_WhenAdjustmentAddDaysOverflowsMaxValue_ShouldSilentlySkip()
	{
		NotableDateRule rule = new()
		{
			Name = "Overflow Probe",
			Strategy = DateResolutionStrategy.Fixed,
			Category = NotableDateCategory.Holiday,
			Month = 12,
			Day = 30,
			FirstYear = 9999,
			LastYear = 9999,
			IsNonWorkingDay = true,
			Adjustments = ImmutableArray.Create(new ObservanceAdjustment
			{
				Key = "overflow",
				Trigger = AdjustmentTrigger.Always,
				Action = AdjustmentAction.AddDays,
				OffsetDays = 100, // 30 Dec 9999 + 100 = year 10000 → overflow
				IsNonWorkingDay = true,
			}),
		};

		NotableDateService service = BuildService(rule);

		// Pipeline should silently skip the adjustment, leaving the base anchor visible.
		IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
			new DateTime(9999, 12, 1),
			DateTime.MaxValue.Date);

		NotableDate match = resolved.Single(n => n.Name == "Overflow Probe");
		Assert.AreEqual(new DateTime(9999, 12, 30), match.Date);
		Assert.IsFalse(match.WasAdjusted);
	}

	/// <summary>
	/// Documents a current pipeline gap: an <see cref="INotableDateAlgorithm" /> that throws an arbitrary exception (anything
	/// other than <see cref="InvalidOperationException" />) propagates out of the resolver and aborts the entire request.
	/// Re-enable this test when the pipeline catches algorithm exceptions and treats them as a missing date for the year.
	/// </summary>
	[Ignore("Pending: pipeline does not currently catch arbitrary exceptions thrown by INotableDateAlgorithm implementations.")]
	[TestMethod]
	public void Resolve_WhenAlgorithmThrowsArbitraryException_ShouldSilentlySkipThatYear()
	{
		NotableDateRule rule = new()
		{
			Name = "Throwing Algorithm Anchor",
			Strategy = DateResolutionStrategy.Algorithm,
			Category = NotableDateCategory.Religious,
			AlgorithmKey = "thrower",
		};

		NotableDateAlgorithmRegistry registry = new();
		registry.Register("thrower", new ThrowingAlgorithm());

		NotableDateService service = new(
			ruleProviders: new[] { (INotableDateRuleProvider)new InMemoryRuleProvider(rule) },
			weekendDefinition: CalendarWeekendDefinition.SaturdaySunday,
			algorithmRegistry: registry);

		IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
			new DateTime(2026, 1, 1),
			new DateTime(2026, 12, 31));

		Assert.IsFalse(resolved.Any(n => n.Name == "Throwing Algorithm Anchor"));
	}

	// =====================================================================================================================
	// Helpers — local stubs scoped to this partial
	// =====================================================================================================================

	/// <summary>
	/// An algorithm that always returns <see langword="null" />, simulating a calendar system that does not produce a date
	/// for the requested year.
	/// </summary>
	private sealed class AlwaysNullAlgorithm : INotableDateAlgorithm
	{
		public DateTime? GetDate(int year, System.Globalization.Calendar? calendar = null) => null;
	}

	/// <summary>
	/// An algorithm that always throws — used to document the pipeline's current behaviour when an algorithm misbehaves.
	/// </summary>
	private sealed class ThrowingAlgorithm : INotableDateAlgorithm
	{
		public DateTime? GetDate(int year, System.Globalization.Calendar? calendar = null) =>
			throw new InvalidCastException("Simulated algorithm failure.");
	}

	/// <summary>
	/// A custom adjustment handler that always throws — used to verify the adjuster swallows handler exceptions.
	/// </summary>
	private sealed class ThrowingAdjustmentHandler : IAdjustmentHandler
	{
		public AdjustmentHandlerResult Apply(AdjustmentHandlerContext context) =>
			throw new InvalidOperationException("Simulated handler failure.");
	}
}
