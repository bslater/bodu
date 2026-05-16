// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ObservanceAdjustmentBuilder.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using Bodu.Extensions;

namespace Bodu.Globalization.Calendar.Builder;

/// <summary>
/// Provides a fluent interface for constructing an <see cref="ObservanceAdjustment" /> and its corresponding XML or JSON representation.
/// </summary>
/// <remarks>
/// <para>
/// An <see cref="ObservanceAdjustmentBuilder" /> is obtained via
/// <see cref="NotableDateRuleBuilder.AddAdjustment(string, System.Action{ObservanceAdjustmentBuilder})" />. The builder accumulates
/// adjustment properties and produces a domain object, a schema-valid <c>&lt;Adjustment&gt;</c> XML element, and a schema-valid
/// <c>adjustment</c> JSON object when the enclosing rule is built.
/// </para>
/// <para>
/// Only <see cref="When(AdjustmentTrigger)" /> and <see cref="Action(AdjustmentAction)" /> are required; all other properties are optional
/// and default to the same values as their <see cref="ObservanceAdjustment" /> counterparts.
/// </para>
/// <para>
/// <see cref="AddHandlerParameter(string, string)" /> and <see cref="MaxAdjustmentReachDays(int)" /> populate
/// <see cref="ObservanceAdjustment.HandlerParameters" /> and <see cref="ObservanceAdjustment.MaxAdjustmentReachDays" /> respectively.
/// Both fields are programmatic-only — they are not part of the <c>NotableDates.xsd</c> or <c>NotableDates.schema.json</c>
/// schemas, so values supplied here are honoured by <see cref="Build(string)" /> but omitted from
/// <see cref="ToXElement(string, XNamespace)" /> and <see cref="ToJsonNode(string)" />.
/// </para>
/// </remarks>
public sealed class ObservanceAdjustmentBuilder
{
    /// <summary>The activation condition set via <see cref="When(AdjustmentTrigger)" />, or <see langword="null" /> until configured.</summary>
    private AdjustmentTrigger? _trigger;

    /// <summary>The modification action set via <see cref="Action(AdjustmentAction)" />, or <see langword="null" /> until configured.</summary>
    private AdjustmentAction? _action;

    /// <summary>The day-of-week qualifier set via <see cref="OnDayOfWeek(DayOfWeek)" />, consumed by <see cref="AdjustmentTrigger.IfDayOfWeek" />.</summary>
    private DayOfWeek? _dayOfWeek;

    /// <summary>The non-working override set via <see cref="NonWorking(bool)" />, or <see langword="null" /> to inherit the rule's value.</summary>
    private bool? _isNonWorkingDay;

    /// <summary>The signed day offset set via <see cref="OffsetDays(int)" />, consumed by <see cref="AdjustmentAction.AddDays" />. Defaults to <c>0</c>.</summary>
    private int _offsetDays;

    /// <summary>The territory scope set via <see cref="Territory(string)" />, or <see langword="null" /> when unscoped.</summary>
    private string? _territoryCode;

    /// <summary>The calendar scope set via <see cref="CalendarType(Type)" />, or <see langword="null" /> when unscoped.</summary>
    private Type? _calendarType;

    /// <summary>The inclusive earliest effective year set via <see cref="FromYear(int)" />, or <see langword="null" /> for no lower bound.</summary>
    private int? _effectiveFromYear;

    /// <summary>The inclusive latest effective year set via <see cref="ToYear(int)" />, or <see langword="null" /> for no upper bound.</summary>
    private int? _effectiveToYear;

    /// <summary>The month component of the comparison date set via <see cref="ComparisonDate(int, int)" />, paired with <see cref="_comparisonDay" />.</summary>
    private int? _comparisonMonth;

    /// <summary>The day component of the comparison date set via <see cref="ComparisonDate(int, int)" />, paired with <see cref="_comparisonMonth" />.</summary>
    private int? _comparisonDay;

    /// <summary>The ordinal occurrence set via <see cref="OrdinalOccurrence(WeekOfMonthOrdinal)" />, consumed by <see cref="AdjustmentTrigger.IfNthOccurrenceInMonth" />.</summary>
    private WeekOfMonthOrdinal? _weekOrdinal;

    /// <summary>The target rule name set via <see cref="Target(string)" />, consumed by <see cref="AdjustmentAction.ReplaceWithNamedDate" />.</summary>
    private string? _targetRuleName;

    /// <summary>The evaluation priority set via <see cref="Priority(int)" /> (lower wins). Defaults to <c>100</c> to mirror <see cref="ObservanceAdjustment.Priority" />.</summary>
    private int _priority = 100;

    /// <summary>The custom-handler registry key set via <see cref="HandlerKey(string)" />, consumed by <see cref="AdjustmentTrigger.Custom" />/<see cref="AdjustmentAction.Custom" />.</summary>
    private string? _handlerKey;

    /// <summary>The handler parameter accumulator populated by <see cref="AddHandlerParameter(string, string)" />, or <see langword="null" /> when none are authored.</summary>
    private Dictionary<string, string>? _handlerParameters;

    /// <summary>The symmetric reach envelope in days set via <see cref="MaxAdjustmentReachDays(int)" />, or <see langword="null" /> to use the action-specific default heuristic.</summary>
    private int? _maxAdjustmentReachDays;

    /// <summary>
    /// Sets the condition that activates this adjustment.
    /// </summary>
    /// <param name="trigger">The activation condition.</param>
    /// <returns>This builder instance, for method chaining.</returns>
    public ObservanceAdjustmentBuilder When(AdjustmentTrigger trigger)
    {
        _trigger = trigger;
        return this;
    }

    /// <summary>
    /// Sets the modification applied when this adjustment activates.
    /// </summary>
    /// <param name="action">The action to apply.</param>
    /// <returns>This builder instance, for method chaining.</returns>
    public ObservanceAdjustmentBuilder Action(AdjustmentAction action)
    {
        _action = action;
        return this;
    }

    /// <summary>
    /// Sets the day of week required by <see cref="AdjustmentTrigger.IfDayOfWeek" />.
    /// </summary>
    /// <param name="dayOfWeek">The required day of week.</param>
    /// <returns>This builder instance, for method chaining.</returns>
    public ObservanceAdjustmentBuilder OnDayOfWeek(DayOfWeek dayOfWeek)
    {
        _dayOfWeek = dayOfWeek;
        return this;
    }

    /// <summary>
    /// Sets whether the adjusted date is treated as a non-working day.
    /// </summary>
    /// <param name="value"><see langword="true" /> to mark the adjusted date as non-working; <see langword="false" /> otherwise.</param>
    /// <returns>This builder instance, for method chaining.</returns>
    public ObservanceAdjustmentBuilder NonWorking(bool value = true)
    {
        _isNonWorkingDay = value;
        return this;
    }

    /// <summary>
    /// Sets the day offset applied by <see cref="AdjustmentAction.AddDays" />. Negative values move the date backwards.
    /// </summary>
    /// <param name="days">The number of days to add; may be negative.</param>
    /// <returns>This builder instance, for method chaining.</returns>
    public ObservanceAdjustmentBuilder OffsetDays(int days)
    {
        _offsetDays = days;
        return this;
    }

    /// <summary>
    /// Scopes this adjustment to the specified territory codes.
    /// </summary>
    /// <param name="code">A comma-separated list of ISO 3166 territory codes (e.g. <c>"AU"</c>, <c>"AU-NSW,AU-VIC"</c>). Must not be <see langword="null" /> or whitespace.</param>
    /// <returns>This builder instance, for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="code" /> is <see langword="null" />, empty, or whitespace.</exception>
    public ObservanceAdjustmentBuilder Territory(string code)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(code);
        _territoryCode = code;
        return this;
    }

    /// <summary>
    /// Scopes this adjustment to the specified calendar system.
    /// </summary>
    /// <param name="calendarType">The CLR <see cref="Type" /> of the calendar (must derive from <see cref="System.Globalization.Calendar" />). Must not be <see langword="null" />.</param>
    /// <returns>This builder instance, for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="calendarType" /> is <see langword="null" />.</exception>
    public ObservanceAdjustmentBuilder CalendarType(Type calendarType)
    {
        ThrowHelper.ThrowIfNull(calendarType);
        _calendarType = calendarType;
        return this;
    }

    /// <summary>
    /// Sets the inclusive first year from which this adjustment is effective.
    /// </summary>
    /// <param name="year">The first applicable year.</param>
    /// <returns>This builder instance, for method chaining.</returns>
    public ObservanceAdjustmentBuilder FromYear(int year)
    {
        _effectiveFromYear = year;
        return this;
    }

    /// <summary>
    /// Sets the inclusive last year until which this adjustment is effective.
    /// </summary>
    /// <param name="year">The last applicable year.</param>
    /// <returns>This builder instance, for method chaining.</returns>
    public ObservanceAdjustmentBuilder ToYear(int year)
    {
        _effectiveToYear = year;
        return this;
    }

    /// <summary>
    /// Sets the comparison date used by <see cref="AdjustmentTrigger.IfBeforeFixedDate" /> and
    /// <see cref="AdjustmentTrigger.IfAfterFixedDate" />. Only the month and day are significant; the year is replaced at resolution time.
    /// </summary>
    /// <param name="month">The comparison month (1–12).</param>
    /// <param name="day">The comparison day of month (1–31).</param>
    /// <returns>This builder instance, for method chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="month" /> is less than 1 or greater than 12, or <paramref name="day" /> is less than 1 or greater than 31.
    /// </exception>
    public ObservanceAdjustmentBuilder ComparisonDate(int month, int day)
    {
        ThrowHelper.ThrowIfLessThan(month, 1);
        ThrowHelper.ThrowIfGreaterThan(month, 12);
        ThrowHelper.ThrowIfLessThan(day, 1);
        ThrowHelper.ThrowIfGreaterThan(day, 31);
        _comparisonMonth = month;
        _comparisonDay = day;
        return this;
    }

    /// <summary>
    /// Sets the ordinal occurrence required by <see cref="AdjustmentTrigger.IfNthOccurrenceInMonth" />.
    /// </summary>
    /// <param name="ordinal">The week-of-month ordinal (e.g. <see cref="WeekOfMonthOrdinal.Second" /> for the second occurrence).</param>
    /// <returns>This builder instance, for method chaining.</returns>
    public ObservanceAdjustmentBuilder OrdinalOccurrence(WeekOfMonthOrdinal ordinal)
    {
        _weekOrdinal = ordinal;
        return this;
    }

    /// <summary>
    /// Sets the name of another <see cref="NotableDateRule" /> referenced by <see cref="AdjustmentAction.ReplaceWithNamedDate" />.
    /// </summary>
    /// <param name="ruleName">The canonical name of the target rule. Must not be <see langword="null" /> or whitespace.</param>
    /// <returns>This builder instance, for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="ruleName" /> is <see langword="null" />, empty, or whitespace.</exception>
    public ObservanceAdjustmentBuilder Target(string ruleName)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(ruleName);
        _targetRuleName = ruleName;
        return this;
    }

    /// <summary>
    /// Sets the evaluation priority. Lower values are evaluated first.
    /// </summary>
    /// <param name="priority">The priority value.</param>
    /// <returns>This builder instance, for method chaining.</returns>
    public ObservanceAdjustmentBuilder Priority(int priority)
    {
        _priority = priority;
        return this;
    }

    /// <summary>
    /// Sets the registry key used to look up a custom <see cref="IAdjustmentHandler" />.
    /// </summary>
    /// <param name="key">The handler registry key. Must not be <see langword="null" /> or whitespace.</param>
    /// <returns>This builder instance, for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="key" /> is <see langword="null" />, empty, or whitespace.</exception>
    public ObservanceAdjustmentBuilder HandlerKey(string key)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(key);
        _handlerKey = key;
        return this;
    }

    /// <summary>
    /// Adds a key/value pair to the parameters forwarded to the registered <see cref="IAdjustmentHandler" />.
    /// </summary>
    /// <param name="key">The parameter name. Must not be <see langword="null" />, empty, or whitespace.</param>
    /// <param name="value">The parameter value. Must not be <see langword="null" />; an empty string is permitted.</param>
    /// <returns>This builder instance, for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// Repeated calls with the same <paramref name="key" /> replace the previously authored value
    /// (last-write-wins). Values populate <see cref="ObservanceAdjustment.HandlerParameters" /> and are
    /// consumed only by registered <see cref="IAdjustmentHandler" /> implementations. The dictionary is
    /// not part of the <c>NotableDates.xsd</c> or <c>NotableDates.schema.json</c> schemas, so values
    /// supplied here are honoured by <see cref="Build(string)" /> but omitted from
    /// <see cref="ToXElement(string, XNamespace)" /> and <see cref="ToJsonNode(string)" />.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown when <paramref name="key" /> is <see langword="null" />, empty, or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is <see langword="null" />.</exception>
    public ObservanceAdjustmentBuilder AddHandlerParameter(string key, string value)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(key);
        ThrowHelper.ThrowIfNull(value);
        _handlerParameters ??= new Dictionary<string, string>(StringComparer.Ordinal);
        _handlerParameters[key] = value;
        return this;
    }

    /// <summary>
    /// Sets the maximum reach in days that this adjustment can shift the calculated date in either direction.
    /// </summary>
    /// <param name="days">The symmetric envelope in days. Must be non-negative.</param>
    /// <returns>This builder instance, for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// Consumed by the prototype range-resolution pipeline (<c>NotableDateService.ResolveNotableDatesInRange</c>) to
    /// size the per-rule and global fringe envelope when an adjustment's actual reach exceeds the action's default
    /// heuristic. The value populates <see cref="ObservanceAdjustment.MaxAdjustmentReachDays" />. It is not part of
    /// the <c>NotableDates.xsd</c> or <c>NotableDates.schema.json</c> schemas, so values supplied here are honoured
    /// by <see cref="Build(string)" /> but omitted from <see cref="ToXElement(string, XNamespace)" /> and
    /// <see cref="ToJsonNode(string)" />.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="days" /> is negative.</exception>
    public ObservanceAdjustmentBuilder MaxAdjustmentReachDays(int days)
    {
        ThrowHelper.ThrowIfLessThan(days, 0);
        _maxAdjustmentReachDays = days;
        return this;
    }

    /// <summary>
    /// Creates an independent copy of this builder, suitable for use as a template that can then be tweaked for
    /// per-variant adjustments without mutating the source.
    /// </summary>
    /// <returns>A new <see cref="ObservanceAdjustmentBuilder" /> with the same configuration as this instance.</returns>
    /// <remarks>
    /// <para>
    /// Scalar fields are copied by value. The handler-parameter dictionary, when authored, is cloned so that
    /// later additions to either builder do not bleed across the boundary.
    /// </para>
    /// </remarks>
    public ObservanceAdjustmentBuilder Clone()
    {
        ObservanceAdjustmentBuilder copy = new()
        {
            _trigger = _trigger,
            _action = _action,
            _dayOfWeek = _dayOfWeek,
            _isNonWorkingDay = _isNonWorkingDay,
            _offsetDays = _offsetDays,
            _territoryCode = _territoryCode,
            _calendarType = _calendarType,
            _effectiveFromYear = _effectiveFromYear,
            _effectiveToYear = _effectiveToYear,
            _comparisonMonth = _comparisonMonth,
            _comparisonDay = _comparisonDay,
            _weekOrdinal = _weekOrdinal,
            _targetRuleName = _targetRuleName,
            _priority = _priority,
            _handlerKey = _handlerKey,
            _maxAdjustmentReachDays = _maxAdjustmentReachDays,
        };

        if (_handlerParameters is not null)
            copy._handlerParameters = new Dictionary<string, string>(_handlerParameters, StringComparer.Ordinal);

        return copy;
    }
    /// <summary>
    /// Builds an <see cref="ObservanceAdjustment" /> record from the current builder state.
    /// </summary>
    /// <param name="key">The adjustment key used for inheritance merging. Must not be <see langword="null" /> or whitespace.</param>
    /// <returns>The constructed <see cref="ObservanceAdjustment" />.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="key" /> is <see langword="null" />, empty, or whitespace.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="When(AdjustmentTrigger)" /> or <see cref="Action(AdjustmentAction)" /> has not been called.
    /// </exception>
    internal ObservanceAdjustment Build(string key)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(key);
        if (_trigger is null)
            throw new InvalidOperationException(BuilderResourceStrings.Op_Invalid_AdjustmentTriggerMissingBuild);
        if (_action is null)
            throw new InvalidOperationException(BuilderResourceStrings.Op_Invalid_AdjustmentActionMissingBuild);

        DateTime? comparisonDate = (_comparisonMonth.HasValue && _comparisonDay.HasValue)
            ? new DateTime(2000, _comparisonMonth.Value, _comparisonDay.Value, 0, 0, 0, DateTimeKind.Unspecified)
            : null;

        return new ObservanceAdjustment
        {
            Key = key,
            Trigger = _trigger.Value,
            Action = _action.Value,
            DayOfWeek = _dayOfWeek,
            IsNonWorkingDay = _isNonWorkingDay,
            OffsetDays = _offsetDays,
            TerritoryCode = _territoryCode,
            CalendarType = _calendarType,
            EffectiveFromYear = _effectiveFromYear,
            EffectiveToYear = _effectiveToYear,
            ComparisonDate = comparisonDate,
            WeekOrdinal = _weekOrdinal,
            TargetRuleName = _targetRuleName,
            Priority = _priority,
            HandlerKey = _handlerKey,
            HandlerParameters = _handlerParameters is null
                ? null
                : new Dictionary<string, string>(_handlerParameters, StringComparer.Ordinal),
            MaxAdjustmentReachDays = _maxAdjustmentReachDays,
        };
    }

    /// <summary>
    /// Builds a schema-valid <c>&lt;Adjustment&gt;</c> <see cref="XElement" /> from the current builder state.
    /// </summary>
    /// <param name="key">The adjustment key. Must not be <see langword="null" /> or whitespace.</param>
    /// <param name="ns">The XML namespace for the <c>NotableDates</c> schema.</param>
    /// <returns>The constructed <see cref="XElement" />.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="When(AdjustmentTrigger)" /> or <see cref="Action(AdjustmentAction)" /> has not been called.
    /// </exception>
    internal XElement ToXElement(string key, XNamespace ns)
    {
        if (_trigger is null)
            throw new InvalidOperationException(BuilderResourceStrings.Op_Invalid_AdjustmentTriggerMissingSerialise);
        if (_action is null)
            throw new InvalidOperationException(BuilderResourceStrings.Op_Invalid_AdjustmentActionMissingSerialise);

        XElement element = new(ns + "Adjustment",
            new XAttribute("key", key),
            new XAttribute("when", _trigger.Value.ToString()),
            new XAttribute("action", _action.Value.ToString()));

        if (_dayOfWeek.HasValue)
            element.Add(new XAttribute("dayOfWeek", _dayOfWeek.Value.ToString()));

        if (_weekOrdinal.HasValue)
            element.Add(new XAttribute("weekOrdinal", _weekOrdinal.Value.ToString()));

        if (_offsetDays != 0)
            element.Add(new XAttribute("days", _offsetDays.ToString(CultureInfo.InvariantCulture)));

        if (_priority != 100)
            element.Add(new XAttribute("priority", _priority.ToString(CultureInfo.InvariantCulture)));

        if (_isNonWorkingDay.HasValue)
            element.Add(new XAttribute("nonWorking", _isNonWorkingDay.Value ? "true" : "false"));

        if (_territoryCode is not null)
            element.Add(new XAttribute("territory", _territoryCode));

        if (_calendarType is not null)
            element.Add(new XAttribute("calendarType", _calendarType.AssemblyQualifiedName ?? _calendarType.FullName ?? _calendarType.Name));

        if (_effectiveFromYear.HasValue)
            element.Add(new XAttribute("fromYear", _effectiveFromYear.Value.ToString(CultureInfo.InvariantCulture)));

        if (_effectiveToYear.HasValue)
            element.Add(new XAttribute("toYear", _effectiveToYear.Value.ToString(CultureInfo.InvariantCulture)));

        if (_comparisonMonth.HasValue && _comparisonDay.HasValue)
        {
            element.Add(new XAttribute("comparisonMonth", GregorianMonthName(_comparisonMonth.Value)));
            element.Add(new XAttribute("comparisonDay", _comparisonDay.Value.ToString(CultureInfo.InvariantCulture)));
        }

        if (_targetRuleName is not null)
            element.Add(new XAttribute("target", _targetRuleName));

        if (_handlerKey is not null)
            element.Add(new XAttribute("handlerKey", _handlerKey));

        return element;
    }

    /// <summary>
    /// Builds a schema-valid <c>adjustment</c> <see cref="JsonObject" /> from the current builder state.
    /// </summary>
    /// <param name="key">The adjustment key. Must not be <see langword="null" /> or whitespace.</param>
    /// <returns>The constructed <see cref="JsonObject" /> conforming to the adjustment entry of <c>NotableDates.schema.json</c>.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="When(AdjustmentTrigger)" /> or <see cref="Action(AdjustmentAction)" /> has not been called.
    /// </exception>
    internal JsonObject ToJsonNode(string key)
    {
        if (_trigger is null)
            throw new InvalidOperationException(BuilderResourceStrings.Op_Invalid_AdjustmentTriggerMissingSerialise);
        if (_action is null)
            throw new InvalidOperationException(BuilderResourceStrings.Op_Invalid_AdjustmentActionMissingSerialise);

        JsonObject node = new()
        {
            ["key"] = key,
            ["when"] = _trigger.Value.ToString(),
            ["action"] = _action.Value.ToString(),
        };

        if (_dayOfWeek.HasValue) node["dayOfWeek"] = _dayOfWeek.Value.ToString();
        if (_weekOrdinal.HasValue) node["weekOrdinal"] = _weekOrdinal.Value.ToString();
        if (_offsetDays != 0) node["days"] = _offsetDays;
        if (_priority != 100) node["priority"] = _priority;
        if (_isNonWorkingDay.HasValue) node["nonWorking"] = _isNonWorkingDay.Value;
        if (_territoryCode is not null) node["territory"] = _territoryCode;
        if (_calendarType is not null) node["calendarType"] = _calendarType.AssemblyQualifiedName ?? _calendarType.FullName ?? _calendarType.Name;
        if (_effectiveFromYear.HasValue) node["fromYear"] = _effectiveFromYear.Value;
        if (_effectiveToYear.HasValue) node["toYear"] = _effectiveToYear.Value;

        if (_comparisonMonth.HasValue && _comparisonDay.HasValue)
        {
            node["comparisonMonth"] = GregorianMonthName(_comparisonMonth.Value);
            node["comparisonDay"] = _comparisonDay.Value;
        }

        if (_targetRuleName is not null) node["target"] = _targetRuleName;
        if (_handlerKey is not null) node["handlerKey"] = _handlerKey;

        return node;
    }

    /// <summary>
    /// Converts a Gregorian month number (1–12) to its full English name.
    /// </summary>
    /// <param name="month">The month number.</param>
    /// <returns>The full English month name (e.g. <c>"March"</c>).</returns>
    private static string GregorianMonthName(int month) =>
        new DateTime(2000, month, 1).ToString("MMMM", CultureInfo.InvariantCulture);
}
