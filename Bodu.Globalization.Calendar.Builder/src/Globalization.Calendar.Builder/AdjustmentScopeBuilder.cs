// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdjustmentScopeBuilder.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Builder;

/// <summary>
/// Provides a fluent surface for narrowing the scope of an adjustment policy by territory, calendar, category,
/// concept, rule, and year, restricting the occurrences the policy may transform.
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
    /// The inclusive lower year bound, or <see langword="null" /> when unbounded.
    /// </summary>
    private int? _fromYear;

    /// <summary>
    /// The inclusive upper year bound, or <see langword="null" /> when unbounded.
    /// </summary>
    private int? _toYear;

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
        this._territories;

    /// <summary>
    /// Gets the calendar systems in scope.
    /// </summary>
    /// <returns>The calendar systems; empty when no calendar restriction applies.</returns>
    internal IReadOnlyList<CalendarSystem> Calendars =>
        this._calendars;

    /// <summary>
    /// Gets the categories in scope.
    /// </summary>
    /// <returns>The categories; empty when no category restriction applies.</returns>
    internal IReadOnlyList<NotableDateCategory> Categories =>
        this._categories;

    /// <summary>
    /// Gets the concept identifiers in scope.
    /// </summary>
    /// <returns>The concept identifiers; empty when no concept restriction applies.</returns>
    internal IReadOnlyList<string> NotableDateRefs =>
        this._notableDateRefs;

    /// <summary>
    /// Gets the concept-and-rule pairs in scope.
    /// </summary>
    /// <returns>The rule references; empty when no rule restriction applies.</returns>
    internal IReadOnlyList<(string NotableDateRef, string RuleRef)> RuleRefs =>
        this._ruleRefs;

    /// <summary>
    /// Gets the explicit included years.
    /// </summary>
    /// <returns>The included years; empty when no inclusion list applies.</returns>
    internal IReadOnlyList<int> OnlyYears =>
        this._onlyYears;

    /// <summary>
    /// Gets the explicit excluded years.
    /// </summary>
    /// <returns>The excluded years; empty when no exclusion list applies.</returns>
    internal IReadOnlyList<int> ExceptYears =>
        this._exceptYears;

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
    /// Gets a value indicating whether the scope declares any restriction.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when at least one scope element or bound is set; otherwise, <see langword="false" />.
    /// </returns>
    internal bool HasAnyValue =>
        this._territories.Count > 0
        || this._calendars.Count > 0
        || this._categories.Count > 0
        || this._notableDateRefs.Count > 0
        || this._ruleRefs.Count > 0
        || this._onlyYears.Count > 0
        || this._exceptYears.Count > 0
        || this._fromYear is not null
        || this._toYear is not null;

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

        this._territories.Add(code);
        return this;
    }

    /// <summary>
    /// Adds a calendar system to the scope.
    /// </summary>
    /// <param name="calendar">The calendar system.</param>
    /// <returns>The same <see cref="AdjustmentScopeBuilder" /> instance, enabling chained calls.</returns>
    public AdjustmentScopeBuilder ForCalendar(CalendarSystem calendar)
    {
        this._calendars.Add(calendar);
        return this;
    }

    /// <summary>
    /// Adds a category to the scope.
    /// </summary>
    /// <param name="category">The category.</param>
    /// <returns>The same <see cref="AdjustmentScopeBuilder" /> instance, enabling chained calls.</returns>
    public AdjustmentScopeBuilder ForCategory(NotableDateCategory category)
    {
        this._categories.Add(category);
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

        this._notableDateRefs.Add(notableDateRef);
        return this;
    }

    /// <summary>
    /// Adds a concept-and-rule reference to the scope.
    /// </summary>
    /// <param name="notableDateRef">The identifier of the concept that owns the rule.</param>
    /// <param name="ruleRef">The identifier of the rule within the concept.</param>
    /// <returns>The same <see cref="AdjustmentScopeBuilder" /> instance, enabling chained calls.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="notableDateRef" /> or <paramref name="ruleRef" /> is <see langword="null" />, empty, or white-space.
    /// </exception>
    public AdjustmentScopeBuilder ForRule(string notableDateRef, string ruleRef)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(notableDateRef);
        ThrowHelper.ThrowIfNullOrWhiteSpace(ruleRef);

        this._ruleRefs.Add((notableDateRef, ruleRef));
        return this;
    }

    /// <summary>
    /// Sets the inclusive lower year bound of the scope.
    /// </summary>
    /// <param name="year">The first year in scope.</param>
    /// <returns>The same <see cref="AdjustmentScopeBuilder" /> instance, enabling chained calls.</returns>
    public AdjustmentScopeBuilder FromYear(int year)
    {
        this._fromYear = year;
        return this;
    }

    /// <summary>
    /// Sets the inclusive upper year bound of the scope.
    /// </summary>
    /// <param name="year">The last year in scope.</param>
    /// <returns>The same <see cref="AdjustmentScopeBuilder" /> instance, enabling chained calls.</returns>
    public AdjustmentScopeBuilder ToYear(int year)
    {
        this._toYear = year;
        return this;
    }

    /// <summary>
    /// Adds an explicitly included year to the scope.
    /// </summary>
    /// <param name="year">The year to include.</param>
    /// <returns>The same <see cref="AdjustmentScopeBuilder" /> instance, enabling chained calls.</returns>
    public AdjustmentScopeBuilder OnlyYear(int year)
    {
        this._onlyYears.Add(year);
        return this;
    }

    /// <summary>
    /// Adds an explicitly excluded year to the scope.
    /// </summary>
    /// <param name="year">The year to exclude.</param>
    /// <returns>The same <see cref="AdjustmentScopeBuilder" /> instance, enabling chained calls.</returns>
    public AdjustmentScopeBuilder ExceptYear(int year)
    {
        this._exceptYears.Add(year);
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
        this._territories.Clear();
        this._territories.AddRange(territories);
        this._calendars.Clear();
        this._calendars.AddRange(calendars);
        this._categories.Clear();
        this._categories.AddRange(categories);
        this._notableDateRefs.Clear();
        this._notableDateRefs.AddRange(notableDateRefs);
        this._ruleRefs.Clear();
        this._ruleRefs.AddRange(ruleRefs);
        this._onlyYears.Clear();
        this._onlyYears.AddRange(onlyYears);
        this._exceptYears.Clear();
        this._exceptYears.AddRange(exceptYears);
        this._fromYear = fromYear;
        this._toYear = toYear;
    }

    /// <summary>
    /// Creates a deep copy of this scope builder.
    /// </summary>
    /// <returns>A new <see cref="AdjustmentScopeBuilder" /> carrying the same configured state.</returns>
    internal AdjustmentScopeBuilder Clone()
    {
        AdjustmentScopeBuilder clone = new();
        clone.SetParsedValues(
            this._territories,
            this._calendars,
            this._categories,
            this._notableDateRefs,
            this._ruleRefs,
            this._onlyYears,
            this._exceptYears,
            this._fromYear,
            this._toYear);
        return clone;
    }
}
