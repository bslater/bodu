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
/// This slice evaluates adjustments against occurrences that are already present in the current request window. Dynamic
/// expansion for adjustment candidates outside the known window is introduced separately.
/// </para>
/// </remarks>
internal sealed class NotableDateResolutionAdjustmentProcessor : INotableDateResolutionAdjustmentProcessor
{
    private readonly CalendarWeekendDefinition weekendDefinition;
    private readonly IWeekendDefinitionProvider? weekendProvider;
    private readonly IAdjustmentHandlerRegistry? handlerRegistry;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotableDateResolutionAdjustmentProcessor" /> class.
    /// </summary>
    /// <param name="weekendDefinition">The weekend definition used for weekend checks.</param>
    /// <param name="weekendProvider">The optional custom weekend provider.</param>
    /// <param name="handlerRegistry">The optional custom adjustment handler registry.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="weekendDefinition" /> is not defined.</exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="weekendProvider" /> is <see langword="null" /> when <paramref name="weekendDefinition" /> is
    /// <see cref="CalendarWeekendDefinition.Custom" />.
    /// </exception>
    public NotableDateResolutionAdjustmentProcessor(
        CalendarWeekendDefinition weekendDefinition,
        IWeekendDefinitionProvider? weekendProvider = null,
        IAdjustmentHandlerRegistry? handlerRegistry = null)
    {
        if (!Enum.IsDefined(weekendDefinition))
            throw new ArgumentOutOfRangeException(nameof(weekendDefinition), weekendDefinition, "The weekend definition is not defined.");

        if (weekendDefinition == CalendarWeekendDefinition.Custom && weekendProvider is null)
            throw new ArgumentNullException(nameof(weekendProvider));

        this.weekendDefinition = weekendDefinition;
        this.weekendProvider = weekendProvider;
        this.handlerRegistry = handlerRegistry;
    }

    /// <inheritdoc />
    public void ApplyAdjustments(
        NotableDateResolutionWindow window,
        IReadOnlyList<ResolvedNotableDateOccurrence> occurrences)
    {
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(occurrences);

        NotableDateAdjuster adjuster = CreateAdjuster(window);

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

                string? emittedTerritory = !string.IsNullOrEmpty(adjustment.TerritoryCode)
                    ? adjustment.TerritoryCode
                    : occurrence.TerritoryCode;

                bool isNonWorking = result.IsNonWorkingOverride ?? occurrence.Rule.IsNonWorkingDay ?? false;

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

                window.AddAdjusted(adjustedDate);
            }
        }
    }

    /// <summary>
    /// Creates an adjustment evaluator bound to the supplied resolution window.
    /// </summary>
    /// <param name="window">The resolution window.</param>
    /// <returns>The adjustment evaluator.</returns>
    private NotableDateAdjuster CreateAdjuster(NotableDateResolutionWindow window) =>
        new(
            IsWeekend,
            (date, territoryCode, calendarType) => window.IsNonWorkingDay(date, territoryCode, calendarType, IsWeekend),
            weekendDefinition,
            weekendProvider,
            handlerRegistry,
            (ruleName, year, territoryCode, calendarType) => window.ResolveByName(ruleName, year, territoryCode, calendarType));

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
    /// Determines whether a date falls on a configured weekend.
    /// </summary>
    /// <param name="date">The date to test.</param>
    /// <returns><see langword="true" /> when the date is a weekend; otherwise, <see langword="false" />.</returns>
    private bool IsWeekend(DateTime date) => date.IsWeekend(weekendDefinition, weekendProvider);
}
