// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleBuilder.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Xml.Linq;

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
/// <seealso cref="NotableDateDefinitionBuilder" />
/// <seealso cref="AdjustmentPolicyBuilder" />
public sealed class NotableDateRuleBuilder
{
    /// <summary>
    /// The territory codes scoping the rule, in declaration order.
    /// </summary>
    private readonly List<string> _territories = new();

    /// <summary>
    /// The exclusive list of years for which the rule applies, in declaration order.
    /// </summary>
    private readonly List<int> _onlyYears = new();

    /// <summary>
    /// The years for which the rule is suppressed, in declaration order.
    /// </summary>
    private readonly List<int> _exceptYears = new();

    /// <summary>
    /// The rule-specific tags, in declaration order.
    /// </summary>
    private readonly List<string> _tags = new();

    /// <summary>
    /// The adjustment policy identifiers applied to the rule, in declaration order.
    /// </summary>
    private readonly List<string> _adjustments = new();

    /// <summary>
    /// The stable identifier of the rule within its concept.
    /// </summary>
    private string _id;

    /// <summary>
    /// The configured selection priority, or <see langword="null" /> when the schema default applies.
    /// </summary>
    private int? _priority;

    /// <summary>
    /// The configured category override, or <see langword="null" /> to inherit the concept's category.
    /// </summary>
    private NotableDateCategory? _category;

    /// <summary>
    /// The configured non-working override, or <see langword="null" /> to inherit the concept's default.
    /// </summary>
    private bool? _nonWorking;

    /// <summary>
    /// The configured duration override in days, or <see langword="null" /> to inherit the concept's default.
    /// </summary>
    private int? _durationDays;

    /// <summary>
    /// The optional authoring comment, or <see langword="null" /> when unset.
    /// </summary>
    private string? _comment;

    /// <summary>
    /// The applicability calendar system, or <see langword="null" /> when the Gregorian default applies.
    /// </summary>
    private CalendarSystem? _calendar;

    /// <summary>
    /// The inclusive lower year bound, or <see langword="null" /> when unbounded.
    /// </summary>
    private int? _fromYear;

    /// <summary>
    /// The inclusive upper year bound, or <see langword="null" /> when unbounded.
    /// </summary>
    private int? _toYear;

    /// <summary>
    /// The recurrence interval in years, or <see langword="null" /> when the rule applies every year.
    /// </summary>
    private int? _everyYears;

    /// <summary>
    /// The anchor year for interval recurrence, or <see langword="null" /> when unset.
    /// </summary>
    private int? _anchorYear;

    /// <summary>
    /// The single strategy element in the document namespace, or <see langword="null" /> until a strategy is set.
    /// </summary>
    private XElement? _strategy;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotableDateRuleBuilder" /> class.
    /// </summary>
    /// <param name="id">The stable identifier of the rule within its concept.</param>
    internal NotableDateRuleBuilder(string id)
    {
        this._id = id;
    }

    /// <summary>
    /// Gets the stable identifier of the rule.
    /// </summary>
    /// <returns>The rule identifier.</returns>
    internal string Id =>
        this._id;

    /// <summary>
    /// Gets the configured selection priority.
    /// </summary>
    /// <returns>The priority, or <see langword="null" /> when unset.</returns>
    internal int? Priority =>
        this._priority;

    /// <summary>
    /// Gets the configured category override.
    /// </summary>
    /// <returns>The category, or <see langword="null" /> when inherited.</returns>
    internal NotableDateCategory? Category =>
        this._category;

    /// <summary>
    /// Gets the configured non-working override.
    /// </summary>
    /// <returns>The flag, or <see langword="null" /> when inherited.</returns>
    internal bool? NonWorking =>
        this._nonWorking;

    /// <summary>
    /// Gets the configured duration override.
    /// </summary>
    /// <returns>The duration in days, or <see langword="null" /> when inherited.</returns>
    internal int? DurationDays =>
        this._durationDays;

    /// <summary>
    /// Gets the configured authoring comment.
    /// </summary>
    /// <returns>The comment, or <see langword="null" /> when unset.</returns>
    internal string? Comment =>
        this._comment;

    /// <summary>
    /// Gets the configured applicability calendar system.
    /// </summary>
    /// <returns>The calendar system, or <see langword="null" /> when the Gregorian default applies.</returns>
    internal CalendarSystem? Calendar =>
        this._calendar;

    /// <summary>
    /// Gets the inclusive lower year bound.
    /// </summary>
    /// <returns>The lower bound, or <see langword="null" /> when unbounded.</returns>
    internal int? FromYearValue =>
        this._fromYear;

    /// <summary>
    /// Gets the inclusive upper year bound.
    /// </summary>
    /// <returns>The upper bound, or <see langword="null" /> when unbounded.</returns>
    internal int? ToYearValue =>
        this._toYear;

    /// <summary>
    /// Gets the recurrence interval in years.
    /// </summary>
    /// <returns>The interval, or <see langword="null" /> when the rule applies every year.</returns>
    internal int? EveryYearsValue =>
        this._everyYears;

    /// <summary>
    /// Gets the anchor year for interval recurrence.
    /// </summary>
    /// <returns>The anchor year, or <see langword="null" /> when unset.</returns>
    internal int? AnchorYearValue =>
        this._anchorYear;

    /// <summary>
    /// Gets the territory codes scoping the rule.
    /// </summary>
    /// <returns>The territory codes; empty when the rule applies to all territories.</returns>
    internal IReadOnlyList<string> Territories =>
        this._territories;

    /// <summary>
    /// Gets the exclusive list of years for which the rule applies.
    /// </summary>
    /// <returns>The years; empty when no exclusive list is set.</returns>
    internal IReadOnlyList<int> OnlyYearsValues =>
        this._onlyYears;

    /// <summary>
    /// Gets the years for which the rule is suppressed.
    /// </summary>
    /// <returns>The years; empty when no exception list is set.</returns>
    internal IReadOnlyList<int> ExceptYearsValues =>
        this._exceptYears;

    /// <summary>
    /// Gets the rule-specific tags.
    /// </summary>
    /// <returns>The tags; empty when none are configured.</returns>
    internal IReadOnlyList<string> Tags =>
        this._tags;

    /// <summary>
    /// Gets the adjustment policy identifiers applied to the rule.
    /// </summary>
    /// <returns>The policy identifiers; empty when none are configured.</returns>
    internal IReadOnlyList<string> Adjustments =>
        this._adjustments;

    /// <summary>
    /// Gets the single strategy element in the document namespace.
    /// </summary>
    /// <returns>The strategy element, or <see langword="null" /> when no strategy is set.</returns>
    internal XElement? Strategy =>
        this._strategy;

    /// <summary>
    /// Gets a value indicating whether the applicability scope declares any values.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when at least one applicability value is set; otherwise, <see langword="false" />.
    /// </returns>
    internal bool HasApplicability =>
        this._calendar is not null
        || this._fromYear is not null
        || this._toYear is not null
        || this._everyYears is not null
        || this._anchorYear is not null
        || this._territories.Count > 0
        || this._onlyYears.Count > 0
        || this._exceptYears.Count > 0;

    /// <summary>
    /// Sets the selection priority of the rule.
    /// </summary>
    /// <param name="priority">The numeric selection priority.</param>
    /// <returns>The same <see cref="NotableDateRuleBuilder" /> instance, enabling chained calls.</returns>
    public NotableDateRuleBuilder WithPriority(int priority)
    {
        this._priority = priority;
        return this;
    }

    /// <summary>
    /// Sets the category override of the rule.
    /// </summary>
    /// <param name="category">The category that overrides the concept's category.</param>
    /// <returns>The same <see cref="NotableDateRuleBuilder" /> instance, enabling chained calls.</returns>
    public NotableDateRuleBuilder WithCategory(NotableDateCategory category)
    {
        this._category = category;
        return this;
    }

    /// <summary>
    /// Sets whether occurrences of the rule are non-working days.
    /// </summary>
    /// <param name="value">The non-working flag.</param>
    /// <returns>The same <see cref="NotableDateRuleBuilder" /> instance, enabling chained calls.</returns>
    public NotableDateRuleBuilder AsNonWorking(bool value = true)
    {
        this._nonWorking = value;
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

        this._durationDays = durationDays;
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

        this._comment = comment;
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

        this._tags.Add(tag);
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

        this._tags.Clear();
        this._tags.AddRange(tags);
        return this;
    }

    /// <summary>
    /// Sets the calendar system of the rule's applicability.
    /// </summary>
    /// <param name="calendar">The calendar system.</param>
    /// <returns>The same <see cref="NotableDateRuleBuilder" /> instance, enabling chained calls.</returns>
    public NotableDateRuleBuilder ForCalendar(CalendarSystem calendar)
    {
        this._calendar = calendar;
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

        this._territories.Add(code);
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

        this._territories.Clear();
        this._territories.AddRange(codes);
        return this;
    }

    /// <summary>
    /// Sets the inclusive lower year bound of the rule's applicability.
    /// </summary>
    /// <param name="year">The first year for which the rule applies.</param>
    /// <returns>The same <see cref="NotableDateRuleBuilder" /> instance, enabling chained calls.</returns>
    public NotableDateRuleBuilder FromYear(int year)
    {
        this._fromYear = year;
        return this;
    }

    /// <summary>
    /// Sets the inclusive upper year bound of the rule's applicability.
    /// </summary>
    /// <param name="year">The last year for which the rule applies.</param>
    /// <returns>The same <see cref="NotableDateRuleBuilder" /> instance, enabling chained calls.</returns>
    public NotableDateRuleBuilder ToYear(int year)
    {
        this._toYear = year;
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

        this._everyYears = years;
        return this;
    }

    /// <summary>
    /// Sets the anchor year used by interval recurrence.
    /// </summary>
    /// <param name="year">The anchor year.</param>
    /// <returns>The same <see cref="NotableDateRuleBuilder" /> instance, enabling chained calls.</returns>
    public NotableDateRuleBuilder AnchorYear(int year)
    {
        this._anchorYear = year;
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

        this._onlyYears.Clear();
        this._onlyYears.AddRange(years);
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

        this._exceptYears.Clear();
        this._exceptYears.AddRange(years);
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

        return this.Fixed(BuilderXml.GetMonthName(month), day, skipLeapMonth, sweepCalendarYears);
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

        XElement element = new(BuilderXml.Namespace + "Fixed", new XAttribute("month", month), new XAttribute("day", BuilderXml.Int(day)));
        if (skipLeapMonth) element.SetAttributeValue("skipLeapMonth", BuilderXml.Bool(true));
        if (sweepCalendarYears) element.SetAttributeValue("sweepCalendarYears", BuilderXml.Bool(true));

        return this.SetStrategy(element);
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
            BuilderXml.Namespace + "DayOfWeekInMonth",
            new XAttribute("month", BuilderXml.GetMonthName(month)),
            new XAttribute("dayOfWeek", dayOfWeek.ToString()),
            new XAttribute("weekOrdinal", weekOrdinal.ToString()));

        return this.SetStrategy(element);
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
            BuilderXml.Namespace + "WeekdayNearDate",
            new XAttribute("month", BuilderXml.GetMonthName(month)),
            new XAttribute("day", BuilderXml.Int(day)),
            new XAttribute("dayOfWeek", dayOfWeek.ToString()),
            new XAttribute("direction", direction.ToString()));

        return this.SetStrategy(element);
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
            BuilderXml.Namespace + "RelativeWeekdayInMonth",
            new XAttribute("month", BuilderXml.GetMonthName(month)),
            new XAttribute("dayOfWeek", dayOfWeek.ToString()),
            new XAttribute("weekOrdinal", weekOrdinal.ToString()),
            new XAttribute("relativeDayOfWeek", relativeDayOfWeek.ToString()),
            new XAttribute("direction", direction.ToString()));

        return this.SetStrategy(element);
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
            BuilderXml.Namespace + "OffsetFromRule",
            new XAttribute("notableDateRef", notableDateRef),
            new XAttribute("offsetDays", BuilderXml.Int(offsetDays)));
        if (!string.IsNullOrEmpty(ruleRef)) element.SetAttributeValue("ruleRef", ruleRef);

        return this.SetStrategy(element);
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

        XElement element = new(BuilderXml.Namespace + "Algorithm", new XAttribute("key", key));

        return this.SetStrategy(element);
    }

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

        this._adjustments.Add(policyRef);
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

        this._adjustments.Clear();
        this._adjustments.AddRange(policyRefs);
        return this;
    }

    /// <summary>
    /// Replaces the rule's strategy directly when reconstructing a builder from a parsed document.
    /// </summary>
    /// <param name="strategy">The strategy element in the document namespace, or <see langword="null" />.</param>
    internal void SetParsedStrategy(XElement? strategy) =>
        this._strategy = strategy;

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
        this._priority = priority;
        this._category = category;
        this._nonWorking = nonWorking;
        this._durationDays = durationDays;
        this._comment = comment;
        this._calendar = calendar;
        this._fromYear = fromYear;
        this._toYear = toYear;
        this._everyYears = everyYears;
        this._anchorYear = anchorYear;
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
        this._territories.Clear();
        this._territories.AddRange(territories);
        this._onlyYears.Clear();
        this._onlyYears.AddRange(onlyYears);
        this._exceptYears.Clear();
        this._exceptYears.AddRange(exceptYears);
        this._tags.Clear();
        this._tags.AddRange(tags);
        this._adjustments.Clear();
        this._adjustments.AddRange(adjustments);
    }

    /// <summary>
    /// Creates a deep copy of this rule builder.
    /// </summary>
    /// <returns>A new <see cref="NotableDateRuleBuilder" /> carrying the same configured state.</returns>
    internal NotableDateRuleBuilder Clone()
    {
        NotableDateRuleBuilder clone = new(this._id);
        clone.SetParsedScalars(
            this._priority,
            this._category,
            this._nonWorking,
            this._durationDays,
            this._comment,
            this._calendar,
            this._fromYear,
            this._toYear,
            this._everyYears,
            this._anchorYear);
        clone.SetParsedCollections(this._territories, this._onlyYears, this._exceptYears, this._tags, this._adjustments);
        clone._strategy = this._strategy is null ? null : new XElement(this._strategy);
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
        if (this._strategy is not null)
            throw new InvalidOperationException(BuilderResourceStrings.Op_Invalid_RuleStrategyAlreadySet);

        this._strategy = element;
        return this;
    }
}
