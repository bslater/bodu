// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateSchemaType.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Identifies a complex type of the notable-date XML schema whose required attributes and elements the JSON reader,
/// validated by no schema, checks itself, each named as the schema names it and listing the JSON properties it
/// requires.
/// </summary>
internal enum NotableDateSchemaType
{
    /// <summary>
    /// The schema's <c>NotableDateResource</c> type, the document's root element: <c>schemaVersion</c> and
    /// <c>resourceId</c>.
    /// </summary>
    NotableDateResource,

    /// <summary>
    /// The schema's <c>AdjustmentPolicy</c> type, an adjustment policy: its <c>id</c>, <c>trigger</c>, <c>action</c>
    /// and <c>emission</c>.
    /// </summary>
    AdjustmentPolicy,

    /// <summary>
    /// The schema's <c>AdjustmentTrigger</c> type, an adjustment policy's trigger: its <c>type</c>.
    /// </summary>
    AdjustmentTrigger,

    /// <summary>
    /// The schema's <c>AdjustmentAction</c> type, an adjustment policy's action: its <c>type</c>.
    /// </summary>
    AdjustmentAction,

    /// <summary>
    /// The schema's <c>AdjustmentEmission</c> type, an adjustment policy's emission: its <c>mode</c>.
    /// </summary>
    AdjustmentEmission,

    /// <summary>
    /// The schema's <c>Import</c> type, an import: its <c>resource</c>.
    /// </summary>
    Import,

    /// <summary>
    /// The schema's <c>ImportUse</c> type, a use of an imported concept: its <c>notableDateRef</c>.
    /// </summary>
    ImportUse,

    /// <summary>
    /// The schema's <c>NotableDate</c> type, a concept: its <c>id</c>, <c>displayName</c>, <c>category</c> and
    /// <c>rules</c>, which must hold a rule.
    /// </summary>
    NotableDate,

    /// <summary>
    /// The schema's <c>Rule</c> type, a rule: its <c>id</c>.
    /// </summary>
    Rule,

    /// <summary>
    /// The schema's <c>Duration</c> type, a rule's calculated duration: its <c>untilDate</c>.
    /// </summary>
    Duration,

    /// <summary>
    /// The schema's <c>FixedStrategy</c> type, a <c>fixed</c> strategy: its <c>month</c> and <c>day</c>.
    /// </summary>
    FixedStrategy,

    /// <summary>
    /// The schema's <c>DayOfWeekInMonthStrategy</c> type, a <c>dayOfWeekInMonth</c> strategy: its <c>month</c>,
    /// <c>dayOfWeek</c> and <c>weekOrdinal</c>.
    /// </summary>
    DayOfWeekInMonthStrategy,

    /// <summary>
    /// The schema's <c>WeekdayNearDateStrategy</c> type, a <c>weekdayNearDate</c> strategy: its <c>month</c>,
    /// <c>day</c>, <c>dayOfWeek</c> and <c>direction</c>.
    /// </summary>
    WeekdayNearDateStrategy,

    /// <summary>
    /// The schema's <c>RelativeWeekdayInMonthStrategy</c> type, a <c>relativeWeekdayInMonth</c> strategy: its
    /// <c>month</c>, <c>dayOfWeek</c>, <c>weekOrdinal</c>, <c>relativeDayOfWeek</c> and <c>direction</c>.
    /// </summary>
    RelativeWeekdayInMonthStrategy,

    /// <summary>
    /// The schema's <c>OffsetFromRuleStrategy</c> type, an <c>offsetFromRule</c> strategy: its <c>notableDateRef</c>
    /// and <c>offsetDays</c>.
    /// </summary>
    OffsetFromRuleStrategy,

    /// <summary>
    /// The schema's <c>AlgorithmStrategy</c> type, an <c>algorithm</c> strategy: its <c>key</c>.
    /// </summary>
    AlgorithmStrategy,

    /// <summary>
    /// The schema's <c>WeekdayNearRuleStrategy</c> type, a <c>weekdayNearRule</c> strategy: its <c>notableDateRef</c>,
    /// <c>dayOfWeek</c> and <c>direction</c>.
    /// </summary>
    WeekdayNearRuleStrategy,

    /// <summary>
    /// The schema's <c>NthWeekdayFromRuleStrategy</c> type, an <c>nthWeekdayFromRule</c> strategy: its
    /// <c>notableDateRef</c>, <c>dayOfWeek</c> and <c>ordinal</c>.
    /// </summary>
    NthWeekdayFromRuleStrategy,

    /// <summary>
    /// The schema's <c>WorkingDayOffsetFromRuleStrategy</c> type, a <c>workingDayOffsetFromRule</c> strategy: its
    /// <c>notableDateRef</c> and <c>offsetWorkingDays</c>.
    /// </summary>
    WorkingDayOffsetFromRuleStrategy,

    /// <summary>
    /// The schema's <c>WorkingDayInMonthStrategy</c> type, a <c>workingDayInMonth</c> strategy: its <c>month</c> and
    /// <c>ordinal</c>.
    /// </summary>
    WorkingDayInMonthStrategy,

    /// <summary>
    /// The schema's <c>OrdinalDayOfMonthStrategy</c> type, an <c>ordinalDayOfMonth</c> strategy: its <c>month</c> and
    /// <c>ordinal</c>.
    /// </summary>
    OrdinalDayOfMonthStrategy,

    /// <summary>
    /// The schema's <c>IsoWeekDateStrategy</c> type, an <c>isoWeekDate</c> strategy: its <c>week</c> and
    /// <c>dayOfWeek</c>.
    /// </summary>
    IsoWeekDateStrategy,

    /// <summary>
    /// The schema's <c>DayOfYearStrategy</c> type, a <c>dayOfYear</c> strategy: its <c>ordinal</c>.
    /// </summary>
    DayOfYearStrategy,

    /// <summary>
    /// The schema's <c>DailyIntervalRecurrence</c> type, a <c>dailyInterval</c> recurrence: its <c>anchorDate</c>.
    /// </summary>
    DailyIntervalRecurrence,

    /// <summary>
    /// The schema's <c>WeeklyRecurrence</c> type, a <c>weekly</c> recurrence: its <c>days</c>, which must hold a day.
    /// </summary>
    WeeklyRecurrence,

    /// <summary>
    /// The schema's <c>MonthlyDayRecurrence</c> type, a <c>monthlyDay</c> recurrence: its <c>dayOfMonth</c>.
    /// </summary>
    MonthlyDayRecurrence,

    /// <summary>
    /// The schema's <c>MonthlyWeekdayRecurrence</c> type, a <c>monthlyWeekday</c> recurrence: its <c>dayOfWeek</c> and
    /// <c>weekOrdinal</c>.
    /// </summary>
    MonthlyWeekdayRecurrence,

    /// <summary>
    /// The schema's <c>AddRule</c> type, an <c>AddRule</c> override: its <c>notableDateRef</c> and <c>rule</c>.
    /// </summary>
    AddRule,

    /// <summary>
    /// The schema's <c>PatchRule</c> type, a <c>PatchRule</c> override: its <c>notableDateRef</c> and <c>ruleRef</c>.
    /// </summary>
    PatchRule,

    /// <summary>
    /// The schema's <c>RuleRef</c> type, a qualified rule reference, a <c>RemoveRule</c> override or a scope's
    /// <c>ruleRefs</c> object: its <c>notableDateRef</c> and <c>ruleRef</c>.
    /// </summary>
    RuleRef,
}
