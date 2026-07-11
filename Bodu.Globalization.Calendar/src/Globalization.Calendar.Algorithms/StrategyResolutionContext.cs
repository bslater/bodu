// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StrategyResolutionContext.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Algorithms;

/// <summary>
/// Provides a strategy with access to the surrounding resource so that referential strategies (such as
/// <see cref="OffsetFromRuleStrategy" />) can resolve the occurrence of another rule for the same year.
/// </summary>
/// <remarks>
/// <para>
/// The context maintains an in-progress set keyed by rule identity so that a circular reference between offset rules
/// resolves to <see langword="null" /> rather than recursing without end.
/// </para>
/// </remarks>
public sealed class StrategyResolutionContext
{
    /// <summary>The resource that referential strategies resolve against.</summary>
    private readonly NotableDateResource _resource;

    /// <summary>The identities currently being resolved, used to break circular references.</summary>
    private readonly HashSet<NotableDateRuleIdentity> _inProgress = new();

    /// <summary>The memoized non-working-day sets, keyed by territory and Gregorian year.</summary>
    private readonly Dictionary<(string Territory, int Year), HashSet<DateOnly>> _nonWorkingCache = new();

    /// <summary>The non-working-day computations currently in progress, used to break re-entrant working-day lookups.</summary>
    private readonly HashSet<(string Territory, int Year)> _nonWorkingInProgress = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="StrategyResolutionContext" /> class.
    /// </summary>
    /// <param name="resource">The resource that referential strategies resolve against.</param>
    /// <exception cref="ArgumentNullException"><paramref name="resource" /> is <see langword="null" />.</exception>
    public StrategyResolutionContext(NotableDateResource resource)
        : this(resource, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StrategyResolutionContext" /> class with a custom algorithm
    /// registry.
    /// </summary>
    /// <param name="resource">The resource that referential strategies resolve against.</param>
    /// <param name="algorithms">The custom algorithm registry, or <see langword="null" /> for built-ins only.</param>
    /// <param name="territory">
    /// The territory being resolved, consulted by business-day strategies; defaults to an empty territory.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="resource" /> is <see langword="null" />.</exception>
    public StrategyResolutionContext(NotableDateResource resource, INotableDateAlgorithmRegistry? algorithms, string? territory = null)
    {
        ThrowHelper.ThrowIfNull(resource);

        _resource = resource;
        Algorithms = algorithms;
        Territory = territory ?? string.Empty;
    }

    /// <summary>
    /// Gets the custom algorithm registry consulted for keys the built-in catalogue does not recognize.
    /// </summary>
    /// <value>The registry, or <see langword="null" /> when only built-in algorithms are available.</value>
    public INotableDateAlgorithmRegistry? Algorithms { get; }

    /// <summary>
    /// Gets the territory being resolved, used by business-day strategies to determine non-working days.
    /// </summary>
    /// <value>The territory code; an empty string when the context is not territory-scoped.</value>
    public string Territory { get; }

    /// <summary>
    /// Resolves the occurrence of a referenced rule for the supplied Gregorian year.
    /// </summary>
    /// <param name="notableDateRef">The identifier of the referenced notable-date concept.</param>
    /// <param name="ruleRef">
    /// The identifier of the referenced rule, or <see langword="null" /> to use its sole rule.
    /// </param>
    /// <param name="year">The Gregorian year to resolve.</param>
    /// <returns>
    /// The referenced occurrence, or <see langword="null" /> when the reference cannot be resolved (missing, ambiguous,
    /// circular, or producing no occurrence).
    /// </returns>
    public DateOnly? ResolveReference(string notableDateRef, string? ruleRef, int year)
    {
        NotableDateDefinition? definition = _resource.FindDefinition(notableDateRef);
        if (definition is null)
            return null;

        NotableDateRule? rule = SelectRule(definition, ruleRef);
        if (rule is null)
            return null;

        // A recurrence rule has no single occurrence, so it cannot serve as a singular reference; the validator reports
        // such a reference as an error.
        if (rule.Strategy is not IDateCalculationStrategy strategy)
            return null;

        NotableDateRuleIdentity identity = _resource.GetIdentity(definition, rule);
        if (!_inProgress.Add(identity))
            return null;

        try
        {
            return strategy.Calculate(year, this);
        }
        finally
        {
            _inProgress.Remove(identity);
        }
    }

    /// <summary>
    /// Determines whether a rule referenced by id resolves to a recurrence source, which cannot be used as a singular
    /// reference.
    /// </summary>
    /// <param name="notableDateRef">The identifier of the referenced notable-date concept.</param>
    /// <param name="ruleRef">The identifier of the referenced rule, or <see langword="null" /> to use its sole rule.</param>
    /// <returns>
    /// <see langword="true" /> when the reference resolves to a rule whose occurrence source is a recurrence; otherwise
    /// <see langword="false" />.
    /// </returns>
    public bool ReferenceIsRecurring(string notableDateRef, string? ruleRef)
    {
        NotableDateDefinition? definition = _resource.FindDefinition(notableDateRef);
        if (definition is null)
            return false;

        return SelectRule(definition, ruleRef)?.Recurrence is not null;
    }

    /// <summary>
    /// Determines whether a date is a working day for a territory, using the resource's working-week pattern and every
    /// applicable non-working notable-date occurrence.
    /// </summary>
    /// <param name="date">The date to test.</param>
    /// <param name="territory">The territory whose non-working days apply.</param>
    /// <returns><see langword="true" /> when the date is a working day; otherwise <see langword="false" />.</returns>
    /// <remarks>
    /// The result is deterministic and independent of resource-declaration order: a non-working day is a rest day
    /// outside the working week, or a day claimed by a non-working rule's base occurrence (and its fixed-duration span).
    /// </remarks>
    public bool IsWorkingDay(DateOnly date, string territory)
    {
        ThrowHelper.ThrowIfNull(territory);

        if (!_resource.ResolutionPolicy.WorkingWeek.Contains(date.DayOfWeek))
            return false;

        // A fixed-duration span anchored in the previous year can spill into this one, so consult both years.
        if (GetNonWorkingDates(territory, date.Year).Contains(date))
            return false;

        return date.Year <= 1 || !GetNonWorkingDates(territory, date.Year - 1).Contains(date);
    }

    /// <summary>
    /// Moves a date by a signed number of working days for a territory, skipping non-working days.
    /// </summary>
    /// <param name="origin">The origin date, which is not counted as one of the offset working days.</param>
    /// <param name="workingDays">The signed number of working days to move; positive forward, negative backward.</param>
    /// <param name="territory">The territory whose non-working days apply.</param>
    /// <returns>
    /// The resulting date, or <see langword="null" /> when the move would run past the representable date range.
    /// </returns>
    public DateOnly? AddWorkingDays(DateOnly origin, int workingDays, string territory)
    {
        ThrowHelper.ThrowIfNull(territory);

        if (workingDays == 0)
            return origin;

        int step = workingDays > 0 ? 1 : -1;
        int remaining = Math.Abs(workingDays);
        DateOnly cursor = origin;

        while (remaining > 0)
        {
            if ((step > 0 && cursor == DateOnly.MaxValue) || (step < 0 && cursor == DateOnly.MinValue))
                return null;

            cursor = cursor.AddDays(step);
            if (IsWorkingDay(cursor, territory))
                remaining--;
        }

        return cursor;
    }

    /// <summary>
    /// Computes and memoizes the non-working dates for a territory and Gregorian year, expanding each non-working rule's
    /// base occurrences by their fixed-duration span.
    /// </summary>
    /// <param name="territory">The territory whose non-working days apply.</param>
    /// <param name="year">The Gregorian year to compute.</param>
    /// <returns>The set of non-working dates claimed by non-working rules in the year.</returns>
    private HashSet<DateOnly> GetNonWorkingDates(string territory, int year)
    {
        (string territory, int year) key = (territory, year);
        if (_nonWorkingCache.TryGetValue(key, out HashSet<DateOnly>? cached))
            return cached;

        // Break re-entrancy: a working-day strategy consulted while building this set falls back to rest-day-only.
        if (!_nonWorkingInProgress.Add(key))
            return [];

        try
        {
            HashSet<DateOnly> dates = new();
            DateRange yearRange = new(new DateOnly(year, 1, 1), new DateOnly(year, 12, 31));

            foreach (NotableDateDefinition definition in _resource.NotableDates)
            {
                foreach (NotableDateRule rule in definition.Rules)
                {
                    if (!rule.Applicability.AppliesTo(territory, year))
                        continue;

                    bool nonWorking = rule.NonWorking ?? definition.DefaultNonWorkingDay;
                    if (!nonWorking)
                        continue;

                    // Calculated-duration spans are not expanded here (they would require re-entering resolution); a
                    // fixed or inherited day count is expanded so multi-day shutdowns claim their whole span.
                    int days = Math.Max(1, (rule.Duration as FixedDurationDefinition)?.Days ?? definition.DefaultDurationDays);

                    foreach (DateOnly baseDate in OccurrenceSourceEnumerator.Enumerate(rule, year, yearRange, this))
                    {
                        for (int offset = 0; offset < days; offset++)
                        {
                            dates.Add(baseDate.AddDays(offset));
                            if (baseDate.DayNumber > DateOnly.MaxValue.DayNumber - days)
                                break;
                        }
                    }
                }
            }

            _nonWorkingCache[key] = dates;
            return dates;
        }
        finally
        {
            _nonWorkingInProgress.Remove(key);
        }
    }

    /// <summary>
    /// Selects the referenced rule, requiring an unambiguous match.
    /// </summary>
    /// <param name="definition">The referenced concept.</param>
    /// <param name="ruleRef">The requested rule identifier, or <see langword="null" /> to use the sole rule.</param>
    /// <returns>The matching rule, or <see langword="null" /> when missing or ambiguous.</returns>
    private static NotableDateRule? SelectRule(NotableDateDefinition definition, string? ruleRef)
    {
        if (!string.IsNullOrEmpty(ruleRef))
        {
            foreach (NotableDateRule candidate in definition.Rules)
            {
                if (string.Equals(candidate.Id, ruleRef, StringComparison.Ordinal))
                    return candidate;
            }

            return null;
        }

        return definition.Rules.Count == 1 ? definition.Rules[0] : null;
    }
}
