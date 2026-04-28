// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleBuilder.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Extensions;
using System.Collections.Immutable;
using System.Globalization;
using System.Xml.Linq;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Provides a fluent interface for constructing a single <see cref="NotableDateRule" /> and its corresponding XML representation.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="NotableDateRuleBuilder" /> is obtained via
/// <see cref="NotableDateBuilder.AddRule(System.Action{NotableDateRuleBuilder})" />. The builder accumulates rule properties and
/// produces both a domain object and a schema-valid <c>&lt;Rule&gt;</c> XML element when the enclosing document is built.
/// </para>
/// <para>
/// Exactly one strategy method — <see cref="Fixed(int, int, bool, bool)" />, <see cref="Fixed(string, int, bool, bool)" />,
/// <see cref="DayOfWeekInMonth(int, DayOfWeek, WeekOfMonthOrdinal)" />, <see cref="OffsetFromAnchor(string, int)" />, or
/// <see cref="Algorithm(string?, Type?, string?, int?)" /> — must be called before the rule can be built or serialised.
/// </para>
/// </remarks>
public sealed class NotableDateRuleBuilder
{
    // Common rule fields
    private string? _ruleName;
    private NotableDateCategory _category;
    private bool? _isNonWorkingDay;
    private int? _firstYear;
    private int? _lastYear;
    private int? _occurrenceYears;
    private int? _durationDays;
    private int? _priority;
    private string? _territoryCode;
    private Type? _calendarType;
    private string? _comment;
    private readonly List<string> _tags = [];
    private readonly List<(string Key, ObservanceAdjustmentBuilder Builder)> _adjustments = [];

    // Strategy discriminator
    private DateResolutionStrategy? _strategy;

    // Fixed strategy
    private int? _fixedMonthNumber;
    private string? _fixedMonthToken;
    private int? _fixedDay;
    private bool _skipLeapMonth;
    private bool _sweepCalendarYears;

    // DayOfWeekInMonth strategy
    private int? _dowMonth;
    private DayOfWeek? _dowDayOfWeek;
    private WeekOfMonthOrdinal? _dowWeekOrdinal;

    // OffsetFromAnchor strategy
    private string? _anchorRuleName;
    private int? _offsetDays;

    // Algorithm strategy
    private string? _algorithmKey;
    private Type? _algorithmType;
    private string? _algorithmMonth;
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
    public NotableDateRuleBuilder Fixed(int month, int day, bool skipLeapMonth = false, bool sweepCalendarYears = false)
    {
        ThrowHelper.ThrowIfLessThan(month, 1);
        ThrowHelper.ThrowIfGreaterThan(month, 13);
        ThrowHelper.ThrowIfLessThan(day, 1);
        ThrowHelper.ThrowIfGreaterThan(day, 31);
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
    public NotableDateRuleBuilder Fixed(string monthToken, int day, bool skipLeapMonth = false, bool sweepCalendarYears = false)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(monthToken);
        ThrowHelper.ThrowIfLessThan(day, 1);
        ThrowHelper.ThrowIfGreaterThan(day, 31);
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
    public NotableDateRuleBuilder DayOfWeekInMonth(int month, DayOfWeek dayOfWeek, WeekOfMonthOrdinal weekOrdinal)
    {
        ThrowHelper.ThrowIfLessThan(month, 1);
        ThrowHelper.ThrowIfGreaterThan(month, 12);
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
    public NotableDateRuleBuilder OffsetFromAnchor(string anchorRuleName, int offsetDays)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(anchorRuleName);
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
    public NotableDateRuleBuilder Algorithm(string? key = null, Type? algorithmType = null, string? month = null, int? day = null)
    {
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

        ImmutableArray<ObservanceAdjustment> adjustments = _adjustments
            .Select(a => a.Builder.Build(a.Key))
            .ToImmutableArray();

        ImmutableHashSet<string> tags = _tags.ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);

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

        string effectiveName = _ruleName ?? GenerateDefaultRuleName(notableDateName);

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

        foreach (string tag in _tags)
            element.Add(new XElement(ns + "Tag", tag));

        foreach ((string key, ObservanceAdjustmentBuilder builder) in _adjustments)
            element.Add(builder.ToXElement(key, ns));

        return element;
    }

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
        string token = _fixedMonthToken!;

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

        if (int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out int numeric) && numeric is >= 1 and <= 13)
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

        int? hebrewFixed = token switch
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
            DateResolutionStrategy.DayOfWeekInMonth => new XElement(ns + "DayOfWeekInMonth",
                new XAttribute("month", GregorianMonthName(_dowMonth!.Value)),
                new XAttribute("dayOfWeek", _dowDayOfWeek!.Value.ToString()),
                new XAttribute("weekOrdinal", _dowWeekOrdinal!.Value.ToString())),
            DateResolutionStrategy.OffsetFromAnchor => new XElement(ns + "OffsetFromAnchor",
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
        string monthAttr = _fixedMonthNumber.HasValue
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
    /// Generates a descriptive rule name from the notable date name and active strategy when no explicit rule name is set.
    /// </summary>
    /// <param name="notableDateName">The canonical notable date name.</param>
    /// <returns>A descriptive rule name suitable for the XML <c>name</c> attribute.</returns>
    private string GenerateDefaultRuleName(string notableDateName)
    {
        string strategySuffix = _strategy!.Value switch
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
    /// Converts a Gregorian month number (1–12) to its full English name.
    /// </summary>
    /// <param name="month">The month number.</param>
    /// <returns>The full English month name (e.g. <c>"March"</c>).</returns>
    private static string GregorianMonthName(int month) =>
        new DateTime(2000, month, 1).ToString("MMMM", CultureInfo.InvariantCulture);
}
