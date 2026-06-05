// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleJsonParserTests.WeekdayNearDate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies that <see cref="NotableDateRuleJsonParser" /> parses the <c>weekdayNearDate</c> strategy object — on the
/// rule body and inside a <c>useFrom</c> override body — and rejects malformed authoring via schema validation, in
/// parity with the XML pathway.
/// </summary>
public partial class NotableDateRuleJsonParserTests
{
    private const string WeekdayNearDateRuleJson = @"{
        ""notableDates"": [ {
            ""name"": ""Midsummer Day"",
            ""rules"": [ {
                ""name"": ""midsummer"",
                ""category"": ""Holiday"",
                ""territory"": ""SE"",
                ""nonWorking"": true,
                ""weekdayNearDate"": { ""dayOfWeek"": ""Saturday"", ""month"": ""June"", ""day"": 20, ""direction"": ""OnOrAfter"" }
            } ]
        } ]
    }";

    /// <summary>
    /// Verifies that a <c>weekdayNearDate</c> rule populates the strategy and all four strategy fields.
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenWeekdayNearDateRule_ShouldPopulateStrategyAndFields()
    {
        NotableDateRule rule = NotableDateRuleJsonParser.ParseJson(WeekdayNearDateRuleJson).Single();

        Assert.AreEqual(DateResolutionStrategy.WeekdayNearDate, rule.Strategy);
        Assert.AreEqual(6, rule.Month);
        Assert.AreEqual(20, rule.Day);
        Assert.AreEqual(DayOfWeek.Saturday, rule.DayOfWeek);
        Assert.AreEqual(WeekdayProximity.OnOrAfter, rule.WeekdayProximity);
    }

    /// <summary>
    /// Verifies that the <c>direction</c> token maps to each <see cref="WeekdayProximity" /> value.
    /// </summary>
    /// <param name="direction">The authored <c>direction</c> token.</param>
    /// <param name="expected">The expected parsed <see cref="WeekdayProximity" />.</param>
    [TestMethod]
    [DataRow("OnOrAfter", WeekdayProximity.OnOrAfter)]
    [DataRow("OnOrBefore", WeekdayProximity.OnOrBefore)]
    [DataRow("Nearest", WeekdayProximity.Nearest)]
    public void ParseJson_WhenWeekdayNearDateDirection_ShouldMapToProximity(string direction, WeekdayProximity expected)
    {
        var json = $@"{{
            ""notableDates"": [ {{
                ""name"": ""Sample"",
                ""rules"": [ {{
                    ""name"": ""sample"",
                    ""category"": ""Holiday"",
                    ""weekdayNearDate"": {{ ""dayOfWeek"": ""Monday"", ""month"": ""November"", ""day"": 22, ""direction"": ""{direction}"" }}
                }} ]
            }} ]
        }}";

        NotableDateRule rule = NotableDateRuleJsonParser.ParseJson(json).Single();

        Assert.AreEqual(expected, rule.WeekdayProximity);
    }

    /// <summary>
    /// Verifies that a <c>weekdayNearDate</c> strategy inside a <c>useFrom</c> override body is parsed onto the
    /// directive's override payload.
    /// </summary>
    [TestMethod]
    public void ParseDocument_WhenOverrideUsesWeekdayNearDate_ShouldPopulateOverrideBody()
    {
        const string json = @"{
            ""useFrom"": [ {
                ""resource"": ""shared.json"",
                ""uses"": [ {
                    ""name"": ""Midsummer Day"",
                    ""rule"": {
                        ""name"": ""midsummer-fi"",
                        ""category"": ""Holiday"",
                        ""weekdayNearDate"": { ""dayOfWeek"": ""Saturday"", ""month"": ""June"", ""day"": 20, ""direction"": ""OnOrAfter"" }
                    }
                } ]
            } ]
        }";

        ParsedNotableDateDocument document = NotableDateRuleJsonParser.ParseDocument(json);
        NotableDateRuleOverrideBody body = document.UseGroups.Single().Uses.Single().OverrideBody!;

        Assert.AreEqual(DateResolutionStrategy.WeekdayNearDate, body.Strategy);
        Assert.AreEqual(6, body.Month);
        Assert.AreEqual(20, body.Day);
        Assert.AreEqual(DayOfWeek.Saturday, body.DayOfWeek);
        Assert.AreEqual(WeekdayProximity.OnOrAfter, body.WeekdayProximity);
    }

    /// <summary>
    /// Verifies that omitting the required <c>direction</c> field fails schema validation.
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenWeekdayNearDateMissingDirection_ShouldThrowExactly()
    {
        const string json = @"{
            ""notableDates"": [ {
                ""name"": ""Sample"",
                ""rules"": [ {
                    ""name"": ""sample"",
                    ""category"": ""Holiday"",
                    ""weekdayNearDate"": { ""dayOfWeek"": ""Saturday"", ""month"": ""June"", ""day"": 20 }
                } ]
            } ]
        }";

        Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = NotableDateRuleJsonParser.ParseJson(json);
        });
    }

    /// <summary>
    /// Verifies that declaring two strategy objects on a single rule fails schema validation (the strategy selection
    /// permits exactly one).
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenRuleDeclaresFixedAndWeekdayNearDate_ShouldThrowExactly()
    {
        const string json = @"{
            ""notableDates"": [ {
                ""name"": ""Sample"",
                ""rules"": [ {
                    ""name"": ""sample"",
                    ""category"": ""Holiday"",
                    ""fixed"": { ""month"": ""June"", ""day"": 20 },
                    ""weekdayNearDate"": { ""dayOfWeek"": ""Saturday"", ""month"": ""June"", ""day"": 20, ""direction"": ""OnOrAfter"" }
                } ]
            } ]
        }";

        Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = NotableDateRuleJsonParser.ParseJson(json);
        });
    }
}
