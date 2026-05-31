// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleParserTests.OverrideBody.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Extensions;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies that <see cref="NotableDateRuleParser" /> parses the nested <c>&lt;Rule&gt;</c> override body of a
/// <c>&lt;Use&gt;</c> directive into a populated <see cref="NotableDateRuleOverrideBody" />, covering every
/// supported strategy element and merge field.
/// </summary>
public partial class NotableDateRuleParserTests
{
    /// <summary>
    /// Verifies that an override body whose Fixed strategy is present populates month, day, and the leap-month flags.
    /// </summary>
    [TestMethod]
    public void ParseDocument_WhenXmlOverrideUsesFixedStrategy_ShouldPopulateFixedFields()
    {
        const string xml = @"
			<NotableDates xmlns=""urn:bodu:globalization:calendar"">
				<UseFrom resource=""shared.xml"">
					<Use name=""Anzac Day"">
						<Rule name=""Anzac Day Rule"" category=""Holiday"" territory=""AU"">
							<Fixed month=""April"" day=""25"" skipLeapMonth=""true"" sweepCalendarYears=""true"" />
						</Rule>
					</Use>
				</UseFrom>
			</NotableDates>";

        ParsedNotableDateDocument document = NotableDateRuleParser.ParseDocument(xml);
        NotableDateRuleOverrideBody body = document.UseGroups.Single().Uses.Single().OverrideBody!;

        Assert.IsNotNull(body);
        Assert.AreEqual(DateResolutionStrategy.Fixed, body.Strategy);
        Assert.AreEqual(4, body.Month);
        Assert.AreEqual(25, body.Day);
        Assert.IsTrue(body.SkipLeapMonth);
        Assert.IsTrue(body.SweepCalendarYears);
    }

    /// <summary>
    /// Verifies that an override body whose Fixed strategy uses a Hebrew alias month populates
    /// <see cref="NotableDateRuleOverrideBody.CalendarMonthAlias" />.
    /// </summary>
    [TestMethod]
    public void ParseDocument_WhenXmlOverrideFixedUsesHebrewAlias_ShouldPopulateCalendarMonthAlias()
    {
        const string xml = @"
			<NotableDates xmlns=""urn:bodu:globalization:calendar"">
				<UseFrom resource=""shared.xml"">
					<Use name=""Passover"">
						<Rule name=""Passover Rule"" category=""Religious"" calendarType=""System.Globalization.HebrewCalendar"">
							<Fixed month=""Nisan"" day=""15"" />
						</Rule>
					</Use>
				</UseFrom>
			</NotableDates>";

        ParsedNotableDateDocument document = NotableDateRuleParser.ParseDocument(xml);
        NotableDateRuleOverrideBody body = document.UseGroups.Single().Uses.Single().OverrideBody!;

        Assert.IsNull(body.Month);
        Assert.AreEqual("Nisan", body.CalendarMonthAlias);
    }

    /// <summary>
    /// Verifies that an override body whose OffsetFromAnchor strategy is present populates anchor name and offset.
    /// </summary>
    [TestMethod]
    public void ParseDocument_WhenXmlOverrideUsesOffsetFromAnchorStrategy_ShouldPopulateAnchorAndOffset()
    {
        const string xml = @"
			<NotableDates xmlns=""urn:bodu:globalization:calendar"">
				<UseFrom resource=""shared.xml"">
					<Use name=""Good Friday"">
						<Rule name=""Good Friday Rule"" category=""Holiday"">
							<OffsetFromAnchor name=""Easter Sunday"" offset=""-2"" />
						</Rule>
					</Use>
				</UseFrom>
			</NotableDates>";

        ParsedNotableDateDocument document = NotableDateRuleParser.ParseDocument(xml);
        NotableDateRuleOverrideBody body = document.UseGroups.Single().Uses.Single().OverrideBody!;

        Assert.AreEqual(DateResolutionStrategy.OffsetFromAnchor, body.Strategy);
        Assert.AreEqual("Easter Sunday", body.AnchorRuleName);
        Assert.AreEqual(-2, body.OffsetDays);
    }

    /// <summary>
    /// Verifies that an override body whose DayOfWeekInMonth strategy is present populates month, weekday, and ordinal.
    /// </summary>
    [TestMethod]
    public void ParseDocument_WhenXmlOverrideUsesDayOfWeekInMonthStrategy_ShouldPopulateWeekdayAndOrdinal()
    {
        const string xml = @"
			<NotableDates xmlns=""urn:bodu:globalization:calendar"">
				<UseFrom resource=""shared.xml"">
					<Use name=""Labour Day"">
						<Rule name=""Labour Day Rule"" category=""Holiday"">
							<DayOfWeekInMonth month=""September"" dayOfWeek=""Monday"" weekOrdinal=""First"" />
						</Rule>
					</Use>
				</UseFrom>
			</NotableDates>";

        ParsedNotableDateDocument document = NotableDateRuleParser.ParseDocument(xml);
        NotableDateRuleOverrideBody body = document.UseGroups.Single().Uses.Single().OverrideBody!;

        Assert.AreEqual(DateResolutionStrategy.DayOfWeekInMonth, body.Strategy);
        Assert.AreEqual(9, body.Month);
        Assert.AreEqual(DayOfWeek.Monday, body.DayOfWeek);
        Assert.AreEqual(WeekOfMonthOrdinal.First, body.WeekOrdinal);
    }

    /// <summary>
    /// Verifies that an override body whose Algorithm strategy is present populates the algorithm key.
    /// </summary>
    [TestMethod]
    public void ParseDocument_WhenXmlOverrideUsesAlgorithmStrategy_ShouldPopulateAlgorithmKey()
    {
        const string xml = @"
			<NotableDates xmlns=""urn:bodu:globalization:calendar"">
				<UseFrom resource=""shared.xml"">
					<Use name=""Easter Sunday"">
						<Rule name=""Easter Sunday Rule"" category=""Religious"">
							<Algorithm key=""easter-sunday"" />
						</Rule>
					</Use>
				</UseFrom>
			</NotableDates>";

        ParsedNotableDateDocument document = NotableDateRuleParser.ParseDocument(xml);
        NotableDateRuleOverrideBody body = document.UseGroups.Single().Uses.Single().OverrideBody!;

        Assert.AreEqual(DateResolutionStrategy.Algorithm, body.Strategy);
        Assert.AreEqual("easter-sunday", body.AlgorithmKey);
    }

    /// <summary>
    /// Verifies that an override body without any strategy element leaves
    /// <see cref="NotableDateRuleOverrideBody.Strategy" /> at <see langword="null" /> so the source rule's
    /// strategy is inherited.
    /// </summary>
    [TestMethod]
    public void ParseDocument_WhenXmlOverrideHasNoStrategy_ShouldLeaveStrategyNull()
    {
        const string xml = @"
			<NotableDates xmlns=""urn:bodu:globalization:calendar"">
				<UseFrom resource=""shared.xml"">
					<Use name=""Inherited"">
						<Rule name=""Inherited Rule"" category=""Holiday"" territory=""US"" priority=""5"" />
					</Use>
				</UseFrom>
			</NotableDates>";

        ParsedNotableDateDocument document = NotableDateRuleParser.ParseDocument(xml);
        NotableDateRuleOverrideBody body = document.UseGroups.Single().Uses.Single().OverrideBody!;

        Assert.IsNull(body.Strategy);
        Assert.AreEqual(NotableDateCategory.Holiday, body.Category);
        Assert.AreEqual("US", body.TerritoryCode);
        Assert.AreEqual(5, body.Priority);
    }

    /// <summary>
    /// Verifies that override body Tag elements propagate onto <see cref="NotableDateRuleOverrideBody.Tags" />.
    /// </summary>
    [TestMethod]
    public void ParseDocument_WhenXmlOverrideHasTags_ShouldPopulateTags()
    {
        const string xml = @"
			<NotableDates xmlns=""urn:bodu:globalization:calendar"">
				<UseFrom resource=""shared.xml"">
					<Use name=""Tagged"">
						<Rule name=""Tagged Rule"" category=""Holiday"">
							<Tag>Public</Tag>
							<Tag>Federal</Tag>
						</Rule>
					</Use>
				</UseFrom>
			</NotableDates>";

        ParsedNotableDateDocument document = NotableDateRuleParser.ParseDocument(xml);
        NotableDateRuleOverrideBody body = document.UseGroups.Single().Uses.Single().OverrideBody!;

        Assert.AreEqual(2, body.Tags.Length);
        CollectionAssert.AreEquivalent(new[] { "Public", "Federal" }, body.Tags.ToArray());
    }

    /// <summary>
    /// Verifies that an override body's Adjustment child elements propagate onto
    /// <see cref="NotableDateRuleOverrideBody.Adjustments" />, retaining key, trigger, and action.
    /// </summary>
    [TestMethod]
    public void ParseDocument_WhenXmlOverrideHasAdjustments_ShouldPopulateAdjustments()
    {
        const string xml = @"
			<NotableDates xmlns=""urn:bodu:globalization:calendar"">
				<UseFrom resource=""shared.xml"">
					<Use name=""Adjusted"">
						<Rule name=""Adjusted Rule"" category=""Holiday"">
							<Adjustment key=""weekend-roll"" when=""IfWeekend"" action=""MoveToNextWeekday"" />
						</Rule>
					</Use>
				</UseFrom>
			</NotableDates>";

        ParsedNotableDateDocument document = NotableDateRuleParser.ParseDocument(xml);
        NotableDateRuleOverrideBody body = document.UseGroups.Single().Uses.Single().OverrideBody!;

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
    public void ParseDocument_WhenXmlOverrideHasDuplicateAdjustmentKeys_ShouldThrowExactly()
    {
        const string xml = @"
			<NotableDates xmlns=""urn:bodu:globalization:calendar"">
				<UseFrom resource=""shared.xml"">
					<Use name=""Duplicate"">
						<Rule name=""Duplicate Rule"" category=""Holiday"">
							<Adjustment key=""same"" when=""IfWeekend"" action=""MoveToNextWeekday"" />
							<Adjustment key=""same"" when=""Always"" action=""None"" />
						</Rule>
					</Use>
				</UseFrom>
			</NotableDates>";

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = NotableDateRuleParser.ParseDocument(xml);
        });
    }

    /// <summary>
    /// Verifies that a UseFrom directive's <c>UseAll</c> child element is parsed onto
    /// <see cref="NotableDateRuleUseGroup.UseAll" />.
    /// </summary>
    [TestMethod]
    public void ParseDocument_WhenXmlUseFromUsesUseAll_ShouldSetUseAllFlag()
    {
        const string xml = @"
			<NotableDates xmlns=""urn:bodu:globalization:calendar"">
				<UseFrom resource=""shared.xml"">
					<UseAll />
				</UseFrom>
			</NotableDates>";

        ParsedNotableDateDocument document = NotableDateRuleParser.ParseDocument(xml);
        NotableDateRuleUseGroup group = document.UseGroups.Single();

        Assert.IsTrue(group.UseAll);
        Assert.AreEqual("shared.xml", group.SourceResource);
        Assert.AreEqual(0, group.Uses.Length);
    }

    /// <summary>
    /// Verifies that a Use directive with the <c>clearTags</c>, <c>clearAdjustments</c>, and
    /// <c>clearInherited</c> flags propagates each flag.
    /// </summary>
    [TestMethod]
    public void ParseDocument_WhenXmlUseDirectiveSetsClearFlags_ShouldPropagateFlags()
    {
        const string xml = @"
			<NotableDates xmlns=""urn:bodu:globalization:calendar"">
				<UseFrom resource=""shared.xml"">
					<Use name=""Cleared""
					     clearTags=""true""
					     clearAdjustments=""true""
					     clearInherited=""true"" />
				</UseFrom>
			</NotableDates>";

        ParsedNotableDateDocument document = NotableDateRuleParser.ParseDocument(xml);
        NotableDateRuleUseDirective directive = document.UseGroups.Single().Uses.Single();

        Assert.IsTrue(directive.ClearTags);
        Assert.IsTrue(directive.ClearAdjustments);
        Assert.IsTrue(directive.ClearInherited);
    }

    /// <summary>
    /// Verifies that a Use directive's <c>as</c> attribute is captured as
    /// <see cref="NotableDateRuleUseDirective.LocalName" />.
    /// </summary>
    [TestMethod]
    public void ParseDocument_WhenXmlUseDirectiveUsesAsAttribute_ShouldPopulateLocalName()
    {
        const string xml = @"
			<NotableDates xmlns=""urn:bodu:globalization:calendar"">
				<UseFrom resource=""shared.xml"">
					<Use name=""Christmas Day"" as=""Christmas"" />
				</UseFrom>
			</NotableDates>";

        ParsedNotableDateDocument document = NotableDateRuleParser.ParseDocument(xml);
        NotableDateRuleUseDirective directive = document.UseGroups.Single().Uses.Single();

        Assert.AreEqual("Christmas Day", directive.SourceRuleName);
        Assert.AreEqual("Christmas", directive.LocalName);
    }

    /// <summary>
    /// Verifies that a Use directive without a nested <c>&lt;Rule&gt;</c> child leaves
    /// <see cref="NotableDateRuleUseDirective.OverrideBody" /> at <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ParseDocument_WhenXmlUseDirectiveHasNoOverrideBody_ShouldLeaveOverrideBodyNull()
    {
        const string xml = @"
			<NotableDates xmlns=""urn:bodu:globalization:calendar"">
				<UseFrom resource=""shared.xml"">
					<Use name=""Plain"" />
				</UseFrom>
			</NotableDates>";

        ParsedNotableDateDocument document = NotableDateRuleParser.ParseDocument(xml);
        NotableDateRuleUseDirective directive = document.UseGroups.Single().Uses.Single();

        Assert.IsNull(directive.OverrideBody);
    }
}
