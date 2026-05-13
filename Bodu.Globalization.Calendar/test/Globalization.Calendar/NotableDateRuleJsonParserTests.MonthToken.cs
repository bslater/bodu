// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleJsonParserTests.MonthToken.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Linq;

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
	/// Verifies that a Fixed rule authored with a numeric month token outside the supported 1..13 range surfaces as
	/// a <see cref="FormatException" />.
	/// </summary>
	[TestMethod]
	public void ParseJson_WhenFixedMonthIsOutOfRangeNumeric_ShouldThrowFormatException()
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

		Assert.ThrowsExactly<FormatException>(() =>
		{
			_ = NotableDateRuleJsonParser.ParseJson(json);
		});
	}

	/// <summary>
	/// Verifies that a Fixed rule authored with the zero numeric month value is rejected with a
	/// <see cref="FormatException" />.
	/// </summary>
	[TestMethod]
	public void ParseJson_WhenFixedMonthIsZeroNumeric_ShouldThrowFormatException()
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

		Assert.ThrowsExactly<FormatException>(() =>
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
	/// Verifies that a DayOfWeekInMonth rule authored with a numeric month token resolves to the corresponding integer.
	/// </summary>
	[TestMethod]
	public void ParseJson_WhenDayOfWeekInMonthIsNumericMonth_ShouldReturnNumericMonth()
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

		NotableDateRule rule = NotableDateRuleJsonParser.ParseJson(json).Single();

		Assert.AreEqual(10, rule.Month);
	}

	/// <summary>
	/// Verifies that a DayOfWeekInMonth rule authored with a numeric month outside 1..13 throws
	/// <see cref="FormatException" />.
	/// </summary>
	[TestMethod]
	public void ParseJson_WhenDayOfWeekInMonthIsOutOfRangeMonth_ShouldThrowFormatException()
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

		Assert.ThrowsExactly<FormatException>(() =>
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
}
