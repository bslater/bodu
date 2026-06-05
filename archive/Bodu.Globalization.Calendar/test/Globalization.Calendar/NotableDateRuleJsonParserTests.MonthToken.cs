// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleJsonParserTests.MonthToken.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the month-token parsing surface of <see cref="NotableDateRuleJsonParser" />, including numeric months,
/// English month names, Hebrew month aliases, and invalid tokens.
/// </summary>
public partial class NotableDateRuleJsonParserTests
{
    /// <summary>
    /// Verifies that a Fixed rule authored with a numeric month token (e.g. <c>"7"</c>) populates
    /// <see cref="NotableDateRule.Month" /> as the corresponding integer.
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenFixedMonthIsNumeric_ShouldReturnNumericMonth()
    {
        const string json = @"{
			""notableDates"": [
				{ ""name"": ""Numeric Month"", ""rules"": [ {
					""name"": ""Numeric Month Rule"",
					""category"": ""Holiday"",
					""fixed"": { ""month"": ""7"", ""day"": 4 }
				} ] }
			]
		}";

        NotableDateRule rule = NotableDateRuleJsonParser.ParseJson(json).Single();

        Assert.AreEqual(7, rule.Month);
        Assert.AreEqual(4, rule.Day);
        Assert.IsNull(rule.CalendarMonthAlias);
    }

    /// <summary>
    /// Verifies that a Fixed rule authored with the maximum supported numeric month (<c>"13"</c>) is accepted to
    /// support lunisolar calendars with intercalary months.
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenFixedMonthIsMaximumNumericMonth_ShouldReturnNumericMonth()
    {
        const string json = @"{
			""notableDates"": [
				{ ""name"": ""Intercalary"", ""rules"": [ {
					""name"": ""Intercalary Rule"",
					""category"": ""Cultural"",
					""calendarType"": ""System.Globalization.ChineseLunisolarCalendar"",
					""fixed"": { ""month"": ""13"", ""day"": 1 }
				} ] }
			]
		}";

        NotableDateRule rule = NotableDateRuleJsonParser.ParseJson(json).Single();

        Assert.AreEqual(13, rule.Month);
    }

    /// <summary>
    /// Verifies that a Fixed rule authored with a full English month name resolves to the corresponding integer.
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenFixedMonthIsEnglishName_ShouldReturnExpectedMonth()
    {
        const string json = @"{
			""notableDates"": [
				{ ""name"": ""English Month"", ""rules"": [ {
					""name"": ""English Month Rule"",
					""category"": ""Holiday"",
					""fixed"": { ""month"": ""November"", ""day"": 11 }
				} ] }
			]
		}";

        NotableDateRule rule = NotableDateRuleJsonParser.ParseJson(json).Single();

        Assert.AreEqual(11, rule.Month);
        Assert.AreEqual(11, rule.Day);
    }

    /// <summary>
    /// Verifies that a Fixed rule authored with a numeric month token outside the supported 1..13 range is rejected
    /// by schema validation as <see cref="JsonException" /> via the <c>monthOrNumber</c> oneOf constraint.
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenFixedMonthIsOutOfRangeNumeric_ShouldThrowExactly()
    {
        const string json = @"{
			""notableDates"": [
				{ ""name"": ""OutOfRange"", ""rules"": [ {
					""name"": ""Out Of Range Rule"",
					""category"": ""Holiday"",
					""fixed"": { ""month"": ""14"", ""day"": 1 }
				} ] }
			]
		}";

        Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = NotableDateRuleJsonParser.ParseJson(json);
        });
    }

    /// <summary>
    /// Verifies that a Fixed rule authored with the zero numeric month value is rejected by schema validation as
    /// <see cref="JsonException" /> via the <c>monthOrNumber</c> oneOf constraint.
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenFixedMonthIsZeroNumeric_ShouldThrowExactly()
    {
        const string json = @"{
			""notableDates"": [
				{ ""name"": ""Zero"", ""rules"": [ {
					""name"": ""Zero Rule"",
					""category"": ""Holiday"",
					""fixed"": { ""month"": ""0"", ""day"": 1 }
				} ] }
			]
		}";

        Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = NotableDateRuleJsonParser.ParseJson(json);
        });
    }

    /// <summary>
    /// Verifies that a Fixed rule whose month token is a simple Hebrew month name (e.g. <c>"Tishri"</c>) resolves to
    /// the corresponding integer and leaves <see cref="NotableDateRule.CalendarMonthAlias" /> unset.
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenFixedMonthIsSimpleHebrewMonth_ShouldReturnNumericMonth()
    {
        const string json = @"{
			""notableDates"": [
				{ ""name"": ""Rosh Hashanah"", ""rules"": [ {
					""name"": ""Rosh Hashanah Rule"",
					""category"": ""Religious"",
					""calendarType"": ""System.Globalization.HebrewCalendar"",
					""fixed"": { ""month"": ""Tishri"", ""day"": 1 }
				} ] }
			]
		}";

        NotableDateRule rule = NotableDateRuleJsonParser.ParseJson(json).Single();

        Assert.AreEqual(1, rule.Month);
        Assert.IsNull(rule.CalendarMonthAlias);
    }

    /// <summary>
    /// Verifies that a Fixed rule whose month token is an AdarII Hebrew month alias resolves to integer 7.
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenFixedMonthIsAdarII_ShouldReturnExpectedMonth()
    {
        const string json = @"{
			""notableDates"": [
				{ ""name"": ""Purim"", ""rules"": [ {
					""name"": ""Purim Rule"",
					""category"": ""Religious"",
					""calendarType"": ""System.Globalization.HebrewCalendar"",
					""fixed"": { ""month"": ""AdarII"", ""day"": 14 }
				} ] }
			]
		}";

        NotableDateRule rule = NotableDateRuleJsonParser.ParseJson(json).Single();

        Assert.AreEqual(7, rule.Month);
        Assert.IsNull(rule.CalendarMonthAlias);
    }

    /// <summary>
    /// Verifies that a Fixed rule whose month token is a leap-year-dependent Hebrew month (e.g. <c>"Nisan"</c>)
    /// populates <see cref="NotableDateRule.CalendarMonthAlias" /> and leaves <see cref="NotableDateRule.Month" /> unset.
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenFixedMonthIsLeapDependentHebrewAlias_ShouldPopulateCalendarMonthAlias()
    {
        const string json = @"{
			""notableDates"": [
				{ ""name"": ""Passover"", ""rules"": [ {
					""name"": ""Passover Rule"",
					""category"": ""Religious"",
					""calendarType"": ""System.Globalization.HebrewCalendar"",
					""fixed"": { ""month"": ""Nisan"", ""day"": 15 }
				} ] }
			]
		}";

        NotableDateRule rule = NotableDateRuleJsonParser.ParseJson(json).Single();

        Assert.IsNull(rule.Month);
        Assert.AreEqual("Nisan", rule.CalendarMonthAlias);
    }

    /// <summary>
    /// Verifies that a Fixed rule whose month token is <c>"LastAdar"</c> populates the calendar month alias.
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenFixedMonthIsLastAdar_ShouldPopulateCalendarMonthAlias()
    {
        const string json = @"{
			""notableDates"": [
				{ ""name"": ""Purim Katan"", ""rules"": [ {
					""name"": ""Purim Katan Rule"",
					""category"": ""Religious"",
					""calendarType"": ""System.Globalization.HebrewCalendar"",
					""fixed"": { ""month"": ""LastAdar"", ""day"": 14 }
				} ] }
			]
		}";

        NotableDateRule rule = NotableDateRuleJsonParser.ParseJson(json).Single();

        Assert.IsNull(rule.Month);
        Assert.AreEqual("LastAdar", rule.CalendarMonthAlias);
    }

    /// <summary>
    /// Verifies that a DayOfWeekInMonth rule authored with a numeric month token is rejected by schema validation
    /// as <see cref="JsonException" />. The <c>dayOfWeekInMonthStrategy.month</c> field is typed as <c>monthName</c>
    /// (Gregorian enum names only), matching the XSD's literal type.
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenDayOfWeekInMonthIsNumericMonth_ShouldThrowExactly()
    {
        const string json = @"{
			""notableDates"": [
				{ ""name"": ""Numeric Weekday"", ""rules"": [ {
					""name"": ""Numeric Weekday Rule"",
					""category"": ""Observance"",
					""dayOfWeekInMonth"": { ""month"": ""10"", ""dayOfWeek"": ""Monday"", ""weekOrdinal"": ""Second"" }
				} ] }
			]
		}";

        Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = NotableDateRuleJsonParser.ParseJson(json);
        });
    }

    /// <summary>
    /// Verifies that a DayOfWeekInMonth rule authored with a numeric month outside 1..13 is rejected by schema
    /// validation as <see cref="JsonException" /> via the <c>monthName</c> enum constraint.
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenDayOfWeekInMonthIsOutOfRangeMonth_ShouldThrowExactly()
    {
        const string json = @"{
			""notableDates"": [
				{ ""name"": ""Bad Weekday"", ""rules"": [ {
					""name"": ""Bad Weekday Rule"",
					""category"": ""Observance"",
					""dayOfWeekInMonth"": { ""month"": ""20"", ""dayOfWeek"": ""Monday"", ""weekOrdinal"": ""Second"" }
				} ] }
			]
		}";

        Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = NotableDateRuleJsonParser.ParseJson(json);
        });
    }

    /// <summary>
    /// Verifies that an adjustment whose <c>comparisonMonth</c> attribute uses an English month name resolves
    /// to a synthetic <see cref="DateTime" /> in the conventional sentinel year (2000).
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenAdjustmentComparisonMonthAndDayPresent_ShouldPopulateComparisonDate()
    {
        const string json = @"{
			""notableDates"": [
				{ ""name"": ""Boxing Day"", ""rules"": [ {
					""name"": ""Boxing Day Rule"",
					""category"": ""Holiday"",
					""fixed"": { ""month"": ""December"", ""day"": 26 },
					""adjustments"": [
						{ ""key"": ""after-christmas"", ""when"": ""IfWeekend"", ""action"": ""MoveToNextWeekday"", ""comparisonMonth"": ""December"", ""comparisonDay"": 25 }
					]
				} ] }
			]
		}";

        NotableDateRule rule = NotableDateRuleJsonParser.ParseJson(json).Single();

        Assert.AreEqual(1, rule.Adjustments.Length);
        Assert.IsNotNull(rule.Adjustments[0].ComparisonDate);
        Assert.AreEqual(12, rule.Adjustments[0].ComparisonDate!.Value.Month);
        Assert.AreEqual(25, rule.Adjustments[0].ComparisonDate!.Value.Day);
    }

    /// <summary>
    /// Verifies that an adjustment without <c>comparisonMonth</c> or <c>comparisonDay</c> leaves
    /// <see cref="ObservanceAdjustment.ComparisonDate" /> unset.
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenAdjustmentComparisonMonthAndDayAbsent_ShouldLeaveComparisonDateNull()
    {
        const string json = @"{
			""notableDates"": [
				{ ""name"": ""No Comparison"", ""rules"": [ {
					""name"": ""No Comparison Rule"",
					""category"": ""Holiday"",
					""fixed"": { ""month"": ""December"", ""day"": 26 },
					""adjustments"": [
						{ ""key"": ""simple"", ""when"": ""IfWeekend"", ""action"": ""MoveToNextWeekday"" }
					]
				} ] }
			]
		}";

        NotableDateRule rule = NotableDateRuleJsonParser.ParseJson(json).Single();

        Assert.AreEqual(1, rule.Adjustments.Length);
        Assert.IsNull(rule.Adjustments[0].ComparisonDate);
    }

    /// <summary>
    /// Verifies that every supported English Gregorian month name on a Fixed strategy resolves to the matching
    /// integer 1..12. Exercises <c>ParseMonthToken</c> across the full Gregorian month enumeration.
    /// </summary>
    [TestCategory("Regression")]
    [DataRow("January", 1)]
    [DataRow("February", 2)]
    [DataRow("March", 3)]
    [DataRow("April", 4)]
    [DataRow("May", 5)]
    [DataRow("June", 6)]
    [DataRow("July", 7)]
    [DataRow("August", 8)]
    [DataRow("September", 9)]
    [DataRow("October", 10)]
    [DataRow("November", 11)]
    [DataRow("December", 12)]
    [TestMethod]
    public void ParseJson_WhenFixedMonthIsEnglishMonthName_ShouldReturnExpectedMonth(string monthToken, int expectedMonth)
    {
        var json = BuildFixedMonthJson(monthToken);

        NotableDateRule rule = NotableDateRuleJsonParser.ParseJson(json).Single();

        Assert.AreEqual(expectedMonth, rule.Month);
        Assert.IsNull(rule.CalendarMonthAlias);
    }

    /// <summary>
    /// Verifies that every simple Hebrew month name (those whose calendar position does not depend on the leap year)
    /// resolves to its canonical integer 1..7 via <c>ParseMonthToken</c>'s Hebrew alias path.
    /// </summary>
    [TestCategory("Regression")]
    [DataRow("Tishri", 1)]
    [DataRow("Heshvan", 2)]
    [DataRow("Kislev", 3)]
    [DataRow("Tevet", 4)]
    [DataRow("Shevat", 5)]
    [DataRow("AdarI", 6)]
    [DataRow("AdarII", 7)]
    [TestMethod]
    public void ParseJson_WhenFixedMonthIsSimpleHebrewName_ShouldReturnExpectedMonth(string monthToken, int expectedMonth)
    {
        var json = BuildFixedMonthJson(monthToken, calendarType: "System.Globalization.HebrewCalendar", category: "Religious");

        NotableDateRule rule = NotableDateRuleJsonParser.ParseJson(json).Single();

        Assert.AreEqual(expectedMonth, rule.Month);
        Assert.IsNull(rule.CalendarMonthAlias);
    }

    /// <summary>
    /// Verifies that every leap-year-dependent Hebrew month name populates <see cref="NotableDateRule.CalendarMonthAlias" />
    /// and leaves <see cref="NotableDateRule.Month" /> unset so the resolver can pick the runtime-correct slot.
    /// </summary>
    [TestCategory("Regression")]
    [DataRow("LastAdar")]
    [DataRow("Nisan")]
    [DataRow("Iyar")]
    [DataRow("Sivan")]
    [DataRow("Tammuz")]
    [DataRow("Av")]
    [DataRow("Elul")]
    [TestMethod]
    public void ParseJson_WhenFixedMonthIsLeapDependentHebrewAlias_ShouldPopulateCalendarMonthAlias(string monthAlias)
    {
        var json = BuildFixedMonthJson(monthAlias, calendarType: "System.Globalization.HebrewCalendar", category: "Religious");

        NotableDateRule rule = NotableDateRuleJsonParser.ParseJson(json).Single();

        Assert.IsNull(rule.Month);
        Assert.AreEqual(monthAlias, rule.CalendarMonthAlias);
    }

    /// <summary>
    /// Verifies that every supported string-form numeric month token <c>"1"</c> through <c>"12"</c> resolves to the
    /// matching integer in the default Gregorian context, exercising the schema's string-numeric pattern branch and
    /// <c>ParseMonthToken</c>'s numeric alternative. The lunisolar-only <c>"13"</c> case is covered separately by
    /// <see cref="ParseJson_WhenFixedMonthIsMaximumNumericMonth_ShouldReturnNumericMonth" /> with an explicit
    /// non-Gregorian <c>calendarType</c>.
    /// </summary>
    [TestCategory("Regression")]
    [DataRow("1", 1)]
    [DataRow("2", 2)]
    [DataRow("3", 3)]
    [DataRow("4", 4)]
    [DataRow("5", 5)]
    [DataRow("6", 6)]
    [DataRow("7", 7)]
    [DataRow("8", 8)]
    [DataRow("9", 9)]
    [DataRow("10", 10)]
    [DataRow("11", 11)]
    [DataRow("12", 12)]
    [TestMethod]
    public void ParseJson_WhenFixedMonthIsStringNumeric_ShouldReturnExpectedMonth(string monthToken, int expectedMonth)
    {
        var json = BuildFixedMonthJson(monthToken);

        NotableDateRule rule = NotableDateRuleJsonParser.ParseJson(json).Single();

        Assert.AreEqual(expectedMonth, rule.Month);
        Assert.IsNull(rule.CalendarMonthAlias);
    }

    /// <summary>
    /// Builds a minimal Fixed-strategy notable-date document with the supplied month token, optional calendar type,
    /// and optional category. The helper centralises the boilerplate consumed by the month-token data-driven tests so
    /// each row varies only the under-test value.
    /// </summary>
    /// <param name="monthToken">The month token to author on the Fixed strategy.</param>
    /// <param name="calendarType">An optional calendar-type assembly-qualified name to author on the rule.</param>
    /// <param name="category">The notable-date category enum value to author. Defaults to <c>"Holiday"</c>.</param>
    /// <returns>A JSON document with one notable date and one Fixed rule.</returns>
    private static string BuildFixedMonthJson(string monthToken, string? calendarType = null, string category = "Holiday")
    {
        var calendarTypeClause = calendarType is null ? string.Empty : $@", ""calendarType"": ""{calendarType}""";
        return $@"{{
			""notableDates"": [
				{{ ""name"": ""MonthTokenTest"", ""rules"": [ {{
					""name"": ""MonthTokenTestRule"",
					""category"": ""{category}""{calendarTypeClause},
					""fixed"": {{ ""month"": ""{monthToken}"", ""day"": 1 }}
				}} ] }}
			]
		}}";
    }
}
