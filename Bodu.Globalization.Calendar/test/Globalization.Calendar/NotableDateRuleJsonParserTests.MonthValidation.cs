// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleJsonParserTests.MonthValidation.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies calendar-aware month validation: numeric month 13 is rejected for Gregorian rules (where 13 has no
/// meaning) but accepted for lunisolar / Hebrew calendars that carry intercalary months.
/// </summary>
public partial class NotableDateRuleJsonParserTests
{
    /// <summary>
    /// Verifies that a Fixed rule authored without a <c>calendarType</c> (Gregorian by default) and a numeric
    /// month token of <c>"13"</c> is rejected at parse time with a <see cref="FormatException" /> whose message
    /// names the offending token and the Gregorian 1–12 bound.
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenFixedMonthIsThirteenWithoutCalendarType_ShouldThrowExactly()
    {
        const string json = @"{
			""notableDates"": [
				{ ""name"": ""Bad Month"", ""rules"": [ {
					""name"": ""Bad Month Rule"",
					""category"": ""Holiday"",
					""fixed"": { ""month"": ""13"", ""day"": 1 }
				} ] }
			]
		}";

        FormatException ex = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = NotableDateRuleJsonParser.ParseJson(json);
        });

        Assert.IsTrue(ex.Message.Contains("'13'", StringComparison.Ordinal));
        Assert.IsTrue(ex.Message.Contains("1–12", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that a Fixed rule with an explicit Gregorian <c>calendarType</c> and a numeric month token of
    /// <c>"13"</c> is rejected at parse time. Gregorian has only 12 months; the value cannot be honoured.
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenFixedMonthIsThirteenWithGregorianCalendarType_ShouldThrowExactly()
    {
        const string json = @"{
			""notableDates"": [
				{ ""name"": ""Bad Month"", ""rules"": [ {
					""name"": ""Bad Month Rule"",
					""category"": ""Holiday"",
					""calendarType"": ""System.Globalization.GregorianCalendar"",
					""fixed"": { ""month"": ""13"", ""day"": 1 }
				} ] }
			]
		}";

        FormatException ex = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = NotableDateRuleJsonParser.ParseJson(json);
        });

        Assert.IsTrue(ex.Message.Contains("'13'", StringComparison.Ordinal));
        Assert.IsTrue(ex.Message.Contains("1–12", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that a Fixed rule with the Hebrew calendar accepts numeric month token <c>"13"</c>, which represents
    /// the intercalary Adar I in a Hebrew leap year. The lunisolar branch in <c>ParseMonthToken</c> is exercised
    /// for a calendar other than <see cref="System.Globalization.ChineseLunisolarCalendar" />.
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenFixedMonthIsThirteenWithHebrewCalendarType_ShouldReturnNumericMonth()
    {
        const string json = @"{
			""notableDates"": [
				{ ""name"": ""Intercalary Hebrew"", ""rules"": [ {
					""name"": ""Intercalary Hebrew Rule"",
					""category"": ""Religious"",
					""calendarType"": ""System.Globalization.HebrewCalendar"",
					""fixed"": { ""month"": ""13"", ""day"": 1 }
				} ] }
			]
		}";

        NotableDateRule rule = NotableDateRuleJsonParser.ParseJson(json).Single();

        Assert.AreEqual(13, rule.Month);
        Assert.IsNull(rule.CalendarMonthAlias);
    }
}
