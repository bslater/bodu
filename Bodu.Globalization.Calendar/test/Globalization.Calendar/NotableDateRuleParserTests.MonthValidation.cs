// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleParserTests.MonthValidation.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies calendar-aware month validation on the XML parser: numeric month 13 is rejected for Gregorian rules
/// (where 13 has no meaning) but accepted for lunisolar / Hebrew calendars that carry intercalary months. Mirrors
/// the JSON parser's MonthValidation coverage so the two surfaces stay in lockstep.
/// </summary>
public partial class NotableDateRuleParserTests
{
    /// <summary>
    /// Verifies that a Fixed rule authored without a <c>calendarType</c> (Gregorian by default) and a numeric
    /// month token of <c>"13"</c> is rejected at parse time with a <see cref="FormatException" /> whose message
    /// names the offending token and the Gregorian 1–12 bound.
    /// </summary>
    [TestMethod]
    public void ParseXml_WhenFixedMonthIsThirteenWithoutCalendarType_ShouldThrowExactly()
    {
        const string xml = @"
			<NotableDates xmlns=""urn:bodu:globalization:calendar"">
				<NotableDate name=""Bad Month"">
					<Rule name=""Bad Month Rule"" category=""Holiday"">
						<Fixed month=""13"" day=""1"" />
					</Rule>
				</NotableDate>
			</NotableDates>";

        FormatException ex = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = NotableDateRuleParser.ParseXml(xml);
        });

        Assert.IsTrue(ex.Message.Contains("'13'", StringComparison.Ordinal));
        Assert.IsTrue(ex.Message.Contains("1–12", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that a Fixed rule with an explicit Gregorian <c>calendarType</c> and a numeric month token of
    /// <c>"13"</c> is rejected at parse time. Gregorian has only 12 months; the value cannot be honoured.
    /// </summary>
    [TestMethod]
    public void ParseXml_WhenFixedMonthIsThirteenWithGregorianCalendarType_ShouldThrowExactly()
    {
        const string xml = @"
			<NotableDates xmlns=""urn:bodu:globalization:calendar"">
				<NotableDate name=""Bad Month"">
					<Rule name=""Bad Month Rule""
					      category=""Holiday""
					      calendarType=""System.Globalization.GregorianCalendar"">
						<Fixed month=""13"" day=""1"" />
					</Rule>
				</NotableDate>
			</NotableDates>";

        FormatException ex = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = NotableDateRuleParser.ParseXml(xml);
        });

        Assert.IsTrue(ex.Message.Contains("'13'", StringComparison.Ordinal));
        Assert.IsTrue(ex.Message.Contains("1–12", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that a Fixed rule with the Hebrew calendar accepts numeric month token <c>"13"</c>, which represents
    /// the intercalary Adar I in a Hebrew leap year. Confirms the lunisolar branch in <c>ParseMonthToken</c> is
    /// reached for a calendar other than <see cref="System.Globalization.ChineseLunisolarCalendar" />.
    /// </summary>
    [TestMethod]
    public void ParseXml_WhenFixedMonthIsThirteenWithHebrewCalendarType_ShouldReturnNumericMonth()
    {
        const string xml = @"
			<NotableDates xmlns=""urn:bodu:globalization:calendar"">
				<NotableDate name=""Intercalary Hebrew"">
					<Rule name=""Intercalary Hebrew Rule""
					      category=""Religious""
					      calendarType=""System.Globalization.HebrewCalendar"">
						<Fixed month=""13"" day=""1"" />
					</Rule>
				</NotableDate>
			</NotableDates>";

        NotableDateRule rule = NotableDateRuleParser.ParseXml(xml).Single();

        Assert.AreEqual(13, rule.Month);
        Assert.IsNull(rule.CalendarMonthAlias);
    }
}
