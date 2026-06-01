// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleParserTests.Adjustments.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;
using Bodu.Extensions;

namespace Bodu.Globalization.Calendar;

public partial class NotableDateRuleParserTests
{
    /// <summary>
    /// Returns the display name for a data-driven adjustment test case by reading the first element of the data row.
    /// </summary>
    public static string GetAdjustmentTestDisplayName(MethodInfo _, object[] data) =>
        (string)data[0];

    /// <summary>
    /// Builds a minimal schema-valid <c>&lt;NotableDates&gt;</c> XML document containing a single Fixed rule with
    /// one <c>&lt;Adjustment&gt;</c> element whose attributes are supplied by <paramref name="adjustmentAttributes" />.
    /// </summary>
    private static string BuildAdjustmentXml(string adjustmentAttributes) =>
        $@"<NotableDates xmlns=""urn:bodu:globalization:calendar"">
				<NotableDate name=""Test"">
					<Rule name=""Test Rule"" category=""Holiday"">
						<Fixed month=""January"" day=""1"" />
						<Adjustment {adjustmentAttributes} />
					</Rule>
				</NotableDate>
			</NotableDates>";

    // ── Group 1: Trigger enum mapping ─────────────────────────────────────────

    /// <summary>
    /// Gets the test cases that pair a minimal XML fragment with the <see cref="AdjustmentTrigger" /> value it encodes
    /// via the <c>when</c> attribute.
    /// </summary>
    public static IEnumerable<object[]> AdjustmentTriggerCases =>
    [
        [
            "Always trigger maps to Always",
            BuildAdjustmentXml(@"key=""t"" when=""Always"" action=""None"""),
            AdjustmentTrigger.Always,
        ],
        [
            "IfWeekend trigger maps to IfWeekend",
            BuildAdjustmentXml(@"key=""t"" when=""IfWeekend"" action=""None"""),
            AdjustmentTrigger.IfWeekend,
        ],
        [
            "IfWeekday trigger maps to IfWeekday",
            BuildAdjustmentXml(@"key=""t"" when=""IfWeekday"" action=""None"""),
            AdjustmentTrigger.IfWeekday,
        ],
        [
            "IfLeapYear trigger maps to IfLeapYear",
            BuildAdjustmentXml(@"key=""t"" when=""IfLeapYear"" action=""None"""),
            AdjustmentTrigger.IfLeapYear,
        ],
        [
            "IfNonWorkingDay trigger maps to IfNonWorkingDay",
            BuildAdjustmentXml(@"key=""t"" when=""IfNonWorkingDay"" action=""None"""),
            AdjustmentTrigger.IfNonWorkingDay,
        ],
        [
            "IfDayOfWeek trigger maps to IfDayOfWeek",
            BuildAdjustmentXml(@"key=""t"" when=""IfDayOfWeek"" action=""None"" dayOfWeek=""Monday"""),
            AdjustmentTrigger.IfDayOfWeek,
        ],
        [
            "IfBeforeFixedDate trigger maps to IfBeforeFixedDate",
            BuildAdjustmentXml(@"key=""t"" when=""IfBeforeFixedDate"" action=""None"" comparisonMonth=""January"" comparisonDay=""1"""),
            AdjustmentTrigger.IfBeforeFixedDate,
        ],
        [
            "IfAfterFixedDate trigger maps to IfAfterFixedDate",
            BuildAdjustmentXml(@"key=""t"" when=""IfAfterFixedDate"" action=""None"" comparisonMonth=""January"" comparisonDay=""1"""),
            AdjustmentTrigger.IfAfterFixedDate,
        ],
        [
            "IfNthOccurrenceInMonth trigger maps to IfNthOccurrenceInMonth",
            BuildAdjustmentXml(@"key=""t"" when=""IfNthOccurrenceInMonth"" action=""None"" dayOfWeek=""Monday"" weekOrdinal=""First"""),
            AdjustmentTrigger.IfNthOccurrenceInMonth,
        ],
        [
            "Custom trigger maps to Custom",
            BuildAdjustmentXml(@"key=""t"" when=""Custom"" action=""None"" handlerKey=""my-handler"""),
            AdjustmentTrigger.Custom,
        ],
    ];

    /// <summary>
    /// Verifies that every <see cref="AdjustmentTrigger" /> value is correctly parsed from the <c>when</c> attribute
    /// of an <c>&lt;Adjustment&gt;</c> XML element, using the XML fragment supplied by each test case.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(AdjustmentTriggerCases), DynamicDataDisplayName = nameof(GetAdjustmentTestDisplayName))]
    public void ParseXml_WhenAdjustmentTriggerProvided_ShouldMapTrigger(
        string displayName,
        string xmlFragment,
        AdjustmentTrigger expectedTrigger)
    {
        NotableDateRule rule = NotableDateRuleParser.ParseXml(xmlFragment).Single();

        Assert.AreEqual(expectedTrigger, rule.Adjustments.Single().Trigger);
    }

    // ── Group 2: Action enum mapping ──────────────────────────────────────────

    /// <summary>
    /// Gets the test cases that pair a minimal XML fragment with the <see cref="AdjustmentAction" /> value it encodes
    /// via the <c>action</c> attribute.
    /// </summary>
    public static IEnumerable<object[]> AdjustmentActionCases =>
    [
        [
            "None action maps to None",
            BuildAdjustmentXml(@"key=""t"" when=""Always"" action=""None"""),
            AdjustmentAction.None,
        ],
        [
            "AddDays action maps to AddDays",
            BuildAdjustmentXml(@"key=""t"" when=""Always"" action=""AddDays"" days=""7"""),
            AdjustmentAction.AddDays,
        ],
        [
            "MoveToNextWeekday action maps to MoveToNextWeekday",
            BuildAdjustmentXml(@"key=""t"" when=""Always"" action=""MoveToNextWeekday"""),
            AdjustmentAction.MoveToNextWeekday,
        ],
        [
            "MoveToPreviousWeekday action maps to MoveToPreviousWeekday",
            BuildAdjustmentXml(@"key=""t"" when=""Always"" action=""MoveToPreviousWeekday"""),
            AdjustmentAction.MoveToPreviousWeekday,
        ],
        [
            "MoveToNextNonWorkingDay action maps to MoveToNextNonWorkingDay",
            BuildAdjustmentXml(@"key=""t"" when=""Always"" action=""MoveToNextNonWorkingDay"""),
            AdjustmentAction.MoveToNextNonWorkingDay,
        ],
        [
            "ReplaceWithNamedDate action maps to ReplaceWithNamedDate",
            BuildAdjustmentXml(@"key=""t"" when=""Always"" action=""ReplaceWithNamedDate"" target=""Easter Sunday"""),
            AdjustmentAction.ReplaceWithNamedDate,
        ],
        [
            "Custom action maps to Custom",
            BuildAdjustmentXml(@"key=""t"" when=""Always"" action=""Custom"" handlerKey=""my-handler"""),
            AdjustmentAction.Custom,
        ],
    ];

    /// <summary>
    /// Verifies that every <see cref="AdjustmentAction" /> value is correctly parsed from the <c>action</c> attribute
    /// of an <c>&lt;Adjustment&gt;</c> XML element, using the XML fragment supplied by each test case.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(AdjustmentActionCases), DynamicDataDisplayName = nameof(GetAdjustmentTestDisplayName))]
    public void ParseXml_WhenAdjustmentActionProvided_ShouldMapAction(
        string displayName,
        string xmlFragment,
        AdjustmentAction expectedAction)
    {
        NotableDateRule rule = NotableDateRuleParser.ParseXml(xmlFragment).Single();

        Assert.AreEqual(expectedAction, rule.Adjustments.Single().Action);
    }

    // ── Group 3a: DayOfWeek attribute ─────────────────────────────────────────

    /// <summary>
    /// Gets the test cases that pair a minimal XML fragment with the <see cref="DayOfWeek" /> value it encodes via
    /// the <c>dayOfWeek</c> attribute.
    /// </summary>
    public static IEnumerable<object[]> AdjustmentDayOfWeekCases =>
    [
        [
            "dayOfWeek Monday maps to DayOfWeek.Monday",
            BuildAdjustmentXml(@"key=""t"" when=""IfDayOfWeek"" action=""None"" dayOfWeek=""Monday"""),
            DayOfWeek.Monday,
        ],
        [
            "dayOfWeek Tuesday maps to DayOfWeek.Tuesday",
            BuildAdjustmentXml(@"key=""t"" when=""IfDayOfWeek"" action=""None"" dayOfWeek=""Tuesday"""),
            DayOfWeek.Tuesday,
        ],
        [
            "dayOfWeek Wednesday maps to DayOfWeek.Wednesday",
            BuildAdjustmentXml(@"key=""t"" when=""IfDayOfWeek"" action=""None"" dayOfWeek=""Wednesday"""),
            DayOfWeek.Wednesday,
        ],
        [
            "dayOfWeek Thursday maps to DayOfWeek.Thursday",
            BuildAdjustmentXml(@"key=""t"" when=""IfDayOfWeek"" action=""None"" dayOfWeek=""Thursday"""),
            DayOfWeek.Thursday,
        ],
        [
            "dayOfWeek Friday maps to DayOfWeek.Friday",
            BuildAdjustmentXml(@"key=""t"" when=""IfDayOfWeek"" action=""None"" dayOfWeek=""Friday"""),
            DayOfWeek.Friday,
        ],
        [
            "dayOfWeek Saturday maps to DayOfWeek.Saturday",
            BuildAdjustmentXml(@"key=""t"" when=""IfDayOfWeek"" action=""None"" dayOfWeek=""Saturday"""),
            DayOfWeek.Saturday,
        ],
        [
            "dayOfWeek Sunday maps to DayOfWeek.Sunday",
            BuildAdjustmentXml(@"key=""t"" when=""IfDayOfWeek"" action=""None"" dayOfWeek=""Sunday"""),
            DayOfWeek.Sunday,
        ],
    ];

    /// <summary>
    /// Verifies that every <see cref="DayOfWeek" /> value is correctly parsed from the <c>dayOfWeek</c> attribute of
    /// an <c>&lt;Adjustment&gt;</c> XML element, using the XML fragment supplied by each test case.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(AdjustmentDayOfWeekCases), DynamicDataDisplayName = nameof(GetAdjustmentTestDisplayName))]
    public void ParseXml_WhenIfDayOfWeekTriggerProvided_ShouldMapDayOfWeek(
        string displayName,
        string xmlFragment,
        DayOfWeek expectedDayOfWeek)
    {
        NotableDateRule rule = NotableDateRuleParser.ParseXml(xmlFragment).Single();

        Assert.AreEqual(expectedDayOfWeek, rule.Adjustments.Single().DayOfWeek);
    }

    // ── Group 3b: WeekOrdinal attribute ───────────────────────────────────────

    /// <summary>
    /// Gets the test cases that pair a minimal XML fragment with the <see cref="WeekOfMonthOrdinal" /> value it
    /// encodes via the <c>weekOrdinal</c> attribute.
    /// </summary>
    public static IEnumerable<object[]> AdjustmentWeekOrdinalCases =>
    [
        [
            "weekOrdinal First maps to WeekOfMonthOrdinal.First",
            BuildAdjustmentXml(@"key=""t"" when=""IfNthOccurrenceInMonth"" action=""None"" dayOfWeek=""Monday"" weekOrdinal=""First"""),
            WeekOfMonthOrdinal.First,
        ],
        [
            "weekOrdinal Second maps to WeekOfMonthOrdinal.Second",
            BuildAdjustmentXml(@"key=""t"" when=""IfNthOccurrenceInMonth"" action=""None"" dayOfWeek=""Monday"" weekOrdinal=""Second"""),
            WeekOfMonthOrdinal.Second,
        ],
        [
            "weekOrdinal Third maps to WeekOfMonthOrdinal.Third",
            BuildAdjustmentXml(@"key=""t"" when=""IfNthOccurrenceInMonth"" action=""None"" dayOfWeek=""Monday"" weekOrdinal=""Third"""),
            WeekOfMonthOrdinal.Third,
        ],
        [
            "weekOrdinal Fourth maps to WeekOfMonthOrdinal.Fourth",
            BuildAdjustmentXml(@"key=""t"" when=""IfNthOccurrenceInMonth"" action=""None"" dayOfWeek=""Monday"" weekOrdinal=""Fourth"""),
            WeekOfMonthOrdinal.Fourth,
        ],
        [
            "weekOrdinal Fifth maps to WeekOfMonthOrdinal.Fifth",
            BuildAdjustmentXml(@"key=""t"" when=""IfNthOccurrenceInMonth"" action=""None"" dayOfWeek=""Monday"" weekOrdinal=""Fifth"""),
            WeekOfMonthOrdinal.Fifth,
        ],
        [
            "weekOrdinal Last maps to WeekOfMonthOrdinal.Last",
            BuildAdjustmentXml(@"key=""t"" when=""IfNthOccurrenceInMonth"" action=""None"" dayOfWeek=""Monday"" weekOrdinal=""Last"""),
            WeekOfMonthOrdinal.Last,
        ],
    ];

    /// <summary>
    /// Verifies that every <see cref="WeekOfMonthOrdinal" /> value is correctly parsed from the <c>weekOrdinal</c>
    /// attribute of an <c>&lt;Adjustment&gt;</c> XML element, using the XML fragment supplied by each test case.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(AdjustmentWeekOrdinalCases), DynamicDataDisplayName = nameof(GetAdjustmentTestDisplayName))]
    public void ParseXml_WhenIfNthOccurrenceInMonthTriggerProvided_ShouldMapWeekOrdinal(
        string displayName,
        string xmlFragment,
        WeekOfMonthOrdinal expectedOrdinal)
    {
        NotableDateRule rule = NotableDateRuleParser.ParseXml(xmlFragment).Single();

        Assert.AreEqual(expectedOrdinal, rule.Adjustments.Single().WeekOrdinal);
    }

    // ── Group 3c: ComparisonDate attributes ───────────────────────────────────

    /// <summary>
    /// Gets the test cases that pair a minimal XML fragment with the expected comparison month and day encoded via
    /// the <c>comparisonMonth</c> and <c>comparisonDay</c> attributes.
    /// </summary>
    public static IEnumerable<object[]> AdjustmentComparisonDateCases =>
    [
        [
            "IfBeforeFixedDate with January 1 maps ComparisonDate",
            BuildAdjustmentXml(@"key=""t"" when=""IfBeforeFixedDate"" action=""None"" comparisonMonth=""January"" comparisonDay=""1"""),
            1,
            1,
        ],
        [
            "IfBeforeFixedDate with June 15 maps ComparisonDate",
            BuildAdjustmentXml(@"key=""t"" when=""IfBeforeFixedDate"" action=""None"" comparisonMonth=""June"" comparisonDay=""15"""),
            6,
            15,
        ],
        [
            "IfAfterFixedDate with March 31 maps ComparisonDate",
            BuildAdjustmentXml(@"key=""t"" when=""IfAfterFixedDate"" action=""None"" comparisonMonth=""March"" comparisonDay=""31"""),
            3,
            31,
        ],
        [
            "IfAfterFixedDate with December 25 maps ComparisonDate",
            BuildAdjustmentXml(@"key=""t"" when=""IfAfterFixedDate"" action=""None"" comparisonMonth=""December"" comparisonDay=""25"""),
            12,
            25,
        ],
    ];

    /// <summary>
    /// Verifies that the <c>comparisonMonth</c> and <c>comparisonDay</c> attributes of an <c>&lt;Adjustment&gt;</c>
    /// XML element are correctly parsed into <see cref="ObservanceAdjustment.ComparisonDate" />, using the XML
    /// fragment supplied by each test case.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(AdjustmentComparisonDateCases), DynamicDataDisplayName = nameof(GetAdjustmentTestDisplayName))]
    public void ParseXml_WhenComparisonDateAttributesProvided_ShouldMapComparisonDate(
        string displayName,
        string xmlFragment,
        int expectedMonth,
        int expectedDay)
    {
        NotableDateRule rule = NotableDateRuleParser.ParseXml(xmlFragment).Single();
        DateTime? comparisonDate = rule.Adjustments.Single().ComparisonDate;

        Assert.IsNotNull(comparisonDate);
        Assert.AreEqual(expectedMonth, comparisonDate.Value.Month);
        Assert.AreEqual(expectedDay, comparisonDate.Value.Day);
    }

    // ── Group 4a: OffsetDays ──────────────────────────────────────────────────

    /// <summary>
    /// Gets the test cases that pair a minimal XML fragment with the expected <see cref="ObservanceAdjustment.OffsetDays" />
    /// value it encodes via the <c>days</c> attribute.
    /// </summary>
    public static IEnumerable<object[]> AdjustmentOffsetDaysCases =>
    [
        [
            "Positive day offset maps OffsetDays to 7",
            BuildAdjustmentXml(@"key=""t"" when=""Always"" action=""AddDays"" days=""7"""),
            7,
        ],
        [
            "Negative day offset maps OffsetDays to -3",
            BuildAdjustmentXml(@"key=""t"" when=""Always"" action=""AddDays"" days=""-3"""),
            -3,
        ],
        [
            "Zero day offset maps OffsetDays to 0",
            BuildAdjustmentXml(@"key=""t"" when=""Always"" action=""AddDays"" days=""0"""),
            0,
        ],
    ];

    /// <summary>
    /// Verifies that the <c>days</c> attribute of an <c>&lt;Adjustment&gt;</c> XML element is correctly parsed into
    /// <see cref="ObservanceAdjustment.OffsetDays" />, including positive, negative, and zero values, using the XML
    /// fragment supplied by each test case.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(AdjustmentOffsetDaysCases), DynamicDataDisplayName = nameof(GetAdjustmentTestDisplayName))]
    public void ParseXml_WhenAddDaysActionProvided_ShouldMapOffsetDays(
        string displayName,
        string xmlFragment,
        int expectedOffset)
    {
        NotableDateRule rule = NotableDateRuleParser.ParseXml(xmlFragment).Single();

        Assert.AreEqual(expectedOffset, rule.Adjustments.Single().OffsetDays);
    }

    // ── Group 4b: TargetRuleName ──────────────────────────────────────────────

    /// <summary>
    /// Gets the test cases that pair a minimal XML fragment with the expected
    /// <see cref="ObservanceAdjustment.TargetRuleName" /> value it encodes via the <c>target</c> attribute.
    /// </summary>
    public static IEnumerable<object[]> AdjustmentTargetRuleNameCases =>
    [
        [
            "target Easter Sunday maps TargetRuleName",
            BuildAdjustmentXml(@"key=""t"" when=""Always"" action=""ReplaceWithNamedDate"" target=""Easter Sunday"""),
            "Easter Sunday",
        ],
        [
            "target New Year's Day maps TargetRuleName",
            BuildAdjustmentXml(@"key=""t"" when=""Always"" action=""ReplaceWithNamedDate"" target=""New Year's Day"""),
            "New Year's Day",
        ],
    ];

    /// <summary>
    /// Verifies that the <c>target</c> attribute of an <c>&lt;Adjustment&gt;</c> XML element is correctly parsed into
    /// <see cref="ObservanceAdjustment.TargetRuleName" />, using the XML fragment supplied by each test case.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(AdjustmentTargetRuleNameCases), DynamicDataDisplayName = nameof(GetAdjustmentTestDisplayName))]
    public void ParseXml_WhenReplaceWithNamedDateActionProvided_ShouldMapTargetRuleName(
        string displayName,
        string xmlFragment,
        string expectedTargetRuleName)
    {
        NotableDateRule rule = NotableDateRuleParser.ParseXml(xmlFragment).Single();

        Assert.AreEqual(expectedTargetRuleName, rule.Adjustments.Single().TargetRuleName);
    }

    // ── Group 4c: HandlerKey ──────────────────────────────────────────────────

    /// <summary>
    /// Gets the test cases that pair a minimal XML fragment with the expected <see cref="ObservanceAdjustment.HandlerKey" />
    /// value it encodes via the <c>handlerKey</c> attribute, covering both the Custom trigger and Custom action variants.
    /// </summary>
    public static IEnumerable<object[]> AdjustmentHandlerKeyCases =>
    [
        [
            "Custom trigger with handlerKey maps HandlerKey",
            BuildAdjustmentXml(@"key=""t"" when=""Custom"" action=""None"" handlerKey=""my-trigger-handler"""),
            "my-trigger-handler",
        ],
        [
            "Custom action with handlerKey maps HandlerKey",
            BuildAdjustmentXml(@"key=""t"" when=""Always"" action=""Custom"" handlerKey=""my-action-handler"""),
            "my-action-handler",
        ],
    ];

    /// <summary>
    /// Verifies that the <c>handlerKey</c> attribute of an <c>&lt;Adjustment&gt;</c> XML element is correctly parsed
    /// into <see cref="ObservanceAdjustment.HandlerKey" /> for both the Custom trigger and Custom action variants,
    /// using the XML fragment supplied by each test case.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(AdjustmentHandlerKeyCases), DynamicDataDisplayName = nameof(GetAdjustmentTestDisplayName))]
    public void ParseXml_WhenCustomHandlerKeyProvided_ShouldMapHandlerKey(
        string displayName,
        string xmlFragment,
        string expectedHandlerKey)
    {
        NotableDateRule rule = NotableDateRuleParser.ParseXml(xmlFragment).Single();

        Assert.AreEqual(expectedHandlerKey, rule.Adjustments.Single().HandlerKey);
    }

    // ── Group 5a: Key ─────────────────────────────────────────────────────────

    /// <summary>
    /// Gets the test cases that pair a minimal XML fragment with the expected <see cref="ObservanceAdjustment.Key" />
    /// value it encodes via the <c>key</c> attribute.
    /// </summary>
    public static IEnumerable<object[]> AdjustmentKeyCases =>
    [
        [
            "Key weekend-roll maps Key",
            BuildAdjustmentXml(@"key=""weekend-roll"" when=""Always"" action=""None"""),
            "weekend-roll",
        ],
        [
            "Key boxing-day-sub maps Key",
            BuildAdjustmentXml(@"key=""boxing-day-sub"" when=""Always"" action=""None"""),
            "boxing-day-sub",
        ],
        [
            "Key anzac-day-wa maps Key",
            BuildAdjustmentXml(@"key=""anzac-day-wa"" when=""Always"" action=""None"""),
            "anzac-day-wa",
        ],
    ];

    /// <summary>
    /// Verifies that the <c>key</c> attribute of an <c>&lt;Adjustment&gt;</c> XML element is correctly parsed into
    /// <see cref="ObservanceAdjustment.Key" />, using the XML fragment supplied by each test case.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(AdjustmentKeyCases), DynamicDataDisplayName = nameof(GetAdjustmentTestDisplayName))]
    public void ParseXml_WhenAdjustmentKeyProvided_ShouldMapKey(
        string displayName,
        string xmlFragment,
        string expectedKey)
    {
        NotableDateRule rule = NotableDateRuleParser.ParseXml(xmlFragment).Single();

        Assert.AreEqual(expectedKey, rule.Adjustments.Single().Key);
    }

    // ── Group 5b: Priority ────────────────────────────────────────────────────

    /// <summary>
    /// Gets the test cases that pair a minimal XML fragment with the expected <see cref="ObservanceAdjustment.Priority" />
    /// value it encodes via the <c>priority</c> attribute.
    /// </summary>
    public static IEnumerable<object[]> AdjustmentPriorityCases =>
    [
        [
            "Priority 1 maps Priority",
            BuildAdjustmentXml(@"key=""t"" when=""Always"" action=""None"" priority=""1"""),
            1,
        ],
        [
            "Priority 50 maps Priority",
            BuildAdjustmentXml(@"key=""t"" when=""Always"" action=""None"" priority=""50"""),
            50,
        ],
        [
            "Priority 200 maps Priority",
            BuildAdjustmentXml(@"key=""t"" when=""Always"" action=""None"" priority=""200"""),
            200,
        ],
    ];

    /// <summary>
    /// Verifies that the <c>priority</c> attribute of an <c>&lt;Adjustment&gt;</c> XML element is correctly parsed
    /// into <see cref="ObservanceAdjustment.Priority" />, using the XML fragment supplied by each test case.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(AdjustmentPriorityCases), DynamicDataDisplayName = nameof(GetAdjustmentTestDisplayName))]
    public void ParseXml_WhenAdjustmentPriorityProvided_ShouldMapPriority(
        string displayName,
        string xmlFragment,
        int expectedPriority)
    {
        NotableDateRule rule = NotableDateRuleParser.ParseXml(xmlFragment).Single();

        Assert.AreEqual(expectedPriority, rule.Adjustments.Single().Priority);
    }

    // ── Group 5c: TerritoryCode ───────────────────────────────────────────────

    /// <summary>
    /// Gets the test cases that pair a minimal XML fragment with the expected <see cref="ObservanceAdjustment.TerritoryCode" />
    /// value it encodes via the <c>territory</c> attribute, covering single and comma-separated values.
    /// </summary>
    public static IEnumerable<object[]> AdjustmentTerritoryCases =>
    [
        [
            "Territory AU maps TerritoryCode",
            BuildAdjustmentXml(@"key=""t"" when=""Always"" action=""None"" territory=""AU"""),
            "AU",
        ],
        [
            "Territory AU-NSW maps TerritoryCode",
            BuildAdjustmentXml(@"key=""t"" when=""Always"" action=""None"" territory=""AU-NSW"""),
            "AU-NSW",
        ],
        [
            "Territory AU-NSW,AU-VIC maps TerritoryCode",
            BuildAdjustmentXml(@"key=""t"" when=""Always"" action=""None"" territory=""AU-NSW,AU-VIC"""),
            "AU-NSW,AU-VIC",
        ],
    ];

    /// <summary>
    /// Verifies that the <c>territory</c> attribute of an <c>&lt;Adjustment&gt;</c> XML element is correctly parsed
    /// into <see cref="ObservanceAdjustment.TerritoryCode" />, including single and comma-separated values, using the
    /// XML fragment supplied by each test case.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(AdjustmentTerritoryCases), DynamicDataDisplayName = nameof(GetAdjustmentTestDisplayName))]
    public void ParseXml_WhenAdjustmentTerritoryProvided_ShouldMapTerritoryCode(
        string displayName,
        string xmlFragment,
        string expectedTerritory)
    {
        NotableDateRule rule = NotableDateRuleParser.ParseXml(xmlFragment).Single();

        Assert.AreEqual(expectedTerritory, rule.Adjustments.Single().TerritoryCode);
    }

    // ── Group 5d: CalendarType ────────────────────────────────────────────────

    /// <summary>
    /// Gets the test cases that pair a minimal XML fragment with the expected <see cref="ObservanceAdjustment.CalendarType" />
    /// it encodes via the <c>calendarType</c> attribute.
    /// </summary>
    public static IEnumerable<object[]> AdjustmentCalendarTypeCases =>
    [
        [
            "GregorianCalendar calendarType maps CalendarType",
            BuildAdjustmentXml(@"key=""t"" when=""Always"" action=""None"" calendarType=""System.Globalization.GregorianCalendar"""),
            typeof(System.Globalization.GregorianCalendar),
        ],
    ];

    /// <summary>
    /// Verifies that the <c>calendarType</c> attribute of an <c>&lt;Adjustment&gt;</c> XML element is correctly
    /// resolved to the corresponding CLR <see cref="Type" />, using the XML fragment supplied by each test case.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(AdjustmentCalendarTypeCases), DynamicDataDisplayName = nameof(GetAdjustmentTestDisplayName))]
    public void ParseXml_WhenAdjustmentCalendarTypeProvided_ShouldMapCalendarType(
        string displayName,
        string xmlFragment,
        Type expectedCalendarType)
    {
        NotableDateRule rule = NotableDateRuleParser.ParseXml(xmlFragment).Single();

        Assert.AreEqual(expectedCalendarType, rule.Adjustments.Single().CalendarType);
    }

    // ── Group 5e: Effective year bounds ───────────────────────────────────────

    /// <summary>
    /// Gets the test cases that pair a minimal XML fragment with the expected
    /// <see cref="ObservanceAdjustment.EffectiveFromYear" /> and <see cref="ObservanceAdjustment.EffectiveToYear" />
    /// values each encodes via the <c>fromYear</c> and <c>toYear</c> attributes.
    /// </summary>
    public static IEnumerable<object[]> AdjustmentEffectiveYearCases =>
    [
        [
            "Both fromYear and toYear set maps effective year bounds",
            BuildAdjustmentXml(@"key=""t"" when=""Always"" action=""None"" fromYear=""2010"" toYear=""2030"""),
            (int?)2010,
            (int?)2030,
        ],
        [
            "Only fromYear set maps EffectiveFromYear with null EffectiveToYear",
            BuildAdjustmentXml(@"key=""t"" when=""Always"" action=""None"" fromYear=""2015"""),
            (int?)2015,
            (int?)null!,
        ],
        [
            "Only toYear set maps null EffectiveFromYear with EffectiveToYear",
            BuildAdjustmentXml(@"key=""t"" when=""Always"" action=""None"" toYear=""2025"""),
            (int?)null!,
            (int?)2025,
        ],
    ];

    /// <summary>
    /// Verifies that the <c>fromYear</c> and <c>toYear</c> attributes of an <c>&lt;Adjustment&gt;</c> XML element are
    /// correctly parsed into <see cref="ObservanceAdjustment.EffectiveFromYear" /> and
    /// <see cref="ObservanceAdjustment.EffectiveToYear" />, including single-bound and both-bounds cases, using the
    /// XML fragment supplied by each test case.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(AdjustmentEffectiveYearCases), DynamicDataDisplayName = nameof(GetAdjustmentTestDisplayName))]
    public void ParseXml_WhenAdjustmentEffectiveYearsProvided_ShouldMapYearBounds(
        string displayName,
        string xmlFragment,
        int? expectedFromYear,
        int? expectedToYear)
    {
        NotableDateRule rule = NotableDateRuleParser.ParseXml(xmlFragment).Single();
        ObservanceAdjustment adjustment = rule.Adjustments.Single();

        Assert.AreEqual(expectedFromYear, adjustment.EffectiveFromYear);
        Assert.AreEqual(expectedToYear, adjustment.EffectiveToYear);
    }

    // ── Group 5f: IsNonWorkingDay ─────────────────────────────────────────────

    /// <summary>
    /// Gets the test cases that pair a minimal XML fragment with the expected <see cref="ObservanceAdjustment.IsNonWorkingDay" />
    /// value it encodes via the <c>nonWorking</c> attribute.
    /// </summary>
    public static IEnumerable<object[]> AdjustmentIsNonWorkingDayCases =>
    [
        [
            "nonWorking true maps IsNonWorkingDay to true",
            BuildAdjustmentXml(@"key=""t"" when=""Always"" action=""None"" nonWorking=""true"""),
            true,
        ],
        [
            "nonWorking false maps IsNonWorkingDay to false",
            BuildAdjustmentXml(@"key=""t"" when=""Always"" action=""None"" nonWorking=""false"""),
            false,
        ],
    ];

    /// <summary>
    /// Verifies that the <c>nonWorking</c> attribute of an <c>&lt;Adjustment&gt;</c> XML element is correctly parsed
    /// into <see cref="ObservanceAdjustment.IsNonWorkingDay" /> for both <see langword="true" /> and
    /// <see langword="false" /> values, using the XML fragment supplied by each test case.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(AdjustmentIsNonWorkingDayCases), DynamicDataDisplayName = nameof(GetAdjustmentTestDisplayName))]
    public void ParseXml_WhenAdjustmentNonWorkingProvided_ShouldMapIsNonWorkingDay(
        string displayName,
        string xmlFragment,
        bool expectedIsNonWorkingDay)
    {
        NotableDateRule rule = NotableDateRuleParser.ParseXml(xmlFragment).Single();

        Assert.AreEqual(expectedIsNonWorkingDay, rule.Adjustments.Single().IsNonWorkingDay);
    }
}
