// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleParserTests.MonthToken.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Linq;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the month-token parsing surface of <see cref="NotableDateRuleParser" />, including English month
/// names, numeric months for lunisolar calendars, and Hebrew month aliases.
/// </summary>
public partial class NotableDateRuleParserTests
{
	/// <summary>
	/// Verifies that a Fixed rule authored with a full English month name (e.g. <c>"November"</c>) populates
	/// <see cref="NotableDateRule.Month" /> with the corresponding integer.
	/// </summary>
	[TestMethod]
	public void ParseXml_WhenFixedMonthIsEnglishName_ShouldReturnExpectedMonth()
	{
		const string xml = @"
			<NotableDates xmlns=""urn:bodu:globalization:calendar"">
				<NotableDate name=""Armistice Day"">
					<Rule name=""Armistice Day Rule"" category=""Remembrance"">
						<Fixed month=""November"" day=""11"" />
					</Rule>
				</NotableDate>
			</NotableDates>";

		NotableDateRule rule = NotableDateRuleParser.ParseXml(xml).Single();

		Assert.AreEqual(11, rule.Month);
		Assert.IsNull(rule.CalendarMonthAlias);
	}

	/// <summary>
	/// Verifies that a Fixed rule authored with the maximum supported numeric month token (<c>"13"</c>) is accepted
	/// and produces a numeric month value.
	/// </summary>
	[TestMethod]
	public void ParseXml_WhenFixedMonthIsMaximumNumeric_ShouldReturnExpectedMonth()
	{
		const string xml = @"
			<NotableDates xmlns=""urn:bodu:globalization:calendar"">
				<NotableDate name=""Intercalary"">
					<Rule name=""Intercalary Rule""
					      category=""Cultural""
					      calendarType=""System.Globalization.ChineseLunisolarCalendar"">
						<Fixed month=""13"" day=""1"" />
					</Rule>
				</NotableDate>
			</NotableDates>";

		NotableDateRule rule = NotableDateRuleParser.ParseXml(xml).Single();

		Assert.AreEqual(13, rule.Month);
	}

	/// <summary>
	/// Verifies that a Fixed rule authored with a simple Hebrew month name (e.g. <c>"Tishri"</c>) populates
	/// <see cref="NotableDateRule.Month" /> with the corresponding 1-based integer.
	/// </summary>
	[TestMethod]
	public void ParseXml_WhenFixedMonthIsSimpleHebrewMonth_ShouldReturnNumericMonth()
	{
		const string xml = @"
			<NotableDates xmlns=""urn:bodu:globalization:calendar"">
				<NotableDate name=""Rosh Hashanah"">
					<Rule name=""Rosh Hashanah Rule""
					      category=""Religious""
					      calendarType=""System.Globalization.HebrewCalendar"">
						<Fixed month=""Tishri"" day=""1"" />
					</Rule>
				</NotableDate>
			</NotableDates>";

		NotableDateRule rule = NotableDateRuleParser.ParseXml(xml).Single();

		Assert.AreEqual(1, rule.Month);
		Assert.IsNull(rule.CalendarMonthAlias);
	}

	/// <summary>
	/// Verifies that the simple Hebrew month <c>"AdarII"</c> resolves to integer 7 with no calendar alias.
	/// </summary>
	[TestMethod]
	public void ParseXml_WhenFixedMonthIsAdarII_ShouldReturnExpectedMonth()
	{
		const string xml = @"
			<NotableDates xmlns=""urn:bodu:globalization:calendar"">
				<NotableDate name=""Purim"">
					<Rule name=""Purim Rule""
					      category=""Religious""
					      calendarType=""System.Globalization.HebrewCalendar"">
						<Fixed month=""AdarII"" day=""14"" />
					</Rule>
				</NotableDate>
			</NotableDates>";

		NotableDateRule rule = NotableDateRuleParser.ParseXml(xml).Single();

		Assert.AreEqual(7, rule.Month);
		Assert.IsNull(rule.CalendarMonthAlias);
	}

	/// <summary>
	/// Verifies that a leap-year-dependent Hebrew month name (e.g. <c>"Nisan"</c>) populates
	/// <see cref="NotableDateRule.CalendarMonthAlias" /> and leaves <see cref="NotableDateRule.Month" /> unset.
	/// </summary>
	[TestMethod]
	public void ParseXml_WhenFixedMonthIsNisanAlias_ShouldPopulateCalendarMonthAlias()
	{
		const string xml = @"
			<NotableDates xmlns=""urn:bodu:globalization:calendar"">
				<NotableDate name=""Passover"">
					<Rule name=""Passover Rule""
					      category=""Religious""
					      calendarType=""System.Globalization.HebrewCalendar"">
						<Fixed month=""Nisan"" day=""15"" />
					</Rule>
				</NotableDate>
			</NotableDates>";

		NotableDateRule rule = NotableDateRuleParser.ParseXml(xml).Single();

		Assert.IsNull(rule.Month);
		Assert.AreEqual("Nisan", rule.CalendarMonthAlias);
	}

	/// <summary>
	/// Verifies that the leap-year-dependent Hebrew alias <c>"LastAdar"</c> populates
	/// <see cref="NotableDateRule.CalendarMonthAlias" />.
	/// </summary>
	[TestMethod]
	public void ParseXml_WhenFixedMonthIsLastAdar_ShouldPopulateCalendarMonthAlias()
	{
		const string xml = @"
			<NotableDates xmlns=""urn:bodu:globalization:calendar"">
				<NotableDate name=""Purim Katan"">
					<Rule name=""Purim Katan Rule""
					      category=""Religious""
					      calendarType=""System.Globalization.HebrewCalendar"">
						<Fixed month=""LastAdar"" day=""14"" />
					</Rule>
				</NotableDate>
			</NotableDates>";

		NotableDateRule rule = NotableDateRuleParser.ParseXml(xml).Single();

		Assert.IsNull(rule.Month);
		Assert.AreEqual("LastAdar", rule.CalendarMonthAlias);
	}

	/// <summary>
	/// Verifies that a DayOfWeekInMonth rule's <c>month</c> attribute is parsed as a Gregorian month name and
	/// populates <see cref="NotableDateRule.Month" />.
	/// </summary>
	[TestMethod]
	public void ParseXml_WhenDayOfWeekInMonthIsEnglishName_ShouldReturnNumericMonth()
	{
		const string xml = @"
			<NotableDates xmlns=""urn:bodu:globalization:calendar"">
				<NotableDate name=""Labour Day"">
					<Rule name=""Labour Day Rule"" category=""Holiday"">
						<DayOfWeekInMonth month=""September"" dayOfWeek=""Monday"" weekOrdinal=""First"" />
					</Rule>
				</NotableDate>
			</NotableDates>";

		NotableDateRule rule = NotableDateRuleParser.ParseXml(xml).Single();

		Assert.AreEqual(9, rule.Month);
	}

	/// <summary>
	/// Verifies that an Adjustment authored with <c>comparisonMonth</c> and <c>comparisonDay</c> attributes
	/// resolves to a synthetic <see cref="ObservanceAdjustment.ComparisonDate" /> using the year 2000 sentinel.
	/// </summary>
	[TestMethod]
	public void ParseXml_WhenAdjustmentHasComparisonMonthAndDay_ShouldPopulateComparisonDate()
	{
		const string xml = @"
			<NotableDates xmlns=""urn:bodu:globalization:calendar"">
				<NotableDate name=""Boxing Day"">
					<Rule name=""Boxing Day Rule"" category=""Holiday"">
						<Fixed month=""December"" day=""26"" />
						<Adjustment
							key=""after-christmas""
							when=""IfBeforeFixedDate""
							action=""MoveToNextWeekday""
							comparisonMonth=""December""
							comparisonDay=""25"" />
					</Rule>
				</NotableDate>
			</NotableDates>";

		NotableDateRule rule = NotableDateRuleParser.ParseXml(xml).Single();

		Assert.AreEqual(1, rule.Adjustments.Length);
		Assert.IsNotNull(rule.Adjustments[0].ComparisonDate);
		Assert.AreEqual(12, rule.Adjustments[0].ComparisonDate!.Value.Month);
		Assert.AreEqual(25, rule.Adjustments[0].ComparisonDate!.Value.Day);
	}

	/// <summary>
	/// Verifies that an Adjustment authored without <c>comparisonMonth</c> and <c>comparisonDay</c> attributes
	/// leaves <see cref="ObservanceAdjustment.ComparisonDate" /> at <see langword="null" />.
	/// </summary>
	[TestMethod]
	public void ParseXml_WhenAdjustmentOmitsComparisonMonthAndDay_ShouldLeaveComparisonDateNull()
	{
		const string xml = @"
			<NotableDates xmlns=""urn:bodu:globalization:calendar"">
				<NotableDate name=""Simple"">
					<Rule name=""Simple Rule"" category=""Holiday"">
						<Fixed month=""December"" day=""26"" />
						<Adjustment key=""simple-roll"" when=""IfWeekend"" action=""MoveToNextWeekday"" />
					</Rule>
				</NotableDate>
			</NotableDates>";

		NotableDateRule rule = NotableDateRuleParser.ParseXml(xml).Single();

		Assert.AreEqual(1, rule.Adjustments.Length);
		Assert.IsNull(rule.Adjustments[0].ComparisonDate);
	}
}
