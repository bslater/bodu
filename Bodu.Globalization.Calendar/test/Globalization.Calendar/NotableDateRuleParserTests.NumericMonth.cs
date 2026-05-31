// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleParserTests.NumericMonth.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public partial class NotableDateRuleParserTests
{
    private const string LunarFixedRuleXml = @"
		<NotableDates xmlns=""urn:bodu:globalization:calendar"">
			<NotableDate name=""Lunar New Year"">
				<Rule name=""Lunar New Year""
				      category=""Cultural""
				      firstYear=""1901""
				      lastYear=""2100""
				      calendarType=""System.Globalization.ChineseLunisolarCalendar"">
					<Fixed month=""1"" day=""1"" />
					<Tag>LunarCalendar</Tag>
				</Rule>
			</NotableDate>
		</NotableDates>";

    /// <summary>
    /// Verifies that a Fixed rule authored with a numeric month value (e.g. <c>month="1"</c>) is parsed
    /// correctly and stored as the corresponding integer in <see cref="NotableDateRule.Month" />.
    /// </summary>
    [TestMethod]
    public void ParseXml_WhenFixedRuleWithNumericMonth_ShouldPopulateMonthAsInteger()
    {
        NotableDateRule rule = NotableDateRuleParser.ParseXml(LunarFixedRuleXml).Single();

        Assert.AreEqual(DateResolutionStrategy.Fixed, rule.Strategy);
        Assert.AreEqual(1, rule.Month);
        Assert.AreEqual(1, rule.Day);
    }

    /// <summary>
    /// Verifies that a Fixed rule with a <c>calendarType</c> attribute resolves the attribute value to the
    /// corresponding <see cref="Type" /> and stores it in <see cref="NotableDateRule.CalendarType" />.
    /// </summary>
    [TestMethod]
    public void ParseXml_WhenFixedRuleWithNumericMonthAndCalendarType_ShouldPopulateCalendarType()
    {
        NotableDateRule rule = NotableDateRuleParser.ParseXml(LunarFixedRuleXml).Single();

        Assert.IsNotNull(rule.CalendarType);
        Assert.AreEqual(typeof(System.Globalization.ChineseLunisolarCalendar), rule.CalendarType);
    }
}
