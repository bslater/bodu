// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateSchemaRequirements.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Holds the JSON properties each complex type of the notable-date XML schema requires, the attributes it declares
/// <c>use="required"</c> and the elements it requires at least once, so the JSON reader and the document builder report
/// a missing one as the schema reports it in XML.
/// </summary>
/// <remarks>
/// A required element that holds a list (a concept's <c>Rules</c>, a weekly recurrence's <c>Day</c> elements) is a JSON
/// array that must hold an item. An element the schema requires as one of a choice (a rule's strategy or recurrence, an
/// end strategy's kind) is not listed: <c>BODU-CAL-RULE-SOURCE</c> and <c>BODU-CAL-DURATION-NOEND</c> report it.
/// </remarks>
internal static class NotableDateSchemaRequirements
{
    /// <summary>The JSON properties each schema type requires, in the order they are checked.</summary>
    private static readonly Dictionary<NotableDateSchemaType, string[]> s_requiredProperties = new()
    {
        [NotableDateSchemaType.NotableDateResource] = ["schemaVersion", "resourceId"],
        [NotableDateSchemaType.AdjustmentPolicy] = ["id", "trigger", "action", "emission"],
        [NotableDateSchemaType.AdjustmentTrigger] = ["type"],
        [NotableDateSchemaType.AdjustmentAction] = ["type"],
        [NotableDateSchemaType.AdjustmentEmission] = ["mode"],
        [NotableDateSchemaType.Import] = ["resource"],
        [NotableDateSchemaType.ImportUse] = ["notableDateRef"],
        [NotableDateSchemaType.NotableDate] = ["id", "displayName", "category", "rules"],
        [NotableDateSchemaType.Rule] = ["id"],
        [NotableDateSchemaType.Duration] = ["untilDate"],
        [NotableDateSchemaType.FixedStrategy] = ["month", "day"],
        [NotableDateSchemaType.DayOfWeekInMonthStrategy] = ["month", "dayOfWeek", "weekOrdinal"],
        [NotableDateSchemaType.WeekdayNearDateStrategy] = ["month", "day", "dayOfWeek", "direction"],
        [NotableDateSchemaType.RelativeWeekdayInMonthStrategy] = ["month", "dayOfWeek", "weekOrdinal", "relativeDayOfWeek", "direction"],
        [NotableDateSchemaType.OffsetFromRuleStrategy] = ["notableDateRef", "offsetDays"],
        [NotableDateSchemaType.AlgorithmStrategy] = ["key"],
        [NotableDateSchemaType.WeekdayNearRuleStrategy] = ["notableDateRef", "dayOfWeek", "direction"],
        [NotableDateSchemaType.NthWeekdayFromRuleStrategy] = ["notableDateRef", "dayOfWeek", "ordinal"],
        [NotableDateSchemaType.WorkingDayOffsetFromRuleStrategy] = ["notableDateRef", "offsetWorkingDays"],
        [NotableDateSchemaType.WorkingDayInMonthStrategy] = ["month", "ordinal"],
        [NotableDateSchemaType.OrdinalDayOfMonthStrategy] = ["month", "ordinal"],
        [NotableDateSchemaType.IsoWeekDateStrategy] = ["week", "dayOfWeek"],
        [NotableDateSchemaType.DayOfYearStrategy] = ["ordinal"],
        [NotableDateSchemaType.DailyIntervalRecurrence] = ["anchorDate"],
        [NotableDateSchemaType.WeeklyRecurrence] = ["days"],
        [NotableDateSchemaType.MonthlyDayRecurrence] = ["dayOfMonth"],
        [NotableDateSchemaType.MonthlyWeekdayRecurrence] = ["dayOfWeek", "weekOrdinal"],
        [NotableDateSchemaType.AddRule] = ["notableDateRef", "rule"],
        [NotableDateSchemaType.PatchRule] = ["notableDateRef", "ruleRef"],
        [NotableDateSchemaType.RuleRef] = ["notableDateRef", "ruleRef"],
    };

    /// <summary>The required properties that hold a list, which the schema requires to hold an item.</summary>
    private static readonly HashSet<(NotableDateSchemaType Type, string Property)> s_requiredLists =
    [
        (NotableDateSchemaType.NotableDate, "rules"),
        (NotableDateSchemaType.WeeklyRecurrence, "days"),
    ];

    /// <summary>
    /// Returns the JSON properties a schema type requires.
    /// </summary>
    /// <param name="type">The schema type.</param>
    /// <returns>The names of the required properties, in the order they are checked.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="type" /> names no schema type.</exception>
    internal static IReadOnlyList<string> GetRequiredProperties(NotableDateSchemaType type) =>
        s_requiredProperties.TryGetValue(type, out string[]? properties) ? properties : throw new ArgumentOutOfRangeException(nameof(type));

    /// <summary>
    /// Determines whether a property a schema type requires holds a list, which must hold an item.
    /// </summary>
    /// <param name="type">The schema type.</param>
    /// <param name="property">The name of the required property.</param>
    /// <returns>
    /// <see langword="true" /> when the property is a list the schema requires to hold an item; otherwise
    /// <see langword="false" />.
    /// </returns>
    internal static bool RequiresItems(NotableDateSchemaType type, string property) =>
        s_requiredLists.Contains((type, property));

    /// <summary>
    /// Creates the <c>BODU-CAL-SCHEMA</c> error for an object that lacks a property its schema type requires.
    /// </summary>
    /// <param name="type">The object's schema type.</param>
    /// <param name="property">The name of the missing property.</param>
    /// <returns>The diagnostic.</returns>
    internal static NotableDateValidationDiagnostic MissingProperty(NotableDateSchemaType type, string property) =>
        new(
            NotableDateValidationSeverity.Error,
            "BODU-CAL-SCHEMA",
            string.Format(CultureInfo.CurrentCulture, CalendarResourceStrings.Validation_SchemaRequiredPropertyMissing, type.ToString(), property));

    /// <summary>
    /// Creates the <c>BODU-CAL-SCHEMA</c> error for an object whose required list holds no item.
    /// </summary>
    /// <param name="type">The object's schema type.</param>
    /// <param name="property">The name of the empty list.</param>
    /// <returns>The diagnostic.</returns>
    internal static NotableDateValidationDiagnostic EmptyList(NotableDateSchemaType type, string property) =>
        new(
            NotableDateValidationSeverity.Error,
            "BODU-CAL-SCHEMA",
            string.Format(CultureInfo.CurrentCulture, CalendarResourceStrings.Validation_SchemaRequiredListEmpty, type.ToString(), property));
}
