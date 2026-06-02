// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleJsonParserTests.RelativeWeekdayInMonth.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Extensions;
using System.Text.Json;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies that <see cref="NotableDateRuleJsonParser" /> parses the <c>relativeWeekdayInMonth</c> strategy object — on
/// the rule body and inside a <c>useFrom</c> override body — and rejects malformed authoring via schema validation, in
/// parity with the XML pathway.
/// </summary>
public partial class NotableDateRuleJsonParserTests
{
    private const string RelativeWeekdayInMonthRuleJson = @"{
        ""notableDates"": [ {
            ""name"": ""Election Day"",
            ""rules"": [ {
                ""name"": ""election"",
                ""category"": ""Civic"",
                ""territory"": ""US"",
                ""relativeWeekdayInMonth"": { ""month"": ""November"", ""weekOrdinal"": ""First"", ""dayOfWeek"": ""Monday"", ""relativeDayOfWeek"": ""Tuesday"", ""direction"": ""OnOrAfter"" }
            } ]
        } ]
    }";

    /// <summary>
    /// Verifies that a <c>relativeWeekdayInMonth</c> rule populates the strategy, the anchor fields, the target weekday,
    /// and the direction.
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenRelativeWeekdayInMonthRule_ShouldPopulateStrategyAndFields()
    {
        NotableDateRule rule = NotableDateRuleJsonParser.ParseJson(RelativeWeekdayInMonthRuleJson).Single();

        Assert.AreEqual(DateResolutionStrategy.RelativeWeekdayInMonth, rule.Strategy);
        Assert.AreEqual(11, rule.Month);
        Assert.AreEqual(DayOfWeek.Monday, rule.DayOfWeek);
        Assert.AreEqual(WeekOfMonthOrdinal.First, rule.WeekOrdinal);
        Assert.AreEqual(DayOfWeek.Tuesday, rule.RelativeDayOfWeek);
        Assert.AreEqual(WeekdayProximity.OnOrAfter, rule.WeekdayProximity);
    }

    /// <summary>
    /// Verifies that a <c>relativeWeekdayInMonth</c> strategy inside a <c>useFrom</c> override body is parsed onto the
    /// directive's override payload.
    /// </summary>
    [TestMethod]
    public void ParseDocument_WhenOverrideUsesRelativeWeekdayInMonth_ShouldPopulateOverrideBody()
    {
        const string json = @"{
            ""useFrom"": [ {
                ""resource"": ""shared.json"",
                ""uses"": [ {
                    ""name"": ""Election Day"",
                    ""rule"": {
                        ""name"": ""election-us"",
                        ""category"": ""Civic"",
                        ""relativeWeekdayInMonth"": { ""month"": ""November"", ""weekOrdinal"": ""First"", ""dayOfWeek"": ""Monday"", ""relativeDayOfWeek"": ""Tuesday"", ""direction"": ""OnOrAfter"" }
                    }
                } ]
            } ]
        }";

        ParsedNotableDateDocument document = NotableDateRuleJsonParser.ParseDocument(json);
        NotableDateRuleOverrideBody body = document.UseGroups.Single().Uses.Single().OverrideBody!;

        Assert.AreEqual(DateResolutionStrategy.RelativeWeekdayInMonth, body.Strategy);
        Assert.AreEqual(11, body.Month);
        Assert.AreEqual(DayOfWeek.Monday, body.DayOfWeek);
        Assert.AreEqual(WeekOfMonthOrdinal.First, body.WeekOrdinal);
        Assert.AreEqual(DayOfWeek.Tuesday, body.RelativeDayOfWeek);
        Assert.AreEqual(WeekdayProximity.OnOrAfter, body.WeekdayProximity);
    }

    /// <summary>
    /// Verifies that omitting the required <c>relativeDayOfWeek</c> field fails schema validation.
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenRelativeWeekdayInMonthMissingRelativeDayOfWeek_ShouldThrowExactly()
    {
        const string json = @"{
            ""notableDates"": [ {
                ""name"": ""Sample"",
                ""rules"": [ {
                    ""name"": ""sample"",
                    ""category"": ""Civic"",
                    ""relativeWeekdayInMonth"": { ""month"": ""November"", ""weekOrdinal"": ""First"", ""dayOfWeek"": ""Monday"", ""direction"": ""OnOrAfter"" }
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
    public void ParseJson_WhenRuleDeclaresDayOfWeekInMonthAndRelativeWeekdayInMonth_ShouldThrowExactly()
    {
        const string json = @"{
            ""notableDates"": [ {
                ""name"": ""Sample"",
                ""rules"": [ {
                    ""name"": ""sample"",
                    ""category"": ""Civic"",
                    ""dayOfWeekInMonth"": { ""month"": ""November"", ""dayOfWeek"": ""Monday"", ""weekOrdinal"": ""First"" },
                    ""relativeWeekdayInMonth"": { ""month"": ""November"", ""weekOrdinal"": ""First"", ""dayOfWeek"": ""Monday"", ""relativeDayOfWeek"": ""Tuesday"", ""direction"": ""OnOrAfter"" }
                } ]
            } ]
        }";

        Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = NotableDateRuleJsonParser.ParseJson(json);
        });
    }
}
