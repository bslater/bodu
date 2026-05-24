// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleParserTests.FixedExtensions.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public partial class NotableDateRuleParserTests
{
    private const string SkipLeapMonthXml = @"
		<NotableDates xmlns=""urn:bodu:globalization:calendar"">
			<NotableDate name=""Dragon Boat Festival"">
				<Rule name=""Dragon Boat Festival""
				      category=""Cultural""
				      firstYear=""1901""
				      lastYear=""2100""
				      calendarType=""System.Globalization.ChineseLunisolarCalendar"">
					<Fixed month=""5"" day=""5"" skipLeapMonth=""true"" />
				</Rule>
			</NotableDate>
		</NotableDates>";

    private const string SweepCalendarYearsIntegerMonthXml = @"
		<NotableDates xmlns=""urn:bodu:globalization:calendar"">
			<NotableDate name=""Rosh Hashanah"">
				<Rule name=""Rosh Hashanah""
				      category=""Religious""
				      calendarType=""System.Globalization.HebrewCalendar"">
					<Fixed month=""Tishri"" day=""1"" sweepCalendarYears=""true"" />
				</Rule>
			</NotableDate>
		</NotableDates>";

    private const string SweepCalendarYearsAliasMonthXml = @"
		<NotableDates xmlns=""urn:bodu:globalization:calendar"">
			<NotableDate name=""Purim"">
				<Rule name=""Purim""
				      category=""Religious""
				      calendarType=""System.Globalization.HebrewCalendar"">
					<Fixed month=""LastAdar"" day=""14"" sweepCalendarYears=""true"" />
				</Rule>
			</NotableDate>
		</NotableDates>";

    private const string NisanMonthAliasXml = @"
		<NotableDates xmlns=""urn:bodu:globalization:calendar"">
			<NotableDate name=""Passover"">
				<Rule name=""Passover""
				      category=""Religious""
				      calendarType=""System.Globalization.HebrewCalendar"">
					<Fixed month=""Nisan"" day=""15"" sweepCalendarYears=""true"" />
				</Rule>
			</NotableDate>
		</NotableDates>";

    /// <summary>
    /// Verifies that a Fixed rule with <c>skipLeapMonth="true"</c> is parsed correctly and
    /// <see cref="NotableDateRule.SkipLeapMonth" /> is set to <see langword="true" />.
    /// </summary>
    [TestMethod]
    public void ParseXml_WhenFixedRuleWithSkipLeapMonth_ShouldSetSkipLeapMonthTrue()
    {
        NotableDateRule rule = NotableDateRuleParser.ParseXml(SkipLeapMonthXml).Single();

        Assert.AreEqual(DateResolutionStrategy.Fixed, rule.Strategy);
        Assert.AreEqual(5, rule.Month);
        Assert.AreEqual(5, rule.Day);
        Assert.IsTrue(rule.SkipLeapMonth);
        Assert.IsFalse(rule.SweepCalendarYears);
    }

    /// <summary>
    /// Verifies that a Fixed rule with a Hebrew month name of fixed calendar position (e.g. <c>month="Tishri"</c>)
    /// is parsed to the corresponding integer (1) in <see cref="NotableDateRule.Month" />.
    /// </summary>
    [TestMethod]
    public void ParseXml_WhenFixedRuleWithSimpleHebrewMonthName_ShouldPopulateMonthAsInteger()
    {
        NotableDateRule rule = NotableDateRuleParser.ParseXml(SweepCalendarYearsIntegerMonthXml).Single();

        Assert.AreEqual(DateResolutionStrategy.Fixed, rule.Strategy);
        Assert.AreEqual(1, rule.Month);
        Assert.AreEqual(1, rule.Day);
        Assert.IsNull(rule.CalendarMonthAlias);
        Assert.IsTrue(rule.SweepCalendarYears);
    }

    /// <summary>
    /// Verifies that a Fixed rule with a complex Hebrew month alias (e.g. <c>month="LastAdar"</c>)
    /// stores <see langword="null" /> in <see cref="NotableDateRule.Month" /> and the alias string
    /// in <see cref="NotableDateRule.CalendarMonthAlias" />.
    /// </summary>
    [TestMethod]
    public void ParseXml_WhenFixedRuleWithComplexHebrewMonthAlias_ShouldPopulateCalendarMonthAlias()
    {
        NotableDateRule rule = NotableDateRuleParser.ParseXml(SweepCalendarYearsAliasMonthXml).Single();

        Assert.AreEqual(DateResolutionStrategy.Fixed, rule.Strategy);
        Assert.IsNull(rule.Month);
        Assert.AreEqual("LastAdar", rule.CalendarMonthAlias);
        Assert.AreEqual(14, rule.Day);
        Assert.IsTrue(rule.SweepCalendarYears);
    }

    /// <summary>
    /// Verifies that the Hebrew month alias <c>"Nisan"</c> is stored in
    /// <see cref="NotableDateRule.CalendarMonthAlias" /> (not as an integer, since Nisan's calendar
    /// position is leap-year dependent).
    /// </summary>
    [TestMethod]
    public void ParseXml_WhenFixedRuleWithNisanAlias_ShouldPopulateCalendarMonthAlias()
    {
        NotableDateRule rule = NotableDateRuleParser.ParseXml(NisanMonthAliasXml).Single();

        Assert.IsNull(rule.Month);
        Assert.AreEqual("Nisan", rule.CalendarMonthAlias);
        Assert.AreEqual(15, rule.Day);
        Assert.IsTrue(rule.SweepCalendarYears);
        Assert.AreEqual(typeof(System.Globalization.HebrewCalendar), rule.CalendarType);
    }
}
