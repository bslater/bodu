// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateResolutionAdjustmentProcessor.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Extensions;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Applies observance adjustments using the chronological resolution window as the non-working-day context.
/// </summary>
/// <remarks>
/// <para>
/// This processor deliberately uses the supplied <see cref="NotableDateResolutionWindow" /> instead of calling back into the
/// legacy public service cache. That keeps adjustment evaluation inside the new chronological pipeline.
/// </para>
/// <para>
/// When adjustment evaluation asks whether a candidate date outside the known request window is non-working, this processor
/// can expand the known window and materialise neighbouring occurrences as blockers without emitting those blockers to the
/// caller.
/// </para>
/// </remarks>
internal sealed class NotableDateResolutionAdjustmentProcessor : INotableDateResolutionAdjustmentProcessor
{
    private readonly CalendarWeekendDefinition weekendDefinition;
    private readonly IWeekendDefinitionProvider? weekendProvider;
    private readonly IAdjustmentHandlerRegistry? handlerRegistry;
    private readonly INotableDateRuleOccurrenceResolver? occurrenceResolver;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotableDateResolutionAdjustmentProcessor" /> class.
    /// </summary>
    /// <param name="weekendDefinition">The weekend definition used for weekend checks.</param>
    /// <param name="weekendProvider">The optional custom weekend provider.</param>
    /// <param name="handlerRegistry">The optional custom adjustment handler registry.</param>
    /// <param name="occurrenceResolver">The optional occurrence resolver used to expand neighbouring blocker dates.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="weekendDefinition" /> is not defined.</exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="weekendProvider" /> is <see langword="null" /> when <paramref name="weekendDefinition" /> is
    /// <see cref="CalendarWeekendDefinition.Custom" />.
    /// </exception>
    public NotableDateResolutionAdjustmentProcessor(
        CalendarWeekendDefinition weekendDefinition,
        IWeekendDefinitionProvider? weekendProvider = null,
        IAdjustmentHandlerRegistry? handlerRegistry = null,
        INotableDateRuleOccurrenceResolver? occurrenceResolver = null)
    {
        if (!Enum.IsDefined(weekendDefinition))
            throw new ArgumentOutOfRangeException(nameof(weekendDefinition), weekendDefinition, "The weekend definition is not defined.");

        if (weekendDefinition == CalendarWeekendDefinition.Custom && weekendProvider is null)
            throw new ArgumentNullException(nameof(weekendProvider));

        this.weekendDefinition = weekendDefinition;
        this.weekendProvider = weekendProvider;
        this.handlerRegistry = handlerRegistry;
        this.occurrenceResolver = occurrenceResolver;
    }

    /// <inheritdoc />
    public void ApplyAdjustments(
        NotableDateResolutionWindow window,
        NotableDateResolutionRequest request,
        IReadOnlyList<ResolvedNotableDateOccurrence> occurrences)
    {
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(occurrences);

        DateTime knownStart = request.StartDate;
        DateTime knownEnd = request.EndDate;

        HashSet<DateTime> materialisedDates = CreateMaterialisedDateSet(request.StartDate, request.EndDate);
        NotableDateAdjuster? adjuster = null;

        void EnsureKnown(DateTime candidateDate, string? territoryCode, Type? calendarType)
        {
            DateTime day = candidateDate.Date;

            if (materialisedDates.Contains(day))
                return;

            if (day > knownEnd)
            {
                for (DateTime current = knownEnd.AddDays(1); current <= day; current = current.AddDays(1))
                {
                    MaterialiseBlockerDate(current, territoryCode, calendarType, materialisedDates, window, request, adjuster!);
                }

                knownEnd = day > knownEnd ? day : knownEnd;
                return;
            }

            if (day < knownStart)
            {
                for (DateTime current = knownStart.AddDays(-1); current >= day; current = current.AddDays(-1))
                {
                    MaterialiseBlockerDate(current, territoryCode, calendarType, materialisedDates, window, request, adjuster!);
                }

                knownStart = day < knownStart ? day : knownStart;
                return;
            }

            MaterialiseBlockerDate(day, territoryCode, calendarType, materialisedDates, window, request, adjuster!);
        }

        adjuster = CreateAdjuster(window, EnsureKnown);

        ApplyOccurrenceAdjustments(
            adjuster,
            window,
            request,
            occurrences,
            emitAdjustedDates: true);
    }

    /// <summary>
    /// Applies adjustments for the supplied occurrences.
    /// </summary>
    /// <param name="adjuster">The adjustment evaluator.</param>
    /// <param name="window">The resolution window.</param>
    /// <param name="request">The originating resolution request.</param>
    /// <param name="occurrences">The occurrences to process.</param>
    /// <param name="emitAdjustedDates">
    /// <see langword="true" /> to emit adjusted occurrences when they match the request projection; otherwise,
    /// <see langword="false" /> to add adjusted dates as blockers only.
    /// </param>
    private void ApplyOccurrenceAdjustments(
        NotableDateAdjuster adjuster,
        NotableDateResolutionWindow window,
        NotableDateResolutionRequest request,
        IReadOnlyList<ResolvedNotableDateOccurrence> occurrences,
        bool emitAdjustedDates)
    {
        foreach (ResolvedNotableDateOccurrence occurrence in OrderForAdjustment(occurrences))
        {
            foreach (ObservanceAdjustment adjustment in occurrence.Rule.Adjustments.OrderBy(adjustment => adjustment.Priority))
            {
                if (!NotableDateAdjuster.IsInScope(
                    adjustment,
                    occurrence.AnchorDate.Year,
                    occurrence.TerritoryCode,
                    occurrence.Rule.CalendarType))
                {
                    continue;
                }

                AdjustmentApplyResult result = adjuster.Apply(
                    adjustment,
                    occurrence.Rule,
                    occurrence.AnchorDate,
                    occurrence.TerritoryCode,
                    occurrence.Rule.CalendarType);

                if (!result.Activated || result.AdjustedDate.Date == occurrence.AnchorDate.Date)
                    continue;

                var emittedTerritory = !string.IsNullOrEmpty(adjustment.TerritoryCode)
                    ? adjustment.TerritoryCode
                    : occurrence.TerritoryCode;

                var isNonWorking = result.IsNonWorkingOverride ?? occurrence.Rule.IsNonWorkingDay ?? false;

                AdjustmentReason reason = new(
                    occurrence.AnchorDate,
                    result.Trigger,
                    result.Action,
                    result.HandlerKey);

                NotableDate adjustedDate = BuildAdjustedNotableDate(
                    occurrence.Rule,
                    result.AdjustedDate,
                    emittedTerritory,
                    reason,
                    isNonWorking);

                if (emitAdjustedDates && ShouldEmitAdjustedDate(occurrence, adjustedDate, request))
                {
                    window.AddAdjusted(adjustedDate);
                }
                else
                {
                    window.AddBlocker(adjustedDate);
                }
            }
        }
    }

    /// <summary>
    /// Materialises a neighbouring date as blocker context.
    /// </summary>
    /// <param name="date">The date to materialise.</param>
    /// <param name="territoryCode">The optional territory context.</param>
    /// <param name="calendarType">The optional calendar type context.</param>
    /// <param name="materialisedDates">The set of materialised anchor dates.</param>
    /// <param name="window">The resolution window.</param>
    /// <param name="request">The originating request.</param>
    /// <param name="adjuster">The adjustment evaluator.</param>
    private void MaterialiseBlockerDate(
        DateTime date,
        string? territoryCode,
        Type? calendarType,
        HashSet<DateTime> materialisedDates,
        NotableDateResolutionWindow window,
        NotableDateResolutionRequest request,
        NotableDateAdjuster adjuster)
    {
        DateTime day = date.Date;

        if (!materialisedDates.Add(day))
            return;

        window.ExpandBackwardTo(day);
        window.ExpandForwardTo(day);

        if (occurrenceResolver is null)
            return;

        NotableDateResolutionRequest blockerRequest = new(
            day,
            day,
            NotableDateResolutionProjection.ObservedDate,
            territoryCode,
            calendarType);

        IReadOnlyList<ResolvedNotableDateOccurrence> blockers = occurrenceResolver.ResolveOccurrences(blockerRequest);

        foreach (ResolvedNotableDateOccurrence blocker in blockers)
        {
            window.AddBlocker(blocker.BaseDate);
        }

        ApplyOccurrenceAdjustments(
            adjuster,
            window,
            request,
            blockers,
            emitAdjustedDates: false);
    }

    /// <summary>
    /// Creates an adjustment evaluator bound to the supplied resolution window.
    /// </summary>
    /// <param name="window">The resolution window.</param>
    /// <param name="ensureKnown">Callback used to materialise neighbouring candidate dates.</param>
    /// <returns>The adjustment evaluator.</returns>
    private NotableDateAdjuster CreateAdjuster(
        NotableDateResolutionWindow window,
        Action<DateTime, string?, Type?> ensureKnown) =>
        new(
            IsWeekend,
            (date, territoryCode, calendarType) =>
            {
                ensureKnown(date, territoryCode, calendarType);

                return window.IsNonWorkingDay(date, territoryCode, calendarType, IsWeekend);
            },
            weekendDefinition,
            weekendProvider,
            handlerRegistry,
            (ruleName, year, territoryCode, calendarType) => window.ResolveByName(ruleName, year, territoryCode, calendarType));

    /// <summary>
    /// Determines whether an adjusted occurrence should be emitted for the original request.
    /// </summary>
    /// <param name="occurrence">The originating occurrence.</param>
    /// <param name="adjustedDate">The adjusted date.</param>
    /// <param name="request">The original request.</param>
    /// <returns><see langword="true" /> when the adjusted date should be returned to the caller.</returns>
    private static bool ShouldEmitAdjustedDate(
        ResolvedNotableDateOccurrence occurrence,
        NotableDate adjustedDate,
        NotableDateResolutionRequest request) =>
        request.Projection switch
        {
            NotableDateResolutionProjection.AnchorDate =>
                occurrence.AnchorDate.Date >= request.StartDate &&
                occurrence.AnchorDate.Date <= request.EndDate,

            NotableDateResolutionProjection.ObservedDate =>
                Intersects(request.StartDate, request.EndDate, adjustedDate.Date, adjustedDate.EndDate),

            _ => throw new ArgumentOutOfRangeException(nameof(request), request.Projection, "The requested projection is not supported."),
        };

    /// <summary>
    /// Orders occurrences deterministically for adjustment processing.
    /// </summary>
    /// <param name="occurrences">The occurrences to order.</param>
    /// <returns>The ordered occurrences.</returns>
    private static IEnumerable<ResolvedNotableDateOccurrence> OrderForAdjustment(IEnumerable<ResolvedNotableDateOccurrence> occurrences) =>
        occurrences
            .OrderBy(occurrence => occurrence.AnchorDate)
            .ThenBy(occurrence => occurrence.Rule.Priority)
            .ThenBy(occurrence => occurrence.Rule.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(occurrence => occurrence.TerritoryCode, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Constructs an adjusted notable date from a rule and adjustment outcome.
    /// </summary>
    /// <param name="rule">The originating rule.</param>
    /// <param name="date">The adjusted date.</param>
    /// <param name="territory">The emitted territory code.</param>
    /// <param name="adjustmentReason">The adjustment reason.</param>
    /// <param name="isNonWorking">Whether the adjusted date is non-working.</param>
    /// <returns>The adjusted notable date.</returns>
    private static NotableDate BuildAdjustedNotableDate(
        NotableDateRule rule,
        DateTime date,
        string? territory,
        AdjustmentReason adjustmentReason,
        bool isNonWorking) =>
        new()
        {
            Date = date.Date,
            Name = rule.Name,
            Category = rule.Category,
            DurationDays = Math.Max(1, rule.DurationDays),
            IsNonWorkingDay = isNonWorking,
            CalendarType = rule.CalendarType,
            TerritoryCode = territory,
            Tags = rule.Tags,
            Comment = rule.Comment,
            AdjustmentReason = adjustmentReason,
        };

    /// <summary>
    /// Creates the initial set of materialised dates for a request.
    /// </summary>
    /// <param name="startDate">The inclusive start date.</param>
    /// <param name="endDate">The inclusive end date.</param>
    /// <returns>The materialised date set.</returns>
    private static HashSet<DateTime> CreateMaterialisedDateSet(DateTime startDate, DateTime endDate)
    {
        HashSet<DateTime> dates = new();

        for (DateTime date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
        {
            dates.Add(date);
        }

        return dates;
    }

    /// <summary>
    /// Determines whether two inclusive date spans intersect.
    /// </summary>
    /// <param name="leftStart">The first span start.</param>
    /// <param name="leftEnd">The first span end.</param>
    /// <param name="rightStart">The second span start.</param>
    /// <param name="rightEnd">The second span end.</param>
    /// <returns><see langword="true" /> when the spans intersect.</returns>
    private static bool Intersects(
        DateTime leftStart,
        DateTime leftEnd,
        DateTime rightStart,
        DateTime rightEnd) =>
        rightStart.Date <= leftEnd.Date &&
        rightEnd.Date >= leftStart.Date;

    /// <summary>
    /// Determines whether a date falls on a configured weekend.
    /// </summary>
    /// <param name="date">The date to test.</param>
    /// <returns><see langword="true" /> when the date is a weekend; otherwise, <see langword="false" />.</returns>
    private bool IsWeekend(DateTime date) => date.IsWeekend(weekendDefinition, weekendProvider);
}
