// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdjustmentScopeBuilder.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Builder;

/// <summary>
/// Provides a fluent surface for narrowing the scope of an adjustment policy by territory, calendar, category, concept,
/// rule, and year, restricting the occurrences the policy may transform.
/// </summary>
public sealed class AdjustmentScopeBuilder
{
    /// <summary>
    /// The territory codes in scope, in declaration order.
    /// </summary>
    private readonly List<string> _territories = new();

    /// <summary>
    /// The calendar systems in scope, in declaration order.
    /// </summary>
    private readonly List<CalendarSystem> _calendars = new();

    /// <summary>
    /// The categories in scope, in declaration order.
    /// </summary>
    private readonly List<NotableDateCategory> _categories = new();

    /// <summary>
    /// The concept identifiers in scope, in declaration order.
    /// </summary>
    private readonly List<string> _notableDateRefs = new();

    /// <summary>
    /// The concept-and-rule pairs in scope, in declaration order.
    /// </summary>
    private readonly List<(string NotableDateRef, string RuleRef)> _ruleRefs = new();

    /// <summary>
    /// The explicit included years, in declaration order.
    /// </summary>
    private readonly List<int> _onlyYears = new();

    /// <summary>
    /// The explicit excluded years, in declaration order.
    /// </summary>
    private readonly List<int> _exceptYears = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="AdjustmentScopeBuilder" /> class with an empty scope.
    /// </summary>
    internal AdjustmentScopeBuilder()
    {
    }

    /// <summary>
    /// Gets the territory codes in scope.
    /// </summary>
    /// <returns>The territory codes; empty when no territory restriction applies.</returns>
    internal IReadOnlyList<string> Territories =>
        _territories;

    /// <summary>
    /// Gets the calendar systems in scope.
    /// </summary>
    /// <returns>The calendar systems; empty when no calendar restriction applies.</returns>
    internal IReadOnlyList<CalendarSystem> Calendars =>
        _calendars;

    /// <summary>
    /// Gets the categories in scope.
    /// </summary>
    /// <returns>The categories; empty when no category restriction applies.</returns>
    internal IReadOnlyList<NotableDateCategory> Categories =>
        _categories;

    /// <summary>
    /// Gets the concept identifiers in scope.
    /// </summary>
    /// <returns>The concept identifiers; empty when no concept restriction applies.</returns>
    internal IReadOnlyList<string> NotableDateRefs =>
        _notableDateRefs;

    /// <summary>
    /// Gets the concept-and-rule pairs in scope.
    /// </summary>
    /// <returns>The rule references; empty when no rule restriction applies.</returns>
    internal IReadOnlyList<(string NotableDateRef, string RuleRef)> RuleRefs =>
        _ruleRefs;

    /// <summary>
    /// Gets the explicit included years.
    /// </summary>
    /// <returns>The included years; empty when no inclusion list applies.</returns>
    internal IReadOnlyList<int> OnlyYears =>
        _onlyYears;

    /// <summary>
    /// Gets the explicit excluded years.
    /// </summary>
    /// <returns>The excluded years; empty when no exclusion list applies.</returns>
    internal IReadOnlyList<int> ExceptYears =>
        _exceptYears;

    /// <summary>
    /// Gets the inclusive lower year bound.
    /// </summary>
    /// <returns>The lower bound, or <see langword="null" /> when unbounded.</returns>
    internal int? FromYearValue { get; private set; }

    /// <summary>
    /// Gets the inclusive upper year bound.
    /// </summary>
    /// <returns>The upper bound, or <see langword="null" /> when unbounded.</returns>
    internal int? ToYearValue { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the scope declares any restriction.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when at least one scope element or bound is set; otherwise, <see langword="false" />.
    /// </returns>
    internal bool HasAnyValue =>
        _territories.Count > 0
        || _calendars.Count > 0
        || _categories.Count > 0
        || _notableDateRefs.Count > 0
        || _ruleRefs.Count > 0
        || _onlyYears.Count > 0
        || _exceptYears.Count > 0
        || FromYearValue is not null
        || ToYearValue is not null;

    /// <summary>
    /// Adds a territory code to the scope.
    /// </summary>
    /// <param name="code">The ISO territory code.</param>
    /// <returns>The same <see cref="AdjustmentScopeBuilder" /> instance, enabling chained calls.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="code" /> is <see langword="null" />, empty, or white-space.
    /// </exception>
    public AdjustmentScopeBuilder ForTerritory(string code)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(code);

        _territories.Add(code);
        return this;
    }

    /// <summary>
    /// Adds a calendar system to the scope.
    /// </summary>
    /// <param name="calendar">The calendar system.</param>
    /// <returns>The same <see cref="AdjustmentScopeBuilder" /> instance, enabling chained calls.</returns>
    public AdjustmentScopeBuilder ForCalendar(CalendarSystem calendar)
    {
        _calendars.Add(calendar);
        return this;
    }

    /// <summary>
    /// Adds a category to the scope.
    /// </summary>
    /// <param name="category">The category.</param>
    /// <returns>The same <see cref="AdjustmentScopeBuilder" /> instance, enabling chained calls.</returns>
    public AdjustmentScopeBuilder ForCategory(NotableDateCategory category)
    {
        _categories.Add(category);
        return this;
    }

    /// <summary>
    /// Adds a concept reference to the scope.
    /// </summary>
    /// <param name="notableDateRef">The identifier of the concept to include.</param>
    /// <returns>The same <see cref="AdjustmentScopeBuilder" /> instance, enabling chained calls.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="notableDateRef" /> is <see langword="null" />, empty, or white-space.
    /// </exception>
    public AdjustmentScopeBuilder ForNotableDate(string notableDateRef)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(notableDateRef);

        _notableDateRefs.Add(notableDateRef);
        return this;
    }

    /// <summary>
    /// Adds a concept-and-rule reference to the scope.
    /// </summary>
    /// <param name="notableDateRef">The identifier of the concept that owns the rule.</param>
    /// <param name="ruleRef">The identifier of the rule within the concept.</param>
    /// <returns>The same <see cref="AdjustmentScopeBuilder" /> instance, enabling chained calls.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="notableDateRef" /> or <paramref name="ruleRef" /> is <see langword="null" />, empty, or
    /// white-space.
    /// </exception>
    public AdjustmentScopeBuilder ForRule(string notableDateRef, string ruleRef)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(notableDateRef);
        ThrowHelper.ThrowIfNullOrWhiteSpace(ruleRef);

        _ruleRefs.Add((notableDateRef, ruleRef));
        return this;
    }

    /// <summary>
    /// Sets the inclusive lower year bound of the scope.
    /// </summary>
    /// <param name="year">The first year in scope.</param>
    /// <returns>The same <see cref="AdjustmentScopeBuilder" /> instance, enabling chained calls.</returns>
    public AdjustmentScopeBuilder FromYear(int year)
    {
        FromYearValue = year;
        return this;
    }

    /// <summary>
    /// Sets the inclusive upper year bound of the scope.
    /// </summary>
    /// <param name="year">The last year in scope.</param>
    /// <returns>The same <see cref="AdjustmentScopeBuilder" /> instance, enabling chained calls.</returns>
    public AdjustmentScopeBuilder ToYear(int year)
    {
        ToYearValue = year;
        return this;
    }

    /// <summary>
    /// Adds an explicitly included year to the scope.
    /// </summary>
    /// <param name="year">The year to include.</param>
    /// <returns>The same <see cref="AdjustmentScopeBuilder" /> instance, enabling chained calls.</returns>
    public AdjustmentScopeBuilder OnlyYear(int year)
    {
        _onlyYears.Add(year);
        return this;
    }

    /// <summary>
    /// Adds an explicitly excluded year to the scope.
    /// </summary>
    /// <param name="year">The year to exclude.</param>
    /// <returns>The same <see cref="AdjustmentScopeBuilder" /> instance, enabling chained calls.</returns>
    public AdjustmentScopeBuilder ExceptYear(int year)
    {
        _exceptYears.Add(year);
        return this;
    }

    /// <summary>
    /// Sets the scope state directly when reconstructing a builder from a parsed document.
    /// </summary>
    /// <param name="territories">The territory codes.</param>
    /// <param name="calendars">The calendar systems.</param>
    /// <param name="categories">The categories.</param>
    /// <param name="notableDateRefs">The concept identifiers.</param>
    /// <param name="ruleRefs">The concept-and-rule pairs.</param>
    /// <param name="onlyYears">The included years.</param>
    /// <param name="exceptYears">The excluded years.</param>
    /// <param name="fromYear">The inclusive lower year bound, or <see langword="null" />.</param>
    /// <param name="toYear">The inclusive upper year bound, or <see langword="null" />.</param>
    internal void SetParsedValues(
        IEnumerable<string> territories,
        IEnumerable<CalendarSystem> calendars,
        IEnumerable<NotableDateCategory> categories,
        IEnumerable<string> notableDateRefs,
        IEnumerable<(string NotableDateRef, string RuleRef)> ruleRefs,
        IEnumerable<int> onlyYears,
        IEnumerable<int> exceptYears,
        int? fromYear,
        int? toYear)
    {
        _territories.Clear();
        _territories.AddRange(territories);
        _calendars.Clear();
        _calendars.AddRange(calendars);
        _categories.Clear();
        _categories.AddRange(categories);
        _notableDateRefs.Clear();
        _notableDateRefs.AddRange(notableDateRefs);
        _ruleRefs.Clear();
        _ruleRefs.AddRange(ruleRefs);
        _onlyYears.Clear();
        _onlyYears.AddRange(onlyYears);
        _exceptYears.Clear();
        _exceptYears.AddRange(exceptYears);
        FromYearValue = fromYear;
        ToYearValue = toYear;
    }

    /// <summary>
    /// Creates a deep copy of this scope builder.
    /// </summary>
    /// <returns>A new <see cref="AdjustmentScopeBuilder" /> carrying the same configured state.</returns>
    internal AdjustmentScopeBuilder Clone()
    {
        AdjustmentScopeBuilder clone = new();
        clone.SetParsedValues(
            _territories,
            _calendars,
            _categories,
            _notableDateRefs,
            _ruleRefs,
            _onlyYears,
            _exceptYears,
            FromYearValue,
            ToYearValue);
        return clone;
    }
}
