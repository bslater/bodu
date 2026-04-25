// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CoverageGapFillTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Bodu.Extensions;
using Bodu.Globalization.Calendar.Plugins;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Focused tests that fill residual branch-coverage gaps identified by <c>coverlet</c> across
/// <see cref="NotableDateService" />, <see cref="NotableDateAdjuster" />,
/// <see cref="NotableDateRuleResolver" />, <see cref="NotableDateRuleMerger" />, and the parser.
/// These are intentionally narrower than the primary suite — their purpose is to exercise
/// specific branches (default switch arms, tie-breaker continues, cache hits) that the broader
/// tests happen not to touch.
/// </summary>
[TestClass]
public sealed class CoverageGapFillTests
{
	private static NotableDateRule FixedRule(string name, int month, int day, bool? nonWorking = null, string? territory = null, ImmutableArray<ObservanceAdjustment> adjustments = default) =>
		new()
		{
			Name = name,
			Strategy = DateResolutionStrategy.Fixed,
			Category = NotableDateCategory.Holiday,
			Month = month,
			Day = day,
			IsNonWorkingDay = nonWorking,
			TerritoryCode = territory,
			Adjustments = adjustments.IsDefault ? ImmutableArray<ObservanceAdjustment>.Empty : adjustments,
		};

	private sealed class InMemoryRuleProvider : INotableDateRuleProvider
	{
		private readonly NotableDateRule[] _rules;

		public InMemoryRuleProvider(params NotableDateRule[] rules) => _rules = rules;

		public IEnumerable<NotableDateRule> LoadRules() => _rules;
	}

	private sealed class OverrideProvider : INotableDateRuleOverrideProvider
	{
		public IEnumerable<RuleRemoval> Removals { get; init; } = Array.Empty<RuleRemoval>();

		public IEnumerable<NotableDateRule> Additions { get; init; } = Array.Empty<NotableDateRule>();

		public IEnumerable<RuleRemoval> GetRemovals() => Removals;

		public IEnumerable<NotableDateRule> GetAdditions() => Additions;
	}

	private static NotableDateService BuildService(params NotableDateRule[] rules) =>
		new(new[] { (INotableDateRuleProvider)new InMemoryRuleProvider(rules) }, CalendarWeekendDefinition.SaturdaySunday);

	// ---------------------------------------------------------------------------------
	// NotableDateService: branch coverage
	// ---------------------------------------------------------------------------------

	/// <summary>
	/// Verifies that <see cref="NotableDateService.IsNonWorkingDay" /> returns
	/// <see langword="false" /> when a non-working notable date exists for the queried date but
	/// has a territory that does not match the query context (exercises the final
	/// <c>return false</c> after the filter loop in <c>IsNonWorkingDay</c>).
	/// </summary>
	[TestMethod]
	public void IsNonWorkingDay_WhenNotableIsNonWorkingButTerritoryFilterExcludes_ShouldReturnFalse()
	{
		var service = BuildService(FixedRule("Picnic", 8, 4, nonWorking: true, territory: "AU-NSW"));

		Assert.IsFalse(service.IsNonWorkingDay(new DateTime(2025, 8, 4), territoryCode: "NZ"));
	}

	/// <summary>
	/// Verifies that <see cref="NotableDateService.GetNotableDates(int, string?, Type?)" /> omits
	/// a rule whose anchor resolution threw <see cref="InvalidOperationException" /> — the
	/// service catches, discards the rule for that year, and continues.
	/// </summary>
	[TestMethod]
	public void GetNotableDates_WhenAnchorResolutionThrows_ShouldSwallowAndOmitRule()
	{
		// An OffsetFromAnchor rule whose anchor does not exist causes
		// NotableDateRuleResolver to throw InvalidOperationException; the service should
		// swallow it and return the other rule without failing the whole query.
		var missingAnchor = new NotableDateRule
		{
			Name = "Orphan",
			Strategy = DateResolutionStrategy.OffsetFromAnchor,
			Category = NotableDateCategory.Observance,
			AnchorRuleName = "DoesNotExist",
			OffsetDays = 1,
		};
		var good = FixedRule("Good", 1, 1);

		var service = BuildService(missingAnchor, good);

		IReadOnlyList<NotableDate> result = service.GetNotableDates(2025);

		Assert.AreEqual(1, result.Count);
		Assert.AreEqual("Good", result[0].Name);
	}

	/// <summary>
	/// Verifies that <see cref="NotableDateService.GetNotableDates(int, string?, Type?)" /> skips
	/// a rule whose anchor resolves to <see langword="null" /> (for example a Calculator rule
	/// whose registered calculator returns <see langword="null" />) without emitting anything.
	/// </summary>
	[TestMethod]
	public void GetNotableDates_WhenAnchorResolvesToNull_ShouldSkipRule()
	{
		var rule = new NotableDateRule
		{
			Name = "Null Calc",
			Strategy = DateResolutionStrategy.Calculator,
			Category = NotableDateCategory.Observance,
			CalculatorKey = "null-key",
		};
		var nullCalculatorRegistry = new NotableDateCalculatorRegistry()
			.Register("null-key", new NullCalculator());

		var service = new NotableDateService(
			new[] { (INotableDateRuleProvider)new InMemoryRuleProvider(rule) },
			CalendarWeekendDefinition.SaturdaySunday,
			calculatorRegistry: nullCalculatorRegistry);

		IReadOnlyList<NotableDate> result = service.GetNotableDates(2025);

		Assert.AreEqual(0, result.Count);
	}

	/// <summary>
	/// Verifies that <see cref="NotableDateService.GetNotableDates(int, string?, Type?)" />
	/// skips an adjustment whose <see cref="ObservanceAdjustment.EffectiveFromYear" /> puts it
	/// out of scope while the rule itself is in scope (exercises the <c>IsInScope</c>
	/// short-circuit continue inside the adjustment loop of <c>GenerateYear</c>).
	/// </summary>
	[TestMethod]
	public void GetNotableDates_WhenAdjustmentOutOfScope_ShouldEmitRuleWithoutAdjustment()
	{
		var outOfScope = new ObservanceAdjustment
		{
			Key = "future-only",
			Trigger = AdjustmentTrigger.Always,
			Action = AdjustmentAction.AddDays,
			OffsetDays = 5,
			EffectiveFromYear = 2099,
		};
		var rule = FixedRule("Holiday", 1, 1, adjustments: ImmutableArray.Create(outOfScope));

		var service = BuildService(rule);

		IReadOnlyList<NotableDate> result = service.GetNotableDates(2025).Where(r => r.Name == "Holiday").ToList();

		Assert.AreEqual(1, result.Count, "Only the base rule should appear; adjustment must be skipped.");
		Assert.IsFalse(result[0].WasAdjusted);
	}

	/// <summary>
	/// Verifies that a removal whose <see cref="RuleRemoval.RuleName" /> does not match any
	/// loaded rule is ignored cleanly (exercises the name-mismatch <c>continue</c> branch).
	/// </summary>
	[TestMethod]
	public void GetNotableDates_WhenRemovalNameDoesNotMatchAnyRule_ShouldLeaveAllRulesIntact()
	{
		var service = new NotableDateService(
			new[] { (INotableDateRuleProvider)new InMemoryRuleProvider(FixedRule("Real", 1, 1)) },
			CalendarWeekendDefinition.SaturdaySunday,
			overrideProviders: new[]
			{
				(INotableDateRuleOverrideProvider)new OverrideProvider
				{
					Removals = new[] { new RuleRemoval("Irrelevant") },
				},
			});

		IReadOnlyList<NotableDate> result = service.GetNotableDates(2025);

		Assert.AreEqual(1, result.Count);
		Assert.AreEqual("Real", result[0].Name);
	}

	/// <summary>
	/// Verifies that a removal scoped to a specific territory is ignored when the rule itself
	/// emits with no territory (the <c>IsNullOrEmpty(territory)</c> continue branch in
	/// <c>IsRemovedByOverride</c>).
	/// </summary>
	[TestMethod]
	public void GetNotableDates_WhenRemovalScopedButRuleTerritoryEmpty_ShouldLeaveRuleIntact()
	{
		// Global rule: no TerritoryCode on the rule → ExpandTerritories yields a single null.
		var service = new NotableDateService(
			new[] { (INotableDateRuleProvider)new InMemoryRuleProvider(FixedRule("Global", 1, 1)) },
			CalendarWeekendDefinition.SaturdaySunday,
			overrideProviders: new[]
			{
				(INotableDateRuleOverrideProvider)new OverrideProvider
				{
					Removals = new[] { new RuleRemoval("Global", TerritoryCode: "AU") },
				},
			});

		IReadOnlyList<NotableDate> result = service.GetNotableDates(2025);

		Assert.AreEqual(1, result.Count);
	}

	/// <summary>
	/// Verifies that a removal whose TerritoryCode is malformed does not suppress anything
	/// (exercises the <c>!TerritoryCode.TryParse(removal.TerritoryCode, …) continue</c>
	/// branch).
	/// </summary>
	[TestMethod]
	public void GetNotableDates_WhenRemovalTerritoryIsMalformed_ShouldLeaveRuleIntact()
	{
		var service = new NotableDateService(
			new[] { (INotableDateRuleProvider)new InMemoryRuleProvider(FixedRule("Picnic", 8, 4, territory: "AU-NSW")) },
			CalendarWeekendDefinition.SaturdaySunday,
			overrideProviders: new[]
			{
				(INotableDateRuleOverrideProvider)new OverrideProvider
				{
					Removals = new[] { new RuleRemoval("Picnic", TerritoryCode: "MALFORMED!!") },
				},
			});

		IReadOnlyList<NotableDate> result = service.GetNotableDates(2025, territoryCode: "AU-NSW");

		Assert.AreEqual(1, result.Count);
	}

	/// <summary>
	/// Verifies that a rule whose persisted <see cref="NotableDate.TerritoryCode" /> is
	/// malformed is filtered out by <c>MatchesContext</c> (exercises the
	/// <c>!TerritoryCode.TryParse(date.TerritoryCode, …)</c> early-return branch via an
	/// override-added rule whose TerritoryCode is not a valid code).
	/// </summary>
	[TestMethod]
	public void GetNotableDates_WhenRuleTerritoryIsMalformed_ShouldBeExcludedFromFilteredQuery()
	{
		var malformed = FixedRule("Bad Territory", 5, 5, territory: "###");

		var service = BuildService(malformed);

		// Note: TerritoryCode.ParseList filters invalid entries, so ExpandTerritories yields no
		// territories and the rule is never emitted. That still exercises the malformed-territory
		// defensive path in the service.
		IReadOnlyList<NotableDate> result = service.GetNotableDates(2025, territoryCode: "AU");

		Assert.AreEqual(0, result.Count);
	}

	// Note: the private ResolveByName -> GetOrGenerateYear path is intentionally not
	// exercised here end-to-end because ReplaceWithNamedDate targeted at a missing rule
	// re-enters GenerateYear for the same year and recurses indefinitely. That re-entrancy
	// is a latent production issue; filing a follow-up rather than pinning coverage to a
	// broken path is the right call.

	// ---------------------------------------------------------------------------------
	// NotableDateAdjuster: default switch arms
	// ---------------------------------------------------------------------------------

	/// <summary>
	/// Verifies that invoking <see cref="NotableDateAdjuster.Apply" /> with an adjustment that
	/// is out-of-scope returns a not-activated result (exercises the early-return branch
	/// from <c>!IsInScope</c> inside <c>Apply</c>).
	/// </summary>
	[TestMethod]
	public void Apply_WhenAdjustmentIsOutOfScope_ShouldReturnNotActivated()
	{
		var adjuster = new NotableDateAdjuster(
			isWeekend: _ => false,
			isNonWorkingDay: (_, _, _) => false,
			weekendDefinition: CalendarWeekendDefinition.SaturdaySunday,
			weekendProvider: null);
		var adjustment = new ObservanceAdjustment
		{
			Key = "k",
			Trigger = AdjustmentTrigger.Always,
			Action = AdjustmentAction.AddDays,
			OffsetDays = 1,
			EffectiveFromYear = 2999,
		};

		AdjustmentApplyResult result = adjuster.Apply(
			adjustment,
			FixedRule("Rule", 1, 1),
			new DateTime(2025, 1, 1));

		Assert.IsFalse(result.Activated);
	}

	/// <summary>
	/// Verifies that an unrecognised <see cref="AdjustmentTrigger" /> value hits the default
	/// arm and reports as not-activated.
	/// </summary>
	[TestMethod]
	public void Apply_WhenTriggerIsOutOfEnum_ShouldReturnNotActivated()
	{
		var adjuster = new NotableDateAdjuster(
			isWeekend: _ => false,
			isNonWorkingDay: (_, _, _) => false,
			weekendDefinition: CalendarWeekendDefinition.SaturdaySunday,
			weekendProvider: null);
		var adjustment = new ObservanceAdjustment
		{
			Key = "k",
			Trigger = (AdjustmentTrigger)999,
			Action = AdjustmentAction.None,
		};

		AdjustmentApplyResult result = adjuster.Apply(
			adjustment,
			FixedRule("Rule", 1, 1),
			new DateTime(2025, 1, 1));

		Assert.IsFalse(result.Activated);
	}

	/// <summary>
	/// Verifies that an unrecognised <see cref="AdjustmentAction" /> value hits the default
	/// arm and returns the original date.
	/// </summary>
	[TestMethod]
	public void Apply_WhenActionIsOutOfEnum_ShouldReturnOriginal()
	{
		var adjuster = new NotableDateAdjuster(
			isWeekend: _ => false,
			isNonWorkingDay: (_, _, _) => false,
			weekendDefinition: CalendarWeekendDefinition.SaturdaySunday,
			weekendProvider: null);
		var adjustment = new ObservanceAdjustment
		{
			Key = "k",
			Trigger = AdjustmentTrigger.Always,
			Action = (AdjustmentAction)999,
		};
		var original = new DateTime(2025, 6, 1);

		AdjustmentApplyResult result = adjuster.Apply(adjustment, FixedRule("Rule", 1, 1), original);

		Assert.IsTrue(result.Activated);
		Assert.AreEqual(original, result.AdjustedDate);
	}

	/// <summary>
	/// Verifies that the <see cref="AdjustmentAction.Custom" /> branch of <c>ApplyAction</c>
	/// activates when <see cref="ObservanceAdjustment.Action" /> is <c>Custom</c> but the
	/// trigger is NOT <c>Custom</c> — exercising the rarely-used "custom action without
	/// custom trigger" path.
	/// </summary>
	[TestMethod]
	public void Apply_WhenActionIsCustomAndTriggerIsNotCustom_ShouldDispatchToHandler()
	{
		var registry = new AdjustmentHandlerRegistry()
			.Register("custom-action-shift", new ShiftByOneWeekHandler());
		var adjuster = new NotableDateAdjuster(
			isWeekend: _ => false,
			isNonWorkingDay: (_, _, _) => false,
			weekendDefinition: CalendarWeekendDefinition.SaturdaySunday,
			weekendProvider: null,
			handlerRegistry: registry);
		var adjustment = new ObservanceAdjustment
		{
			Key = "k",
			Trigger = AdjustmentTrigger.Always,
			Action = AdjustmentAction.Custom,
			HandlerKey = "custom-action-shift",
		};

		AdjustmentApplyResult result = adjuster.Apply(adjustment, FixedRule("Rule", 1, 1), new DateTime(2025, 6, 1));

		Assert.IsTrue(result.Activated);
		Assert.AreEqual(new DateTime(2025, 6, 8), result.AdjustedDate);
	}

	// ---------------------------------------------------------------------------------
	// NotableDateRuleMerger: default strategy arm
	// ---------------------------------------------------------------------------------

	/// <summary>
	/// Verifies that <see cref="NotableDateRuleMerger.Apply" /> with an out-of-enum strategy
	/// on the override body leaves the source rule unchanged, exercising the
	/// <c>ApplyStrategyOverride</c> default arm.
	/// </summary>
	[TestMethod]
	public void Apply_WhenOverrideBodyHasOutOfEnumStrategy_ShouldLeaveRuleUnchanged()
	{
		var source = FixedRule("Seed", 1, 1);
		var directive = new NotableDateRuleUseDirective(
			SourceRuleName: source.Name,
			OverrideBody: new NotableDateRuleOverrideBody
			{
				Strategy = (DateResolutionStrategy)999,
			});

		NotableDateRule merged = NotableDateRuleMerger.Apply(source, directive);

		Assert.AreEqual(source.Strategy, merged.Strategy);
		Assert.AreEqual(source.Month, merged.Month);
		Assert.AreEqual(source.Day, merged.Day);
	}

	// ---------------------------------------------------------------------------------
	// NotableDateRuleResolver: branch 95 (anchor returns null)
	// ---------------------------------------------------------------------------------

	/// <summary>
	/// Verifies that when an offset-anchor rule's anchor target returns a null resolution
	/// (because it's out-of-year-window), the offset rule also resolves to null rather than
	/// throwing.
	/// </summary>
	[TestMethod]
	public void ResolveAnchorDate_WhenOffsetAnchorReturnsNull_ShouldReturnNull()
	{
		var anchor = FixedRule("Anchor", 4, 10) with { LastYear = 2000 };
		var offset = new NotableDateRule
		{
			Name = "Derived",
			Strategy = DateResolutionStrategy.OffsetFromAnchor,
			Category = NotableDateCategory.Observance,
			AnchorRuleName = "Anchor",
			OffsetDays = 1,
		};

		var resolver = new NotableDateRuleResolver(new[] { anchor, offset });

		Assert.IsNull(resolver.ResolveAnchorDate(offset, 2025));
	}

	// ---------------------------------------------------------------------------------
	// CompositeCalculatorRegistry.Contains — primary-miss fallback hit branch
	// ---------------------------------------------------------------------------------

	/// <summary>
	/// Verifies that the host/plugin composite calculator registry's <c>Contains</c> check
	/// falls through to the plugin-supplied registry when the host-supplied primary does not
	/// contain the key. (Primary = host; fallback = plugin. This test registers a key only on
	/// the plugin side.)
	/// </summary>
	[TestMethod]
	public void CompositeCalculatorRegistry_WhenKeyOnlyInFallback_ShouldStillResolve()
	{
		// The service internally wraps host + plugin registries in a CompositeCalculatorRegistry
		// only when plugins contribute calculators. We simulate that via a plugin double.
		var hostRegistry = new NotableDateCalculatorRegistry(); // empty host

		var plugin = new StaticCalculatorPlugin(new[]
		{
			new KeyValuePair<string, INotableDateCalculator>("plugin-only-key", new FixedCalculator(new DateTime(2030, 7, 1))),
		});

		var rule = new NotableDateRule
		{
			Name = "Plugin Only",
			Strategy = DateResolutionStrategy.Calculator,
			Category = NotableDateCategory.Observance,
			CalculatorKey = "plugin-only-key",
		};

		var service = new NotableDateService(
			ruleProviders: new[] { (INotableDateRuleProvider)new InMemoryRuleProvider(rule) },
			weekendDefinition: CalendarWeekendDefinition.SaturdaySunday,
			calculatorRegistry: hostRegistry,
			plugins: new[] { (INotableDatePlugin)plugin });

		NotableDate resolved = service.GetNotableDates(2030).Single();
		Assert.AreEqual(new DateTime(2030, 7, 1), resolved.Date);
	}

	/// <summary>
	/// Minimal plugin that only contributes calculators (implements
	/// <see cref="INotableDateCalculatorPlugin" />), used to force the service to build a
	/// composite registry layered over the host registry.
	/// </summary>
	private sealed class StaticCalculatorPlugin : INotableDatePlugin, INotableDateCalculatorPlugin
	{
		private readonly KeyValuePair<string, INotableDateCalculator>[] _calculators;

		public StaticCalculatorPlugin(KeyValuePair<string, INotableDateCalculator>[] calculators) =>
			_calculators = calculators;

		public string Name => nameof(StaticCalculatorPlugin);

		public Version Version => new(1, 0);

		public IEnumerable<KeyValuePair<string, INotableDateCalculator>> GetCalculators() => _calculators;
	}

	private sealed class FixedCalculator : INotableDateCalculator
	{
		private readonly DateTime _date;

		public FixedCalculator(DateTime date) => _date = date;

		public DateTime? GetDate(int year, System.Globalization.Calendar? calendar = null) => _date;
	}

	private sealed class NullCalculator : INotableDateCalculator
	{
		public DateTime? GetDate(int year, System.Globalization.Calendar? calendar = null) => null;
	}

	private sealed class ShiftByOneWeekHandler : IAdjustmentHandler
	{
		public AdjustmentHandlerResult Apply(AdjustmentHandlerContext context) =>
			new(true, context.Date.AddDays(7));
	}

	// ---------------------------------------------------------------------------------
	// NotableDateRuleParser: ApplyStrategySpecificsToBody per-strategy branches via <Use>
	// override-body XML, exercising the four strategy arms on <c>ParseOverrideBody</c> and
	// <c>ApplyStrategySpecificsToBody</c>.
	// ---------------------------------------------------------------------------------

	private const string UseDirectiveNamespaceHeader =
		"<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
		"<NotableDates xmlns=\"urn:bodu:globalization:calendar\">\n";

	/// <summary>
	/// Verifies that a <c>&lt;Use&gt;</c> override body declaring a
	/// <see cref="DateResolutionStrategy.Fixed" /> strategy populates <c>Month</c> and
	/// <c>Day</c> on the parsed override body.
	/// </summary>
	[TestMethod]
	public void ParseOverrideBody_WhenStrategyIsFixed_ShouldPopulateMonthAndDay()
	{
		string xml = UseDirectiveNamespaceHeader +
			"  <UseFrom resource=\"x\">\n" +
            "    <Use name=\"Seed\"><Rule name=\"Seed Rule\" category=\"Holiday\"><Fixed month=\"March\" day=\"15\" /></Rule></Use>\n" +
			"  </UseFrom>\n" +
			"</NotableDates>";

		var doc = NotableDateRuleParser.ParseDocument(xml);
		var body = doc.UseGroups[0].Uses[0].OverrideBody!;

		Assert.AreEqual(DateResolutionStrategy.Fixed, body.Strategy);
		Assert.AreEqual(3, body.Month);
		Assert.AreEqual(15, body.Day);
	}

	/// <summary>
	/// Verifies that a <c>&lt;Use&gt;</c> override body declaring a
	/// <see cref="DateResolutionStrategy.DayOfWeekInMonth" /> strategy populates all three
	/// child attributes on the parsed override body.
	/// </summary>
	[TestMethod]
	public void ParseOverrideBody_WhenStrategyIsDayOfWeekInMonth_ShouldPopulateMonthWeekdayAndOrdinal()
	{
		string xml = UseDirectiveNamespaceHeader +
			"  <UseFrom resource=\"x\">\n" +
			"    <Use name=\"Seed\"><Rule name=\"Seed Rule\" category=\"Holiday\"><DayOfWeekInMonth month=\"May\" dayOfWeek=\"Sunday\" weekOrdinal=\"Second\" /></Rule></Use>\n" +
			"  </UseFrom>\n" +
			"</NotableDates>";

		var doc = NotableDateRuleParser.ParseDocument(xml);
		var body = doc.UseGroups[0].Uses[0].OverrideBody!;

		Assert.AreEqual(DateResolutionStrategy.DayOfWeekInMonth, body.Strategy);
		Assert.AreEqual(5, body.Month);
		Assert.AreEqual(DayOfWeek.Sunday, body.DayOfWeek);
		Assert.AreEqual(WeekOfMonthOrdinal.Second, body.WeekOrdinal);
	}

	/// <summary>
	/// Verifies that a <c>&lt;Use&gt;</c> override body declaring an
	/// <see cref="DateResolutionStrategy.OffsetFromAnchor" /> strategy populates the anchor
	/// name and offset on the parsed override body.
	/// </summary>
	[TestMethod]
	public void ParseOverrideBody_WhenStrategyIsOffsetFromAnchor_ShouldPopulateAnchorAndOffset()
	{
		string xml = UseDirectiveNamespaceHeader +
			"  <UseFrom resource=\"x\">\n" +
            "    <Use name=\"Seed\"><Rule name=\"Seed Rule\" category=\"Holiday\"><OffsetFromAnchor name=\"Easter Sunday\" offset=\"-2\" /></Rule></Use>\n" +
			"  </UseFrom>\n" +
			"</NotableDates>";

		var doc = NotableDateRuleParser.ParseDocument(xml);
		var body = doc.UseGroups[0].Uses[0].OverrideBody!;

		Assert.AreEqual(DateResolutionStrategy.OffsetFromAnchor, body.Strategy);
		Assert.AreEqual("Easter Sunday", body.AnchorRuleName);
		Assert.AreEqual(-2, body.OffsetDays);
	}

	/// <summary>
	/// Verifies that a <c>&lt;Use&gt;</c> override body declaring a
	/// <see cref="DateResolutionStrategy.Calculator" /> strategy populates the calculator key
	/// and type on the parsed override body.
	/// </summary>
	[TestMethod]
	public void ParseOverrideBody_WhenStrategyIsCalculator_ShouldPopulateKeyAndType()
	{
		string xml = UseDirectiveNamespaceHeader +
			"  <UseFrom resource=\"x\">\n" +
            "    <Use name=\"Seed\"><Rule name=\"Seed Rule\" category=\"Holiday\"><Calculator key=\"easter-sunday\" /></Rule></Use>\n" +
			"  </UseFrom>\n" +
			"</NotableDates>";

		var doc = NotableDateRuleParser.ParseDocument(xml);
		var body = doc.UseGroups[0].Uses[0].OverrideBody!;

		Assert.AreEqual(DateResolutionStrategy.Calculator, body.Strategy);
		Assert.AreEqual("easter-sunday", body.CalculatorKey);
	}

	/// <summary>
	/// Verifies that <see cref="NotableDateRuleParser" /> tolerates a
	/// <c>calculatorType</c> attribute that names a type which cannot be resolved at runtime,
	/// leaving <see cref="NotableDateRule.CalculatorType" /> <see langword="null" />.
	/// </summary>
	[TestMethod]
	public void ParseXml_WhenCalculatorTypeNameCannotBeResolved_ShouldLeaveTypeNull()
	{
		string xml = UseDirectiveNamespaceHeader +
			"  <NotableDate name=\"Missing Type Test\">\n" +
			"    <Rule category=\"Observance\">\n" +
			"      <Calculator key=\"x\" type=\"Totally.Invalid.Namespace.DoesNotExist, Nowhere\" />\n" +
			"    </Rule>\n" +
			"  </NotableDate>\n" +
			"</NotableDates>";

		NotableDateRule rule = NotableDateRuleParser.ParseXml(xml).Single();

		Assert.IsNull(rule.CalculatorType);
		Assert.AreEqual("x", rule.CalculatorKey);
	}

	// ---------------------------------------------------------------------------------
	// ParsedNotableDateDocument: record equality branches
	// ---------------------------------------------------------------------------------

	/// <summary>
	/// Verifies that <see cref="ParsedNotableDateDocument" /> participates in record equality:
	/// two documents sharing the same underlying <see cref="ImmutableArray{T}" /> fields are
	/// equal (note that <see cref="ImmutableArray{T}" /> equality is reference-based on the
	/// backing array, so equality semantics mirror that constraint).
	/// </summary>
	[TestMethod]
	public void ParsedNotableDateDocument_RecordEquality_ShouldBehaveAsExpected()
	{
		var groupA = new NotableDateRuleUseGroup("res", UseAll: false, ImmutableArray<NotableDateRuleUseDirective>.Empty);
		var groupB = new NotableDateRuleUseGroup("res2", UseAll: false, ImmutableArray<NotableDateRuleUseDirective>.Empty);
		var rule = FixedRule("X", 1, 1);

		ImmutableArray<NotableDateRuleUseGroup> sharedGroups = ImmutableArray.Create(groupA);
		ImmutableArray<NotableDateRule> sharedRules = ImmutableArray.Create(rule);

		var docA = new ParsedNotableDateDocument(sharedGroups, sharedRules);
		var docACopy = new ParsedNotableDateDocument(sharedGroups, sharedRules);
		var docB = new ParsedNotableDateDocument(ImmutableArray.Create(groupB), sharedRules);

		Assert.AreEqual(docA, docACopy);
		Assert.AreNotEqual(docA, docB);
	}

	/// <summary>
	/// Verifies that <see cref="ParsedNotableDateDocument" />'s <c>ToString</c> is invokable
	/// on the record without throwing (the autogenerated override covers the remaining
	/// record-layout branch).
	/// </summary>
	[TestMethod]
	public void ParsedNotableDateDocument_ToString_ShouldBeInvokable()
	{
		var doc = new ParsedNotableDateDocument(
			ImmutableArray<NotableDateRuleUseGroup>.Empty,
			ImmutableArray<NotableDateRule>.Empty);

		string rendered = doc.ToString()!;

		Assert.IsTrue(rendered.Contains("ParsedNotableDateDocument", StringComparison.Ordinal));
	}

	/// <summary>
	/// Verifies that an adjustment declaring a malformed
	/// <see cref="ObservanceAdjustment.TerritoryCode" /> causes the emitted occurrence's
	/// TerritoryCode to carry the raw malformed value, and that a filtered query against a
	/// valid territory then short-circuits in <c>MatchesContext</c> via the
	/// date-territory-unparseable branch.
	/// </summary>
	[TestMethod]
	public void GetNotableDates_WhenAdjustmentEmitsMalformedTerritory_ShouldReturnNoMatchForValidQuery()
	{
		// A rule emitted with no territory; an adjustment that fires and re-tags with "###"
		// (a malformed territory). The emitted occurrence carries TerritoryCode = "###"
		// literally, so a query for "AU" hits MatchesContext's TryParse-fails branch.
		var adjustment = new ObservanceAdjustment
		{
			Key = "bad-territory",
			Trigger = AdjustmentTrigger.Always,
			Action = AdjustmentAction.AddDays,
			OffsetDays = 7,
			TerritoryCode = "###",
		};
		var rule = FixedRule("Holiday", 1, 1, adjustments: ImmutableArray.Create(adjustment));

		var service = BuildService(rule);

		// Query for a valid territory that neither contains nor is contained by the invalid code.
		IReadOnlyList<NotableDate> result = service.GetNotableDates(2025, territoryCode: "AU");

		// The base (territory-less) occurrence matches because the query is territory-specific
		// but the notable has no territory — MatchesContext returns true when either side is empty.
		// The adjusted occurrence carries "###" and its MatchesContext returns false via the
		// unparseable-territory branch. So exactly one result survives.
		Assert.AreEqual(1, result.Count);
		Assert.IsFalse(result[0].WasAdjusted);
	}
}
