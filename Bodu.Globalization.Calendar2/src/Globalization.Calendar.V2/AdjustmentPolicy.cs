// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdjustmentPolicy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.V2;

/// <summary>
/// Represents a reusable, named adjustment policy that transforms, supplements, or suppresses calculated occurrences.
/// </summary>
/// <remarks>
/// <para>
/// A policy composes a <see cref="Scope" /> that limits where it applies, a <see cref="Trigger" /> that decides whether
/// it fires, an <see cref="Action" /> that computes the observed date, and an <see cref="Emission" /> mode that decides
/// which occurrences are emitted. Policies are referenced by rules through stable ids.
/// </para>
/// <para>
/// Working-day actions honour <see cref="SkipWeekends" /> and <see cref="SkipNonWorkingDates" />: a day is skipped
/// while seeking the observed date if it is a weekend (when <see cref="SkipWeekends" /> is set) or already claimed by
/// another non-working occurrence (when <see cref="SkipNonWorkingDates" /> is set). The latter is what advances a
/// substitute past a day already taken by another holiday.
/// </para>
/// </remarks>
public sealed class AdjustmentPolicy
{
    /// <summary>
    /// The number of days a bounded working-day search scans when no explicit maximum is configured.
    /// </summary>
    private const int DefaultMaxSearchDays = 7;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdjustmentPolicy" /> class.
    /// </summary>
    /// <param name="id">The stable identifier of the policy.</param>
    /// <param name="priority">The selection priority of the policy.</param>
    /// <param name="scope">The scope that limits where the policy applies.</param>
    /// <param name="trigger">The condition under which the policy fires.</param>
    /// <param name="triggerWeekdays">
    /// The weekdays the <see cref="AdjustmentTrigger.IfDayOfWeek" /> trigger reacts to.
    /// </param>
    /// <param name="action">The transformation applied when the policy fires.</param>
    /// <param name="actionWeekday">
    /// The target weekday for weekday-seeking actions, or <see langword="null" /> when unused.
    /// </param>
    /// <param name="actionDays">The day delta for <see cref="AdjustmentAction.AddDays" />.</param>
    /// <param name="maxSearchDays">
    /// The maximum number of days a working-day search may scan, or <see langword="null" /> for the default.
    /// </param>
    /// <param name="skipWeekends">Whether a working-day search skips weekends.</param>
    /// <param name="skipNonWorkingDates">
    /// Whether a working-day search skips days already claimed by other non-working occurrences.
    /// </param>
    /// <param name="emission">The emission mode applied when the policy fires.</param>
    /// <param name="reason">The reason recorded against an observed occurrence, if any.</param>
    /// <param name="nonWorking">The non-working-day flag applied to the observed occurrence, if specified.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="id" />, <paramref name="scope" />, or <paramref name="triggerWeekdays" /> is
    /// <see langword="null" />.
    /// </exception>
    public AdjustmentPolicy(
        string id,
        int priority,
        AdjustmentScope scope,
        AdjustmentTrigger trigger,
        IEnumerable<DayOfWeek> triggerWeekdays,
        AdjustmentAction action,
        DayOfWeek? actionWeekday,
        int actionDays,
        int? maxSearchDays,
        bool skipWeekends,
        bool skipNonWorkingDates,
        EmissionMode emission,
        string? reason,
        bool? nonWorking)
    {
        ThrowHelper.ThrowIfNull(id);
        ThrowHelper.ThrowIfNull(scope);
        ThrowHelper.ThrowIfNull(triggerWeekdays);

        this.Id = id;
        this.Priority = priority;
        this.Scope = scope;
        this.Trigger = trigger;
        this.TriggerWeekdays = triggerWeekdays.ToArray();
        this.Action = action;
        this.ActionWeekday = actionWeekday;
        this.ActionDays = actionDays;
        this.MaxSearchDays = maxSearchDays;
        this.SkipWeekends = skipWeekends;
        this.SkipNonWorkingDates = skipNonWorkingDates;
        this.Emission = emission;
        this.Reason = reason;
        this.NonWorking = nonWorking;
    }

    /// <summary>
    /// Gets the stable identifier of the policy.
    /// </summary>
    /// <returns>The policy id.</returns>
    public string Id { get; }

    /// <summary>
    /// Gets the selection priority of the policy.
    /// </summary>
    /// <returns>The numeric priority.</returns>
    public int Priority { get; }

    /// <summary>
    /// Gets the scope that limits where the policy applies.
    /// </summary>
    /// <returns>The <see cref="AdjustmentScope" />.</returns>
    public AdjustmentScope Scope { get; }

    /// <summary>
    /// Gets the condition under which the policy fires.
    /// </summary>
    /// <returns>The <see cref="AdjustmentTrigger" />.</returns>
    public AdjustmentTrigger Trigger { get; }

    /// <summary>
    /// Gets the weekdays the <see cref="AdjustmentTrigger.IfDayOfWeek" /> trigger reacts to.
    /// </summary>
    /// <returns>The configured weekdays; empty when the trigger does not use weekdays.</returns>
    public IReadOnlyList<DayOfWeek> TriggerWeekdays { get; }

    /// <summary>
    /// Gets the transformation applied when the policy fires.
    /// </summary>
    /// <returns>The <see cref="AdjustmentAction" />.</returns>
    public AdjustmentAction Action { get; }

    /// <summary>
    /// Gets the target weekday for weekday-seeking actions.
    /// </summary>
    /// <returns>The target weekday, or <see langword="null" /> when the action does not seek a weekday.</returns>
    public DayOfWeek? ActionWeekday { get; }

    /// <summary>
    /// Gets the day delta applied by <see cref="AdjustmentAction.AddDays" />.
    /// </summary>
    /// <returns>The signed number of days.</returns>
    public int ActionDays { get; }

    /// <summary>
    /// Gets the maximum number of days a working-day search may scan.
    /// </summary>
    /// <returns>The configured search bound, or <see langword="null" /> when the default applies.</returns>
    public int? MaxSearchDays { get; }

    /// <summary>
    /// Gets a value indicating whether a working-day search skips weekends.
    /// </summary>
    /// <returns><see langword="true" /> when weekends are skipped; otherwise <see langword="false" />.</returns>
    public bool SkipWeekends { get; }

    /// <summary>
    /// Gets a value indicating whether a working-day search skips days already claimed by other non-working
    /// occurrences.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when occupied non-working days are skipped; otherwise <see langword="false" />.
    /// </returns>
    public bool SkipNonWorkingDates { get; }

    /// <summary>
    /// Gets the emission mode applied when the policy fires.
    /// </summary>
    /// <returns>The <see cref="EmissionMode" />.</returns>
    public EmissionMode Emission { get; }

    /// <summary>
    /// Gets the reason recorded against an observed occurrence.
    /// </summary>
    /// <returns>The reason text, or <see langword="null" /> when none is configured.</returns>
    public string? Reason { get; }

    /// <summary>
    /// Gets the non-working-day flag applied to the observed occurrence.
    /// </summary>
    /// <returns>The flag, or <see langword="null" /> when the rule's default applies.</returns>
    public bool? NonWorking { get; }

    /// <summary>
    /// Determines whether the policy fires for an occurrence on the supplied date.
    /// </summary>
    /// <param name="date">The calculated occurrence date.</param>
    /// <returns><see langword="true" /> if the trigger fires; otherwise <see langword="false" />.</returns>
    public bool IsTriggered(DateOnly date) =>
        this.Trigger switch
        {
            AdjustmentTrigger.Always => true,
            AdjustmentTrigger.IfDayOfWeek => this.TriggerWeekdays.Contains(date.DayOfWeek),
            AdjustmentTrigger.IfWeekend => date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday,
            AdjustmentTrigger.IfWeekday => date.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday,
            _ => false,
        };

    /// <summary>
    /// Applies the policy's action to the supplied occurrence date.
    /// </summary>
    /// <param name="date">The calculated occurrence date.</param>
    /// <param name="isOccupied">
    /// A predicate reporting whether a candidate day is already claimed by another non-working occurrence.
    /// </param>
    /// <returns>The transformed (observed) date; the input date when the action makes no change.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="isOccupied" /> is <see langword="null" />.</exception>
    public DateOnly ApplyAction(DateOnly date, Func<DateOnly, bool> isOccupied)
    {
        ThrowHelper.ThrowIfNull(isOccupied);

        return this.Action switch
        {
            AdjustmentAction.AddDays => date.AddDays(this.ActionDays),
            AdjustmentAction.MoveToNextWeekday => this.ActionWeekday is DayOfWeek next ? WeekdayMath.OnOrAfter(date, next) : date,
            AdjustmentAction.MoveToPreviousWeekday => this.ActionWeekday is DayOfWeek previous ? WeekdayMath.OnOrBefore(date, previous) : date,
            AdjustmentAction.MoveToNextWorkingDay => this.SeekWorkingDay(date, step: 1, isOccupied),
            AdjustmentAction.MoveToPreviousWorkingDay => this.SeekWorkingDay(date, step: -1, isOccupied),
            _ => date,
        };
    }

    /// <summary>
    /// Seeks the nearest working day in the requested direction, starting strictly past the supplied date and skipping
    /// blocked days until a free working day is found or the search bound is reached.
    /// </summary>
    /// <param name="date">The starting date.</param>
    /// <param name="step">The direction of travel: <c>+1</c> forward, <c>-1</c> backward.</param>
    /// <param name="isOccupied">A predicate reporting whether a candidate day is already claimed.</param>
    /// <returns>The first working day found, or the last scanned day when the bound is reached.</returns>
    private DateOnly SeekWorkingDay(DateOnly date, int step, Func<DateOnly, bool> isOccupied)
    {
        int bound = this.MaxSearchDays ?? DefaultMaxSearchDays;

        DateOnly cursor = date.AddDays(step);
        for (int i = 0; i < bound && this.IsBlocked(cursor, isOccupied); i++)
            cursor = cursor.AddDays(step);

        return cursor;
    }

    /// <summary>
    /// Determines whether the supplied day is blocked for a working-day search under this policy's skip rules.
    /// </summary>
    /// <param name="date">The candidate day.</param>
    /// <param name="isOccupied">A predicate reporting whether the day is already claimed.</param>
    /// <returns><see langword="true" /> if the day is blocked; otherwise <see langword="false" />.</returns>
    private bool IsBlocked(DateOnly date, Func<DateOnly, bool> isOccupied) =>
        (this.SkipWeekends && date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        || (this.SkipNonWorkingDates && isOccupied(date));
}
