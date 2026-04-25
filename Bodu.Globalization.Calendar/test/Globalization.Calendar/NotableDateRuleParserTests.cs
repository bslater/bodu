// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleParserTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Extensions;
using System.Linq;
using System.Xml.Linq;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies that <see cref="NotableDateRuleParser" /> correctly maps every supported XML construct onto its
/// <see cref="NotableDateRule" /> equivalent.
/// </summary>
[TestClass]
public partial class NotableDateRuleParserTests
{
	/// <summary>
	/// Verifies that parsing a Fixed rule populates the rule name from the parent NotableDate element when no override is supplied.
	/// </summary>
	[TestMethod]
	public void ParseXml_WhenFixedRule_ShouldPopulateName()
	{
		var rule = NotableDateRuleParser.ParseXml(FixedRuleXml).Single();

		Assert.AreEqual("Fixed Rule Test", rule.Name);
	}

	/// <summary>
	/// Verifies that parsing a Fixed rule populates the resolution strategy as <see cref="DateResolutionStrategy.Fixed" />.
	/// </summary>
	[TestMethod]
	public void ParseXml_WhenFixedRule_ShouldPopulateStrategy()
	{
		var rule = NotableDateRuleParser.ParseXml(FixedRuleXml).Single();

		Assert.AreEqual(DateResolutionStrategy.Fixed, rule.Strategy);
	}

	/// <summary>
	/// Verifies that parsing a Fixed rule populates the explicit category attribute.
	/// </summary>
	[TestMethod]
	public void ParseXml_WhenFixedRule_ShouldPopulateCategory()
	{
		var rule = NotableDateRuleParser.ParseXml(FixedRuleXml).Single();

		Assert.AreEqual(NotableDateCategory.Holiday, rule.Category);
	}

	/// <summary>
	/// Verifies that parsing a Fixed rule populates the inclusive year window.
	/// </summary>
	[TestMethod]
	public void ParseXml_WhenFixedRule_ShouldPopulateYearWindow()
	{
		var rule = NotableDateRuleParser.ParseXml(FixedRuleXml).Single();

		Assert.AreEqual(2000, rule.FirstYear);
		Assert.AreEqual(2100, rule.LastYear);
	}

	/// <summary>
	/// Verifies that parsing a Fixed rule populates the recurrence cadence.
	/// </summary>
	[TestMethod]
	public void ParseXml_WhenFixedRule_ShouldPopulateOccurrenceYears()
	{
		var rule = NotableDateRuleParser.ParseXml(FixedRuleXml).Single();

		Assert.AreEqual(4, rule.OccurrenceYears);
	}

	/// <summary>
	/// Verifies that parsing a Fixed rule populates the multi-day duration.
	/// </summary>
	[TestMethod]
	public void ParseXml_WhenFixedRule_ShouldPopulateDurationDays()
	{
		var rule = NotableDateRuleParser.ParseXml(FixedRuleXml).Single();

		Assert.AreEqual(1, rule.DurationDays);
	}

	/// <summary>
	/// Verifies that parsing a Fixed rule populates the priority attribute.
	/// </summary>
	[TestMethod]
	public void ParseXml_WhenFixedRule_ShouldPopulatePriority()
	{
		var rule = NotableDateRuleParser.ParseXml(FixedRuleXml).Single();

		Assert.AreEqual(5, rule.Priority);
	}

	/// <summary>
	/// Verifies that parsing a Fixed rule populates the non-working flag.
	/// </summary>
	[TestMethod]
	public void ParseXml_WhenFixedRule_ShouldPopulateIsNonWorkingDay()
	{
		var rule = NotableDateRuleParser.ParseXml(FixedRuleXml).Single();

		Assert.IsTrue(rule.IsNonWorkingDay);
	}

	/// <summary>
	/// Verifies that parsing a Fixed rule populates the territory string.
	/// </summary>
	[TestMethod]
	public void ParseXml_WhenFixedRule_ShouldPopulateTerritoryCode()
	{
		var rule = NotableDateRuleParser.ParseXml(FixedRuleXml).Single();

		Assert.AreEqual("AU-NSW", rule.TerritoryCode);
	}

	/// <summary>
	/// Verifies that parsing a Fixed rule populates the calendar type from a fully qualified name.
	/// </summary>
	[TestMethod]
	public void ParseXml_WhenFixedRule_ShouldPopulateCalendarType()
	{
		var rule = NotableDateRuleParser.ParseXml(FixedRuleXml).Single();

		Assert.AreEqual(typeof(System.Globalization.GregorianCalendar), rule.CalendarType);
	}

	/// <summary>
	/// Verifies that parsing a Fixed rule populates the freeform comment.
	/// </summary>
	[TestMethod]
	public void ParseXml_WhenFixedRule_ShouldPopulateComment()
	{
		var rule = NotableDateRuleParser.ParseXml(FixedRuleXml).Single();

		Assert.AreEqual("Fixed rule comment.", rule.Comment);
	}

	/// <summary>
	/// Verifies that parsing a Fixed rule extracts every Tag child element into the immutable tag set.
	/// </summary>
	[TestMethod]
	public void ParseXml_WhenFixedRule_ShouldPopulateTags()
	{
		var rule = NotableDateRuleParser.ParseXml(FixedRuleXml).Single();

		Assert.AreEqual(2, rule.Tags.Count);
		Assert.IsTrue(rule.Tags.Contains("Public"));
		Assert.IsTrue(rule.Tags.Contains("Civic"));
	}

	/// <summary>
	/// Verifies that parsing a Fixed rule populates day and month attributes from the Fixed strategy element.
	/// </summary>
	[TestMethod]
	public void ParseXml_WhenFixedRule_ShouldPopulateDayAndMonth()
	{
		var rule = NotableDateRuleParser.ParseXml(FixedRuleXml).Single();

		Assert.AreEqual(1, rule.Day);
		Assert.AreEqual(1, rule.Month);
	}

	/// <summary>
	/// Verifies that parsing a DayOfWeekInMonth rule populates the strategy and weekday/ordinal fields.
	/// </summary>
	[TestMethod]
	public void ParseXml_WhenDayOfWeekInMonthRule_ShouldPopulateStrategyFields()
	{
		var rule = NotableDateRuleParser.ParseXml(DayOfWeekInMonthRuleXml).Single();

		Assert.AreEqual(DateResolutionStrategy.DayOfWeekInMonth, rule.Strategy);
		Assert.AreEqual(5, rule.Month);
		Assert.AreEqual(DayOfWeek.Sunday, rule.DayOfWeek);
		Assert.AreEqual(WeekOfMonthOrdinal.Second, rule.WeekOrdinal);
	}

	/// <summary>
	/// Verifies that parsing a Calculator rule populates the strategy and lookup key.
	/// </summary>
	[TestMethod]
	public void ParseXml_WhenCalculatorRule_ShouldPopulateCalculatorKey()
	{
		var rule = NotableDateRuleParser.ParseXml(CalculatorRuleXml).Single();

		Assert.AreEqual(DateResolutionStrategy.Calculator, rule.Strategy);
		Assert.AreEqual("easter-sunday", rule.CalculatorKey);
	}

	/// <summary>
	/// Verifies that parsing an OffsetFromAnchor rule populates the anchor name and integer offset.
	/// </summary>
	[TestMethod]
	public void ParseXml_WhenOffsetFromAnchorRule_ShouldPopulateAnchorAndOffset()
	{
		var rule = NotableDateRuleParser.ParseXml(OffsetFromAnchorRuleXml).Single();

		Assert.AreEqual(DateResolutionStrategy.OffsetFromAnchor, rule.Strategy);
		Assert.AreEqual("Easter Sunday", rule.AnchorRuleName);
		Assert.AreEqual(-2, rule.OffsetDays);
	}

	/// <summary>
	/// Verifies that parsing a multi-rule document returns one rule per child entry.
	/// </summary>
	[TestMethod]
	public void ParseXml_WhenMultiRuleXml_ShouldReturnAllRules()
	{
		var rules = NotableDateRuleParser.ParseXml(MultiRuleXml);

		Assert.AreEqual(4, rules.Count);
		CollectionAssert.AreEquivalent(
			new[] { "New Year's Day", "Easter Sunday", "Good Friday", "Anzac Day" },
			rules.Select(r => r.Name).ToArray());
	}

	/// <summary>
	/// Verifies that <see cref="NotableDateRuleParser.ParseXml(string)" /> throws when given a null or whitespace payload.
	/// </summary>
	[TestMethod]
	public void ParseXml_WhenInputIsWhitespace_ShouldThrowArgumentNullException()
	{
		Assert.ThrowsExactly<ArgumentNullException>(() => NotableDateRuleParser.ParseXml("   "));
	}

	/// <summary>
	/// Verifies that <see cref="NotableDateRuleParser.ParseDocument(string)" /> exposes UseFrom groups in declaration order, each
	/// pointing at the named source resource.
	/// </summary>
	[TestMethod]
	public void ParseDocument_WhenUseFromPresent_ShouldExposeGroupsInOrder()
	{
		var doc = NotableDateRuleParser.ParseDocument(CherryPickXml);

		Assert.AreEqual(2, doc.UseGroups.Length);
		Assert.AreEqual("Bodu.Globalization.Calendar.Resources.Common.xml", doc.UseGroups[0].SourceResource);
		Assert.AreEqual("Bodu.Globalization.Calendar.Resources.Christian.xml", doc.UseGroups[1].SourceResource);
	}

	/// <summary>
	/// Verifies that <see cref="NotableDateRuleParser.ParseDocument(string)" /> exposes each Use directive's source name and any
	/// scalar overrides (territory, non-working, priority, rename via 'as').
	/// </summary>
	[TestMethod]
	public void ParseDocument_WhenUseDirectivesPresent_ShouldExposeScalarOverrides()
	{
		var doc = NotableDateRuleParser.ParseDocument(CherryPickXml);

		var common = doc.UseGroups[0];
		Assert.AreEqual(2, common.Uses.Length);
		Assert.IsFalse(common.UseAll);
		Assert.AreEqual("New Year's Day", common.Uses[0].SourceRuleName);
		Assert.AreEqual("US", common.Uses[0].TerritoryCode);
		Assert.AreEqual("Halloween", common.Uses[1].SourceRuleName);
		Assert.IsNull(common.Uses[1].TerritoryCode);

		var christian = doc.UseGroups[1];
		Assert.AreEqual(1, christian.Uses.Length);
		Assert.AreEqual("Christmas Day", christian.Uses[0].SourceRuleName);
		Assert.AreEqual("Christmas", christian.Uses[0].LocalName);
		Assert.AreEqual("US", christian.Uses[0].TerritoryCode);
		Assert.IsTrue(christian.Uses[0].IsNonWorkingDay);
		Assert.AreEqual(5, christian.Uses[0].Priority);
	}

	/// <summary>
	/// Verifies that <see cref="NotableDateRuleParser.ParseDocument(string)" /> recognises the wildcard <c>UseAll</c> directive.
	/// </summary>
	[TestMethod]
	public void ParseDocument_WhenUseAllPresent_ShouldFlagGroupAsWildcard()
	{
		var doc = NotableDateRuleParser.ParseDocument(UseAllXml);

		Assert.AreEqual(1, doc.UseGroups.Length);
		Assert.IsTrue(doc.UseGroups[0].UseAll);
		Assert.AreEqual(0, doc.UseGroups[0].Uses.Length);
	}

	/// <summary>
	/// Verifies that <see cref="NotableDateRuleParser.ParseDocument(string)" /> still parses local rules alongside cherry-pick
	/// directives.
	/// </summary>
	[TestMethod]
	public void ParseDocument_WhenLocalRulesPresent_ShouldExposeLocalRules()
	{
		var doc = NotableDateRuleParser.ParseDocument(CherryPickXml);

		Assert.AreEqual(1, doc.LocalRules.Length);
		Assert.AreEqual("Independence Day", doc.LocalRules[0].Name);
	}

	/// <summary>
	/// Verifies that the <see cref="NotableDateRuleParser.ParseXml(System.Xml.Linq.XDocument)" />
	/// overload routes through the same parser pipeline as the string form.
	/// </summary>
	[TestMethod]
	public void ParseXml_WhenCalledWithXDocument_ShouldReturnSameRules()
	{
		XDocument document = XDocument.Parse(FixedRuleXml);

		var rule = NotableDateRuleParser.ParseXml(document).Single();

		Assert.AreEqual("Fixed Rule Test", rule.Name);
		Assert.AreEqual(DateResolutionStrategy.Fixed, rule.Strategy);
	}

	/// <summary>
	/// Verifies that the <see cref="NotableDateRuleParser.ParseDocument(System.Xml.Linq.XDocument)" />
	/// overload returns a parsed document equivalent to the string overload.
	/// </summary>
	[TestMethod]
	public void ParseDocument_WhenCalledWithXDocument_ShouldReturnSameDocument()
	{
		XDocument document = XDocument.Parse(FixedRuleXml);

		var parsed = NotableDateRuleParser.ParseDocument(document);

		Assert.AreEqual(1, parsed.LocalRules.Length);
		Assert.AreEqual("Fixed Rule Test", parsed.LocalRules[0].Name);
	}

	/// <summary>
	/// Verifies that the <see cref="NotableDateRuleParser.ParseDocument(System.Xml.Linq.XDocument)" />
	/// overload rejects a null document via <see cref="ArgumentNullException" />.
	/// </summary>
	[TestMethod]
	public void ParseDocument_WhenXDocumentIsNull_ShouldThrowArgumentNullException()
	{
		var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
		{
			_ = NotableDateRuleParser.ParseDocument((XDocument)null!);
		});

		Assert.AreEqual("document", ex.ParamName);
	}
}
