// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleParserTests.WeekdayNearDate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Xml.Schema;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies that <see cref="NotableDateRuleParser" /> parses the <c>&lt;WeekdayNearDate&gt;</c> strategy element — on
/// the rule body and inside a <c>&lt;Use&gt;</c> override body — and rejects malformed authoring via schema validation.
/// </summary>
public partial class NotableDateRuleParserTests
{
    private const string WeekdayNearDateRuleXml = @"
        <NotableDates xmlns=""urn:bodu:globalization:calendar"">
          <NotableDate name=""Midsummer Day"">
            <Rule name=""midsummer"" category=""Holiday"" territory=""SE"" nonWorking=""true"">
              <WeekdayNearDate dayOfWeek=""Saturday"" month=""June"" day=""20"" direction=""OnOrAfter"" />
            </Rule>
          </NotableDate>
        </NotableDates>";

    /// <summary>
    /// Verifies that a <c>&lt;WeekdayNearDate&gt;</c> rule populates the strategy and all four strategy fields.
    /// </summary>
    [TestMethod]
    public void ParseXml_WhenWeekdayNearDateRule_ShouldPopulateStrategyAndFields()
    {
        NotableDateRule rule = NotableDateRuleParser.ParseXml(WeekdayNearDateRuleXml).Single();

        Assert.AreEqual(DateResolutionStrategy.WeekdayNearDate, rule.Strategy);
        Assert.AreEqual(6, rule.Month);
        Assert.AreEqual(20, rule.Day);
        Assert.AreEqual(DayOfWeek.Saturday, rule.DayOfWeek);
        Assert.AreEqual(WeekdayProximity.OnOrAfter, rule.WeekdayProximity);
    }

    /// <summary>
    /// Verifies that the <c>direction</c> attribute maps to each <see cref="WeekdayProximity" /> value.
    /// </summary>
    /// <param name="direction">The authored <c>direction</c> token.</param>
    /// <param name="expected">The expected parsed <see cref="WeekdayProximity" />.</param>
    [TestMethod]
    [DataRow("OnOrAfter", WeekdayProximity.OnOrAfter)]
    [DataRow("OnOrBefore", WeekdayProximity.OnOrBefore)]
    [DataRow("Nearest", WeekdayProximity.Nearest)]
    public void ParseXml_WhenWeekdayNearDateDirection_ShouldMapToProximity(string direction, WeekdayProximity expected)
    {
        var xml = $@"
            <NotableDates xmlns=""urn:bodu:globalization:calendar"">
              <NotableDate name=""Sample"">
                <Rule name=""sample"" category=""Holiday"">
                  <WeekdayNearDate dayOfWeek=""Monday"" month=""November"" day=""22"" direction=""{direction}"" />
                </Rule>
              </NotableDate>
            </NotableDates>";

        NotableDateRule rule = NotableDateRuleParser.ParseXml(xml).Single();

        Assert.AreEqual(expected, rule.WeekdayProximity);
    }

    /// <summary>
    /// Verifies that a <c>&lt;WeekdayNearDate&gt;</c> strategy inside a <c>&lt;Use&gt;</c> override body is parsed onto
    /// the directive's override payload.
    /// </summary>
    [TestMethod]
    public void ParseDocument_WhenUseOverridesWithWeekdayNearDate_ShouldPopulateOverrideBody()
    {
        const string xml = @"
            <NotableDates xmlns=""urn:bodu:globalization:calendar"">
              <UseFrom resource=""./source.xml"">
                <Use name=""Midsummer Day"" territory=""FI"">
                  <Rule name=""midsummer-fi"" category=""Holiday"">
                    <WeekdayNearDate dayOfWeek=""Saturday"" month=""June"" day=""20"" direction=""OnOrAfter"" />
                  </Rule>
                </Use>
              </UseFrom>
            </NotableDates>";

        ParsedNotableDateDocument document = NotableDateRuleParser.ParseDocument(xml);
        NotableDateRuleOverrideBody body = document.UseGroups.Single().Uses.Single().OverrideBody!;

        Assert.AreEqual(DateResolutionStrategy.WeekdayNearDate, body.Strategy);
        Assert.AreEqual(6, body.Month);
        Assert.AreEqual(20, body.Day);
        Assert.AreEqual(DayOfWeek.Saturday, body.DayOfWeek);
        Assert.AreEqual(WeekdayProximity.OnOrAfter, body.WeekdayProximity);
    }

    /// <summary>
    /// Verifies that omitting the required <c>direction</c> attribute fails schema validation.
    /// </summary>
    [TestMethod]
    public void ParseXml_WhenWeekdayNearDateMissingDirection_ShouldThrowExactly()
    {
        const string xml = @"
            <NotableDates xmlns=""urn:bodu:globalization:calendar"">
              <NotableDate name=""Sample"">
                <Rule name=""sample"" category=""Holiday"">
                  <WeekdayNearDate dayOfWeek=""Saturday"" month=""June"" day=""20"" />
                </Rule>
              </NotableDate>
            </NotableDates>";

        Assert.ThrowsExactly<XmlSchemaValidationException>(() =>
        {
            _ = NotableDateRuleParser.ParseXml(xml);
        });
    }

    /// <summary>
    /// Verifies that an unrecognized <c>direction</c> value fails schema validation.
    /// </summary>
    [TestMethod]
    public void ParseXml_WhenWeekdayNearDateInvalidDirection_ShouldThrowExactly()
    {
        const string xml = @"
            <NotableDates xmlns=""urn:bodu:globalization:calendar"">
              <NotableDate name=""Sample"">
                <Rule name=""sample"" category=""Holiday"">
                  <WeekdayNearDate dayOfWeek=""Saturday"" month=""June"" day=""20"" direction=""Sideways"" />
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
    public void ParseXml_WhenRuleDeclaresFixedAndWeekdayNearDate_ShouldThrowExactly()
    {
        const string xml = @"
            <NotableDates xmlns=""urn:bodu:globalization:calendar"">
              <NotableDate name=""Sample"">
                <Rule name=""sample"" category=""Holiday"">
                  <Fixed month=""June"" day=""20"" />
                  <WeekdayNearDate dayOfWeek=""Saturday"" month=""June"" day=""20"" direction=""OnOrAfter"" />
                </Rule>
              </NotableDate>
            </NotableDates>";

        Assert.ThrowsExactly<XmlSchemaValidationException>(() =>
        {
            _ = NotableDateRuleParser.ParseXml(xml);
        });
    }
}
