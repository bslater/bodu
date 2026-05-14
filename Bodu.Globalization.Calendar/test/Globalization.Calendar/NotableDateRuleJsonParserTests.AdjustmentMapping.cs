// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleJsonParserTests.AdjustmentMapping.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Linq;
using System.Text.Json;
using Bodu.Extensions;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies how the JSON parser maps individual adjustments — required-enum validation for <c>when</c> /
/// <c>action</c>, missing <c>key</c>, and the full scalar surface (territory, calendar type, year window,
/// comparison date, target rule name, handler key, priority).
/// </summary>
public partial class NotableDateRuleJsonParserTests
{
    /// <summary>
    /// Verifies that an adjustment whose <c>key</c> is missing is rejected by schema validation as
    /// <see cref="JsonException" /> via the <c>adjustment.required</c> clause.
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenAdjustmentKeyIsMissing_ShouldThrowJsonException()
    {
        const string json = @"{
			""notableDates"": [
				{ ""name"": ""Holiday"", ""rules"": [ {
					""name"": ""Holiday Rule"",
					""category"": ""Holiday"",
					""fixed"": { ""month"": ""January"", ""day"": 1 },
					""adjustments"": [
						{ ""when"": ""IfWeekend"", ""action"": ""MoveToNextWeekday"" }
					]
				} ] }
			]
		}";

        Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = NotableDateRuleJsonParser.ParseJson(json);
        });
    }

    /// <summary>
    /// Verifies that an adjustment whose <c>when</c> is missing is rejected by schema validation as
    /// <see cref="JsonException" /> via the <c>adjustment.required</c> clause.
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenAdjustmentTriggerIsMissing_ShouldThrowJsonException()
    {
        const string json = @"{
			""notableDates"": [
				{ ""name"": ""Holiday"", ""rules"": [ {
					""name"": ""Holiday Rule"",
					""category"": ""Holiday"",
					""fixed"": { ""month"": ""January"", ""day"": 1 },
					""adjustments"": [
						{ ""key"": ""roll"", ""action"": ""MoveToNextWeekday"" }
					]
				} ] }
			]
		}";

        Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = NotableDateRuleJsonParser.ParseJson(json);
        });
    }

    /// <summary>
    /// Verifies that an adjustment whose <c>when</c> is an unrecognised enum value is rejected by schema
    /// validation as <see cref="JsonException" /> via the <c>adjustmentTrigger</c> enum constraint.
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenAdjustmentTriggerIsUnknown_ShouldThrowJsonException()
    {
        const string json = @"{
			""notableDates"": [
				{ ""name"": ""Holiday"", ""rules"": [ {
					""name"": ""Holiday Rule"",
					""category"": ""Holiday"",
					""fixed"": { ""month"": ""January"", ""day"": 1 },
					""adjustments"": [
						{ ""key"": ""roll"", ""when"": ""IfThirdSundayInThirdQuarter"", ""action"": ""MoveToNextWeekday"" }
					]
				} ] }
			]
		}";

        Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = NotableDateRuleJsonParser.ParseJson(json);
        });
    }

    /// <summary>
    /// Verifies that an adjustment whose <c>action</c> is missing is rejected by schema validation as
    /// <see cref="JsonException" /> via the <c>adjustment.required</c> clause.
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenAdjustmentActionIsMissing_ShouldThrowJsonException()
    {
        const string json = @"{
			""notableDates"": [
				{ ""name"": ""Holiday"", ""rules"": [ {
					""name"": ""Holiday Rule"",
					""category"": ""Holiday"",
					""fixed"": { ""month"": ""January"", ""day"": 1 },
					""adjustments"": [
						{ ""key"": ""roll"", ""when"": ""IfWeekend"" }
					]
				} ] }
			]
		}";

        Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = NotableDateRuleJsonParser.ParseJson(json);
        });
    }

    /// <summary>
    /// Verifies that an adjustment whose <c>action</c> is an unrecognised enum value is rejected by schema
    /// validation as <see cref="JsonException" /> via the <c>adjustmentAction</c> enum constraint.
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenAdjustmentActionIsUnknown_ShouldThrowJsonException()
    {
        const string json = @"{
			""notableDates"": [
				{ ""name"": ""Holiday"", ""rules"": [ {
					""name"": ""Holiday Rule"",
					""category"": ""Holiday"",
					""fixed"": { ""month"": ""January"", ""day"": 1 },
					""adjustments"": [
						{ ""key"": ""roll"", ""when"": ""IfWeekend"", ""action"": ""TeleportThroughTime"" }
					]
				} ] }
			]
		}";

        Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = NotableDateRuleJsonParser.ParseJson(json);
        });
    }

    /// <summary>
    /// Verifies that an adjustment with the full scalar surface populates every corresponding field on the resulting
    /// <see cref="ObservanceAdjustment" />.
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenAdjustmentSuppliesFullScalarSurface_ShouldMapAllFields()
    {
        const string json = @"{
			""notableDates"": [
				{ ""name"": ""Public Holiday"", ""rules"": [ {
					""name"": ""Public Holiday Rule"",
					""category"": ""Holiday"",
					""fixed"": { ""month"": ""December"", ""day"": 26 },
					""adjustments"": [ {
						""key"": ""bk-day-roll"",
						""when"": ""IfNthOccurrenceInMonth"",
						""action"": ""AddDays"",
						""dayOfWeek"": ""Monday"",
						""weekOrdinal"": ""Second"",
						""days"": 2,
						""priority"": 10,
						""nonWorking"": true,
						""territory"": ""AU"",
						""calendarType"": ""System.Globalization.GregorianCalendar"",
						""fromYear"": 2000,
						""toYear"": 2099,
						""comparisonMonth"": ""December"",
						""comparisonDay"": 25,
						""target"": ""Christmas Day"",
						""handlerKey"": ""bank-holiday""
					} ]
				} ] }
			]
		}";

        NotableDateRule rule = NotableDateRuleJsonParser.ParseJson(json).Single();
        ObservanceAdjustment adjustment = rule.Adjustments.Single();

        Assert.AreEqual("bk-day-roll", adjustment.Key);
        Assert.AreEqual(AdjustmentTrigger.IfNthOccurrenceInMonth, adjustment.Trigger);
        Assert.AreEqual(AdjustmentAction.AddDays, adjustment.Action);
        Assert.AreEqual(DayOfWeek.Monday, adjustment.DayOfWeek);
        Assert.AreEqual(WeekOfMonthOrdinal.Second, adjustment.WeekOrdinal);
        Assert.AreEqual(2, adjustment.OffsetDays);
        Assert.AreEqual(10, adjustment.Priority);
        Assert.AreEqual(true, adjustment.IsNonWorkingDay);
        Assert.AreEqual("AU", adjustment.TerritoryCode);
        Assert.AreEqual(typeof(System.Globalization.GregorianCalendar), adjustment.CalendarType);
        Assert.AreEqual(2000, adjustment.EffectiveFromYear);
        Assert.AreEqual(2099, adjustment.EffectiveToYear);
        Assert.AreEqual(12, adjustment.ComparisonDate!.Value.Month);
        Assert.AreEqual(25, adjustment.ComparisonDate.Value.Day);
        Assert.AreEqual("Christmas Day", adjustment.TargetRuleName);
        Assert.AreEqual("bank-holiday", adjustment.HandlerKey);
    }

    /// <summary>
    /// Verifies that an adjustment omitting optional scalars yields the documented defaults — <c>days</c> defaults to 0,
    /// <c>priority</c> defaults to 100, and the remaining nullable fields stay <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenAdjustmentOmitsOptionalScalars_ShouldUseDocumentedDefaults()
    {
        const string json = @"{
			""notableDates"": [
				{ ""name"": ""Holiday"", ""rules"": [ {
					""name"": ""Holiday Rule"",
					""category"": ""Holiday"",
					""fixed"": { ""month"": ""January"", ""day"": 1 },
					""adjustments"": [
						{ ""key"": ""roll"", ""when"": ""IfWeekend"", ""action"": ""MoveToNextWeekday"" }
					]
				} ] }
			]
		}";

        NotableDateRule rule = NotableDateRuleJsonParser.ParseJson(json).Single();
        ObservanceAdjustment adjustment = rule.Adjustments.Single();

        Assert.AreEqual(0, adjustment.OffsetDays);
        Assert.AreEqual(100, adjustment.Priority);
        Assert.IsNull(adjustment.DayOfWeek);
        Assert.IsNull(adjustment.WeekOrdinal);
        Assert.IsNull(adjustment.IsNonWorkingDay);
        Assert.IsNull(adjustment.TerritoryCode);
        Assert.IsNull(adjustment.CalendarType);
        Assert.IsNull(adjustment.EffectiveFromYear);
        Assert.IsNull(adjustment.EffectiveToYear);
        Assert.IsNull(adjustment.ComparisonDate);
        Assert.IsNull(adjustment.TargetRuleName);
        Assert.IsNull(adjustment.HandlerKey);
    }
}
