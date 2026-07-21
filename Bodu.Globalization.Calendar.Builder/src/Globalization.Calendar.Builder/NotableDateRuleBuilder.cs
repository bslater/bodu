// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleBuilder.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Xml.Linq;
using Bodu.Globalization.Calendar.Algorithms;

namespace Bodu.Globalization.Calendar.Builder;

/// <summary>
/// Provides a fluent surface for authoring a single rule: its selection scalars, applicability scope, exactly one
/// calculation strategy, rule-specific tags, and the adjustment policies applied to its occurrences.
/// </summary>
/// <remarks>
/// <para>
/// A rule must declare exactly one calculation strategy — for example <see cref="Fixed(int, int, bool, bool)" />,
/// <see cref="DayOfWeekInMonth(int, DayOfWeek, WeekOrdinal)" />,
/// <see cref="WeekdayNearDate(int, int, DayOfWeek, WeekdayProximity)" />,
/// <see cref="OffsetFromRule(string, int, string)" />, or <see cref="Algorithm(string)" />. Scope the rule to a
/// calendar, territories, or a year window with the <c>For…</c> / <c>…Year(s)</c> members, and attach reusable
/// adjustment policies with <see cref="WithAdjustment(string)" />. Configure the builder inside
/// <see cref="NotableDateDefinitionBuilder.AddRule(string, System.Action{NotableDateRuleBuilder})" />.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // U.S. Thanksgiving: the fourth Thursday of November, scoped to the United States.
/// definition.AddRule("us", r => r
///     .ForTerritory("US")
///     .DayOfWeekInMonth(11, DayOfWeek.Thursday, WeekOrdinal.Fourth)
///     .AsNonWorking());
///]]>
/// </code>
/// </example>
/// <seealso cref="NotableDateDefinitionBuilder" /> <seealso cref="AdjustmentPolicyBuilder" />
/// <seealso href="../guides/calendar/rule-authoring.html">Authoring notable date rules (guide)</seealso>
/// <seealso href="../guides/calendar/rule-reference.html">NotableDateRule and adjustment-policy reference (guide)
/// </seealso>
public sealed class NotableDateRuleBuilder
{
    /// <summary>The territory codes scoping the rule, in declaration order.</summary>
    private readonly List<string> _territories = new();

    /// <summary>The exclusive list of years for which the rule applies, in declaration order.</summary>
    private readonly List<int> _onlyYears = new();

    /// <summary>The years for which the rule is suppressed, in declaration order.</summary>
    private readonly List<int> _exceptYears = new();

    /// <summary>The rule-specific tags, in declaration order.</summary>
    private readonly List<string> _tags = new();

    /// <summary>The adjustment policy identifiers applied to the rule, in declaration order.</summary>
    private readonly List<string> _adjustments = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="NotableDateRuleBuilder" /> class.
    /// </summary>
    /// <param name="id">The stable identifier of the rule within its concept.</param>
    internal NotableDateRuleBuilder(string id)
    {
        Id = id;
    }

    /// <summary>
    /// Gets the stable identifier of the rule.
    /// </summary>
    /// <value>The rule identifier.</value>
    internal string Id { get; }

    /// <summary>
    /// Gets the configured selection priority.
    /// </summary>
    /// <value>The priority, or <see langword="null" /> when unset.</value>
    internal int? Priority { get; private set; }

    /// <summary>
    /// Gets the configured category override.
    /// </summary>
    /// <value>The category, or <see langword="null" /> when inherited.</value>
    internal NotableDateCategory? Category { get; private set; }

    /// <summary>
    /// Gets the configured non-working override.
    /// </summary>
    /// <value>The flag, or <see langword="null" /> when inherited.</value>
    internal bool? NonWorking { get; private set; }

    /// <summary>
    /// Gets the configured duration override.
    /// </summary>
    /// <value>The duration in days, or <see langword="null" /> when inherited.</value>
    internal int? DurationDays { get; private set; }

    /// <summary>
    /// Gets the configured authoring comment.
    /// </summary>
    /// <value>The comment, or <see langword="null" /> when unset.</value>
    internal string? Comment { get; private set; }

    /// <summary>
    /// Gets the configured applicability calendar system.
    /// </summary>
    /// <value>The calendar system, or <see langword="null" /> when the Gregorian default applies.</value>
    internal CalendarSystem? Calendar { get; private set; }

    /// <summary>
    /// Gets the inclusive lower year bound.
    /// </summary>
    /// <value>The lower bound, or <see langword="null" /> when unbounded.</value>
    internal int? FromYearValue { get; private set; }

    /// <summary>
    /// Gets the inclusive upper year bound.
    /// </summary>
    /// <value>The upper bound, or <see langword="null" /> when unbounded.</value>
    internal int? ToYearValue { get; private set; }

    /// <summary>
    /// Gets the recurrence interval in years.
    /// </summary>
    /// <value>The interval, or <see langword="null" /> when the rule applies every year.</value>
    internal int? EveryYearsValue { get; private set; }

    /// <summary>
    /// Gets the anchor year for interval recurrence.
    /// </summary>
    /// <value>The anchor year, or <see langword="null" /> when unset.</value>
    internal int? AnchorYearValue { get; private set; }

    /// <summary>
    /// Gets the territory codes scoping the rule.
    /// </summary>
    /// <value>The territory codes; empty when the rule applies to all territories.</value>
    internal IReadOnlyList<string> Territories =>
        _territories;

    /// <summary>
    /// Gets the exclusive list of years for which the rule applies.
    /// </summary>
    /// <value>The years; empty when no exclusive list is set.</value>
    internal IReadOnlyList<int> OnlyYearsValues =>
        _onlyYears;

    /// <summary>
    /// Gets the years for which the rule is suppressed.
    /// </summary>
    /// <value>The years; empty when no exception list is set.</value>
    internal IReadOnlyList<int> ExceptYearsValues =>
        _exceptYears;

    /// <summary>
    /// Gets the rule-specific tags.
    /// </summary>
    /// <value>The tags; empty when none are configured.</value>
    internal IReadOnlyList<string> Tags =>
        _tags;

    /// <summary>
    /// Gets the adjustment policy identifiers applied to the rule.
    /// </summary>
    /// <value>The policy identifiers; empty when none are configured.</value>
    internal IReadOnlyList<string> Adjustments =>
        _adjustments;

    /// <summary>
    /// Gets the single strategy element in the document namespace.
    /// </summary>
    /// <value>The strategy element, or <see langword="null" /> when no strategy is set.</value>
    internal XElement? Strategy { get; private set; }

    /// <summary>
    /// Gets the single recurrence element in the document namespace.
    /// </summary>
    /// <value>The recurrence element, or <see langword="null" /> when no recurrence is set.</value>
    internal XElement? Recurrence { get; private set; }

    /// <summary>
    /// Gets the calculated end-date strategy element in the document namespace.
    /// </summary>
    /// <value>The end strategy element, or <see langword="null" /> when no calculated duration is set.</value>
    internal XElement? EndStrategy { get; private set; }

    /// <summary>
    /// Gets the start boundary of a calculated end-date duration.
    /// </summary>
    /// <value>The start boundary; <see cref="DateBoundary.Inclusive" /> by default.</value>
    internal DateBoundary EndStartBoundary { get; private set; } = DateBoundary.Inclusive;

    /// <summary>
    /// Gets the end boundary of a calculated end-date duration.
    /// </summary>
    /// <value>The end boundary; <see cref="DateBoundary.Inclusive" /> by default.</value>
    internal DateBoundary EndEndBoundary { get; private set; } = DateBoundary.Inclusive;

    /// <summary>
    /// Gets the end-date selection rule of a calculated end-date duration.
    /// </summary>
    /// <value>The selection rule; <see cref="EndDateSelection.FirstOnOrAfterStart" /> by default.</value>
    internal EndDateSelection EndSelectionValue { get; private set; } = EndDateSelection.FirstOnOrAfterStart;

    /// <summary>
    /// Gets a value indicating whether the applicability scope declares any values.
    /// </summary>
    /// <value>
    /// <see langword="true" /> when at least one applicability value is set; otherwise, <see langword="false" />.
    /// </value>
    internal bool HasApplicability =>
        Calendar is not null
        || FromYearValue is not null
        || ToYearValue is not null
        || EveryYearsValue is not null
        || AnchorYearValue is not null
        || _territories.Count > 0
        || _onlyYears.Count > 0
        || _exceptYears.Count > 0;

    /// <summary>
    /// Sets the selection priority of the rule.
    /// </summary>
    /// <param name="priority">The numeric selection priority.</param>
    /// <returns>The same <see cref="NotableDateRuleBuilder" /> instance, enabling chained calls.</returns>
    public NotableDateRuleBuilder WithPriority(int priority)
    {
        Priority = priority;
        return this;
    }

    /// <summary>
    /// Sets the category override of the rule.
    /// </summary>
    /// <param name="category">The category that overrides the concept's category.</param>
    /// <returns>The same <see cref="NotableDateRuleBuilder" /> instance, enabling chained calls.</returns>
    public NotableDateRuleBuilder WithCategory(NotableDateCategory category)
    {
        Category = category;
        return this;
    }

    /// <summary>
    /// Sets whether occurrences of the rule are non-working days.
    /// </summary>
    /// <param name="value">The non-working flag.</param>
    /// <returns>The same <see cref="NotableDateRuleBuilder" /> instance, enabling chained calls.</returns>
    public NotableDateRuleBuilder AsNonWorking(bool value = true)
    {
        NonWorking = value;
        return this;
    }

    /// <summary>
    /// Sets the duration override of the rule.
    /// </summary>
    /// <param name="durationDays">The duration in days.</param>
    /// <returns>The same <see cref="NotableDateRuleBuilder" /> instance, enabling chained calls.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="durationDays" /> is less than 1.</exception>
    public NotableDateRuleBuilder WithDurationDays(int durationDays)
    {
        ThrowHelper.ThrowIfLessThan(durationDays, 1);
        if (EndStrategy is not null)
            throw new InvalidOperationException(BuilderResourceStrings.Op_Invalid_RuleDurationAlreadySet);

        DurationDays = durationDays;
        return this;
    }

    /// <summary>
    /// Sets the authoring comment of the rule.
    /// </summary>
    /// <param name="comment">The comment text.</param>
    /// <returns>The same <see cref="NotableDateRuleBuilder" /> instance, enabling chained calls.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="comment" /> is <see langword="null" />.</exception>
    public NotableDateRuleBuilder WithComment(string comment)
    {
        ThrowHelper.ThrowIfNull(comment);

        Comment = comment;
        return this;
    }

    /// <summary>
    /// Adds a single tag to the rule.
    /// </summary>
    /// <param name="tag">The tag value.</param>
    /// <returns>The same <see cref="NotableDateRuleBuilder" /> instance, enabling chained calls.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="tag" /> is <see langword="null" />, empty, or white-space.
    /// </exception>
    public NotableDateRuleBuilder AddTag(string tag)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(tag);

        _tags.Add(tag);
        return this;
    }

    /// <summary>
    /// Replaces the rule's tags with the supplied collection.
    /// </summary>
    /// <param name="tags">The tag values.</param>
    /// <returns>The same <see cref="NotableDateRuleBuilder" /> instance, enabling chained calls.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="tags" /> is <see langword="null" />.</exception>
    public NotableDateRuleBuilder WithTags(params string[] tags)
    {
        ThrowHelper.ThrowIfNull(tags);

        _tags.Clear();
        _tags.AddRange(tags);
        return this;
    }

    /// <summary>
    /// Sets the calendar system of the rule's applicability.
    /// </summary>
    /// <param name="calendar">The calendar system.</param>
    /// <returns>The same <see cref="NotableDateRuleBuilder" /> instance, enabling chained calls.</returns>
    public NotableDateRuleBuilder ForCalendar(CalendarSystem calendar)
    {
        Calendar = calendar;
        return this;
    }

    /// <summary>
    /// Adds a single territory code to the rule's applicability.
    /// </summary>
    /// <param name="code">The ISO territory code, for example <c>"US"</c> or <c>"AU-WA"</c>.</param>
    /// <returns>The same <see cref="NotableDateRuleBuilder" /> instance, enabling chained calls.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="code" /> is <see langword="null" />, empty, or white-space.
    /// </exception>
    public NotableDateRuleBuilder ForTerritory(string code)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(code);

        _territories.Add(code);
        return this;
    }

    /// <summary>
    /// Replaces the rule's territory scope with the supplied collection of territory codes.
    /// </summary>
    /// <param name="codes">The ISO territory codes.</param>
    /// <returns>The same <see cref="NotableDateRuleBuilder" /> instance, enabling chained calls.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="codes" /> is <see langword="null" />.</exception>
    public NotableDateRuleBuilder ForTerritories(params string[] codes)
    {
        ThrowHelper.ThrowIfNull(codes);

        _territories.Clear();
        _territories.AddRange(codes);
        return this;
    }

    /// <summary>
    /// Sets the inclusive lower year bound of the rule's applicability.
    /// </summary>
    /// <param name="year">The first year for which the rule applies.</param>
    /// <returns>The same <see cref="NotableDateRuleBuilder" /> instance, enabling chained calls.</returns>
    public NotableDateRuleBuilder FromYear(int year)
    {
        FromYearValue = year;
        return this;
    }

    /// <summary>
    /// Sets the inclusive upper year bound of the rule's applicability.
    /// </summary>
    /// <param name="year">The last year for which the rule applies.</param>
    /// <returns>The same <see cref="NotableDateRuleBuilder" /> instance, enabling chained calls.</returns>
    public NotableDateRuleBuilder ToYear(int year)
    {
        ToYearValue = year;
        return this;
    }

    /// <summary>
    /// Sets the recurrence interval in years for the rule's applicability.
    /// </summary>
    /// <param name="years">The interval between applicable years.</param>
    /// <returns>The same <see cref="NotableDateRuleBuilder" /> instance, enabling chained calls.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="years" /> is less than 1.</exception>
    public NotableDateRuleBuilder EveryYears(int years)
    {
        ThrowHelper.ThrowIfLessThan(years, 1);

        EveryYearsValue = years;
        return this;
    }

    /// <summary>
    /// Sets the anchor year used by interval recurrence.
    /// </summary>
    /// <param name="year">The anchor year.</param>
    /// <returns>The same <see cref="NotableDateRuleBuilder" /> instance, enabling chained calls.</returns>
    public NotableDateRuleBuilder AnchorYear(int year)
    {
        AnchorYearValue = year;
        return this;
    }

    /// <summary>
    /// Replaces the rule's exclusive year list with the supplied years.
    /// </summary>
    /// <param name="years">The years for which the rule applies.</param>
    /// <returns>The same <see cref="NotableDateRuleBuilder" /> instance, enabling chained calls.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="years" /> is <see langword="null" />.</exception>
    public NotableDateRuleBuilder OnlyYears(params int[] years)
    {
        ThrowHelper.ThrowIfNull(years);

        _onlyYears.Clear();
        _onlyYears.AddRange(years);
        return this;
    }

    /// <summary>
    /// Replaces the rule's exception year list with the supplied years.
    /// </summary>
    /// <param name="years">The years for which the rule is suppressed.</param>
    /// <returns>The same <see cref="NotableDateRuleBuilder" /> instance, enabling chained calls.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="years" /> is <see langword="null" />.</exception>
    public NotableDateRuleBuilder ExceptYears(params int[] years)
    {
        ThrowHelper.ThrowIfNull(years);

        _exceptYears.Clear();
        _exceptYears.AddRange(years);
        return this;
    }

    /// <summary>
    /// Configures the rule with a fixed day-of-month strategy using a numeric month.
    /// </summary>
    /// <param name="month">The one-based month number.</param>
    /// <param name="day">The day of the month.</param>
    /// <param name="skipLeapMonth">A value indicating whether a leap month is skipped on lunisolar calendars.</param>
    /// <param name="sweepCalendarYears">
    /// A value indicating whether the date is swept across adjacent calendar years.
    /// </param>
    /// <returns>The same <see cref="NotableDateRuleBuilder" /> instance, enabling chained calls.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="month" /> is outside 1 to 12 or <paramref name="day" /> is outside 1 to 31.
    /// </exception>
    /// <exception cref="InvalidOperationException">A strategy has already been configured on this rule.</exception>
    public NotableDateRuleBuilder Fixed(int month, int day, bool skipLeapMonth = false, bool sweepCalendarYears = false)
    {
        ThrowHelper.ThrowIfLessThan(month, 1);
        ThrowHelper.ThrowIfGreaterThan(month, 12);
        ThrowHelper.ThrowIfLessThan(day, 1);
        ThrowHelper.ThrowIfGreaterThan(day, 31);

        return Fixed(BuilderXml.GetMonthName(month), day, skipLeapMonth, sweepCalendarYears);
    }

    /// <summary>
    /// Configures the rule with a fixed day-of-month strategy using a month name or numeric month token.
    /// </summary>
    /// <param name="month">The full English month name or numeric month token.</param>
    /// <param name="day">The day of the month.</param>
    /// <param name="skipLeapMonth">A value indicating whether a leap month is skipped on lunisolar calendars.</param>
    /// <param name="sweepCalendarYears">
    /// A value indicating whether the date is swept across adjacent calendar years.
    /// </param>
    /// <returns>The same <see cref="NotableDateRuleBuilder" /> instance, enabling chained calls.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="month" /> is <see langword="null" />, empty, or white-space.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="day" /> is outside 1 to 31.</exception>
    /// <exception cref="InvalidOperationException">A strategy has already been configured on this rule.</exception>
    public NotableDateRuleBuilder Fixed(string month, int day, bool skipLeapMonth = false, bool sweepCalendarYears = false)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(month);
        ThrowHelper.ThrowIfLessThan(day, 1);
        ThrowHelper.ThrowIfGreaterThan(day, 31);

        XElement element = new(BuilderXml.s_namespace + "Fixed", new XAttribute("month", month), new XAttribute("day", BuilderXml.Int(day)));
        if (skipLeapMonth) element.SetAttributeValue("skipLeapMonth", BuilderXml.Bool(true));
        if (sweepCalendarYears) element.SetAttributeValue("sweepCalendarYears", BuilderXml.Bool(true));

        return SetStrategy(element);
    }

    /// <summary>
    /// Configures the rule with a day-of-week-in-month strategy.
    /// </summary>
    /// <param name="month">The one-based month number.</param>
    /// <param name="dayOfWeek">The day of the week to select.</param>
    /// <param name="weekOrdinal">The ordinal position of the weekday within the month.</param>
    /// <returns>The same <see cref="NotableDateRuleBuilder" /> instance, enabling chained calls.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="month" /> is outside 1 to 12.</exception>
    /// <exception cref="InvalidOperationException">A strategy has already been configured on this rule.</exception>
    public NotableDateRuleBuilder DayOfWeekInMonth(int month, DayOfWeek dayOfWeek, WeekOrdinal weekOrdinal)
    {
        ThrowHelper.ThrowIfLessThan(month, 1);
        ThrowHelper.ThrowIfGreaterThan(month, 12);

        XElement element = new(
            BuilderXml.s_namespace + "DayOfWeekInMonth",
            new XAttribute("month", BuilderXml.GetMonthName(month)),
            new XAttribute("dayOfWeek", dayOfWeek.ToString()),
            new XAttribute("weekOrdinal", weekOrdinal.ToString()));

        return SetStrategy(element);
    }

    /// <summary>
    /// Configures the rule with a weekday-near-date strategy.
    /// </summary>
    /// <param name="month">The one-based month number.</param>
    /// <param name="day">The reference day of the month.</param>
    /// <param name="dayOfWeek">The day of the week to select.</param>
    /// <param name="direction">The proximity rule relative to the reference date.</param>
    /// <returns>The same <see cref="NotableDateRuleBuilder" /> instance, enabling chained calls.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="month" /> is outside 1 to 12 or <paramref name="day" /> is outside 1 to 31.
    /// </exception>
    /// <exception cref="InvalidOperationException">A strategy has already been configured on this rule.</exception>
    public NotableDateRuleBuilder WeekdayNearDate(int month, int day, DayOfWeek dayOfWeek, WeekdayProximity direction)
    {
        ThrowHelper.ThrowIfLessThan(month, 1);
        ThrowHelper.ThrowIfGreaterThan(month, 12);
        ThrowHelper.ThrowIfLessThan(day, 1);
        ThrowHelper.ThrowIfGreaterThan(day, 31);

        XElement element = new(
            BuilderXml.s_namespace + "WeekdayNearDate",
            new XAttribute("month", BuilderXml.GetMonthName(month)),
            new XAttribute("day", BuilderXml.Int(day)),
            new XAttribute("dayOfWeek", dayOfWeek.ToString()),
            new XAttribute("direction", direction.ToString()));

        return SetStrategy(element);
    }

    /// <summary>
    /// Configures the rule with a relative-weekday-in-month strategy.
    /// </summary>
    /// <param name="month">The one-based month number.</param>
    /// <param name="dayOfWeek">The anchor day of the week.</param>
    /// <param name="weekOrdinal">The ordinal position of the anchor weekday within the month.</param>
    /// <param name="relativeDayOfWeek">The weekday selected relative to the anchor.</param>
    /// <param name="direction">The proximity rule relative to the anchor weekday.</param>
    /// <returns>The same <see cref="NotableDateRuleBuilder" /> instance, enabling chained calls.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="month" /> is outside 1 to 12.</exception>
    /// <exception cref="InvalidOperationException">A strategy has already been configured on this rule.</exception>
    public NotableDateRuleBuilder RelativeWeekdayInMonth(int month, DayOfWeek dayOfWeek, WeekOrdinal weekOrdinal, DayOfWeek relativeDayOfWeek, WeekdayProximity direction)
    {
        ThrowHelper.ThrowIfLessThan(month, 1);
        ThrowHelper.ThrowIfGreaterThan(month, 12);

        XElement element = new(
            BuilderXml.s_namespace + "RelativeWeekdayInMonth",
            new XAttribute("month", BuilderXml.GetMonthName(month)),
            new XAttribute("dayOfWeek", dayOfWeek.ToString()),
            new XAttribute("weekOrdinal", weekOrdinal.ToString()),
            new XAttribute("relativeDayOfWeek", relativeDayOfWeek.ToString()),
            new XAttribute("direction", direction.ToString()));

        return SetStrategy(element);
    }

    /// <summary>
    /// Configures the rule with an offset-from-rule strategy that derives its date from another rule's occurrence.
    /// </summary>
    /// <param name="notableDateRef">The identifier of the concept whose rule supplies the anchor date.</param>
    /// <param name="offsetDays">The signed day offset from the anchor occurrence.</param>
    /// <param name="ruleRef">
    /// The identifier of the anchor rule within the referenced concept, or <see langword="null" />.
    /// </param>
    /// <returns>The same <see cref="NotableDateRuleBuilder" /> instance, enabling chained calls.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="notableDateRef" /> is <see langword="null" />, empty, or white-space.
    /// </exception>
    /// <exception cref="InvalidOperationException">A strategy has already been configured on this rule.</exception>
    public NotableDateRuleBuilder OffsetFromRule(string notableDateRef, int offsetDays, string? ruleRef = null)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(notableDateRef);

        XElement element = new(
            BuilderXml.s_namespace + "OffsetFromRule",
            new XAttribute("notableDateRef", notableDateRef),
            new XAttribute("offsetDays", BuilderXml.Int(offsetDays)));
        if (!string.IsNullOrEmpty(ruleRef)) element.SetAttributeValue("ruleRef", ruleRef);

        return SetStrategy(element);
    }

    /// <summary>
    /// Configures the rule with an algorithm strategy that resolves its date from a named built-in or custom algorithm.
    /// </summary>
    /// <param name="key">The algorithm key, for example <c>"western-easter"</c>.</param>
    /// <returns>The same <see cref="NotableDateRuleBuilder" /> instance, enabling chained calls.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="key" /> is <see langword="null" />, empty, or white-space.
    /// </exception>
    /// <exception cref="InvalidOperationException">A strategy has already been configured on this rule.</exception>
    public NotableDateRuleBuilder Algorithm(string key)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(key);

        XElement element = new(BuilderXml.s_namespace + "Algorithm", new XAttribute("key", key));

        return SetStrategy(element);
    }

    /// <summary>
    /// Configures the rule with an ordinal-day-of-month strategy.
    /// </summary>
    /// <param name="month">The one-based month number.</param>
    /// <param name="ordinal">The signed ordinal position of the day within the month.</param>
    /// <returns>The same <see cref="NotableDateRuleBuilder" /> instance, enabling chained calls.</returns>
    /// <exception cref="InvalidOperationException">
    /// An occurrence source has already been configured on this rule.
    /// </exception>
    public NotableDateRuleBuilder OrdinalDayOfMonth(int month, int ordinal) =>
        SetStrategy(new XElement(
            BuilderXml.s_namespace + "OrdinalDayOfMonth",
            new XAttribute("month", BuilderXml.GetMonthName(month)),
            new XAttribute("ordinal", BuilderXml.Int(ordinal))));

    /// <summary>
    /// Configures the rule with a day-of-year strategy.
    /// </summary>
    /// <param name="ordinal">The signed ordinal position of the day within the year.</param>
    /// <returns>The same <see cref="NotableDateRuleBuilder" /> instance, enabling chained calls.</returns>
    /// <exception cref="InvalidOperationException">
    /// An occurrence source has already been configured on this rule.
    /// </exception>
    public NotableDateRuleBuilder DayOfYear(int ordinal) =>
        SetStrategy(new XElement(BuilderXml.s_namespace + "DayOfYear", new XAttribute("ordinal", BuilderXml.Int(ordinal))));

    /// <summary>
    /// Configures the rule with an ISO-week-date strategy.
    /// </summary>
    /// <param name="week">The one-based ISO-8601 week number.</param>
    /// <param name="dayOfWeek">The weekday within the ISO week.</param>
    /// <returns>The same <see cref="NotableDateRuleBuilder" /> instance, enabling chained calls.</returns>
    /// <exception cref="InvalidOperationException">
    /// An occurrence source has already been configured on this rule.
    /// </exception>
    public NotableDateRuleBuilder IsoWeekDate(int week, DayOfWeek dayOfWeek) =>
        SetStrategy(new XElement(
            BuilderXml.s_namespace + "IsoWeekDate",
            new XAttribute("week", BuilderXml.Int(week)),
            new XAttribute("dayOfWeek", dayOfWeek.ToString())));

    /// <summary>
    /// Configures the rule with a weekday-near-rule strategy that seeks a weekday relative to another rule's
    /// occurrence.
    /// </summary>
    /// <param name="notableDateRef">The identifier of the referenced concept.</param>
    /// <param name="dayOfWeek">The weekday to seek.</param>
    /// <param name="direction">The proximity rule relative to the reference.</param>
    /// <param name="referenceYearOffset">The signed year offset applied to the reference.</param>
    /// <param name="ruleRef">The identifier of the referenced rule, or <see langword="null" />.</param>
    /// <returns>The same <see cref="NotableDateRuleBuilder" /> instance, enabling chained calls.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="notableDateRef" /> is <see langword="null" />, empty, or white-space.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// An occurrence source has already been configured on this rule.
    /// </exception>
    public NotableDateRuleBuilder WeekdayNearRule(string notableDateRef, DayOfWeek dayOfWeek, WeekdayProximity direction, int referenceYearOffset = 0, string? ruleRef = null)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(notableDateRef);

        XElement element = new(
            BuilderXml.s_namespace + "WeekdayNearRule",
            new XAttribute("notableDateRef", notableDateRef),
            new XAttribute("dayOfWeek", dayOfWeek.ToString()),
            new XAttribute("direction", direction.ToString()));
        if (referenceYearOffset != 0) element.SetAttributeValue("referenceYearOffset", BuilderXml.Int(referenceYearOffset));
        if (!string.IsNullOrEmpty(ruleRef)) element.SetAttributeValue("ruleRef", ruleRef);

        return SetStrategy(element);
    }

    /// <summary>
    /// Configures the rule with an nth-weekday-from-rule strategy.
    /// </summary>
    /// <param name="notableDateRef">The identifier of the referenced concept.</param>
    /// <param name="dayOfWeek">The weekday to seek.</param>
    /// <param name="ordinal">The signed ordinal count of matching weekdays from the reference.</param>
    /// <param name="referenceYearOffset">The signed year offset applied to the reference.</param>
    /// <param name="ruleRef">The identifier of the referenced rule, or <see langword="null" />.</param>
    /// <returns>The same <see cref="NotableDateRuleBuilder" /> instance, enabling chained calls.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="notableDateRef" /> is <see langword="null" />, empty, or white-space.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// An occurrence source has already been configured on this rule.
    /// </exception>
    public NotableDateRuleBuilder NthWeekdayFromRule(string notableDateRef, DayOfWeek dayOfWeek, int ordinal, int referenceYearOffset = 0, string? ruleRef = null)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(notableDateRef);

        XElement element = new(
            BuilderXml.s_namespace + "NthWeekdayFromRule",
            new XAttribute("notableDateRef", notableDateRef),
            new XAttribute("dayOfWeek", dayOfWeek.ToString()),
            new XAttribute("ordinal", BuilderXml.Int(ordinal)));
        if (referenceYearOffset != 0) element.SetAttributeValue("referenceYearOffset", BuilderXml.Int(referenceYearOffset));
        if (!string.IsNullOrEmpty(ruleRef)) element.SetAttributeValue("ruleRef", ruleRef);

        return SetStrategy(element);
    }

    /// <summary>
    /// Configures the rule with a working-day-offset-from-rule strategy.
    /// </summary>
    /// <param name="notableDateRef">The identifier of the referenced concept.</param>
    /// <param name="offsetWorkingDays">The signed number of working days from the reference.</param>
    /// <param name="referenceYearOffset">The signed year offset applied to the reference.</param>
    /// <param name="ruleRef">The identifier of the referenced rule, or <see langword="null" />.</param>
    /// <returns>The same <see cref="NotableDateRuleBuilder" /> instance, enabling chained calls.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="notableDateRef" /> is <see langword="null" />, empty, or white-space.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// An occurrence source has already been configured on this rule.
    /// </exception>
    public NotableDateRuleBuilder WorkingDayOffsetFromRule(string notableDateRef, int offsetWorkingDays, int referenceYearOffset = 0, string? ruleRef = null)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(notableDateRef);

        XElement element = new(
            BuilderXml.s_namespace + "WorkingDayOffsetFromRule",
            new XAttribute("notableDateRef", notableDateRef),
            new XAttribute("offsetWorkingDays", BuilderXml.Int(offsetWorkingDays)));
        if (referenceYearOffset != 0) element.SetAttributeValue("referenceYearOffset", BuilderXml.Int(referenceYearOffset));
        if (!string.IsNullOrEmpty(ruleRef)) element.SetAttributeValue("ruleRef", ruleRef);

        return SetStrategy(element);
    }

    /// <summary>
    /// Configures the rule with a working-day-in-month strategy.
    /// </summary>
    /// <param name="month">The one-based month number.</param>
    /// <param name="ordinal">The signed ordinal position of the working day within the month.</param>
    /// <returns>The same <see cref="NotableDateRuleBuilder" /> instance, enabling chained calls.</returns>
    /// <exception cref="InvalidOperationException">
    /// An occurrence source has already been configured on this rule.
    /// </exception>
    public NotableDateRuleBuilder WorkingDayInMonth(int month, int ordinal) =>
        SetStrategy(new XElement(
            BuilderXml.s_namespace + "WorkingDayInMonth",
            new XAttribute("month", BuilderXml.GetMonthName(month)),
            new XAttribute("ordinal", BuilderXml.Int(ordinal))));

    /// <summary>
    /// Configures the rule with a daily-interval recurrence.
    /// </summary>
    /// <param name="anchorDate">The anchor date that defines occurrence zero and the phase of the series.</param>
    /// <param name="intervalDays">The number of calendar days between consecutive occurrences.</param>
    /// <returns>The same <see cref="NotableDateRuleBuilder" /> instance, enabling chained calls.</returns>
    /// <exception cref="InvalidOperationException">
    /// An occurrence source has already been configured on this rule.
    /// </exception>
    public NotableDateRuleBuilder DailyInterval(DateOnly anchorDate, int intervalDays = 1)
    {
        XElement inner = new(BuilderXml.s_namespace + "DailyInterval", new XAttribute("anchorDate", FormatDate(anchorDate)));
        if (intervalDays != 1) inner.SetAttributeValue("intervalDays", BuilderXml.Int(intervalDays));

        return SetRecurrence(inner);
    }

    /// <summary>
    /// Configures the rule with a weekly recurrence on the supplied weekdays.
    /// </summary>
    /// <param name="daysOfWeek">The weekdays to generate occurrences on.</param>
    /// <param name="intervalWeeks">The number of weeks between consecutive occurrences.</param>
    /// <param name="anchorDate">The anchor date that phases each weekday series, or <see langword="null" />.</param>
    /// <returns>The same <see cref="NotableDateRuleBuilder" /> instance, enabling chained calls.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="daysOfWeek" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">
    /// An occurrence source has already been configured on this rule.
    /// </exception>
    public NotableDateRuleBuilder Weekly(IEnumerable<DayOfWeek> daysOfWeek, int intervalWeeks = 1, DateOnly? anchorDate = null)
    {
        ThrowHelper.ThrowIfNull(daysOfWeek);

        XElement inner = new(BuilderXml.s_namespace + "Weekly");
        if (intervalWeeks != 1) inner.SetAttributeValue("intervalWeeks", BuilderXml.Int(intervalWeeks));
        if (anchorDate is DateOnly anchor) inner.SetAttributeValue("anchorDate", FormatDate(anchor));
        foreach (DayOfWeek day in daysOfWeek)
            inner.Add(new XElement(BuilderXml.s_namespace + "Day", new XAttribute("dayOfWeek", day.ToString())));

        return SetRecurrence(inner);
    }

    /// <summary>
    /// Configures the rule with a monthly day-of-month recurrence.
    /// </summary>
    /// <param name="dayOfMonth">The one-based day of the month.</param>
    /// <param name="intervalMonths">The number of months between consecutive occurrences.</param>
    /// <param name="anchorDate">The anchor whose year and month define month zero, or <see langword="null" />.</param>
    /// <param name="invalidDayBehavior">How a month without the requested day is handled.</param>
    /// <returns>The same <see cref="NotableDateRuleBuilder" /> instance, enabling chained calls.</returns>
    /// <exception cref="InvalidOperationException">
    /// An occurrence source has already been configured on this rule.
    /// </exception>
    public NotableDateRuleBuilder MonthlyDay(int dayOfMonth, int intervalMonths = 1, DateOnly? anchorDate = null, InvalidDayOfMonthBehavior invalidDayBehavior = InvalidDayOfMonthBehavior.Skip)
    {
        XElement inner = new(BuilderXml.s_namespace + "MonthlyDay", new XAttribute("dayOfMonth", BuilderXml.Int(dayOfMonth)));
        if (intervalMonths != 1) inner.SetAttributeValue("intervalMonths", BuilderXml.Int(intervalMonths));
        if (anchorDate is DateOnly anchor) inner.SetAttributeValue("anchorDate", FormatDate(anchor));
        if (invalidDayBehavior != InvalidDayOfMonthBehavior.Skip) inner.SetAttributeValue("invalidDayBehavior", invalidDayBehavior.ToString());

        return SetRecurrence(inner);
    }

    /// <summary>
    /// Configures the rule with a monthly ordinal-weekday recurrence.
    /// </summary>
    /// <param name="dayOfWeek">The weekday to select in each participating month.</param>
    /// <param name="weekOrdinal">The ordinal position of the weekday within the month.</param>
    /// <param name="intervalMonths">The number of months between consecutive occurrences.</param>
    /// <param name="anchorDate">The anchor whose year and month define month zero, or <see langword="null" />.</param>
    /// <returns>The same <see cref="NotableDateRuleBuilder" /> instance, enabling chained calls.</returns>
    /// <exception cref="InvalidOperationException">
    /// An occurrence source has already been configured on this rule.
    /// </exception>
    public NotableDateRuleBuilder MonthlyWeekday(DayOfWeek dayOfWeek, WeekOrdinal weekOrdinal, int intervalMonths = 1, DateOnly? anchorDate = null)
    {
        XElement inner = new(
            BuilderXml.s_namespace + "MonthlyWeekday",
            new XAttribute("dayOfWeek", dayOfWeek.ToString()),
            new XAttribute("weekOrdinal", weekOrdinal.ToString()));
        if (intervalMonths != 1) inner.SetAttributeValue("intervalMonths", BuilderXml.Int(intervalMonths));
        if (anchorDate is DateOnly anchor) inner.SetAttributeValue("anchorDate", FormatDate(anchor));

        return SetRecurrence(inner);
    }

    /// <summary>
    /// Configures the rule with a calculated end-date duration whose end anchor is produced by a second strategy.
    /// </summary>
    /// <param name="endStrategy">A configurator that selects the end strategy via one strategy method.</param>
    /// <param name="startBoundary">Whether the start anchor is included in the span.</param>
    /// <param name="endBoundary">Whether the end anchor is included in the span.</param>
    /// <param name="selection">How the end anchor is selected relative to the start anchor.</param>
    /// <returns>The same <see cref="NotableDateRuleBuilder" /> instance, enabling chained calls.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="endStrategy" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">
    /// A fixed duration is already configured, a calculated duration is already configured, or the configurator did not
    /// select a strategy.
    /// </exception>
    public NotableDateRuleBuilder UntilDate(Action<NotableDateRuleBuilder> endStrategy, DateBoundary startBoundary = DateBoundary.Inclusive, DateBoundary endBoundary = DateBoundary.Inclusive, EndDateSelection selection = EndDateSelection.FirstOnOrAfterStart)
    {
        ThrowHelper.ThrowIfNull(endStrategy);
        if (DurationDays is not null || EndStrategy is not null)
            throw new InvalidOperationException(BuilderResourceStrings.Op_Invalid_RuleDurationAlreadySet);

        NotableDateRuleBuilder capture = new("end");
        endStrategy(capture);
        if (capture.Strategy is not XElement selected)
            throw new InvalidOperationException(BuilderResourceStrings.Op_Invalid_RuleDurationAlreadySet);

        EndStrategy = selected;
        EndStartBoundary = startBoundary;
        EndEndBoundary = endBoundary;
        EndSelectionValue = selection;
        return this;
    }

    /// <summary>
    /// Formats a date as an ISO-8601 <c>yyyy-MM-dd</c> token.
    /// </summary>
    /// <param name="date">The date to format.</param>
    /// <returns>The ISO-8601 date token.</returns>
    private static string FormatDate(DateOnly date) =>
        date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    /// <summary>
    /// Adds a single adjustment policy reference to the rule.
    /// </summary>
    /// <param name="policyRef">The identifier of the adjustment policy to apply.</param>
    /// <returns>The same <see cref="NotableDateRuleBuilder" /> instance, enabling chained calls.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="policyRef" /> is <see langword="null" />, empty, or white-space.
    /// </exception>
    public NotableDateRuleBuilder WithAdjustment(string policyRef)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(policyRef);

        _adjustments.Add(policyRef);
        return this;
    }

    /// <summary>
    /// Replaces the rule's adjustment policy references with the supplied collection.
    /// </summary>
    /// <param name="policyRefs">The identifiers of the adjustment policies to apply.</param>
    /// <returns>The same <see cref="NotableDateRuleBuilder" /> instance, enabling chained calls.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="policyRefs" /> is <see langword="null" />.</exception>
    public NotableDateRuleBuilder WithAdjustments(params string[] policyRefs)
    {
        ThrowHelper.ThrowIfNull(policyRefs);

        _adjustments.Clear();
        _adjustments.AddRange(policyRefs);
        return this;
    }

    /// <summary>
    /// Replaces the rule's strategy directly when reconstructing a builder from a parsed document.
    /// </summary>
    /// <param name="strategy">The strategy element in the document namespace, or <see langword="null" />.</param>
    internal void SetParsedStrategy(XElement? strategy) =>
        Strategy = strategy;

    /// <summary>
    /// Replaces the rule's recurrence directly when reconstructing a builder from a parsed document.
    /// </summary>
    /// <param name="recurrence">
    /// The <c>Recurrence</c> element in the document namespace, or <see langword="null" />.
    /// </param>
    internal void SetParsedRecurrence(XElement? recurrence) =>
        Recurrence = recurrence;

    /// <summary>
    /// Replaces the rule's calculated end-date duration directly when reconstructing a builder from a parsed document.
    /// </summary>
    /// <param name="endStrategy">
    /// The end strategy element, or <see langword="null" /> when there is no calculated duration.
    /// </param>
    /// <param name="startBoundary">The start boundary.</param>
    /// <param name="endBoundary">The end boundary.</param>
    /// <param name="selection">The end-date selection rule.</param>
    internal void SetParsedDuration(XElement? endStrategy, DateBoundary startBoundary, DateBoundary endBoundary, EndDateSelection selection)
    {
        EndStrategy = endStrategy;
        EndStartBoundary = startBoundary;
        EndEndBoundary = endBoundary;
        EndSelectionValue = selection;
    }

    /// <summary>
    /// Sets the applicability and scalar state directly when reconstructing a builder from a parsed document.
    /// </summary>
    /// <param name="priority">The selection priority, or <see langword="null" />.</param>
    /// <param name="category">The category override, or <see langword="null" />.</param>
    /// <param name="nonWorking">The non-working override, or <see langword="null" />.</param>
    /// <param name="durationDays">The duration override, or <see langword="null" />.</param>
    /// <param name="comment">The authoring comment, or <see langword="null" />.</param>
    /// <param name="calendar">The applicability calendar system, or <see langword="null" />.</param>
    /// <param name="fromYear">The inclusive lower year bound, or <see langword="null" />.</param>
    /// <param name="toYear">The inclusive upper year bound, or <see langword="null" />.</param>
    /// <param name="everyYears">The recurrence interval in years, or <see langword="null" />.</param>
    /// <param name="anchorYear">The anchor year, or <see langword="null" />.</param>
    internal void SetParsedScalars(
        int? priority,
        NotableDateCategory? category,
        bool? nonWorking,
        int? durationDays,
        string? comment,
        CalendarSystem? calendar,
        int? fromYear,
        int? toYear,
        int? everyYears,
        int? anchorYear)
    {
        Priority = priority;
        Category = category;
        NonWorking = nonWorking;
        DurationDays = durationDays;
        Comment = comment;
        Calendar = calendar;
        FromYearValue = fromYear;
        ToYearValue = toYear;
        EveryYearsValue = everyYears;
        AnchorYearValue = anchorYear;
    }

    /// <summary>
    /// Appends parsed collection state when reconstructing a builder from a parsed document.
    /// </summary>
    /// <param name="territories">The territory codes.</param>
    /// <param name="onlyYears">The exclusive year list.</param>
    /// <param name="exceptYears">The exception year list.</param>
    /// <param name="tags">The rule-specific tags.</param>
    /// <param name="adjustments">The adjustment policy references.</param>
    internal void SetParsedCollections(
        IEnumerable<string> territories,
        IEnumerable<int> onlyYears,
        IEnumerable<int> exceptYears,
        IEnumerable<string> tags,
        IEnumerable<string> adjustments)
    {
        _territories.Clear();
        _territories.AddRange(territories);
        _onlyYears.Clear();
        _onlyYears.AddRange(onlyYears);
        _exceptYears.Clear();
        _exceptYears.AddRange(exceptYears);
        _tags.Clear();
        _tags.AddRange(tags);
        _adjustments.Clear();
        _adjustments.AddRange(adjustments);
    }

    /// <summary>
    /// Creates a deep copy of this rule builder.
    /// </summary>
    /// <returns>A new <see cref="NotableDateRuleBuilder" /> carrying the same configured state.</returns>
    internal NotableDateRuleBuilder Clone()
    {
        NotableDateRuleBuilder clone = new(Id);
        clone.SetParsedScalars(
            Priority,
            Category,
            NonWorking,
            DurationDays,
            Comment,
            Calendar,
            FromYearValue,
            ToYearValue,
            EveryYearsValue,
            AnchorYearValue);
        clone.SetParsedCollections(_territories, _onlyYears, _exceptYears, _tags, _adjustments);
        clone.Strategy = Strategy is null ? null : new XElement(Strategy);
        clone.Recurrence = Recurrence is null ? null : new XElement(Recurrence);
        clone.EndStrategy = EndStrategy is null ? null : new XElement(EndStrategy);
        clone.EndStartBoundary = EndStartBoundary;
        clone.EndEndBoundary = EndEndBoundary;
        clone.EndSelectionValue = EndSelectionValue;
        return clone;
    }

    /// <summary>
    /// Stores the single strategy element, enforcing the one-strategy-per-rule invariant.
    /// </summary>
    /// <param name="element">The strategy element to store.</param>
    /// <returns>The same <see cref="NotableDateRuleBuilder" /> instance, enabling chained calls.</returns>
    /// <exception cref="InvalidOperationException">A strategy has already been configured on this rule.</exception>
    private NotableDateRuleBuilder SetStrategy(XElement element)
    {
        if (Strategy is not null || Recurrence is not null)
            throw new InvalidOperationException(BuilderResourceStrings.Op_Invalid_RuleStrategyAlreadySet);

        Strategy = element;
        return this;
    }

    /// <summary>
    /// Stores the single recurrence element wrapped in a <c>Recurrence</c> element, enforcing the one-occurrence-source
    /// invariant.
    /// </summary>
    /// <param name="inner">The recurrence kind element to store.</param>
    /// <returns>The same <see cref="NotableDateRuleBuilder" /> instance, enabling chained calls.</returns>
    /// <exception cref="InvalidOperationException">
    /// An occurrence source has already been configured on this rule.
    /// </exception>
    private NotableDateRuleBuilder SetRecurrence(XElement inner)
    {
        if (Strategy is not null || Recurrence is not null)
            throw new InvalidOperationException(BuilderResourceStrings.Op_Invalid_RuleStrategyAlreadySet);

        Recurrence = new XElement(BuilderXml.s_namespace + "Recurrence", inner);
        return this;
    }
}
