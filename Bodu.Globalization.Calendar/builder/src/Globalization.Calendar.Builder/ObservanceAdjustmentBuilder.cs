// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ObservanceAdjustmentBuilder.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Extensions;
using System.Globalization;
using System.Xml.Linq;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Provides a fluent interface for constructing an <see cref="ObservanceAdjustment" /> and its corresponding XML representation.
/// </summary>
/// <remarks>
/// <para>
/// An <see cref="ObservanceAdjustmentBuilder" /> is obtained via
/// <see cref="NotableDateRuleBuilder.AddAdjustment(string, System.Action{ObservanceAdjustmentBuilder})" />. The builder accumulates
/// adjustment properties and produces both a domain object and a schema-valid <c>&lt;Adjustment&gt;</c> XML element when the
/// enclosing rule is built.
/// </para>
/// <para>
/// Only <see cref="When(AdjustmentTrigger)" /> and <see cref="Action(AdjustmentAction)" /> are required; all other properties are optional
/// and default to the same values as their <see cref="ObservanceAdjustment" /> counterparts.
/// </para>
/// </remarks>
public sealed class ObservanceAdjustmentBuilder
{
    private AdjustmentTrigger? _trigger;
    private AdjustmentAction? _action;
    private DayOfWeek? _dayOfWeek;
    private bool? _isNonWorkingDay;
    private int _offsetDays;
    private string? _territoryCode;
    private Type? _calendarType;
    private int? _effectiveFromYear;
    private int? _effectiveToYear;
    private int? _comparisonMonth;
    private int? _comparisonDay;
    private WeekOfMonthOrdinal? _weekOrdinal;
    private string? _targetRuleName;
    private int _priority = 100;
    private string? _handlerKey;

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
            throw new InvalidOperationException("An adjustment trigger must be set via When() before building.");
        if (_action is null)
            throw new InvalidOperationException("An adjustment action must be set via Action() before building.");

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
            throw new InvalidOperationException("An adjustment trigger must be set via When() before serialising.");
        if (_action is null)
            throw new InvalidOperationException("An adjustment action must be set via Action() before serialising.");

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
    /// Converts a Gregorian month number (1–12) to its full English name.
    /// </summary>
    /// <param name="month">The month number.</param>
    /// <returns>The full English month name (e.g. <c>"March"</c>).</returns>
    private static string GregorianMonthName(int month) =>
        new DateTime(2000, month, 1).ToString("MMMM", CultureInfo.InvariantCulture);
}
