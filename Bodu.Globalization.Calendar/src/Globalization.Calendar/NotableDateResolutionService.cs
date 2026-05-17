// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateResolutionService.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Extensions;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Provides an internal service facade for the chronological notable-date resolution pipeline.
/// </summary>
/// <remarks>
/// <para>
/// This service is intentionally separate from <see cref="NotableDateService" /> while the revised architecture is being
/// validated. It owns only the new resolution pipeline and avoids the legacy year-cache and recursive generation model.
/// </para>
/// </remarks>
internal sealed class NotableDateResolutionService
{
    private readonly INotableDateResolutionEngine _resolutionEngine;
    private readonly INotableDateCollisionResolver? _collisionResolver;
    private readonly WorkingDaysOfWeek _workingWeek;
    private readonly IWeekendDefinitionProvider? _weekendProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotableDateResolutionService" /> class.
    /// </summary>
    /// <param name="ruleProviders">The rule providers used to load notable-date rules.</param>
    /// <param name="algorithmRegistry">The optional algorithm registry used to resolve algorithm-backed rules.</param>
    /// <param name="collisionResolver">The optional collision resolver applied to emitted dates.</param>
    /// <param name="workingWeek">The working-week pattern used by observance adjustment processing.</param>
    /// <param name="weekendProvider">The optional custom weekend provider.</param>
    /// <param name="adjustmentHandlers">The optional custom adjustment handler registry.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="ruleProviders" /> is <see langword="null" />, or <paramref name="weekendProvider" /> is
    /// <see langword="null" /> when <paramref name="workingWeek" /> is <see cref="WorkingDaysOfWeek.Custom" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="workingWeek" /> is not defined.</exception>
    public NotableDateResolutionService(
        IEnumerable<INotableDateRuleProvider> ruleProviders,
        INotableDateAlgorithmRegistry? algorithmRegistry = null,
        INotableDateCollisionResolver? collisionResolver = null,
        WorkingDaysOfWeek workingWeek = WorkingDaysOfWeek.MondayToFriday,
        IWeekendDefinitionProvider? weekendProvider = null,
        IAdjustmentHandlerRegistry? adjustmentHandlers = null)
    {
        ArgumentNullException.ThrowIfNull(ruleProviders);

        if (!Enum.IsDefined(workingWeek))
            throw new ArgumentOutOfRangeException(nameof(workingWeek), workingWeek, CalendarResourceStrings.Arg_OutOfRange_WorkingWeekUndefined);

        if (workingWeek == WorkingDaysOfWeek.Custom && weekendProvider is null)
            throw new ArgumentNullException(nameof(weekendProvider));

        this.EffectiveRules = LoadRules(ruleProviders);
        this._collisionResolver = collisionResolver;
        this._workingWeek = workingWeek;
        this._weekendProvider = weekendProvider;

        NotableDateRuleResolver ruleResolver = new(this.EffectiveRules, algorithmRegistry);
        ICalculationAnchorResolver calculationAnchors = new CachingCalculationAnchorResolver(this.EffectiveRules, ruleResolver);
        AnchorRelativeRuleIndex anchorRelativeRules = new(this.EffectiveRules);
        INotableDateRuleOccurrenceResolver occurrenceResolver = new NotableDateRuleOccurrenceResolver(
            this.EffectiveRules,
            ruleResolver,
            calculationAnchors,
            anchorRelativeRules);

        INotableDateResolutionAdjustmentProcessor adjustmentProcessor = new NotableDateResolutionAdjustmentProcessor(
            workingWeek,
            weekendProvider,
            adjustmentHandlers,
            occurrenceResolver);

        this._resolutionEngine = new NotableDateResolutionEngine(occurrenceResolver, adjustmentProcessor);
    }

    /// <summary>
    /// Gets the effective rules loaded by this resolution service.
    /// </summary>
    /// <returns>A read-only list of <see cref="NotableDateRule" /> entries forming the post-merge effective rule set.</returns>
    internal IReadOnlyList<NotableDateRule> EffectiveRules { get; }

    /// <summary>
    /// Resolves notable dates for the specified request.
    /// </summary>
    /// <param name="request">The resolution request.</param>
    /// <returns>The resolved notable dates.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="request" /> is <see langword="null" />.</exception>
    public IReadOnlyList<NotableDate> Resolve(NotableDateResolutionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        IReadOnlyList<NotableDate> resolved = _resolutionEngine.Resolve(request);

        return ApplyCollisionResolution(resolved);
    }

    /// <summary>
    /// Gets notable dates whose rule anchor date falls within the specified civil year.
    /// </summary>
    /// <param name="year">The civil year to query.</param>
    /// <param name="territoryCode">The optional territory context.</param>
    /// <param name="calendarType">The optional calendar type context.</param>
    /// <param name="filter">The optional notable-date filter.</param>
    /// <returns>The resolved notable dates.</returns>
    public IReadOnlyList<NotableDate> GetNotableDates(
        int year,
        string? territoryCode = null,
        Type? calendarType = null,
        NotableDateFilter? filter = null)
    {
        NotableDateResolutionRequest request = new(
            new DateTime(year, 1, 1),
            new DateTime(year, 12, 31),
            NotableDateResolutionProjection.AnchorDate,
            territoryCode,
            calendarType,
            filter);

        return Resolve(request);
    }

    /// <summary>
    /// Gets notable dates whose observed date span intersects the specified date.
    /// </summary>
    /// <param name="date">The date to query.</param>
    /// <param name="territoryCode">The optional territory context.</param>
    /// <param name="calendarType">The optional calendar type context.</param>
    /// <param name="filter">The optional notable-date filter.</param>
    /// <returns>The resolved notable dates.</returns>
    public IReadOnlyList<NotableDate> GetNotableDates(
        DateTime date,
        string? territoryCode = null,
        Type? calendarType = null,
        NotableDateFilter? filter = null)
    {
        DateTime day = date.Date;

        NotableDateResolutionRequest request = new(
            day,
            day,
            NotableDateResolutionProjection.ObservedDate,
            territoryCode,
            calendarType,
            filter);

        return Resolve(request);
    }

    /// <summary>
    /// Gets notable dates whose observed date span intersects the specified date range.
    /// </summary>
    /// <param name="startDate">The inclusive start date.</param>
    /// <param name="endDate">The inclusive end date.</param>
    /// <param name="territoryCode">The optional territory context.</param>
    /// <param name="calendarType">The optional calendar type context.</param>
    /// <param name="filter">The optional notable-date filter.</param>
    /// <returns>The resolved notable dates.</returns>
    /// <exception cref="ArgumentException"><paramref name="endDate" /> is earlier than <paramref name="startDate" />.</exception>
    public IReadOnlyList<NotableDate> GetNotableDates(
        DateTime startDate,
        DateTime endDate,
        string? territoryCode = null,
        Type? calendarType = null,
        NotableDateFilter? filter = null)
    {
        NotableDateResolutionRequest request = new(
            startDate,
            endDate,
            NotableDateResolutionProjection.ObservedDate,
            territoryCode,
            calendarType,
            filter);

        return Resolve(request);
    }

    /// <summary>
    /// Determines whether the specified date is a weekend or a resolved non-working notable date.
    /// </summary>
    /// <param name="date">The date to test.</param>
    /// <param name="territoryCode">The optional territory context.</param>
    /// <param name="calendarType">The optional calendar type context.</param>
    /// <returns><see langword="true" /> when the date is non-working; otherwise, <see langword="false" />.</returns>
    public bool IsNonWorkingDay(
        DateTime date,
        string? territoryCode = null,
        Type? calendarType = null)
    {
        DateTime day = date.Date;

        if (day.IsWeekend(_workingWeek, _weekendProvider))
            return true;

        return GetNotableDates(day, territoryCode, calendarType)
            .Any(notable => notable.IsNonWorkingDay &&
                notable.Date.Date <= day &&
                notable.EndDate.Date >= day);
    }

    /// <summary>
    /// Loads rules from the configured providers.
    /// </summary>
    /// <param name="ruleProviders">The rule providers.</param>
    /// <returns>The loaded rules.</returns>
    private static IReadOnlyList<NotableDateRule> LoadRules(IEnumerable<INotableDateRuleProvider> ruleProviders)
    {
        List<NotableDateRule> rules = [];

        foreach (INotableDateRuleProvider provider in ruleProviders)
        {
            ArgumentNullException.ThrowIfNull(provider, nameof(ruleProviders));

            rules.AddRange(provider.LoadRules());
        }

        return rules;
    }

    /// <summary>
    /// Applies optional collision resolution and returns deterministic output ordering.
    /// </summary>
    /// <param name="dates">The dates to process.</param>
    /// <returns>The collision-resolved dates.</returns>
    private IReadOnlyList<NotableDate> ApplyCollisionResolution(IReadOnlyList<NotableDate> dates)
    {
        if (_collisionResolver is null)
            return Sort(dates);

        List<NotableDate> resolved = [];

        foreach (IGrouping<DateTime, NotableDate> group in dates.GroupBy(date => date.Date.Date).OrderBy(group => group.Key))
        {
            IReadOnlyList<NotableDate>? groupResult = _collisionResolver.Resolve(group.Key, [.. group]);

            if (groupResult is null)
                continue;

            resolved.AddRange(groupResult);
        }

        return Sort(resolved);
    }

    /// <summary>
    /// Sorts notable dates in deterministic chronological order.
    /// </summary>
    /// <param name="dates">The dates to sort.</param>
    /// <returns>The sorted dates.</returns>
    private static IReadOnlyList<NotableDate> Sort(IEnumerable<NotableDate> dates) =>
        [.. dates
            .OrderBy(date => date.Date)
            .ThenBy(date => date.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(date => date.TerritoryCode, StringComparer.OrdinalIgnoreCase)
            .ThenBy(date => date.CalendarType?.FullName, StringComparer.OrdinalIgnoreCase)];
}
