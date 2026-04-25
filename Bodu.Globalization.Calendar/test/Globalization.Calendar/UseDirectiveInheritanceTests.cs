// ---------------------------------------------------------------------------------------------------------------
// <copyright file="UseDirectiveInheritanceTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Extensions;
using System.Collections.Immutable;
using System.Linq;
using System.Xml.Schema;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the extensible inheritance model for <c>&lt;Use&gt;</c> cherry-pick directives: nested <c>&lt;Rule&gt;</c> override body
/// parsing, additive <c>&lt;Tag&gt;</c> merging, keyed <c>&lt;Adjustment&gt;</c> merging with replace-or-append semantics, and the
/// <c>clearTags</c> / <c>clearAdjustments</c> opt-in reset flags. Parser coverage exercises the XML shape; merger coverage
/// exercises the pure merge algorithm via <see cref="NotableDateRuleMerger" />.
/// </summary>
[TestClass]
public sealed class UseDirectiveInheritanceTests
{
	private const string NamespaceHeader =
		"<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
		"<NotableDates xmlns=\"urn:bodu:globalization:calendar\">\n";

	// ------------------------------------------------------------------------------------------
	// Parser: XML shape
	// ------------------------------------------------------------------------------------------

	/// <summary>
	/// Verifies that an <c>&lt;Adjustment&gt;</c> element without a <c>key</c> attribute fails schema validation at parse time.
	/// </summary>
	[TestMethod]
	public void ParseDocument_WhenAdjustmentMissingKey_ShouldThrowSchemaValidationException()
	{
		var xml = NamespaceHeader +
			"  <NotableDate name=\"Test\">\n" +
			"    <Rule category=\"Holiday\">\n" +
			"      <Fixed month=\"January\" day=\"1\" />\n" +
			"      <Adjustment when=\"IfWeekend\" action=\"MoveToNextWeekday\" />\n" +
			"    </Rule>\n" +
			"  </NotableDate>\n" +
			"</NotableDates>";

		Assert.ThrowsExactly<XmlSchemaValidationException>(() => NotableDateRuleParser.ParseDocument(xml));
	}

	/// <summary>
	/// Verifies that a <c>&lt;Use&gt;</c> directive carrying a nested <c>&lt;Rule&gt;</c> is captured on the parsed directive's
	/// <see cref="NotableDateRuleUseDirective.OverrideBody" /> and records the body's scalar override, tags, and adjustments.
	/// </summary>
	[TestMethod]
	public void ParseDocument_WhenUseHasNestedRuleBody_ShouldPopulateOverrideBody()
	{
		var xml = NamespaceHeader +
			"  <UseFrom resource=\"Bodu.Globalization.Calendar.Resources.Common.xml\">\n" +
			"    <Use name=\"New Year's Day\" clearAdjustments=\"true\">\n" +
            "      <Rule name=\"New Year's Day\" category=\"Holiday\" territory=\"AU\" nonWorking=\"true\">\n" +
			"        <Tag>NationalHoliday</Tag>\n" +
			"        <Adjustment key=\"weekend-roll\" when=\"IfWeekend\" action=\"MoveToNextWeekday\" />\n" +
			"      </Rule>\n" +
			"    </Use>\n" +
			"  </UseFrom>\n" +
			"</NotableDates>";

		var document = NotableDateRuleParser.ParseDocument(xml);
		var directive = document.UseGroups[0].Uses[0];

		Assert.AreEqual("New Year's Day", directive.SourceRuleName);
		Assert.IsTrue(directive.ClearAdjustments);
		Assert.IsNotNull(directive.OverrideBody);
		Assert.AreEqual("AU", directive.OverrideBody!.TerritoryCode);
		Assert.AreEqual(true, directive.OverrideBody.IsNonWorkingDay);
		CollectionAssert.Contains(directive.OverrideBody.Tags.ToArray(), "NationalHoliday");
		Assert.AreEqual(1, directive.OverrideBody.Adjustments.Length);
		Assert.AreEqual("weekend-roll", directive.OverrideBody.Adjustments[0].Key);
	}

	/// <summary>
	/// Verifies that an override body with no strategy element records a null <see cref="NotableDateRuleOverrideBody.Strategy" />,
	/// signalling to the merger that the inherited strategy should be kept intact.
	/// </summary>
	[TestMethod]
	public void ParseDocument_WhenUseHasNestedRuleWithoutStrategy_ShouldLeaveStrategyNull()
	{
		var xml = NamespaceHeader +
			"  <UseFrom resource=\"Bodu.Globalization.Calendar.Resources.Common.xml\">\n" +
			"    <Use name=\"Halloween\">\n" +
            "      <Rule name=\"Halloween\" category=\"Observance\" territory=\"AU\">\n" +
			"        <Tag>Cultural</Tag>\n" +
			"      </Rule>\n" +
			"    </Use>\n" +
			"  </UseFrom>\n" +
			"</NotableDates>";

		var directive = NotableDateRuleParser.ParseDocument(xml).UseGroups[0].Uses[0];

		Assert.IsNotNull(directive.OverrideBody);
		Assert.IsNull(directive.OverrideBody!.Strategy);
	}

	/// <summary>
	/// Verifies that two override adjustments sharing the same key inside one <c>&lt;Use&gt;</c> directive are rejected at parse
	/// time, protecting the uniqueness invariant the merge algorithm relies on.
	/// </summary>
	[TestMethod]
	public void ParseDocument_WhenOverrideDeclaresDuplicateAdjustmentKeys_ShouldThrow()
	{
		var xml = NamespaceHeader +
			"  <UseFrom resource=\"Bodu.Globalization.Calendar.Resources.Common.xml\">\n" +
			"    <Use name=\"New Year's Day\">\n" +
            "      <Rule name=\"New Year's Day\" category=\"Holiday\" territory=\"AU\">\n" +
			"        <Adjustment key=\"dup\" when=\"Always\" action=\"None\" />\n" +
			"        <Adjustment key=\"dup\" when=\"IfWeekend\" action=\"None\" />\n" +
			"      </Rule>\n" +
			"    </Use>\n" +
			"  </UseFrom>\n" +
			"</NotableDates>";

		Assert.ThrowsExactly<InvalidOperationException>(() => NotableDateRuleParser.ParseDocument(xml));
	}

	// ------------------------------------------------------------------------------------------
	// Merger: algorithm
	// ------------------------------------------------------------------------------------------

	/// <summary>
	/// Verifies that applying a directive with no override body falls back to the legacy flat-attribute behaviour — existing
	/// cherry-picks in US.xml / GB.xml / FR.xml / AU.xml continue to produce the same merged rule.
	/// </summary>
	[TestMethod]
	public void Apply_WhenDirectiveHasNoOverrideBody_ShouldApplyFlatAttributesOnly()
	{
		var source = SampleSourceRule();
		var directive = new NotableDateRuleUseDirective(
			SourceRuleName: source.Name,
			TerritoryCode: "AU",
			IsNonWorkingDay: true);

		var merged = NotableDateRuleMerger.Apply(source, directive);

		Assert.AreEqual("AU", merged.TerritoryCode);
		Assert.AreEqual(true, merged.IsNonWorkingDay);
		Assert.AreEqual(source.Strategy, merged.Strategy);
		CollectionAssert.AreEquivalent(source.Tags.ToArray(), merged.Tags.ToArray());
		CollectionAssert.AreEqual(source.Adjustments.ToArray(), merged.Adjustments.ToArray());
	}

	/// <summary>
	/// Verifies that override tags merge additively with inherited tags under set semantics.
	/// </summary>
	[TestMethod]
	public void Apply_WhenOverrideBodyAddsTag_ShouldUnionWithInheritedTags()
	{
		var source = SampleSourceRule() with { Tags = ImmutableHashSet.Create(StringComparer.OrdinalIgnoreCase, "Christian") };
		var directive = new NotableDateRuleUseDirective(
			SourceRuleName: source.Name,
			OverrideBody: new NotableDateRuleOverrideBody
			{
				Tags = ImmutableArray.Create("NationalHoliday"),
			});

		var merged = NotableDateRuleMerger.Apply(source, directive);

		CollectionAssert.AreEquivalent(new[] { "Christian", "NationalHoliday" }, merged.Tags.ToArray());
	}

	/// <summary>
	/// Verifies that an override adjustment with a key matching one on the inherited rule replaces it in place, preserving its
	/// position in the sequence.
	/// </summary>
	[TestMethod]
	public void Apply_WhenOverrideAdjustmentKeyMatchesInherited_ShouldReplaceInPlace()
	{
		var inherited = new ObservanceAdjustment
		{
			Key = "weekend-roll",
			Trigger = AdjustmentTrigger.IfWeekend,
			Action = AdjustmentAction.MoveToNextWeekday,
		};
		var source = SampleSourceRule() with { Adjustments = ImmutableArray.Create(inherited) };

		var overrideAdjustment = new ObservanceAdjustment
		{
			Key = "weekend-roll",
			Trigger = AdjustmentTrigger.IfWeekend,
			Action = AdjustmentAction.MoveToPreviousWeekday,
		};
		var directive = new NotableDateRuleUseDirective(
			SourceRuleName: source.Name,
			OverrideBody: new NotableDateRuleOverrideBody
			{
				Adjustments = ImmutableArray.Create(overrideAdjustment),
			});

		var merged = NotableDateRuleMerger.Apply(source, directive);

		Assert.AreEqual(1, merged.Adjustments.Length);
		Assert.AreEqual(AdjustmentAction.MoveToPreviousWeekday, merged.Adjustments[0].Action);
	}

	/// <summary>
	/// Verifies that an override adjustment carrying a new key is appended to the inherited adjustment list.
	/// </summary>
	[TestMethod]
	public void Apply_WhenOverrideAdjustmentHasNewKey_ShouldAppend()
	{
		var inherited = new ObservanceAdjustment
		{
			Key = "weekend-roll",
			Trigger = AdjustmentTrigger.IfWeekend,
			Action = AdjustmentAction.MoveToNextWeekday,
		};
		var source = SampleSourceRule() with { Adjustments = ImmutableArray.Create(inherited) };

		var extra = new ObservanceAdjustment
		{
			Key = "extra",
			Trigger = AdjustmentTrigger.Always,
			Action = AdjustmentAction.None,
		};
		var directive = new NotableDateRuleUseDirective(
			SourceRuleName: source.Name,
			OverrideBody: new NotableDateRuleOverrideBody
			{
				Adjustments = ImmutableArray.Create(extra),
			});

		var merged = NotableDateRuleMerger.Apply(source, directive);

		Assert.AreEqual(2, merged.Adjustments.Length);
		Assert.AreEqual("weekend-roll", merged.Adjustments[0].Key);
		Assert.AreEqual("extra", merged.Adjustments[1].Key);
	}

	/// <summary>
	/// Verifies that <see cref="NotableDateRuleUseDirective.ClearAdjustments" /> discards the inherited adjustment list before the
	/// override is applied.
	/// </summary>
	[TestMethod]
	public void Apply_WhenClearAdjustmentsTrue_ShouldDropInheritedBeforeMerging()
	{
		var inherited = new ObservanceAdjustment
		{
			Key = "weekend-roll",
			Trigger = AdjustmentTrigger.IfWeekend,
			Action = AdjustmentAction.MoveToNextWeekday,
		};
		var source = SampleSourceRule() with { Adjustments = ImmutableArray.Create(inherited) };

		var custom = new ObservanceAdjustment
		{
			Key = "custom",
			Trigger = AdjustmentTrigger.Always,
			Action = AdjustmentAction.None,
		};
		var directive = new NotableDateRuleUseDirective(
			SourceRuleName: source.Name,
			ClearAdjustments: true,
			OverrideBody: new NotableDateRuleOverrideBody
			{
				Adjustments = ImmutableArray.Create(custom),
			});

		var merged = NotableDateRuleMerger.Apply(source, directive);

		Assert.AreEqual(1, merged.Adjustments.Length);
		Assert.AreEqual("custom", merged.Adjustments[0].Key);
	}

	/// <summary>
	/// Verifies that <see cref="NotableDateRuleUseDirective.ClearTags" /> with override tags yields a rule whose tags are exactly
	/// the override set (inherited tags discarded).
	/// </summary>
	[TestMethod]
	public void Apply_WhenClearTagsTrue_ShouldReplaceInheritedTags()
	{
		var source = SampleSourceRule() with
		{
			Tags = ImmutableHashSet.Create(StringComparer.OrdinalIgnoreCase, "Christian", "Easter"),
		};
		var directive = new NotableDateRuleUseDirective(
			SourceRuleName: source.Name,
			ClearTags: true,
			OverrideBody: new NotableDateRuleOverrideBody
			{
				Tags = ImmutableArray.Create("Secular"),
			});

		var merged = NotableDateRuleMerger.Apply(source, directive);

		CollectionAssert.AreEquivalent(new[] { "Secular" }, merged.Tags.ToArray());
	}

	/// <summary>
	/// Verifies that an override body with no declared strategy preserves the inherited strategy and its strategy-specific fields.
	/// </summary>
	[TestMethod]
	public void Apply_WhenOverrideBodyHasNoStrategy_ShouldKeepInheritedStrategy()
	{
		var source = SampleSourceRule();
		var directive = new NotableDateRuleUseDirective(
			SourceRuleName: source.Name,
			OverrideBody: new NotableDateRuleOverrideBody
			{
				TerritoryCode = "AU",
			});

		var merged = NotableDateRuleMerger.Apply(source, directive);

		Assert.AreEqual(source.Strategy, merged.Strategy);
		Assert.AreEqual(source.Month, merged.Month);
		Assert.AreEqual(source.Day, merged.Day);
	}

	/// <summary>
	/// Verifies that an override body that declares a new strategy replaces the inherited strategy wholesale and clears the stale
	/// strategy-specific fields that no longer apply.
	/// </summary>
	[TestMethod]
	public void Apply_WhenOverrideBodyDeclaresNewStrategy_ShouldReplaceAndClearStaleFields()
	{
		var source = SampleSourceRule(); // Fixed strategy with Month=1, Day=1
		var directive = new NotableDateRuleUseDirective(
			SourceRuleName: source.Name,
			OverrideBody: new NotableDateRuleOverrideBody
			{
				Strategy = DateResolutionStrategy.DayOfWeekInMonth,
				Month = 1,
				DayOfWeek = DayOfWeek.Monday,
				WeekOrdinal = WeekOfMonthOrdinal.First,
			});

		var merged = NotableDateRuleMerger.Apply(source, directive);

		Assert.AreEqual(DateResolutionStrategy.DayOfWeekInMonth, merged.Strategy);
		Assert.AreEqual(1, merged.Month);
		Assert.AreEqual(DayOfWeek.Monday, merged.DayOfWeek);
		Assert.AreEqual(WeekOfMonthOrdinal.First, merged.WeekOrdinal);
		Assert.IsNull(merged.Day, "Fixed.Day should be cleared when the strategy changes.");
	}

	/// <summary>
	/// Verifies the innermost-wins rule: when a flat <c>&lt;Use&gt;</c> attribute and a nested <c>&lt;Rule&gt;</c> body both
	/// specify the same scalar, the body value wins.
	/// </summary>
	[TestMethod]
	public void Apply_WhenFlatAndBodyBothSpecifyTerritory_ShouldUseBody()
	{
		var source = SampleSourceRule();
		var directive = new NotableDateRuleUseDirective(
			SourceRuleName: source.Name,
			TerritoryCode: "AU",
			OverrideBody: new NotableDateRuleOverrideBody
			{
				TerritoryCode = "NZ",
			});

		var merged = NotableDateRuleMerger.Apply(source, directive);

		Assert.AreEqual("NZ", merged.TerritoryCode);
	}

	// ------------------------------------------------------------------------------------------
	// Merger: strategy-override branches
	// ------------------------------------------------------------------------------------------

	/// <summary>
	/// Verifies that each branch of <see cref="NotableDateRuleMerger.ApplyStrategyOverride" />
	/// produces the correct, stale-cleared <see cref="NotableDateRule" /> for every supported
	/// target strategy.
	/// </summary>
	[DataRow(DateResolutionStrategy.Fixed)]
	[DataRow(DateResolutionStrategy.DayOfWeekInMonth)]
	[DataRow(DateResolutionStrategy.OffsetFromAnchor)]
	[DataRow(DateResolutionStrategy.Calculator)]
	[TestMethod]
	public void Apply_ApplyStrategyOverride_ShouldProduceCleanRuleForEveryTargetStrategy(DateResolutionStrategy target)
	{
		// A source rule that has every strategy-specific field set, so we can confirm that the
		// merger clears the fields which no longer apply under the new strategy.
		var source = new NotableDateRule
		{
			Name = "Seed",
			Strategy = DateResolutionStrategy.Fixed,
			Category = NotableDateCategory.Holiday,
			Month = 1,
			Day = 1,
			DayOfWeek = DayOfWeek.Monday,
			WeekOrdinal = WeekOfMonthOrdinal.First,
			AnchorRuleName = "X",
			OffsetDays = 7,
			CalculatorKey = "k",
			CalculatorType = typeof(string),
		};

		var body = new NotableDateRuleOverrideBody
		{
			Strategy = target,
			Month = 5,
			Day = 10,
			DayOfWeek = DayOfWeek.Friday,
			WeekOrdinal = WeekOfMonthOrdinal.Second,
			AnchorRuleName = "Easter Sunday",
			OffsetDays = -2,
			CalculatorKey = "new-key",
			CalculatorType = typeof(int),
		};

		var directive = new NotableDateRuleUseDirective(
			SourceRuleName: source.Name,
			OverrideBody: body);

		NotableDateRule merged = NotableDateRuleMerger.Apply(source, directive);

		Assert.AreEqual(target, merged.Strategy);
		switch (target)
		{
			case DateResolutionStrategy.Fixed:
				Assert.AreEqual(5, merged.Month);
				Assert.AreEqual(10, merged.Day);
				Assert.IsNull(merged.DayOfWeek);
				Assert.IsNull(merged.WeekOrdinal);
				Assert.IsNull(merged.AnchorRuleName);
				Assert.IsNull(merged.OffsetDays);
				Assert.IsNull(merged.CalculatorKey);
				Assert.IsNull(merged.CalculatorType);
				break;

			case DateResolutionStrategy.DayOfWeekInMonth:
				Assert.AreEqual(5, merged.Month);
				Assert.IsNull(merged.Day);
				Assert.AreEqual(DayOfWeek.Friday, merged.DayOfWeek);
				Assert.AreEqual(WeekOfMonthOrdinal.Second, merged.WeekOrdinal);
				Assert.IsNull(merged.AnchorRuleName);
				Assert.IsNull(merged.OffsetDays);
				Assert.IsNull(merged.CalculatorKey);
				Assert.IsNull(merged.CalculatorType);
				break;

			case DateResolutionStrategy.OffsetFromAnchor:
				Assert.IsNull(merged.Month);
				Assert.IsNull(merged.Day);
				Assert.IsNull(merged.DayOfWeek);
				Assert.IsNull(merged.WeekOrdinal);
				Assert.AreEqual("Easter Sunday", merged.AnchorRuleName);
				Assert.AreEqual(-2, merged.OffsetDays);
				Assert.IsNull(merged.CalculatorKey);
				Assert.IsNull(merged.CalculatorType);
				break;

			case DateResolutionStrategy.Calculator:
				Assert.IsNull(merged.Month);
				Assert.IsNull(merged.Day);
				Assert.IsNull(merged.DayOfWeek);
				Assert.IsNull(merged.WeekOrdinal);
				Assert.IsNull(merged.AnchorRuleName);
				Assert.IsNull(merged.OffsetDays);
				Assert.AreEqual("new-key", merged.CalculatorKey);
				Assert.AreEqual(typeof(int), merged.CalculatorType);
				break;
		}
	}

	/// <summary>
	/// Verifies that the name-resolution branch in <see cref="NotableDateRuleMerger" /> prefers
	/// a body-supplied name over the directive's flat <c>as</c> attribute, which in turn
	/// overrides the inherited source name.
	/// </summary>
	[DataRow(null!, null!, "Seed Name")]
	[DataRow("LocalAlias", null!, "LocalAlias")]
	[DataRow(null!, "Body Name", "Body Name")]
	[DataRow("LocalAlias", "Body Name", "Body Name")]
	[DataRow("", null!, "Seed Name")]
	[DataRow(null!, "   ", "Seed Name")]
	[TestMethod]
	public void Apply_ResolveName_Precedence(string? flatLocalName, string? bodyName, string expected)
	{
		var source = new NotableDateRule
		{
			Name = "Seed Name",
			Strategy = DateResolutionStrategy.Fixed,
			Category = NotableDateCategory.Holiday,
			Month = 1,
			Day = 1,
		};

		var directive = new NotableDateRuleUseDirective(
			SourceRuleName: source.Name,
			LocalName: flatLocalName,
			OverrideBody: bodyName is null ? null : new NotableDateRuleOverrideBody { Name = bodyName });

		NotableDateRule merged = NotableDateRuleMerger.Apply(source, directive);

		Assert.AreEqual(expected, merged.Name);
	}

	// ------------------------------------------------------------------------------------------
	// Test fixtures
	// ------------------------------------------------------------------------------------------

	private static NotableDateRule SampleSourceRule() => new()
	{
		Name = "New Year's Day",
		Strategy = DateResolutionStrategy.Fixed,
		Category = NotableDateCategory.Holiday,
		Month = 1,
		Day = 1,
		IsNonWorkingDay = true,
	};
}
