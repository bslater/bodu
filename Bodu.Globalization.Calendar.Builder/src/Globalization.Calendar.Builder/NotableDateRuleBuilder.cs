// // ---------------------------------------------------------------------------------------------------------------
// // <copyright file="NotableDateRuleBuilder.cs" company="PlaceholderCompany">
// //     Copyright (c) PlaceholderCompany. All rights reserved.
// // </copyright>
// // ---------------------------------------------------------------------------------------------------------------

using Bodu.Extensions;
using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json.Nodes;
using System.Xml.Linq;

namespace Bodu.Globalization.Calendar.Builder;

/// <summary>
/// Provides a fluent interface for constructing a single <see cref="NotableDateRule" /> and its corresponding XML
/// or JSON representation.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="NotableDateRuleBuilder" /> is obtained via
/// <see cref="NotableDateBuilder.AddRule(System.Action{NotableDateRuleBuilder})" />. The builder accumulates rule properties and
/// produces a domain object, a schema-valid <c>&lt;Rule&gt;</c> XML element, and a schema-valid <c>rule</c> JSON object when the
/// enclosing document is built.
/// </para>
/// <para>
/// Exactly one strategy method — <see cref="Fixed(int, int, bool, bool)" />, <see cref="Fixed(string, int, bool, bool)" />,
/// <see cref="DayOfWeekInMonth(int, DayOfWeek, WeekOfMonthOrdinal)" />, <see cref="OffsetFromAnchor(string, int)" />, or
/// <see cref="Algorithm(string?, Type?, string?, int?)" /> — must be called before the rule can be built or serialised.
/// Calling a strategy method when a strategy has already been selected throws
/// <see cref="InvalidOperationException" />; each builder instance commits to its strategy on the first
/// successful strategy call.
/// </para>
/// </remarks>
public sealed class NotableDateRuleBuilder
{
    // Common rule fields

    /// <summary>The rule-level identifier set via <see cref="RuleName(string)" />, or <see langword="null" /> when not authored.</summary>
    private string? _ruleName;

    /// <summary>The primary category set via <see cref="Category(NotableDateCategory)" />.</summary>
    private NotableDateCategory _category;

    /// <summary>The non-working flag set via <see cref="NonWorking(bool)" />, or <see langword="null" /> to defer to the calendar default.</summary>
    private bool? _isNonWorkingDay;

    /// <summary>The inclusive first applicable year set via <see cref="FirstYear(int)" />, or <see langword="null" /> for no lower bound.</summary>
    private int? _firstYear;

    /// <summary>The inclusive last applicable year set via <see cref="LastYear(int)" />, or <see langword="null" /> for no upper bound.</summary>
    private int? _lastYear;

    /// <summary>The recurrence cadence set via <see cref="OccurrenceYears(int)" />, or <see langword="null" /> to resolve every applicable year.</summary>
    private int? _occurrenceYears;

    /// <summary>The duration in days set via <see cref="Duration(int)" />, or <see langword="null" /> to default to <c>1</c> at build time.</summary>
    private int? _durationDays;

    /// <summary>The integer priority set via <see cref="Priority(int)" /> (lower wins), or <see langword="null" /> to default to <c>100</c> at build time.</summary>
    private int? _priority;

    /// <summary>The comma-separated territory list set via <see cref="Territory(string)" />, or <see langword="null" /> when unscoped.</summary>
    private string? _territoryCode;

    /// <summary>The calendar type set via <see cref="CalendarType(Type)" />, or <see langword="null" /> to indicate the Gregorian calendar.</summary>
    private Type? _calendarType;

    /// <summary>The free-form comment set via <see cref="Comment(string)" />, or <see langword="null" /> when none is authored.</summary>
    private string? _comment;

    /// <summary>The list of tag values accumulated by <see cref="AddTag(string)" />.</summary>
    private readonly List<string> _tags = [];

    /// <summary>The observance adjustments accumulated by <see cref="AddAdjustment(string, System.Action{ObservanceAdjustmentBuilder})" />, each paired with its merge key.</summary>
    private readonly List<(string Key, ObservanceAdjustmentBuilder Builder)> _adjustments = [];

    // Strategy discriminator

    /// <summary>The selected resolution strategy, set by the strategy-specific configurator (<see cref="Fixed(int, int, bool, bool)" />, <see cref="DayOfWeekInMonth(int, DayOfWeek, WeekOfMonthOrdinal)" />, <see cref="OffsetFromAnchor(string, int)" />, or <see cref="Algorithm(string?, Type?, string?, int?)" />).</summary>
    private DateResolutionStrategy? _strategy;

    // Fixed strategy

    /// <summary>The numeric month set by <see cref="Fixed(int, int, bool, bool)" /> when a Gregorian month number is supplied; mutually exclusive with <see cref="_fixedMonthToken" />.</summary>
    private int? _fixedMonthNumber;

    /// <summary>The textual month token set by <see cref="Fixed(string, int, bool, bool)" /> for non-Gregorian or alias-based months; mutually exclusive with <see cref="_fixedMonthNumber" />.</summary>
    private string? _fixedMonthToken;

    /// <summary>The day-of-month set by either <see cref="Fixed(int, int, bool, bool)" /> overload.</summary>
    private int? _fixedDay;

    /// <summary>The skip-leap-month flag set by either <see cref="Fixed(int, int, bool, bool)" /> overload, consumed by lunisolar resolution.</summary>
    private bool _skipLeapMonth;

    /// <summary>The sweep-calendar-years flag set by either <see cref="Fixed(int, int, bool, bool)" /> overload, consumed by calendars whose year boundary does not align with the Gregorian year.</summary>
    private bool _sweepCalendarYears;

    // DayOfWeekInMonth strategy

    /// <summary>The month component set by <see cref="DayOfWeekInMonth(int, DayOfWeek, WeekOfMonthOrdinal)" />.</summary>
    private int? _dowMonth;

    /// <summary>The weekday set by <see cref="DayOfWeekInMonth(int, DayOfWeek, WeekOfMonthOrdinal)" />.</summary>
    private DayOfWeek? _dowDayOfWeek;

    /// <summary>The ordinal occurrence within the month set by <see cref="DayOfWeekInMonth(int, DayOfWeek, WeekOfMonthOrdinal)" />.</summary>
    private WeekOfMonthOrdinal? _dowWeekOrdinal;

    // OffsetFromAnchor strategy

    /// <summary>The name of the anchor rule set by <see cref="OffsetFromAnchor(string, int)" />.</summary>
    private string? _anchorRuleName;

    /// <summary>The signed day offset relative to the anchor set by <see cref="OffsetFromAnchor(string, int)" />.</summary>
    private int? _offsetDays;

    // Algorithm strategy

    /// <summary>The algorithm registry key set by <see cref="Algorithm(string?, Type?, string?, int?)" />, or <see langword="null" /> when resolving via <see cref="_algorithmType" />.</summary>
    private string? _algorithmKey;

    /// <summary>The algorithm CLR type set by <see cref="Algorithm(string?, Type?, string?, int?)" />, or <see langword="null" /> when resolving via <see cref="_algorithmKey" />.</summary>
    private Type? _algorithmType;

    /// <summary>The optional month token forwarded to the algorithm constructor, set by <see cref="Algorithm(string?, Type?, string?, int?)" />.</summary>
    private string? _algorithmMonth;

    /// <summary>The optional day-of-month forwarded to the algorithm constructor, set by <see cref="Algorithm(string?, Type?, string?, int?)" />.</summary>
    private int? _algorithmDay;

    /// <summary>
    /// Sets the optional rule-level identifier. When a notable date is described by more than one rule, this name allows
    /// <c>&lt;Use&gt;</c> directives to target a specific variant.
    /// </summary>
    /// <param name="name">The rule-level identifier. Must not be <see langword="null" /> or whitespace.</param>
    /// <returns>This builder instance, for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name" /> is <see langword="null" />, empty, or whitespace.</exception>
    public NotableDateRuleBuilder RuleName(string name)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(name);

        _ruleName = name;

        return this;
    }

    /// <summary>
    /// Sets the primary category for the produced notable date.
    /// </summary>
    /// <param name="category">The category.</param>
    /// <returns>This builder instance, for method chaining.</returns>
    public NotableDateRuleBuilder Category(NotableDateCategory category)
    {
        _category = category;

        return this;
    }

    /// <summary>
    /// Sets whether the produced notable date is a non-working day.
    /// </summary>
    /// <param name="value"><see langword="true" /> to flag the date as non-working.</param>
    /// <returns>This builder instance, for method chaining.</returns>
    public NotableDateRuleBuilder NonWorking(bool value = true)
    {
        _isNonWorkingDay = value;

        return this;
    }

    /// <summary>
    /// Sets the inclusive first year the rule is applicable.
    /// </summary>
    /// <param name="year">The first applicable year.</param>
    /// <returns>This builder instance, for method chaining.</returns>
    public NotableDateRuleBuilder FirstYear(int year)
    {
        _firstYear = year;

        return this;
    }

    /// <summary>
    /// Sets the inclusive last year the rule is applicable.
    /// </summary>
    /// <param name="year">The last applicable year.</param>
    /// <returns>This builder instance, for method chaining.</returns>
    public NotableDateRuleBuilder LastYear(int year)
    {
        _lastYear = year;

        return this;
    }

    /// <summary>
    /// Sets the recurrence interval in years. The rule resolves only in years where
    /// <c>(year - firstYear) % occurrenceYears == 0</c>.
    /// </summary>
    /// <param name="years">The recurrence interval in years. Must be at least 1.</param>
    /// <returns>This builder instance, for method chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="years" /> is less than 1.</exception>
    public NotableDateRuleBuilder OccurrenceYears(int years)
    {
        ThrowHelper.ThrowIfLessThan(years, 1);

        _occurrenceYears = years;

        return this;
    }

    /// <summary>
    /// Sets the duration of the notable date in days. Values greater than one describe multi-day spans.
    /// </summary>
    /// <param name="days">The duration in days. Must be at least 1.</param>
    /// <returns>This builder instance, for method chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="days" /> is less than 1.</exception>
    public NotableDateRuleBuilder Duration(int days)
    {
        ThrowHelper.ThrowIfLessThan(days, 1);

        _durationDays = days;

        return this;
    }

    /// <summary>
    /// Scopes the rule to the specified territory codes.
    /// </summary>
    /// <param name="code">A comma-separated list of ISO 3166 territory codes (e.g. <c>"AU"</c>, <c>"AU-NSW,AU-VIC"</c>). Must not be <see langword="null" /> or whitespace.</param>
    /// <returns>This builder instance, for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="code" /> is <see langword="null" />, empty, or whitespace.</exception>
    public NotableDateRuleBuilder Territory(string code)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(code);

        _territoryCode = code;

        return this;
    }

    /// <summary>
    /// Sets the calendar system the rule is authored against (e.g. <see cref="System.Globalization.HebrewCalendar" />,
    /// <see cref="System.Globalization.HijriCalendar" />).
    /// </summary>
    /// <param name="calendarType">
    /// The CLR <see cref="Type" /> of the calendar, which must derive from <see cref="System.Globalization.Calendar" />.
    /// Must not be <see langword="null" />.
    /// </param>
    /// <returns>This builder instance, for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="calendarType" /> is <see langword="null" />.</exception>
    public NotableDateRuleBuilder CalendarType(Type calendarType)
    {
        ThrowHelper.ThrowIfNull(calendarType);

        _calendarType = calendarType;

        return this;
    }

    /// <summary>
    /// Sets the priority used to break ties when multiple rules resolve to the same date. Lower values win.
    /// </summary>
    /// <param name="priority">The priority.</param>
    /// <returns>This builder instance, for method chaining.</returns>
    public NotableDateRuleBuilder Priority(int priority)
    {
        _priority = priority;

        return this;
    }

    /// <summary>
    /// Sets an optional human-readable comment describing the rule's intent or provenance.
    /// </summary>
    /// <param name="comment">The comment text. Must not be <see langword="null" />.</param>
    /// <returns>This builder instance, for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="comment" /> is <see langword="null" />.</exception>
    public NotableDateRuleBuilder Comment(string comment)
    {
        ThrowHelper.ThrowIfNull(comment);

        _comment = comment;

        return this;
    }

    /// <summary>
    /// Adds a tag providing non-exclusive classification (e.g. <c>"Christian"</c>, <c>"Public"</c>, <c>"Federal"</c>).
    /// </summary>
    /// <param name="tag">The tag value. Must not be <see langword="null" /> or whitespace.</param>
    /// <returns>This builder instance, for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="tag" /> is <see langword="null" />, empty, or whitespace.</exception>
    public NotableDateRuleBuilder AddTag(string tag)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(tag);

        _tags.Add(tag);

        return this;
    }

    /// <summary>
    /// Selects the <see cref="DateResolutionStrategy.Fixed" /> strategy using a Gregorian month number and day.
    /// </summary>
    /// <param name="month">The Gregorian month (1–13).</param>
    /// <param name="day">The day of the month (1–31).</param>
    /// <param name="skipLeapMonth">
    /// When <see langword="true" />, the resolver skips any intercalary leap month when resolving against a lunisolar calendar.
    /// </param>
    /// <param name="sweepCalendarYears">
    /// When <see langword="true" />, the resolver checks both overlapping calendar years for non-Gregorian calendars
    /// (e.g. <see cref="System.Globalization.HijriCalendar" />, <see cref="System.Globalization.HebrewCalendar" />).
    /// </param>
    /// <returns>This builder instance, for method chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="month" /> is less than 1 or greater than 13, or <paramref name="day" /> is less than 1 or greater than 31.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a resolution strategy has already been selected for this rule.
    /// </exception>
    public NotableDateRuleBuilder Fixed(int month, int day, bool skipLeapMonth = false, bool sweepCalendarYears = false)
    {
        ThrowHelper.ThrowIfOutOfRange(month, 1, 13);
        ThrowHelper.ThrowIfOutOfRange(day, 1, 31);

        ThrowIfStrategyAlreadySet();

        _strategy = DateResolutionStrategy.Fixed;
        _fixedMonthNumber = month;
        _fixedMonthToken = null;
        _fixedDay = day;
        _skipLeapMonth = skipLeapMonth;
        _sweepCalendarYears = sweepCalendarYears;

        return this;
    }

    /// <summary>
    /// Selects the <see cref="DateResolutionStrategy.Fixed" /> strategy using a calendar-specific month token and day.
    /// </summary>
    /// <param name="monthToken">
    /// The month token in the calendar system — either an English Gregorian month name (<c>"January"</c>–<c>"December"</c>), an
    /// integer (1–13), a Hebrew fixed-position month name (<c>Tishri</c>–<c>AdarII</c>), or a leap-year-dependent Hebrew month alias
    /// (<c>LastAdar</c>, <c>Nisan</c>, <c>Iyar</c>, <c>Sivan</c>, <c>Tammuz</c>, <c>Av</c>, <c>Elul</c>). Must not be <see langword="null" /> or whitespace.
    /// </param>
    /// <param name="day">The day of the month in the calendar system (1–31).</param>
    /// <param name="skipLeapMonth">
    /// When <see langword="true" />, the resolver skips any intercalary leap month when resolving against a lunisolar calendar.
    /// </param>
    /// <param name="sweepCalendarYears">
    /// When <see langword="true" />, the resolver checks both overlapping calendar years for non-Gregorian calendars.
    /// </param>
    /// <returns>This builder instance, for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="monthToken" /> is <see langword="null" />, empty, or whitespace.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="day" /> is less than 1 or greater than 31.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a resolution strategy has already been selected for this rule.
    /// </exception>
    public NotableDateRuleBuilder Fixed(string monthToken, int day, bool skipLeapMonth = false, bool sweepCalendarYears = false)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(monthToken);
        ThrowHelper.ThrowIfOutOfRange(day, 1, 31);
        ThrowIfStrategyAlreadySet();

        _strategy = DateResolutionStrategy.Fixed;
        _fixedMonthToken = monthToken;
        _fixedMonthNumber = null;
        _fixedDay = day;
        _skipLeapMonth = skipLeapMonth;
        _sweepCalendarYears = sweepCalendarYears;

        return this;
    }

    /// <summary>
    /// Selects the <see cref="DateResolutionStrategy.DayOfWeekInMonth" /> strategy, resolving to the <em>n</em>th occurrence of a
    /// specified weekday within a given Gregorian month (for example, the second Monday of October).
    /// </summary>
    /// <param name="month">The Gregorian month (1–12).</param>
    /// <param name="dayOfWeek">The day of the week.</param>
    /// <param name="weekOrdinal">The ordinal occurrence within the month (e.g. <see cref="WeekOfMonthOrdinal.Second" />).</param>
    /// <returns>This builder instance, for method chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="month" /> is less than 1 or greater than 12.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a resolution strategy has already been selected for this rule.
    /// </exception>
    public NotableDateRuleBuilder DayOfWeekInMonth(int month, DayOfWeek dayOfWeek, WeekOfMonthOrdinal weekOrdinal)
    {
        ThrowHelper.ThrowIfOutOfRange(month, 1, 12);
        ThrowIfStrategyAlreadySet();

        _strategy = DateResolutionStrategy.DayOfWeekInMonth;
        _dowMonth = month;
        _dowDayOfWeek = dayOfWeek;
        _dowWeekOrdinal = weekOrdinal;

        return this;
    }

    /// <summary>
    /// Selects the <see cref="DateResolutionStrategy.OffsetFromAnchor" /> strategy, resolving by adding a fixed day offset to the
    /// date produced by another named rule.
    /// </summary>
    /// <param name="anchorRuleName">The canonical name of the anchor <see cref="NotableDateRule" />. Must not be <see langword="null" /> or whitespace.</param>
    /// <param name="offsetDays">The number of days to add to the anchor date. Negative values move the date backwards.</param>
    /// <returns>This builder instance, for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="anchorRuleName" /> is <see langword="null" />, empty, or whitespace.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a resolution strategy has already been selected for this rule.
    /// </exception>
    public NotableDateRuleBuilder OffsetFromAnchor(string anchorRuleName, int offsetDays)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(anchorRuleName);
        ThrowIfStrategyAlreadySet();

        _strategy = DateResolutionStrategy.OffsetFromAnchor;
        _anchorRuleName = anchorRuleName;
        _offsetDays = offsetDays;

        return this;
    }

    /// <summary>
    /// Selects the <see cref="DateResolutionStrategy.Algorithm" /> strategy, delegating date resolution to a registered
    /// <see cref="INotableDateAlgorithm" /> (for example, Easter Sunday or Lunar New Year).
    /// </summary>
    /// <param name="key">The registry key used to look up the algorithm. Either <paramref name="key" /> or <paramref name="algorithmType" /> must be supplied.</param>
    /// <param name="algorithmType">The CLR <see cref="Type" /> of the algorithm implementation, used as a fallback when <paramref name="key" /> is not registered.</param>
    /// <param name="month">An optional month token passed to the algorithm's two-argument constructor.</param>
    /// <param name="day">An optional day of month passed to the algorithm's two-argument constructor.</param>
    /// <returns>This builder instance, for method chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a resolution strategy has already been selected for this rule.
    /// </exception>
    public NotableDateRuleBuilder Algorithm(string? key = null, Type? algorithmType = null, string? month = null, int? day = null)
    {
        ThrowIfStrategyAlreadySet();

        _strategy = DateResolutionStrategy.Algorithm;
        _algorithmKey = key;
        _algorithmType = algorithmType;
        _algorithmMonth = month;
        _algorithmDay = day;

        return this;
    }

    /// <summary>
    /// Adds an <see cref="ObservanceAdjustment" /> to the rule using the specified key and configuration callback.
    /// </summary>
    /// <param name="key">
    /// The adjustment key used for inheritance merging. Must be unique within this rule's adjustment sequence. Must not be
    /// <see langword="null" /> or whitespace.
    /// </param>
    /// <param name="configure">A callback that configures the <see cref="ObservanceAdjustmentBuilder" />. Must not be <see langword="null" />.</param>
    /// <returns>This builder instance, for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="key" /> is <see langword="null" />, empty, or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configure" /> is <see langword="null" />.</exception>
    public NotableDateRuleBuilder AddAdjustment(string key, Action<ObservanceAdjustmentBuilder> configure)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(key);
        ThrowHelper.ThrowIfNull(configure);

        ObservanceAdjustmentBuilder builder = new();
        configure(builder);
        _adjustments.Add((key, builder));

        return this;
    }

    /// <summary>
    /// Removes the first authored tag whose value matches <paramref name="tag" /> using a case-insensitive comparison.
    /// </summary>
    /// <param name="tag">The tag value to remove. Must not be <see langword="null" /> or whitespace.</param>
    /// <returns>This builder instance, for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// No-op when no tag with the supplied value has been added. Comparison uses
    /// <see cref="StringComparer.OrdinalIgnoreCase" /> to match the case-insensitive semantics applied by
    /// <see cref="Build(string)" /> when projecting the tag set onto the immutable rule.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown when <paramref name="tag" /> is <see langword="null" />, empty, or whitespace.</exception>
    public NotableDateRuleBuilder RemoveTag(string tag)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(tag);

        var index = _tags.FindIndex(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase));
        if (index >= 0)
            _tags.RemoveAt(index);
        return this;
    }

    /// <summary>
    /// Removes every tag previously added via <see cref="AddTag(string)" />.
    /// </summary>
    /// <returns>This builder instance, for method chaining.</returns>
    public NotableDateRuleBuilder ClearTags()
    {
        _tags.Clear();
        return this;
    }

    /// <summary>
    /// Removes the adjustment authored against <paramref name="key" />.
    /// </summary>
    /// <param name="key">The adjustment key to remove. Must not be <see langword="null" /> or whitespace.</param>
    /// <returns>This builder instance, for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// No-op when no adjustment with the supplied key has been added. Comparison uses
    /// <see cref="StringComparer.OrdinalIgnoreCase" /> so that callers do not need to remember the exact casing
    /// of the original <see cref="AddAdjustment(string, Action{ObservanceAdjustmentBuilder})" /> call.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown when <paramref name="key" /> is <see langword="null" />, empty, or whitespace.</exception>
    public NotableDateRuleBuilder RemoveAdjustment(string key)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(key);

        var index = _adjustments.FindIndex(a => string.Equals(a.Key, key, StringComparison.OrdinalIgnoreCase));
        if (index >= 0)
            _adjustments.RemoveAt(index);
        return this;
    }

    /// <summary>
    /// Removes every adjustment previously added via
    /// <see cref="AddAdjustment(string, Action{ObservanceAdjustmentBuilder})" />.
    /// </summary>
    /// <returns>This builder instance, for method chaining.</returns>
    public NotableDateRuleBuilder ClearAdjustments()
    {
        _adjustments.Clear();
        return this;
    }

    /// <summary>
    /// Resets the previously selected resolution strategy and clears every strategy-specific field, so that
    /// <see cref="Fixed(int, int, bool, bool)" />, <see cref="Fixed(string, int, bool, bool)" />,
    /// <see cref="DayOfWeekInMonth(int, DayOfWeek, WeekOfMonthOrdinal)" />,
    /// <see cref="OffsetFromAnchor(string, int)" />, or <see cref="Algorithm(string?, Type?, string?, int?)" />
    /// can be called again on the same builder.
    /// </summary>
    /// <returns>This builder instance, for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// Intended for the template-factory pattern: clone a configured builder, call <see cref="ClearStrategy" /> on the
    /// copy, then re-author with a different strategy. Without an explicit reset the single-strategy invariant
    /// would reject the subsequent strategy call.
    /// </para>
    /// </remarks>
    public NotableDateRuleBuilder ClearStrategy()
    {
        _strategy = null;

        _fixedMonthNumber = null;
        _fixedMonthToken = null;
        _fixedDay = null;
        _skipLeapMonth = false;
        _sweepCalendarYears = false;

        _dowMonth = null;
        _dowDayOfWeek = null;
        _dowWeekOrdinal = null;

        _anchorRuleName = null;
        _offsetDays = null;

        _algorithmKey = null;
        _algorithmType = null;
        _algorithmMonth = null;
        _algorithmDay = null;

        return this;
    }

    /// <summary>
    /// Creates an independent copy of this builder, suitable for use as a template that can then be tweaked for
    /// per-variant rules without mutating the source.
    /// </summary>
    /// <returns>A new <see cref="NotableDateRuleBuilder" /> with the same configuration as this instance.</returns>
    /// <remarks>
    /// <para>
    /// Scalar fields are copied by value. Tags are duplicated into an independent list so additions on either side
    /// do not bleed across the boundary. Each authored adjustment is cloned via
    /// <see cref="ObservanceAdjustmentBuilder.Clone" />, so subsequent mutations on the cloned rule's adjustments
    /// remain isolated from the source.
    /// </para>
    /// </remarks>
    public NotableDateRuleBuilder Clone()
    {
        NotableDateRuleBuilder copy = new()
        {
            _ruleName = _ruleName,
            _category = _category,
            _isNonWorkingDay = _isNonWorkingDay,
            _firstYear = _firstYear,
            _lastYear = _lastYear,
            _occurrenceYears = _occurrenceYears,
            _durationDays = _durationDays,
            _priority = _priority,
            _territoryCode = _territoryCode,
            _calendarType = _calendarType,
            _comment = _comment,
            _strategy = _strategy,
            _fixedMonthNumber = _fixedMonthNumber,
            _fixedMonthToken = _fixedMonthToken,
            _fixedDay = _fixedDay,
            _skipLeapMonth = _skipLeapMonth,
            _sweepCalendarYears = _sweepCalendarYears,
            _dowMonth = _dowMonth,
            _dowDayOfWeek = _dowDayOfWeek,
            _dowWeekOrdinal = _dowWeekOrdinal,
            _anchorRuleName = _anchorRuleName,
            _offsetDays = _offsetDays,
            _algorithmKey = _algorithmKey,
            _algorithmType = _algorithmType,
            _algorithmMonth = _algorithmMonth,
            _algorithmDay = _algorithmDay,
        };

        copy._tags.AddRange(_tags);

        foreach ((var key, ObservanceAdjustmentBuilder adjustment) in _adjustments)
            copy._adjustments.Add((key, adjustment.Clone()));

        return copy;
    }

    /// <summary>
    /// Builds a <see cref="NotableDateRule" /> record from the current builder state.
    /// </summary>
    /// <param name="notableDateName">The canonical name of the notable date. Must not be <see langword="null" /> or whitespace.</param>
    /// <returns>The constructed <see cref="NotableDateRule" />.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="notableDateName" /> is <see langword="null" />, empty, or whitespace.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no resolution strategy has been selected.</exception>
    internal NotableDateRule Build(string notableDateName)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(notableDateName);

        if (_strategy is null)
            throw new InvalidOperationException($"A resolution strategy must be selected (Fixed, DayOfWeekInMonth, OffsetFromAnchor, or Algorithm) before building the rule for '{notableDateName}'.");

        var adjustments = _adjustments
            .Select(a => a.Builder.Build(a.Key))
            .ToImmutableArray();

        var tags = _tags.ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);

        NotableDateRule rule = new()
        {
            Name = notableDateName,
            RuleName = _ruleName,
            Strategy = _strategy.Value,
            Category = _category,
            IsNonWorkingDay = _isNonWorkingDay,
            FirstYear = _firstYear,
            LastYear = _lastYear,
            OccurrenceYears = _occurrenceYears,
            DurationDays = _durationDays ?? 1,
            Priority = _priority ?? 100,
            TerritoryCode = _territoryCode,
            CalendarType = _calendarType,
            Comment = _comment,
            Tags = tags,
            Adjustments = adjustments,
        };

        return _strategy.Value switch
        {
            DateResolutionStrategy.Fixed => BuildFixed(rule),
            DateResolutionStrategy.DayOfWeekInMonth => rule with
            {
                Month = _dowMonth,
                DayOfWeek = _dowDayOfWeek,
                WeekOrdinal = _dowWeekOrdinal,
            },
            DateResolutionStrategy.OffsetFromAnchor => rule with
            {
                AnchorRuleName = _anchorRuleName,
                OffsetDays = _offsetDays,
            },
            DateResolutionStrategy.Algorithm => rule with
            {
                AlgorithmKey = _algorithmKey,
                AlgorithmType = _algorithmType,
                AlgorithmMonth = _algorithmMonth,
                AlgorithmDay = _algorithmDay,
            },
            _ => throw new NotSupportedException($"Unsupported strategy: {_strategy.Value}"),
        };
    }

    /// <summary>
    /// Builds a schema-valid <c>&lt;Rule&gt;</c> <see cref="XElement" /> from the current builder state.
    /// </summary>
    /// <param name="notableDateName">
    /// The canonical name of the notable date. Used to generate the XML <c>name</c> attribute when no explicit
    /// <see cref="RuleName(string)" /> has been set.
    /// </param>
    /// <param name="ns">The XML namespace for the <c>NotableDates</c> schema.</param>
    /// <returns>The constructed <see cref="XElement" />.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no resolution strategy has been selected.</exception>
    internal XElement ToXElement(string notableDateName, XNamespace ns)
    {
        if (_strategy is null)
            throw new InvalidOperationException($"A resolution strategy must be selected before serialising the rule for '{notableDateName}'.");

        var effectiveName = _ruleName ?? GenerateDefaultRuleName(notableDateName);

        XElement element = new(ns + "Rule",
            new XAttribute("name", effectiveName),
            new XAttribute("category", _category.ToString()));

        if (_isNonWorkingDay.HasValue)
            element.Add(new XAttribute("nonWorking", _isNonWorkingDay.Value ? "true" : "false"));

        if (_firstYear.HasValue)
            element.Add(new XAttribute("firstYear", _firstYear.Value.ToString(CultureInfo.InvariantCulture)));

        if (_lastYear.HasValue)
            element.Add(new XAttribute("lastYear", _lastYear.Value.ToString(CultureInfo.InvariantCulture)));

        if (_occurrenceYears.HasValue)
            element.Add(new XAttribute("occurrenceYears", _occurrenceYears.Value.ToString(CultureInfo.InvariantCulture)));

        if (_durationDays.HasValue)
            element.Add(new XAttribute("durationDays", _durationDays.Value.ToString(CultureInfo.InvariantCulture)));

        if (_priority.HasValue)
            element.Add(new XAttribute("priority", _priority.Value.ToString(CultureInfo.InvariantCulture)));

        if (_calendarType is not null)
            element.Add(new XAttribute("calendarType", _calendarType.AssemblyQualifiedName ?? _calendarType.FullName ?? _calendarType.Name));

        if (_territoryCode is not null)
            element.Add(new XAttribute("territory", _territoryCode));

        if (_comment is not null)
            element.Add(new XAttribute("comment", _comment));

        element.Add(BuildStrategyElement(ns));

        foreach (var tag in _tags)
            element.Add(new XElement(ns + "Tag", tag));

        foreach ((var key, ObservanceAdjustmentBuilder builder) in _adjustments)
            element.Add(builder.ToXElement(key, ns));

        return element;
    }

    /// <summary>
    /// Builds a schema-valid <c>rule</c> <see cref="JsonObject" /> from the current builder state.
    /// </summary>
    /// <param name="notableDateName">
    /// The canonical name of the notable date. Used to generate the JSON <c>name</c> property when no explicit
    /// <see cref="RuleName(string)" /> has been set.
    /// </param>
    /// <returns>The constructed <see cref="JsonObject" /> conforming to the rule entry of <c>NotableDates.schema.json</c>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no resolution strategy has been selected.</exception>
    internal JsonObject ToJsonNode(string notableDateName)
    {
        if (_strategy is null)
            throw new InvalidOperationException($"A resolution strategy must be selected before serialising the rule for '{notableDateName}'.");

        var effectiveName = _ruleName ?? GenerateDefaultRuleName(notableDateName);

        JsonObject node = new()
        {
            ["name"] = effectiveName,
            ["category"] = _category.ToString(),
        };

        if (_isNonWorkingDay.HasValue) node["nonWorking"] = _isNonWorkingDay.Value;
        if (_firstYear.HasValue) node["firstYear"] = _firstYear.Value;
        if (_lastYear.HasValue) node["lastYear"] = _lastYear.Value;
        if (_occurrenceYears.HasValue) node["occurrenceYears"] = _occurrenceYears.Value;
        if (_durationDays.HasValue) node["durationDays"] = _durationDays.Value;
        if (_priority.HasValue) node["priority"] = _priority.Value;
        if (_calendarType is not null) node["calendarType"] = _calendarType.AssemblyQualifiedName ?? _calendarType.FullName ?? _calendarType.Name;
        if (_territoryCode is not null) node["territory"] = _territoryCode;
        if (_comment is not null) node["comment"] = _comment;

        switch (_strategy.Value)
        {
            case DateResolutionStrategy.Fixed:
                node["fixed"] = BuildFixedJsonNode();
                break;
            case DateResolutionStrategy.DayOfWeekInMonth:
                node["dayOfWeekInMonth"] = new JsonObject
                {
                    ["month"] = GregorianMonthName(_dowMonth!.Value),
                    ["dayOfWeek"] = _dowDayOfWeek!.Value.ToString(),
                    ["weekOrdinal"] = _dowWeekOrdinal!.Value.ToString(),
                };
                break;
            case DateResolutionStrategy.OffsetFromAnchor:
                node["offsetFromAnchor"] = new JsonObject
                {
                    ["name"] = _anchorRuleName!,
                    ["offset"] = _offsetDays!.Value,
                };
                break;
            case DateResolutionStrategy.Algorithm:
                node["algorithm"] = BuildAlgorithmJsonNode();
                break;
            default:
                throw new NotSupportedException($"Unsupported strategy: {_strategy.Value}");
        }

        if (_tags.Count > 0)
        {
            JsonArray tags = [];

            // JsonArray.Add<T>(T) creates a JsonValueCustomized<T> that requires a
            // TypeInfoResolver at serialisation time; the typed JsonValue.Create(string?)
            // overload yields a JsonValuePrimitive that serialises without one.
            foreach (var tag in _tags)
                tags.Add(JsonValue.Create(tag));
            node["tags"] = tags;
        }

        if (_adjustments.Count > 0)
        {
            JsonArray adjustments = [];
            foreach ((var key, ObservanceAdjustmentBuilder builder) in _adjustments)
                adjustments.Add(builder.ToJsonNode(key));
            node["adjustments"] = adjustments;
        }

        return node;
    }

    /// <summary>
    /// Converts a Gregorian month number (1–12) to its full English name.
    /// </summary>
    /// <param name="month">The month number.</param>
    /// <returns>The full English month name (e.g. <c>"March"</c>).</returns>
    private static string GregorianMonthName(int month) =>
        new DateTime(2000, month, 1).ToString("MMMM", CultureInfo.InvariantCulture);

    /// <summary>
    /// Applies the Fixed strategy fields to a base rule record.
    /// </summary>
    /// <param name="rule">The partially-populated rule.</param>
    /// <returns>The rule with Fixed strategy fields applied.</returns>
    private NotableDateRule BuildFixed(NotableDateRule rule)
    {
        if (_fixedMonthNumber.HasValue)
        {
            return rule with
            {
                Month = _fixedMonthNumber.Value,
                CalendarMonthAlias = null,
                Day = _fixedDay,
                SkipLeapMonth = _skipLeapMonth,
                SweepCalendarYears = _sweepCalendarYears,
            };
        }

        // String token — parse similar to the parser's ParseMonthToken logic.
        var token = _fixedMonthToken!;

        if (DateTime.TryParseExact(token, "MMMM", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsed))
        {
            return rule with
            {
                Month = parsed.Month,
                CalendarMonthAlias = null,
                Day = _fixedDay,
                SkipLeapMonth = _skipLeapMonth,
                SweepCalendarYears = _sweepCalendarYears,
            };
        }

        if (int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numeric) && numeric is >= 1 and <= 13)
        {
            return rule with
            {
                Month = numeric,
                CalendarMonthAlias = null,
                Day = _fixedDay,
                SkipLeapMonth = _skipLeapMonth,
                SweepCalendarYears = _sweepCalendarYears,
            };
        }

        var hebrewFixed = token switch
        {
            "Tishri" => 1,
            "Heshvan" => 2,
            "Kislev" => 3,
            "Tevet" => 4,
            "Shevat" => 5,
            "AdarI" => 6,
            "AdarII" => 7,
            _ => (int?)null,
        };

        if (hebrewFixed.HasValue)
        {
            return rule with
            {
                Month = hebrewFixed.Value,
                CalendarMonthAlias = null,
                Day = _fixedDay,
                SkipLeapMonth = _skipLeapMonth,
                SweepCalendarYears = _sweepCalendarYears,
            };
        }

        // Leap-year-dependent Hebrew month alias stored for runtime resolution.
        return rule with
        {
            Month = null,
            CalendarMonthAlias = token,
            Day = _fixedDay,
            SkipLeapMonth = _skipLeapMonth,
            SweepCalendarYears = _sweepCalendarYears,
        };
    }

    /// <summary>
    /// Builds the strategy-specific child element for a <c>&lt;Rule&gt;</c> element.
    /// </summary>
    /// <param name="ns">The XML namespace.</param>
    /// <returns>The strategy element.</returns>
    private XElement BuildStrategyElement(XNamespace ns) =>
        _strategy!.Value switch
        {
            DateResolutionStrategy.Fixed => BuildFixedElement(ns),
            DateResolutionStrategy.DayOfWeekInMonth => new XElement(
                ns + "DayOfWeekInMonth",
                new XAttribute("month", GregorianMonthName(_dowMonth!.Value)),
                new XAttribute("dayOfWeek", _dowDayOfWeek!.Value.ToString()),
                new XAttribute("weekOrdinal", _dowWeekOrdinal!.Value.ToString())),
            DateResolutionStrategy.OffsetFromAnchor => new XElement(
                ns + "OffsetFromAnchor",
                new XAttribute("name", _anchorRuleName!),
                new XAttribute("offset", _offsetDays!.Value.ToString(CultureInfo.InvariantCulture))),
            DateResolutionStrategy.Algorithm => BuildAlgorithmElement(ns),
            _ => throw new NotSupportedException($"Unsupported strategy: {_strategy.Value}"),
        };


    /// <summary>
    /// Builds a <c>&lt;Fixed&gt;</c> element from the current Fixed strategy fields.
    /// </summary>
    /// <param name="ns">The XML namespace.</param>
    /// <returns>The Fixed strategy element.</returns>
    private XElement BuildFixedElement(XNamespace ns)
    {
        var monthAttr = _fixedMonthNumber.HasValue
            ? GregorianMonthName(_fixedMonthNumber.Value)
            : _fixedMonthToken!;

        XElement element = new(ns + "Fixed",
            new XAttribute("month", monthAttr),
            new XAttribute("day", _fixedDay!.Value.ToString(CultureInfo.InvariantCulture)));

        if (_skipLeapMonth)
            element.Add(new XAttribute("skipLeapMonth", "true"));

        if (_sweepCalendarYears)
            element.Add(new XAttribute("sweepCalendarYears", "true"));

        return element;
    }

    /// <summary>
    /// Builds an <c>&lt;Algorithm&gt;</c> element from the current Algorithm strategy fields.
    /// </summary>
    /// <param name="ns">The XML namespace.</param>
    /// <returns>The Algorithm strategy element.</returns>
    private XElement BuildAlgorithmElement(XNamespace ns)
    {
        XElement element = new(ns + "Algorithm");

        if (_algorithmKey is not null)
            element.Add(new XAttribute("key", _algorithmKey));

        if (_algorithmType is not null)
            element.Add(new XAttribute("type", _algorithmType.AssemblyQualifiedName ?? _algorithmType.FullName ?? _algorithmType.Name));

        if (_algorithmMonth is not null)
            element.Add(new XAttribute("month", _algorithmMonth));

        if (_algorithmDay.HasValue)
            element.Add(new XAttribute("day", _algorithmDay.Value.ToString(CultureInfo.InvariantCulture)));

        return element;
    }

    /// <summary>
    /// Builds a <c>fixed</c> <see cref="JsonObject" /> from the current Fixed strategy fields.
    /// </summary>
    /// <returns>The Fixed strategy JSON object.</returns>
    private JsonObject BuildFixedJsonNode()
    {
        var monthAttr = _fixedMonthNumber.HasValue
            ? GregorianMonthName(_fixedMonthNumber.Value)
            : _fixedMonthToken!;

        JsonObject node = new()
        {
            ["month"] = monthAttr,
            ["day"] = _fixedDay!.Value,
        };

        if (_skipLeapMonth) node["skipLeapMonth"] = true;
        if (_sweepCalendarYears) node["sweepCalendarYears"] = true;

        return node;
    }

    /// <summary>
    /// Builds an <c>algorithm</c> <see cref="JsonObject" /> from the current Algorithm strategy fields.
    /// </summary>
    /// <returns>The Algorithm strategy JSON object.</returns>
    private JsonObject BuildAlgorithmJsonNode()
    {
        JsonObject node = [];

        if (_algorithmKey is not null) node["key"] = _algorithmKey;
        if (_algorithmType is not null) node["type"] = _algorithmType.AssemblyQualifiedName ?? _algorithmType.FullName ?? _algorithmType.Name;
        if (_algorithmMonth is not null) node["month"] = _algorithmMonth;
        if (_algorithmDay.HasValue) node["day"] = _algorithmDay.Value;

        return node;
    }

    /// <summary>
    /// Generates a descriptive rule name from the notable date name and active strategy when no explicit rule name is set.
    /// </summary>
    /// <param name="notableDateName">The canonical notable date name.</param>
    /// <returns>A descriptive rule name suitable for the XML <c>name</c> attribute.</returns>
    private string GenerateDefaultRuleName(string notableDateName)
    {
        var strategySuffix = _strategy!.Value switch
        {
            DateResolutionStrategy.Fixed => "Fixed",
            DateResolutionStrategy.DayOfWeekInMonth => "DayOfWeekInMonth",
            DateResolutionStrategy.OffsetFromAnchor => "OffsetFromAnchor",
            DateResolutionStrategy.Algorithm => "Algorithm",
            _ => _strategy.Value.ToString(),
        };

        return $"{notableDateName} ({strategySuffix})";
    }

    /// <summary>
    /// Throws when a resolution strategy has already been selected for this rule. Called by every strategy
    /// configurator to enforce that each builder instance commits to exactly one strategy.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when <see cref="_strategy" /> already has a value.</exception>
    private void ThrowIfStrategyAlreadySet()
    {
        if (_strategy.HasValue)
            throw new InvalidOperationException(
                $"A resolution strategy ('{_strategy.Value}') has already been selected for this rule; " +
                "each rule must specify exactly one strategy.");
    }
}
