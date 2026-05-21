// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleJsonParserTests.EnumSurface.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Extensions;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies that <see cref="NotableDateRuleJsonParser" /> resolves every supported value of each enumeration that
/// participates in the JSON authoring vocabulary — <see cref="NotableDateCategory" />, <see cref="AdjustmentTrigger" />,
/// <see cref="AdjustmentAction" />, <see cref="System.DayOfWeek" />, and <see cref="WeekOfMonthOrdinal" />. The
/// data-driven rows exercise <c>ParseRequiredEnum</c> and <c>ParseOptionalEnum</c> across the full enum surface,
/// catching drift between the schema's enum list and the parser's <c>Enum.TryParse</c> mapping.
/// </summary>
public partial class NotableDateRuleJsonParserTests
{
    /// <summary>
    /// Verifies that each supported <see cref="NotableDateCategory" /> string value on a rule resolves to the
    /// matching enum member through <c>ParseOptionalEnum&lt;NotableDateCategory&gt;</c>.
    /// </summary>
    [TestCategory("Regression")]
    [DataRow("None", NotableDateCategory.None)]
    [DataRow("Holiday", NotableDateCategory.Holiday)]
    [DataRow("Observance", NotableDateCategory.Observance)]
    [DataRow("Remembrance", NotableDateCategory.Remembrance)]
    [DataRow("Cultural", NotableDateCategory.Cultural)]
    [DataRow("Religious", NotableDateCategory.Religious)]
    [DataRow("Seasonal", NotableDateCategory.Seasonal)]
    [DataRow("Civic", NotableDateCategory.Civic)]
    [DataRow("Bank", NotableDateCategory.Bank)]
    [DataRow("School", NotableDateCategory.School)]
    [DataRow("Regional", NotableDateCategory.Regional)]
    [DataRow("Other", NotableDateCategory.Other)]
    [TestMethod]
    public void ParseJson_WhenCategoryUsesSupportedValue_ShouldReturnExpectedCategory(string categoryToken, NotableDateCategory expected)
    {
        var json = $@"{{
			""notableDates"": [
				{{ ""name"": ""CategoryTest"", ""rules"": [ {{
					""name"": ""CategoryTestRule"",
					""category"": ""{categoryToken}"",
					""fixed"": {{ ""month"": ""January"", ""day"": 1 }}
				}} ] }}
			]
		}}";

        NotableDateRule rule = NotableDateRuleJsonParser.ParseJson(json).Single();

        Assert.AreEqual(expected, rule.Category);
    }

    /// <summary>
    /// Verifies that each supported <see cref="AdjustmentTrigger" /> string value on an adjustment resolves to the
    /// matching enum member through <c>ParseRequiredEnum&lt;AdjustmentTrigger&gt;</c>.
    /// </summary>
    [TestCategory("Regression")]
    [DataRow("Always", AdjustmentTrigger.Always)]
    [DataRow("IfDayOfWeek", AdjustmentTrigger.IfDayOfWeek)]
    [DataRow("IfWeekend", AdjustmentTrigger.IfWeekend)]
    [DataRow("IfWeekday", AdjustmentTrigger.IfWeekday)]
    [DataRow("IfNonWorkingDay", AdjustmentTrigger.IfNonWorkingDay)]
    [DataRow("IfBeforeFixedDate", AdjustmentTrigger.IfBeforeFixedDate)]
    [DataRow("IfAfterFixedDate", AdjustmentTrigger.IfAfterFixedDate)]
    [DataRow("IfLeapYear", AdjustmentTrigger.IfLeapYear)]
    [DataRow("IfNthOccurrenceInMonth", AdjustmentTrigger.IfNthOccurrenceInMonth)]
    [DataRow("Custom", AdjustmentTrigger.Custom)]
    [TestMethod]
    public void ParseJson_WhenAdjustmentTriggerUsesSupportedValue_ShouldReturnExpectedTrigger(string triggerToken, AdjustmentTrigger expected)
    {
        var json = $@"{{
			""notableDates"": [
				{{ ""name"": ""TriggerTest"", ""rules"": [ {{
					""name"": ""TriggerTestRule"",
					""category"": ""Holiday"",
					""fixed"": {{ ""month"": ""January"", ""day"": 1 }},
					""adjustments"": [
						{{ ""key"": ""k"", ""when"": ""{triggerToken}"", ""action"": ""None"" }}
					]
				}} ] }}
			]
		}}";

        NotableDateRule rule = NotableDateRuleJsonParser.ParseJson(json).Single();

        Assert.AreEqual(1, rule.Adjustments.Length);
        Assert.AreEqual(expected, rule.Adjustments[0].Trigger);
    }

    /// <summary>
    /// Verifies that each supported <see cref="AdjustmentAction" /> string value on an adjustment resolves to the
    /// matching enum member through <c>ParseRequiredEnum&lt;AdjustmentAction&gt;</c>.
    /// </summary>
    [TestCategory("Regression")]
    [DataRow("None", AdjustmentAction.None)]
    [DataRow("AddDays", AdjustmentAction.AddDays)]
    [DataRow("MoveToNextWeekday", AdjustmentAction.MoveToNextWeekday)]
    [DataRow("MoveToPreviousWeekday", AdjustmentAction.MoveToPreviousWeekday)]
    [DataRow("MoveToNextNonWorkingDay", AdjustmentAction.MoveToNextNonWorkingDay)]
    [DataRow("ReplaceWithNamedDate", AdjustmentAction.ReplaceWithNamedDate)]
    [DataRow("Custom", AdjustmentAction.Custom)]
    [TestMethod]
    public void ParseJson_WhenAdjustmentActionUsesSupportedValue_ShouldReturnExpectedAction(string actionToken, AdjustmentAction expected)
    {
        var json = $@"{{
			""notableDates"": [
				{{ ""name"": ""ActionTest"", ""rules"": [ {{
					""name"": ""ActionTestRule"",
					""category"": ""Holiday"",
					""fixed"": {{ ""month"": ""January"", ""day"": 1 }},
					""adjustments"": [
						{{ ""key"": ""k"", ""when"": ""Always"", ""action"": ""{actionToken}"" }}
					]
				}} ] }}
			]
		}}";

        NotableDateRule rule = NotableDateRuleJsonParser.ParseJson(json).Single();

        Assert.AreEqual(1, rule.Adjustments.Length);
        Assert.AreEqual(expected, rule.Adjustments[0].Action);
    }

    /// <summary>
    /// Verifies that each supported weekday string value on a <c>dayOfWeekInMonth</c> strategy resolves to the
    /// matching <see cref="System.DayOfWeek" /> enum member.
    /// </summary>
    [TestCategory("Regression")]
    [DataRow("Monday", DayOfWeek.Monday)]
    [DataRow("Tuesday", DayOfWeek.Tuesday)]
    [DataRow("Wednesday", DayOfWeek.Wednesday)]
    [DataRow("Thursday", DayOfWeek.Thursday)]
    [DataRow("Friday", DayOfWeek.Friday)]
    [DataRow("Saturday", DayOfWeek.Saturday)]
    [DataRow("Sunday", DayOfWeek.Sunday)]
    [TestMethod]
    public void ParseJson_WhenDayOfWeekInMonthUsesSupportedDay_ShouldReturnExpectedDayOfWeek(string dayToken, DayOfWeek expected)
    {
        var json = $@"{{
			""notableDates"": [
				{{ ""name"": ""WeekdayTest"", ""rules"": [ {{
					""name"": ""WeekdayTestRule"",
					""category"": ""Observance"",
					""dayOfWeekInMonth"": {{ ""month"": ""May"", ""dayOfWeek"": ""{dayToken}"", ""weekOrdinal"": ""First"" }}
				}} ] }}
			]
		}}";

        NotableDateRule rule = NotableDateRuleJsonParser.ParseJson(json).Single();

        Assert.AreEqual(expected, rule.DayOfWeek);
    }

    /// <summary>
    /// Verifies that each supported ordinal string value on a <c>dayOfWeekInMonth</c> strategy resolves to the
    /// matching <see cref="WeekOfMonthOrdinal" /> enum member.
    /// </summary>
    [TestCategory("Regression")]
    [DataRow("First", WeekOfMonthOrdinal.First)]
    [DataRow("Second", WeekOfMonthOrdinal.Second)]
    [DataRow("Third", WeekOfMonthOrdinal.Third)]
    [DataRow("Fourth", WeekOfMonthOrdinal.Fourth)]
    [DataRow("Fifth", WeekOfMonthOrdinal.Fifth)]
    [DataRow("Last", WeekOfMonthOrdinal.Last)]
    [TestMethod]
    public void ParseJson_WhenDayOfWeekInMonthUsesSupportedOrdinal_ShouldReturnExpectedOrdinal(string ordinalToken, WeekOfMonthOrdinal expected)
    {
        var json = $@"{{
			""notableDates"": [
				{{ ""name"": ""OrdinalTest"", ""rules"": [ {{
					""name"": ""OrdinalTestRule"",
					""category"": ""Observance"",
					""dayOfWeekInMonth"": {{ ""month"": ""May"", ""dayOfWeek"": ""Monday"", ""weekOrdinal"": ""{ordinalToken}"" }}
				}} ] }}
			]
		}}";

        NotableDateRule rule = NotableDateRuleJsonParser.ParseJson(json).Single();

        Assert.AreEqual(expected, rule.WeekOrdinal);
    }

    /// <summary>
    /// Verifies that omitting an optional enum-backed field on an adjustment (e.g. <c>dayOfWeek</c>, <c>weekOrdinal</c>)
    /// leaves the corresponding nullable enum at <see langword="null" /> rather than defaulting to the underlying
    /// <c>0</c> member.
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenAdjustmentOptionalEnumsAreOmitted_ShouldLeaveOptionalEnumsNull()
    {
        const string json = @"{
			""notableDates"": [
				{ ""name"": ""OptEnumTest"", ""rules"": [ {
					""name"": ""OptEnumTestRule"",
					""category"": ""Holiday"",
					""fixed"": { ""month"": ""January"", ""day"": 1 },
					""adjustments"": [
						{ ""key"": ""k"", ""when"": ""IfWeekend"", ""action"": ""MoveToNextWeekday"" }
					]
				} ] }
			]
		}";

        NotableDateRule rule = NotableDateRuleJsonParser.ParseJson(json).Single();
        ObservanceAdjustment adjustment = rule.Adjustments.Single();

        Assert.IsNull(adjustment.DayOfWeek);
        Assert.IsNull(adjustment.WeekOrdinal);
    }
}
