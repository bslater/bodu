// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleJsonParserTests.OverrideBody.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Linq;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies that <see cref="NotableDateRuleJsonParser" /> maps the nested <c>rule</c> object on a <c>use</c>
/// directive onto a populated <see cref="NotableDateRuleOverrideBody" />, covering every supported strategy and
/// merge field.
/// </summary>
public partial class NotableDateRuleJsonParserTests
{
	/// <summary>
	/// Verifies that an override body whose <c>fixed</c> strategy populates month and day fields propagates onto
	/// <see cref="NotableDateRuleOverrideBody.Month" /> and <see cref="NotableDateRuleOverrideBody.Day" />.
	/// </summary>
	[TestMethod]
	public void ParseDocument_WhenOverrideUsesFixedStrategy_ShouldPopulateFixedFields()
	{
		const string json = @"{
			""useFrom"": [ {
				""resource"": ""shared.json"",
				""uses"": [ {
					""name"": ""Anzac Day"",
					""rule"": {
						""fixed"": { ""month"": ""April"", ""day"": 25 }
					}
				} ]
			} ]
		}";

		var document = NotableDateRuleJsonParser.ParseDocument(json);
		var directive = document.UseGroups.Single().Uses.Single();

		Assert.IsNotNull(directive.OverrideBody);
		Assert.AreEqual(DateResolutionStrategy.Fixed, directive.OverrideBody!.Strategy);
		Assert.AreEqual(4, directive.OverrideBody.Month);
		Assert.AreEqual(25, directive.OverrideBody.Day);
		Assert.IsFalse(directive.OverrideBody.SkipLeapMonth);
		Assert.IsFalse(directive.OverrideBody.SweepCalendarYears);
	}

	/// <summary>
	/// Verifies that the <c>skipLeapMonth</c> and <c>sweepCalendarYears</c> flags on a Fixed override body propagate.
	/// </summary>
	[TestMethod]
	public void ParseDocument_WhenOverrideFixedHasLeapMonthFlags_ShouldPopulateFlags()
	{
		const string json = @"{
			""useFrom"": [ {
				""resource"": ""shared.json"",
				""uses"": [ {
					""name"": ""Festival"",
					""rule"": {
						""fixed"": { ""month"": ""5"", ""day"": 5, ""skipLeapMonth"": true, ""sweepCalendarYears"": true }
					}
				} ]
			} ]
		}";

		var document = NotableDateRuleJsonParser.ParseDocument(json);
		var body = document.UseGroups.Single().Uses.Single().OverrideBody!;

		Assert.IsTrue(body.SkipLeapMonth);
		Assert.IsTrue(body.SweepCalendarYears);
	}

	/// <summary>
	/// Verifies that an override body whose <c>fixed</c> strategy uses a leap-year-dependent Hebrew alias populates
	/// <see cref="NotableDateRuleOverrideBody.CalendarMonthAlias" />.
	/// </summary>
	[TestMethod]
	public void ParseDocument_WhenOverrideFixedUsesHebrewAlias_ShouldPopulateCalendarMonthAlias()
	{
		const string json = @"{
			""useFrom"": [ {
				""resource"": ""shared.json"",
				""uses"": [ {
					""name"": ""Passover"",
					""rule"": {
						""fixed"": { ""month"": ""Nisan"", ""day"": 15 }
					}
				} ]
			} ]
		}";

		var document = NotableDateRuleJsonParser.ParseDocument(json);
		var body = document.UseGroups.Single().Uses.Single().OverrideBody!;

		Assert.IsNull(body.Month);
		Assert.AreEqual("Nisan", body.CalendarMonthAlias);
	}

	/// <summary>
	/// Verifies that an override body whose <c>offsetFromAnchor</c> strategy populates anchor name and integer offset.
	/// </summary>
	[TestMethod]
	public void ParseDocument_WhenOverrideUsesOffsetFromAnchorStrategy_ShouldPopulateAnchorAndOffset()
	{
		const string json = @"{
			""useFrom"": [ {
				""resource"": ""shared.json"",
				""uses"": [ {
					""name"": ""Good Friday"",
					""rule"": {
						""offsetFromAnchor"": { ""name"": ""Easter Sunday"", ""offset"": -2 }
					}
				} ]
			} ]
		}";

		var document = NotableDateRuleJsonParser.ParseDocument(json);
		var body = document.UseGroups.Single().Uses.Single().OverrideBody!;

		Assert.AreEqual(DateResolutionStrategy.OffsetFromAnchor, body.Strategy);
		Assert.AreEqual("Easter Sunday", body.AnchorRuleName);
		Assert.AreEqual(-2, body.OffsetDays);
	}

	/// <summary>
	/// Verifies that an override body whose <c>dayOfWeekInMonth</c> strategy populates weekday and ordinal.
	/// </summary>
	[TestMethod]
	public void ParseDocument_WhenOverrideUsesDayOfWeekInMonthStrategy_ShouldPopulateWeekdayAndOrdinal()
	{
		const string json = @"{
			""useFrom"": [ {
				""resource"": ""shared.json"",
				""uses"": [ {
					""name"": ""Labour Day"",
					""rule"": {
						""dayOfWeekInMonth"": { ""month"": ""September"", ""dayOfWeek"": ""Monday"", ""weekOrdinal"": ""First"" }
					}
				} ]
			} ]
		}";

		var document = NotableDateRuleJsonParser.ParseDocument(json);
		var body = document.UseGroups.Single().Uses.Single().OverrideBody!;

		Assert.AreEqual(DateResolutionStrategy.DayOfWeekInMonth, body.Strategy);
		Assert.AreEqual(9, body.Month);
		Assert.AreEqual(DayOfWeek.Monday, body.DayOfWeek);
		Assert.AreEqual(WeekOfMonthOrdinal.First, body.WeekOrdinal);
	}

	/// <summary>
	/// Verifies that an override body whose <c>algorithm</c> strategy populates the algorithm key.
	/// </summary>
	[TestMethod]
	public void ParseDocument_WhenOverrideUsesAlgorithmStrategy_ShouldPopulateAlgorithmKey()
	{
		const string json = @"{
			""useFrom"": [ {
				""resource"": ""shared.json"",
				""uses"": [ {
					""name"": ""Easter Sunday"",
					""rule"": {
						""algorithm"": { ""key"": ""easter-sunday"" }
					}
				} ]
			} ]
		}";

		var document = NotableDateRuleJsonParser.ParseDocument(json);
		var body = document.UseGroups.Single().Uses.Single().OverrideBody!;

		Assert.AreEqual(DateResolutionStrategy.Algorithm, body.Strategy);
		Assert.AreEqual("easter-sunday", body.AlgorithmKey);
	}

	/// <summary>
	/// Verifies that an override body without any strategy element leaves <see cref="NotableDateRuleOverrideBody.Strategy" />
	/// at <see langword="null" /> so the source rule's strategy is inherited.
	/// </summary>
	[TestMethod]
	public void ParseDocument_WhenOverrideHasNoStrategy_ShouldLeaveStrategyNull()
	{
		const string json = @"{
			""useFrom"": [ {
				""resource"": ""shared.json"",
				""uses"": [ {
					""name"": ""Inherited"",
					""rule"": { ""territory"": ""US"", ""priority"": 5 }
				} ]
			} ]
		}";

		var document = NotableDateRuleJsonParser.ParseDocument(json);
		var body = document.UseGroups.Single().Uses.Single().OverrideBody!;

		Assert.IsNull(body.Strategy);
		Assert.AreEqual("US", body.TerritoryCode);
		Assert.AreEqual(5, body.Priority);
	}

	/// <summary>
	/// Verifies that an override body declaring more than one strategy surfaces as
	/// <see cref="InvalidOperationException" />.
	/// </summary>
	[TestMethod]
	public void ParseDocument_WhenOverrideHasMultipleStrategies_ShouldThrowInvalidOperationException()
	{
		const string json = @"{
			""useFrom"": [ {
				""resource"": ""shared.json"",
				""uses"": [ {
					""name"": ""Overloaded"",
					""rule"": {
						""fixed"": { ""month"": ""January"", ""day"": 1 },
						""algorithm"": { ""key"": ""anything"" }
					}
				} ]
			} ]
		}";

		Assert.ThrowsExactly<InvalidOperationException>(() =>
		{
			_ = NotableDateRuleJsonParser.ParseDocument(json);
		});
	}

	/// <summary>
	/// Verifies that override body <c>tags</c> propagate onto <see cref="NotableDateRuleOverrideBody.Tags" /> with
	/// whitespace-only entries filtered out.
	/// </summary>
	[TestMethod]
	public void ParseDocument_WhenOverrideHasTags_ShouldPopulateTags()
	{
		const string json = @"{
			""useFrom"": [ {
				""resource"": ""shared.json"",
				""uses"": [ {
					""name"": ""Tagged"",
					""rule"": { ""tags"": [""Public"", ""Federal"", ""   ""] }
				} ]
			} ]
		}";

		var document = NotableDateRuleJsonParser.ParseDocument(json);
		var body = document.UseGroups.Single().Uses.Single().OverrideBody!;

		Assert.AreEqual(2, body.Tags.Length);
		CollectionAssert.AreEquivalent(new[] { "Public", "Federal" }, body.Tags.ToArray());
	}

	/// <summary>
	/// Verifies that an override body's adjustment entries propagate onto
	/// <see cref="NotableDateRuleOverrideBody.Adjustments" />, retaining key, trigger, and action.
	/// </summary>
	[TestMethod]
	public void ParseDocument_WhenOverrideHasAdjustments_ShouldPopulateAdjustments()
	{
		const string json = @"{
			""useFrom"": [ {
				""resource"": ""shared.json"",
				""uses"": [ {
					""name"": ""Adjusted"",
					""rule"": {
						""adjustments"": [
							{ ""key"": ""weekend-roll"", ""when"": ""IfWeekend"", ""action"": ""MoveToNextWeekday"" }
						]
					}
				} ]
			} ]
		}";

		var document = NotableDateRuleJsonParser.ParseDocument(json);
		var body = document.UseGroups.Single().Uses.Single().OverrideBody!;

		Assert.AreEqual(1, body.Adjustments.Length);
		Assert.AreEqual("weekend-roll", body.Adjustments[0].Key);
		Assert.AreEqual(AdjustmentTrigger.IfWeekend, body.Adjustments[0].Trigger);
		Assert.AreEqual(AdjustmentAction.MoveToNextWeekday, body.Adjustments[0].Action);
	}

	/// <summary>
	/// Verifies that an override body with duplicate adjustment keys surfaces as
	/// <see cref="InvalidOperationException" />.
	/// </summary>
	[TestMethod]
	public void ParseDocument_WhenOverrideHasDuplicateAdjustmentKeys_ShouldThrowInvalidOperationException()
	{
		const string json = @"{
			""useFrom"": [ {
				""resource"": ""shared.json"",
				""uses"": [ {
					""name"": ""Duplicate"",
					""rule"": {
						""adjustments"": [
							{ ""key"": ""same"", ""when"": ""IfWeekend"", ""action"": ""MoveToNextWeekday"" },
							{ ""key"": ""same"", ""when"": ""Always"", ""action"": ""None"" }
						]
					}
				} ]
			} ]
		}";

		Assert.ThrowsExactly<InvalidOperationException>(() =>
		{
			_ = NotableDateRuleJsonParser.ParseDocument(json);
		});
	}

	/// <summary>
	/// Verifies that an override body's category, territory, non-working flag, year window, duration, priority, and
	/// comment scalar overrides propagate to the corresponding <see cref="NotableDateRuleOverrideBody" /> fields.
	/// </summary>
	[TestMethod]
	public void ParseDocument_WhenOverrideHasScalarOverrides_ShouldPopulateScalarFields()
	{
		const string json = @"{
			""useFrom"": [ {
				""resource"": ""shared.json"",
				""uses"": [ {
					""name"": ""Scalar"",
					""rule"": {
						""category"": ""Holiday"",
						""territory"": ""AU"",
						""nonWorking"": true,
						""firstYear"": 2010,
						""lastYear"": 2050,
						""occurrenceYears"": 2,
						""durationDays"": 3,
						""priority"": 9,
						""comment"": ""overridden"",
						""calendarType"": ""System.Globalization.GregorianCalendar""
					}
				} ]
			} ]
		}";

		var document = NotableDateRuleJsonParser.ParseDocument(json);
		var body = document.UseGroups.Single().Uses.Single().OverrideBody!;

		Assert.AreEqual(NotableDateCategory.Holiday, body.Category);
		Assert.AreEqual("AU", body.TerritoryCode);
		Assert.AreEqual(true, body.IsNonWorkingDay);
		Assert.AreEqual(2010, body.FirstYear);
		Assert.AreEqual(2050, body.LastYear);
		Assert.AreEqual(2, body.OccurrenceYears);
		Assert.AreEqual(3, body.DurationDays);
		Assert.AreEqual(9, body.Priority);
		Assert.AreEqual("overridden", body.Comment);
		Assert.AreEqual(typeof(System.Globalization.GregorianCalendar), body.CalendarType);
	}

	/// <summary>
	/// Verifies that a <c>use</c> directive's <c>clearTags</c>, <c>clearAdjustments</c>, and <c>clearInherited</c>
	/// flags propagate onto the directive record.
	/// </summary>
	[TestMethod]
	public void ParseDocument_WhenUseDirectiveSetsClearFlags_ShouldPropagateFlags()
	{
		const string json = @"{
			""useFrom"": [ {
				""resource"": ""shared.json"",
				""uses"": [ {
					""name"": ""Clear"",
					""clearTags"": true,
					""clearAdjustments"": true,
					""clearInherited"": true
				} ]
			} ]
		}";

		var document = NotableDateRuleJsonParser.ParseDocument(json);
		var directive = document.UseGroups.Single().Uses.Single();

		Assert.IsTrue(directive.ClearTags);
		Assert.IsTrue(directive.ClearAdjustments);
		Assert.IsTrue(directive.ClearInherited);
	}

	/// <summary>
	/// Verifies that omitting <c>clearTags</c>, <c>clearAdjustments</c>, and <c>clearInherited</c> from a <c>use</c>
	/// directive defaults each flag to <see langword="false" />.
	/// </summary>
	[TestMethod]
	public void ParseDocument_WhenUseDirectiveOmitsClearFlags_ShouldDefaultToFalse()
	{
		const string json = @"{
			""useFrom"": [ {
				""resource"": ""shared.json"",
				""uses"": [ { ""name"": ""NoClear"" } ]
			} ]
		}";

		var document = NotableDateRuleJsonParser.ParseDocument(json);
		var directive = document.UseGroups.Single().Uses.Single();

		Assert.IsFalse(directive.ClearTags);
		Assert.IsFalse(directive.ClearAdjustments);
		Assert.IsFalse(directive.ClearInherited);
	}

	/// <summary>
	/// Verifies that a <c>useFrom</c> entry whose <c>resource</c> is missing surfaces as
	/// <see cref="InvalidOperationException" />.
	/// </summary>
	[TestMethod]
	public void ParseDocument_WhenUseFromResourceIsMissing_ShouldThrowInvalidOperationException()
	{
		const string json = @"{
			""useFrom"": [ { ""uses"": [ { ""name"": ""Anything"" } ] } ]
		}";

		Assert.ThrowsExactly<InvalidOperationException>(() =>
		{
			_ = NotableDateRuleJsonParser.ParseDocument(json);
		});
	}

	/// <summary>
	/// Verifies that a <c>use</c> entry whose <c>name</c> is missing surfaces as
	/// <see cref="InvalidOperationException" />.
	/// </summary>
	[TestMethod]
	public void ParseDocument_WhenUseDirectiveNameIsMissing_ShouldThrowInvalidOperationException()
	{
		const string json = @"{
			""useFrom"": [ {
				""resource"": ""shared.json"",
				""uses"": [ { ""territory"": ""US"" } ]
			} ]
		}";

		Assert.ThrowsExactly<InvalidOperationException>(() =>
		{
			_ = NotableDateRuleJsonParser.ParseDocument(json);
		});
	}

	/// <summary>
	/// Verifies that a <c>use</c> directive with no inner <c>rule</c> object leaves
	/// <see cref="NotableDateRuleUseDirective.OverrideBody" /> at <see langword="null" />.
	/// </summary>
	[TestMethod]
	public void ParseDocument_WhenUseDirectiveHasNoOverrideBody_ShouldLeaveOverrideBodyNull()
	{
		const string json = @"{
			""useFrom"": [ {
				""resource"": ""shared.json"",
				""uses"": [ { ""name"": ""Plain"" } ]
			} ]
		}";

		var document = NotableDateRuleJsonParser.ParseDocument(json);
		var directive = document.UseGroups.Single().Uses.Single();

		Assert.IsNull(directive.OverrideBody);
	}
}
