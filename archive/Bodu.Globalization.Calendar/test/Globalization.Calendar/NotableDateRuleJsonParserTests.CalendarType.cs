// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleJsonParserTests.CalendarType.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text.Json;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies how <see cref="NotableDateRuleJsonParser" /> resolves the optional <c>calendarType</c> field through
/// <c>ParseOptionalType&lt;Calendar&gt;</c>: absent fields leave the parsed type null, valid assembly-qualified
/// names resolve to the matching <see cref="Type" />, and assembly-qualified names that do not derive from
/// <see cref="Calendar" /> are silently dropped rather than thrown. Authoring strings that do not match the
/// schema's <c>assemblyTypeName</c> pattern are rejected by schema validation as <see cref="JsonException" />.
/// </summary>
public partial class NotableDateRuleJsonParserTests
{
    /// <summary>
    /// Verifies that omitting <c>calendarType</c> on a Fixed rule leaves <see cref="NotableDateRule.CalendarType" />
    /// at <see langword="null" /> — the optional-type absent path through <c>ParseOptionalType</c>.
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenCalendarTypeIsMissing_ShouldLeaveCalendarTypeNull()
    {
        const string json = @"{
			""notableDates"": [
				{ ""name"": ""NoCalendar"", ""rules"": [ {
					""name"": ""NoCalendarRule"",
					""category"": ""Holiday"",
					""fixed"": { ""month"": ""January"", ""day"": 1 }
				} ] }
			]
		}";

        NotableDateRule rule = NotableDateRuleJsonParser.ParseJson(json).Single();

        Assert.IsNull(rule.CalendarType);
    }

    /// <summary>
    /// Verifies that a valid <c>calendarType</c> string resolves to the matching <see cref="Calendar" />-derived
    /// <see cref="Type" /> through the optional-type happy path.
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenCalendarTypeIsValidTypeName_ShouldResolveCalendarType()
    {
        const string json = @"{
			""notableDates"": [
				{ ""name"": ""HebrewCalendar"", ""rules"": [ {
					""name"": ""HebrewCalendarRule"",
					""category"": ""Religious"",
					""calendarType"": ""System.Globalization.HebrewCalendar"",
					""fixed"": { ""month"": ""Nisan"", ""day"": 15 }
				} ] }
			]
		}";

        NotableDateRule rule = NotableDateRuleJsonParser.ParseJson(json).Single();

        Assert.AreEqual(typeof(HebrewCalendar), rule.CalendarType);
    }

    /// <summary>
    /// Verifies that a <c>calendarType</c> whose value matches the schema's <c>assemblyTypeName</c> pattern but does
    /// not resolve to a real type throws <see cref="FormatException" /> at parse time. Strict-by-default type
    /// resolution surfaces authoring errors instead of silently dropping the rule's calendar context.
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenCalendarTypeIsUnknownTypeName_ShouldThrowFormatException()
    {
        const string json = @"{
			""notableDates"": [
				{ ""name"": ""UnknownCalendar"", ""rules"": [ {
					""name"": ""UnknownCalendarRule"",
					""category"": ""Holiday"",
					""calendarType"": ""Some.Imaginary.NonexistentCalendar"",
					""fixed"": { ""month"": ""January"", ""day"": 1 }
				} ] }
			]
		}";

        FormatException ex = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = NotableDateRuleJsonParser.ParseJson(json);
        });

        Assert.IsTrue(ex.Message.Contains("Some.Imaginary.NonexistentCalendar", StringComparison.Ordinal));
        Assert.IsTrue(ex.Message.Contains("calendarType", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that a <c>calendarType</c> whose value resolves to a real <see cref="Type" /> that is not assignable
    /// to <see cref="Calendar" /> throws <see cref="FormatException" /> at parse time, naming both the offending type
    /// and the expected base type so the author can correct the rule.
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenCalendarTypeDoesNotDeriveFromCalendar_ShouldThrowFormatException()
    {
        const string json = @"{
			""notableDates"": [
				{ ""name"": ""WrongBase"", ""rules"": [ {
					""name"": ""WrongBaseRule"",
					""category"": ""Holiday"",
					""calendarType"": ""System.String"",
					""fixed"": { ""month"": ""January"", ""day"": 1 }
				} ] }
			]
		}";

        FormatException ex = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = NotableDateRuleJsonParser.ParseJson(json);
        });

        Assert.IsTrue(ex.Message.Contains("System.String", StringComparison.Ordinal));
        Assert.IsTrue(ex.Message.Contains("Calendar", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that a <c>calendarType</c> string that violates the schema's <c>assemblyTypeName</c> pattern
    /// (for example, containing whitespace at an illegal position) is rejected by schema validation as
    /// <see cref="JsonException" /> before reaching the mapper.
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenCalendarTypeViolatesSchemaPattern_ShouldThrowExactly()
    {
        const string json = @"{
			""notableDates"": [
				{ ""name"": ""BadCalendarType"", ""rules"": [ {
					""name"": ""BadCalendarTypeRule"",
					""category"": ""Holiday"",
					""calendarType"": "" leading space"",
					""fixed"": { ""month"": ""January"", ""day"": 1 }
				} ] }
			]
		}";

        Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = NotableDateRuleJsonParser.ParseJson(json);
        });
    }
}
