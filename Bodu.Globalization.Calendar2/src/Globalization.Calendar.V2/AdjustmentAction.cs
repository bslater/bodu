// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdjustmentAction.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.V2;

/// <summary>
/// Describes how an adjustment policy transforms a calculated occurrence date once its trigger has fired.
/// </summary>
/// <remarks>
/// <para>
/// Working-day aware actions treat Saturday and Sunday as non-working days. A pluggable non-working-day calendar that
/// also skips holidays is reserved for a later phase. Searches are bounded by <see cref="MaxSearchDays" />.
/// </para>
/// </remarks>
public sealed class AdjustmentAction
{
    /// <summary>
    /// The number of days a bounded search scans when no explicit maximum is configured.
    /// </summary>
    private const int DefaultMaxSearchDays = 7;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdjustmentAction" /> class.
    /// </summary>
    /// <param name="type">The kind of transformation to apply.</param>
    /// <param name="weekday">
    /// The target weekday for weekday-seeking actions, or <see langword="null" /> when unused.
    /// </param>
    /// <param name="days">The day delta for <see cref="AdjustmentActionType.AddDays" />.</param>
    /// <param name="maxSearchDays">
    /// The maximum number of days a search may scan, or <see langword="null" /> for the default.
    /// </param>
    public AdjustmentAction(AdjustmentActionType type, DayOfWeek? weekday, int days, int? maxSearchDays)
    {
        this.Type = type;
        this.Weekday = weekday;
        this.Days = days;
        this.MaxSearchDays = maxSearchDays;
    }

    /// <summary>
    /// Gets the kind of transformation to apply.
    /// </summary>
    /// <returns>The configured <see cref="AdjustmentActionType" />.</returns>
    public AdjustmentActionType Type { get; }

    /// <summary>
    /// Gets the target weekday for weekday-seeking actions.
    /// </summary>
    /// <returns>The target weekday, or <see langword="null" /> when the action does not seek a weekday.</returns>
    public DayOfWeek? Weekday { get; }

    /// <summary>
    /// Gets the day delta applied by <see cref="AdjustmentActionType.AddDays" />.
    /// </summary>
    /// <returns>The signed number of days.</returns>
    public int Days { get; }

    /// <summary>
    /// Gets the maximum number of days a search may scan.
    /// </summary>
    /// <returns>The configured search bound, or <see langword="null" /> when the default applies.</returns>
    public int? MaxSearchDays { get; }

    /// <summary>
    /// Gets the effective upper bound on the number of days a search may scan.
    /// </summary>
    /// <returns>The configured <see cref="MaxSearchDays" />, or the default when none is configured.</returns>
    public int EffectiveMaxSearchDays =>
        this.MaxSearchDays ?? DefaultMaxSearchDays;

    /// <summary>
    /// Applies the transformation to the supplied occurrence date.
    /// </summary>
    /// <param name="date">The calculated occurrence date.</param>
    /// <returns>The transformed (observed) date; the input date when the action makes no change.</returns>
    public DateOnly Apply(DateOnly date) =>
        this.Type switch
        {
            AdjustmentActionType.AddDays => date.AddDays(this.Days),
            AdjustmentActionType.MoveToNextWeekday => this.SeekWeekday(date, forward: true),
            AdjustmentActionType.MoveToPreviousWeekday => this.SeekWeekday(date, forward: false),
            AdjustmentActionType.MoveToNextWorkingDay => SeekWorkingDay(date, forward: true, this.EffectiveMaxSearchDays),
            AdjustmentActionType.MoveToPreviousWorkingDay => SeekWorkingDay(date, forward: false, this.EffectiveMaxSearchDays),
            _ => date,
        };

    /// <summary>
    /// Seeks the nearest occurrence of <see cref="Weekday" /> in the requested direction.
    /// </summary>
    /// <param name="date">The starting date.</param>
    /// <param name="forward"><see langword="true" /> to search forward; otherwise backward.</param>
    /// <returns>The first date whose weekday matches, or the input date when no target weekday is configured.</returns>
    private DateOnly SeekWeekday(DateOnly date, bool forward)
    {
        if (this.Weekday is not DayOfWeek target)
            return date;

        DateOnly current = date;
        for (int i = 0; i < this.EffectiveMaxSearchDays && current.DayOfWeek != target; i++)
            current = current.AddDays(forward ? 1 : -1);

        return current;
    }

    /// <summary>
    /// Seeks the nearest working day (Monday through Friday) in the requested direction.
    /// </summary>
    /// <param name="date">The starting date.</param>
    /// <param name="forward"><see langword="true" /> to search forward; otherwise backward.</param>
    /// <param name="maxSearchDays">The maximum number of days to scan.</param>
    /// <returns>The first working day found, or the input date when the bound is reached.</returns>
    private static DateOnly SeekWorkingDay(DateOnly date, bool forward, int maxSearchDays)
    {
        DateOnly current = date;
        for (int i = 0; i < maxSearchDays && IsWeekend(current); i++)
            current = current.AddDays(forward ? 1 : -1);

        return current;
    }

    /// <summary>
    /// Determines whether the supplied date falls on a Saturday or Sunday.
    /// </summary>
    /// <param name="date">The date to test.</param>
    /// <returns><see langword="true" /> if the date is a weekend day; otherwise <see langword="false" />.</returns>
    private static bool IsWeekend(DateOnly date) =>
        date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
}
