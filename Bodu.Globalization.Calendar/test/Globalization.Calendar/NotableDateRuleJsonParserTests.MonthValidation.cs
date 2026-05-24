// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleJsonParserTests.MonthValidation.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
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
    /// month token of <c>"13"</c> is rejected at parse time.
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenFixedMonthIsThirteenWithoutCalendarType_ShouldThrow()
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

        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = NotableDateRuleJsonParser.ParseJson(json);
        });
    }

    /// <summary>
    /// Verifies that a Fixed rule with an explicit Gregorian <c>calendarType</c> and a numeric month token of
    /// <c>"13"</c> is rejected at parse time. Gregorian has only 12 months; the value cannot be honoured.
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenFixedMonthIsThirteenWithGregorianCalendarType_ShouldThrow()
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

        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = NotableDateRuleJsonParser.ParseJson(json);
        });
    }
}
