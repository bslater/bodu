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
    /// The shared empty parameter map used when a policy declares no custom-handler parameters.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string> s_emptyParameters = new Dictionary<string, string>(0);

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
    /// <param name="triggerMonth">The comparison month for the fixed-date triggers, or <see langword="null" />.</param>
    /// <param name="triggerDay">The comparison day for the fixed-date triggers, or <see langword="null" />.</param>
    /// <param name="triggerWeekOrdinal">
    /// The week ordinal for <see cref="AdjustmentTrigger.IfNthOccurrenceInMonth" />, or <see langword="null" />.
    /// </param>
    /// <param name="actionNotableDateRef">
    /// The referenced concept for <see cref="AdjustmentAction.ReplaceWithRule" />, or <see langword="null" />.
    /// </param>
    /// <param name="actionRuleRef">
    /// The referenced rule for <see cref="AdjustmentAction.ReplaceWithRule" />, or <see langword="null" /> to use the
    /// referenced concept's sole rule.
    /// </param>
    /// <param name="actionHandlerKey">
    /// The handler key for <see cref="AdjustmentAction.Custom" />, or <see langword="null" />.
    /// </param>
    /// <param name="triggerHandlerKey">
    /// The handler key for <see cref="AdjustmentTrigger.Custom" />, or <see langword="null" />.
    /// </param>
    /// <param name="handlerParameters">
    /// The author-supplied parameters passed to custom trigger and action handlers, or <see langword="null" /> for none.
    /// </param>
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
        bool? nonWorking,
        int? triggerMonth = null,
        int? triggerDay = null,
        WeekOrdinal? triggerWeekOrdinal = null,
        string? actionNotableDateRef = null,
        string? actionRuleRef = null,
        string? actionHandlerKey = null,
        string? triggerHandlerKey = null,
        IReadOnlyDictionary<string, string>? handlerParameters = null)
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
        this.TriggerMonth = triggerMonth;
        this.TriggerDay = triggerDay;
        this.TriggerWeekOrdinal = triggerWeekOrdinal;
        this.ActionNotableDateRef = actionNotableDateRef;
        this.ActionRuleRef = actionRuleRef;
        this.ActionHandlerKey = actionHandlerKey;
        this.TriggerHandlerKey = triggerHandlerKey;
        this.HandlerParameters = handlerParameters ?? s_emptyParameters;
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
    /// Gets the comparison month for the fixed-date triggers.
    /// </summary>
    /// <returns>The one-based month, or <see langword="null" /> when unused.</returns>
    public int? TriggerMonth { get; }

    /// <summary>
    /// Gets the comparison day for the fixed-date triggers.
    /// </summary>
    /// <returns>The one-based day, or <see langword="null" /> when unused.</returns>
    public int? TriggerDay { get; }

    /// <summary>
    /// Gets the week ordinal for <see cref="AdjustmentTrigger.IfNthOccurrenceInMonth" />.
    /// </summary>
    /// <returns>The configured ordinal, or <see langword="null" /> when unused.</returns>
    public WeekOrdinal? TriggerWeekOrdinal { get; }

    /// <summary>
    /// Gets the referenced concept whose occurrence supplies the observed date for
    /// <see cref="AdjustmentAction.ReplaceWithRule" />.
    /// </summary>
    /// <returns>The referenced concept identifier, or <see langword="null" /> when unused.</returns>
    public string? ActionNotableDateRef { get; }

    /// <summary>
    /// Gets the referenced rule whose occurrence supplies the observed date for
    /// <see cref="AdjustmentAction.ReplaceWithRule" />.
    /// </summary>
    /// <returns>
    /// The referenced rule identifier, or <see langword="null" /> to use the referenced concept's sole rule.
    /// </returns>
    public string? ActionRuleRef { get; }

    /// <summary>
    /// Gets the handler key that binds <see cref="AdjustmentAction.Custom" /> to a registered
    /// <see cref="IAdjustmentHandler" />.
    /// </summary>
    /// <returns>The handler key, or <see langword="null" /> when unused.</returns>
    public string? ActionHandlerKey { get; }

    /// <summary>
    /// Gets the handler key that binds <see cref="AdjustmentTrigger.Custom" /> to a registered
    /// <see cref="IAdjustmentTriggerHandler" />.
    /// </summary>
    /// <returns>The handler key, or <see langword="null" /> when unused.</returns>
    public string? TriggerHandlerKey { get; }

    /// <summary>
    /// Gets the author-supplied parameters passed to custom trigger and action handlers.
    /// </summary>
    /// <returns>The parameter map; empty when the policy declares none. The engine treats the values as opaque.</returns>
    public IReadOnlyDictionary<string, string> HandlerParameters { get; }

    /// <summary>
    /// Determines whether the policy fires for an occurrence on the supplied date, evaluating weekend-sensitive
    /// triggers against the supplied working week.
    /// </summary>
    /// <param name="date">The calculated occurrence date.</param>
    /// <param name="workingWeek">The working week that defines which weekdays are working days.</param>
    /// <returns><see langword="true" /> if the trigger fires; otherwise <see langword="false" />.</returns>
    /// <remarks>
    /// The <see cref="AdjustmentTrigger.IfNonWorkingDay" /> and <see cref="AdjustmentTrigger.IfWorkingDay" /> triggers
    /// also depend on whether another non-working occurrence claims the date, which this method cannot determine alone;
    /// they are evaluated by the resolving service and never fire here.
    /// </remarks>
    public bool IsTriggered(DateOnly date, WeekPattern workingWeek) =>
        this.Trigger switch
        {
            AdjustmentTrigger.Always => true,
            AdjustmentTrigger.IfDayOfWeek => this.TriggerWeekdays.Contains(date.DayOfWeek),
            AdjustmentTrigger.IfWeekend => !workingWeek.Contains(date.DayOfWeek),
            AdjustmentTrigger.IfWeekday => workingWeek.Contains(date.DayOfWeek),
            AdjustmentTrigger.IfLeapYear => DateTime.IsLeapYear(date.Year),
            AdjustmentTrigger.IfBeforeFixedDate => this.ComparesFixedDate(date, before: true),
            AdjustmentTrigger.IfAfterFixedDate => this.ComparesFixedDate(date, before: false),
            AdjustmentTrigger.IfNthOccurrenceInMonth => this.IsNthOccurrenceInMonth(date),
            _ => false,
        };

    /// <summary>
    /// Compares an occurrence to the policy's fixed comparison month and day within the same year.
    /// </summary>
    /// <param name="date">The calculated occurrence date.</param>
    /// <param name="before"><see langword="true" /> to test strictly before; otherwise strictly after.</param>
    /// <returns>The comparison result, or <see langword="false" /> when the comparison date is unconfigured.</returns>
    private bool ComparesFixedDate(DateOnly date, bool before)
    {
        if (this.TriggerMonth is not int month || this.TriggerDay is not int day || month is < 1 or > 12)
            return false;

        int clampedDay = Math.Min(day, DateTime.DaysInMonth(date.Year, month));
        DateOnly pivot = new(date.Year, month, clampedDay);

        return before ? date < pivot : date > pivot;
    }

    /// <summary>
    /// Determines whether an occurrence is the configured nth weekday of its month.
    /// </summary>
    /// <param name="date">The calculated occurrence date.</param>
    /// <returns><see langword="true" /> when the occurrence matches the configured ordinal and weekday.</returns>
    private bool IsNthOccurrenceInMonth(DateOnly date)
    {
        if (this.TriggerWeekdays.Count == 0 || this.TriggerWeekOrdinal is not WeekOrdinal ordinal || date.DayOfWeek != this.TriggerWeekdays[0])
            return false;

        if (ordinal == WeekOrdinal.Last)
            return date.Day + 7 > DateTime.DaysInMonth(date.Year, date.Month);

        return ((date.Day - 1) / 7) + 1 == (int)ordinal;
    }

    /// <summary>
    /// Applies the policy's action to the supplied occurrence date, evaluating weekend skips against the supplied
    /// working week.
    /// </summary>
    /// <param name="date">The calculated occurrence date.</param>
    /// <param name="isOccupied">
    /// A predicate reporting whether a candidate day is already claimed by another non-working occurrence.
    /// </param>
    /// <param name="workingWeek">The working week that defines which weekdays are working days.</param>
    /// <returns>The transformed (observed) date; the input date when the action makes no change.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="isOccupied" /> is <see langword="null" />.</exception>
    public DateOnly ApplyAction(DateOnly date, Func<DateOnly, bool> isOccupied, WeekPattern workingWeek)
    {
        ThrowHelper.ThrowIfNull(isOccupied);

        return this.Action switch
        {
            AdjustmentAction.AddDays => date.AddDays(this.ActionDays),
            AdjustmentAction.MoveToNextWeekday => this.ActionWeekday is DayOfWeek next ? WeekdayMath.OnOrAfter(date, next) : date,
            AdjustmentAction.MoveToPreviousWeekday => this.ActionWeekday is DayOfWeek previous ? WeekdayMath.OnOrBefore(date, previous) : date,
            AdjustmentAction.MoveToNextWorkingDay => this.SeekWorkingDay(date, step: 1, isOccupied, workingWeek),
            AdjustmentAction.MoveToPreviousWorkingDay => this.SeekWorkingDay(date, step: -1, isOccupied, workingWeek),
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
    /// <param name="workingWeek">The working week that defines which weekdays are working days.</param>
    /// <returns>The first working day found, or the last scanned day when the bound is reached.</returns>
    private DateOnly SeekWorkingDay(DateOnly date, int step, Func<DateOnly, bool> isOccupied, WeekPattern workingWeek)
    {
        int bound = this.MaxSearchDays ?? DefaultMaxSearchDays;

        DateOnly cursor = date.AddDays(step);
        for (int i = 0; i < bound && this.IsBlocked(cursor, isOccupied, workingWeek); i++)
            cursor = cursor.AddDays(step);

        return cursor;
    }

    /// <summary>
    /// Determines whether the supplied day is blocked for a working-day search under this policy's skip rules.
    /// </summary>
    /// <param name="date">The candidate day.</param>
    /// <param name="isOccupied">A predicate reporting whether the day is already claimed.</param>
    /// <param name="workingWeek">The working week that defines which weekdays are working days.</param>
    /// <returns><see langword="true" /> if the day is blocked; otherwise <see langword="false" />.</returns>
    private bool IsBlocked(DateOnly date, Func<DateOnly, bool> isOccupied, WeekPattern workingWeek) =>
        (this.SkipWeekends && !workingWeek.Contains(date.DayOfWeek))
        || (this.SkipNonWorkingDates && isOccupied(date));
}
