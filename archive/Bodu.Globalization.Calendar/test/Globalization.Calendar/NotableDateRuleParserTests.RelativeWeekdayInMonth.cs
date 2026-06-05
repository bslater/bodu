// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleParserTests.RelativeWeekdayInMonth.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Extensions;
using System.Xml.Schema;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies that <see cref="NotableDateRuleParser" /> parses the <c>&lt;RelativeWeekdayInMonth&gt;</c> strategy element
/// — on the rule body and inside a <c>&lt;Use&gt;</c> override body — and rejects malformed authoring via schema
/// validation.
/// </summary>
public partial class NotableDateRuleParserTests
{
    private const string RelativeWeekdayInMonthRuleXml = @"
        <NotableDates xmlns=""urn:bodu:globalization:calendar"">
          <NotableDate name=""Election Day"">
            <Rule name=""election"" category=""Civic"" territory=""US"">
              <RelativeWeekdayInMonth month=""November"" weekOrdinal=""First"" dayOfWeek=""Monday"" relativeDayOfWeek=""Tuesday"" direction=""OnOrAfter"" />
            </Rule>
          </NotableDate>
        </NotableDates>";

    /// <summary>
    /// Verifies that a <c>&lt;RelativeWeekdayInMonth&gt;</c> rule populates the strategy, the anchor fields, the target
    /// weekday, and the direction.
    /// </summary>
    [TestMethod]
    public void ParseXml_WhenRelativeWeekdayInMonthRule_ShouldPopulateStrategyAndFields()
    {
        NotableDateRule rule = NotableDateRuleParser.ParseXml(RelativeWeekdayInMonthRuleXml).Single();

        Assert.AreEqual(DateResolutionStrategy.RelativeWeekdayInMonth, rule.Strategy);
        Assert.AreEqual(11, rule.Month);
        Assert.AreEqual(DayOfWeek.Monday, rule.DayOfWeek);
        Assert.AreEqual(WeekOfMonthOrdinal.First, rule.WeekOrdinal);
        Assert.AreEqual(DayOfWeek.Tuesday, rule.RelativeDayOfWeek);
        Assert.AreEqual(WeekdayProximity.OnOrAfter, rule.WeekdayProximity);
    }

    /// <summary>
    /// Verifies that a <c>&lt;RelativeWeekdayInMonth&gt;</c> strategy inside a <c>&lt;Use&gt;</c> override body is parsed
    /// onto the directive's override payload.
    /// </summary>
    [TestMethod]
    public void ParseDocument_WhenUseOverridesWithRelativeWeekdayInMonth_ShouldPopulateOverrideBody()
    {
        const string xml = @"
            <NotableDates xmlns=""urn:bodu:globalization:calendar"">
              <UseFrom resource=""./source.xml"">
                <Use name=""Election Day"" territory=""US"">
                  <Rule name=""election-us"" category=""Civic"">
                    <RelativeWeekdayInMonth month=""November"" weekOrdinal=""First"" dayOfWeek=""Monday"" relativeDayOfWeek=""Tuesday"" direction=""OnOrAfter"" />
                  </Rule>
                </Use>
              </UseFrom>
            </NotableDates>";

        ParsedNotableDateDocument document = NotableDateRuleParser.ParseDocument(xml);
        NotableDateRuleOverrideBody body = document.UseGroups.Single().Uses.Single().OverrideBody!;

        Assert.AreEqual(DateResolutionStrategy.RelativeWeekdayInMonth, body.Strategy);
        Assert.AreEqual(11, body.Month);
        Assert.AreEqual(DayOfWeek.Monday, body.DayOfWeek);
        Assert.AreEqual(WeekOfMonthOrdinal.First, body.WeekOrdinal);
        Assert.AreEqual(DayOfWeek.Tuesday, body.RelativeDayOfWeek);
        Assert.AreEqual(WeekdayProximity.OnOrAfter, body.WeekdayProximity);
    }

    /// <summary>
    /// Verifies that omitting the required <c>relativeDayOfWeek</c> attribute fails schema validation.
    /// </summary>
    [TestMethod]
    public void ParseXml_WhenRelativeWeekdayInMonthMissingRelativeDayOfWeek_ShouldThrowExactly()
    {
        const string xml = @"
            <NotableDates xmlns=""urn:bodu:globalization:calendar"">
              <NotableDate name=""Sample"">
                <Rule name=""sample"" category=""Civic"">
                  <RelativeWeekdayInMonth month=""November"" weekOrdinal=""First"" dayOfWeek=""Monday"" direction=""OnOrAfter"" />
                </Rule>
              </NotableDate>
            </NotableDates>";

        Assert.ThrowsExactly<XmlSchemaValidationException>(() =>
        {
            _ = NotableDateRuleParser.ParseXml(xml);
        });
    }

    /// <summary>
    /// Verifies that declaring two strategy elements on a single rule fails schema validation (the strategy choice
    /// permits exactly one).
    /// </summary>
    [TestMethod]
    public void ParseXml_WhenRuleDeclaresDayOfWeekInMonthAndRelativeWeekdayInMonth_ShouldThrowExactly()
    {
        const string xml = @"
            <NotableDates xmlns=""urn:bodu:globalization:calendar"">
              <NotableDate name=""Sample"">
                <Rule name=""sample"" category=""Civic"">
                  <DayOfWeekInMonth month=""November"" dayOfWeek=""Monday"" weekOrdinal=""First"" />
                  <RelativeWeekdayInMonth month=""November"" weekOrdinal=""First"" dayOfWeek=""Monday"" relativeDayOfWeek=""Tuesday"" direction=""OnOrAfter"" />
                </Rule>
              </NotableDate>
            </NotableDates>";

        Assert.ThrowsExactly<XmlSchemaValidationException>(() =>
        {
            _ = NotableDateRuleParser.ParseXml(xml);
        });
    }
}
