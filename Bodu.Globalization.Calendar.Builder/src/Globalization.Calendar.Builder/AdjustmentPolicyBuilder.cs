// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdjustmentPolicyBuilder.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar.RangeResolution;

namespace Bodu.Globalization.Calendar.Builder;

/// <summary>
/// Provides a fluent surface for authoring a reusable adjustment policy: its scope, the trigger that activates it, the
/// action it performs, the emission mode of the resulting occurrence, and any custom handler parameters.
/// </summary>
/// <remarks>
/// <para>
/// A policy reads as a sentence: <c>When</c> a trigger holds, <c>Then</c> perform an action, and <c>Emit</c> the
/// resulting occurrence in the chosen mode. Configure the builder inside
/// <see cref="NotableDateDocumentBuilder.AddAdjustmentPolicy(string, System.Action{AdjustmentPolicyBuilder})" /> and
/// reference the policy from a rule with <see cref="NotableDateRuleBuilder.WithAdjustment(string)" />.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // "If the holiday lands on a weekend, move it to the next working day and emit only the observed date."
/// builder.AddAdjustmentPolicy("weekend-roll", p => p
///     .When(AdjustmentTrigger.IfWeekend)
///     .Then(AdjustmentAction.MoveToNextWorkingDay)
///     .SkipNonWorkingDates()
///     .Emit(EmissionMode.ObservedOnly)
///     .WithReason("Observed (weekend roll-forward)"));
///]]>
/// </code>
/// </example>
/// <seealso cref="NotableDateDocumentBuilder" /> <seealso cref="NotableDateRuleBuilder" />
/// <seealso href="../guides/calendar/adjustment-rules.html">Observance adjustment rules (guide)</seealso>
public sealed class AdjustmentPolicyBuilder
{
    /// <summary>
    /// The weekdays attached to the trigger, in declaration order.
    /// </summary>
    private readonly List<DayOfWeek> _triggerWeekdays = new();

    /// <summary>
    /// The custom handler parameters, keyed by name in insertion order.
    /// </summary>
    private readonly Dictionary<string, string> _parameters = new(StringComparer.Ordinal);

    /// <summary>
    /// Initializes a new instance of the <see cref="AdjustmentPolicyBuilder" /> class.
    /// </summary>
    /// <param name="id">The stable identifier of the policy.</param>
    internal AdjustmentPolicyBuilder(string id)
    {
        Id = id;
    }

    /// <summary>
    /// Gets the stable identifier of the policy.
    /// </summary>
    /// <returns>The policy identifier.</returns>
    internal string Id { get; }

    /// <summary>
    /// Gets the configured priority.
    /// </summary>
    /// <returns>The priority, or <see langword="null" /> when unset.</returns>
    internal int? Priority { get; private set; }

    /// <summary>
    /// Gets the configured description.
    /// </summary>
    /// <returns>The description, or <see langword="null" /> when unset.</returns>
    internal string? Description { get; private set; }

    /// <summary>
    /// Gets the configured scope.
    /// </summary>
    /// <returns>The scope builder, or <see langword="null" /> when the policy applies globally.</returns>
    internal AdjustmentScopeBuilder? Scope { get; private set; }

    /// <summary>
    /// Gets the configured trigger type.
    /// </summary>
    /// <returns>The trigger type, or <see langword="null" /> when unset.</returns>
    internal AdjustmentTrigger? TriggerType { get; private set; }

    /// <summary>
    /// Gets the weekdays attached to the trigger.
    /// </summary>
    /// <returns>The weekdays; empty when none are configured.</returns>
    internal IReadOnlyList<DayOfWeek> TriggerWeekdays =>
        _triggerWeekdays;

    /// <summary>
    /// Gets the trigger comparison month.
    /// </summary>
    /// <returns>The month, or <see langword="null" /> when unset.</returns>
    internal int? TriggerMonth { get; private set; }

    /// <summary>
    /// Gets the trigger comparison day.
    /// </summary>
    /// <returns>The day, or <see langword="null" /> when unset.</returns>
    internal int? TriggerDay { get; private set; }

    /// <summary>
    /// Gets the trigger week ordinal.
    /// </summary>
    /// <returns>The week ordinal, or <see langword="null" /> when unset.</returns>
    internal WeekOrdinal? TriggerWeekOrdinal { get; private set; }

    /// <summary>
    /// Gets the trigger custom-handler key.
    /// </summary>
    /// <returns>The handler key, or <see langword="null" /> when unset.</returns>
    internal string? TriggerHandlerKey { get; private set; }

    /// <summary>
    /// Gets the configured action type.
    /// </summary>
    /// <returns>The action type, or <see langword="null" /> when unset.</returns>
    internal AdjustmentAction? ActionType { get; private set; }

    /// <summary>
    /// Gets the action day offset.
    /// </summary>
    /// <returns>The day offset, or <see langword="null" /> when unset.</returns>
    internal int? ActionDays { get; private set; }

    /// <summary>
    /// Gets the action target weekday.
    /// </summary>
    /// <returns>The weekday, or <see langword="null" /> when unset.</returns>
    internal DayOfWeek? ActionDayOfWeek { get; private set; }

    /// <summary>
    /// Gets the maximum search horizon in days.
    /// </summary>
    /// <returns>The horizon, or <see langword="null" /> when unset.</returns>
    internal int? ActionMaxSearchDays { get; private set; }

    /// <summary>
    /// Gets the action skip-weekends flag.
    /// </summary>
    /// <returns>The flag, or <see langword="null" /> when unset.</returns>
    internal bool? ActionSkipWeekends { get; private set; }

    /// <summary>
    /// Gets the action skip-non-working-dates flag.
    /// </summary>
    /// <returns>The flag, or <see langword="null" /> when unset.</returns>
    internal bool? ActionSkipNonWorkingDates { get; private set; }

    /// <summary>
    /// Gets the replacement concept reference for a replace action.
    /// </summary>
    /// <returns>The concept reference, or <see langword="null" /> when unset.</returns>
    internal string? ActionNotableDateRef { get; private set; }

    /// <summary>
    /// Gets the replacement rule reference for a replace action.
    /// </summary>
    /// <returns>The rule reference, or <see langword="null" /> when unset.</returns>
    internal string? ActionRuleRef { get; private set; }

    /// <summary>
    /// Gets the action custom-handler key.
    /// </summary>
    /// <returns>The handler key, or <see langword="null" /> when unset.</returns>
    internal string? ActionHandlerKey { get; private set; }

    /// <summary>
    /// Gets the configured emission mode.
    /// </summary>
    /// <returns>The emission mode, or <see langword="null" /> when unset.</returns>
    internal EmissionMode? EmissionModeValue { get; private set; }

    /// <summary>
    /// Gets the emission reason.
    /// </summary>
    /// <returns>The reason, or <see langword="null" /> when unset.</returns>
    internal string? EmissionReason { get; private set; }

    /// <summary>
    /// Gets the emission non-working flag.
    /// </summary>
    /// <returns>The flag, or <see langword="null" /> when unset.</returns>
    internal bool? EmissionNonWorking { get; private set; }

    /// <summary>
    /// Gets the custom handler parameters.
    /// </summary>
    /// <returns>The parameter map; empty when none are configured.</returns>
    internal IReadOnlyDictionary<string, string> Parameters =>
        _parameters;

    /// <summary>
    /// Sets the priority of the policy.
    /// </summary>
    /// <param name="priority">The numeric priority.</param>
    /// <returns>The same <see cref="AdjustmentPolicyBuilder" /> instance, enabling chained calls.</returns>
    public AdjustmentPolicyBuilder WithPriority(int priority)
    {
        Priority = priority;
        return this;
    }

    /// <summary>
    /// Sets the human-readable description of the policy.
    /// </summary>
    /// <param name="description">The description text.</param>
    /// <returns>The same <see cref="AdjustmentPolicyBuilder" /> instance, enabling chained calls.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="description" /> is <see langword="null" />.</exception>
    public AdjustmentPolicyBuilder WithDescription(string description)
    {
        ThrowHelper.ThrowIfNull(description);

        Description = description;
        return this;
    }

    /// <summary>
    /// Configures the scope of the policy through the supplied delegate.
    /// </summary>
    /// <param name="configure">A delegate that configures the scope.</param>
    /// <returns>The same <see cref="AdjustmentPolicyBuilder" /> instance, enabling chained calls.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="configure" /> is <see langword="null" />.</exception>
    public AdjustmentPolicyBuilder WithScope(Action<AdjustmentScopeBuilder> configure)
    {
        ThrowHelper.ThrowIfNull(configure);

        AdjustmentScopeBuilder scope = Scope ?? new AdjustmentScopeBuilder();
        configure(scope);
        Scope = scope;
        return this;
    }

    /// <summary>
    /// Sets the trigger type that activates the policy.
    /// </summary>
    /// <param name="type">The trigger type.</param>
    /// <returns>The same <see cref="AdjustmentPolicyBuilder" /> instance, enabling chained calls.</returns>
    public AdjustmentPolicyBuilder When(AdjustmentTrigger type)
    {
        TriggerType = type;
        return this;
    }

    /// <summary>
    /// Replaces the weekdays attached to the trigger with the supplied collection.
    /// </summary>
    /// <param name="days">The weekdays on which the trigger fires.</param>
    /// <returns>The same <see cref="AdjustmentPolicyBuilder" /> instance, enabling chained calls.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="days" /> is <see langword="null" />.</exception>
    public AdjustmentPolicyBuilder OnTriggerWeekdays(params DayOfWeek[] days)
    {
        ThrowHelper.ThrowIfNull(days);

        _triggerWeekdays.Clear();
        _triggerWeekdays.AddRange(days);
        return this;
    }

    /// <summary>
    /// Sets the comparison month used by month-sensitive triggers.
    /// </summary>
    /// <param name="month">The one-based month number.</param>
    /// <returns>The same <see cref="AdjustmentPolicyBuilder" /> instance, enabling chained calls.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="month" /> is outside 1 to 12.</exception>
    public AdjustmentPolicyBuilder WithTriggerMonth(int month)
    {
        ThrowHelper.ThrowIfLessThan(month, 1);
        ThrowHelper.ThrowIfGreaterThan(month, 12);

        TriggerMonth = month;
        return this;
    }

    /// <summary>
    /// Sets the comparison day used by day-sensitive triggers.
    /// </summary>
    /// <param name="day">The day of the month.</param>
    /// <returns>The same <see cref="AdjustmentPolicyBuilder" /> instance, enabling chained calls.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="day" /> is outside 1 to 31.</exception>
    public AdjustmentPolicyBuilder WithTriggerDay(int day)
    {
        ThrowHelper.ThrowIfLessThan(day, 1);
        ThrowHelper.ThrowIfGreaterThan(day, 31);

        TriggerDay = day;
        return this;
    }

    /// <summary>
    /// Sets the week ordinal used by occurrence-sensitive triggers.
    /// </summary>
    /// <param name="weekOrdinal">The week ordinal.</param>
    /// <returns>The same <see cref="AdjustmentPolicyBuilder" /> instance, enabling chained calls.</returns>
    public AdjustmentPolicyBuilder WithTriggerWeekOrdinal(WeekOrdinal weekOrdinal)
    {
        TriggerWeekOrdinal = weekOrdinal;
        return this;
    }

    /// <summary>
    /// Sets the custom-handler key used by a custom trigger.
    /// </summary>
    /// <param name="handlerKey">The custom trigger handler key.</param>
    /// <returns>The same <see cref="AdjustmentPolicyBuilder" /> instance, enabling chained calls.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="handlerKey" /> is <see langword="null" />, empty, or white-space.
    /// </exception>
    public AdjustmentPolicyBuilder WithTriggerHandlerKey(string handlerKey)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(handlerKey);

        TriggerHandlerKey = handlerKey;
        return this;
    }

    /// <summary>
    /// Sets the action type the policy performs when triggered.
    /// </summary>
    /// <param name="type">The action type.</param>
    /// <returns>The same <see cref="AdjustmentPolicyBuilder" /> instance, enabling chained calls.</returns>
    public AdjustmentPolicyBuilder Then(AdjustmentAction type)
    {
        ActionType = type;
        return this;
    }

    /// <summary>
    /// Sets the signed day offset applied by an add-days action.
    /// </summary>
    /// <param name="days">The day offset.</param>
    /// <returns>The same <see cref="AdjustmentPolicyBuilder" /> instance, enabling chained calls.</returns>
    public AdjustmentPolicyBuilder WithActionDays(int days)
    {
        ActionDays = days;
        return this;
    }

    /// <summary>
    /// Sets the target weekday for weekday-seeking actions.
    /// </summary>
    /// <param name="dayOfWeek">The target weekday.</param>
    /// <returns>The same <see cref="AdjustmentPolicyBuilder" /> instance, enabling chained calls.</returns>
    public AdjustmentPolicyBuilder WithActionDayOfWeek(DayOfWeek dayOfWeek)
    {
        ActionDayOfWeek = dayOfWeek;
        return this;
    }

    /// <summary>
    /// Sets the maximum number of days an action searches before giving up.
    /// </summary>
    /// <param name="maxSearchDays">The search horizon in days.</param>
    /// <returns>The same <see cref="AdjustmentPolicyBuilder" /> instance, enabling chained calls.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxSearchDays" /> is less than 1.</exception>
    public AdjustmentPolicyBuilder WithMaxSearchDays(int maxSearchDays)
    {
        ThrowHelper.ThrowIfLessThan(maxSearchDays, 1);

        ActionMaxSearchDays = maxSearchDays;
        return this;
    }

    /// <summary>
    /// Sets whether the action skips weekends while searching.
    /// </summary>
    /// <param name="value">The skip-weekends flag.</param>
    /// <returns>The same <see cref="AdjustmentPolicyBuilder" /> instance, enabling chained calls.</returns>
    public AdjustmentPolicyBuilder SkipWeekends(bool value = true)
    {
        ActionSkipWeekends = value;
        return this;
    }

    /// <summary>
    /// Sets whether the action skips other non-working dates while searching.
    /// </summary>
    /// <param name="value">The skip-non-working-dates flag.</param>
    /// <returns>The same <see cref="AdjustmentPolicyBuilder" /> instance, enabling chained calls.</returns>
    public AdjustmentPolicyBuilder SkipNonWorkingDates(bool value = true)
    {
        ActionSkipNonWorkingDates = value;
        return this;
    }

    /// <summary>
    /// Sets the replacement rule reference for a replace-with-rule action.
    /// </summary>
    /// <param name="notableDateRef">The identifier of the replacement concept.</param>
    /// <param name="ruleRef">The identifier of the replacement rule, or <see langword="null" />.</param>
    /// <returns>The same <see cref="AdjustmentPolicyBuilder" /> instance, enabling chained calls.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="notableDateRef" /> is <see langword="null" />, empty, or white-space.
    /// </exception>
    public AdjustmentPolicyBuilder WithReplacementRule(string notableDateRef, string? ruleRef = null)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(notableDateRef);

        ActionNotableDateRef = notableDateRef;
        ActionRuleRef = string.IsNullOrEmpty(ruleRef) ? null : ruleRef;
        return this;
    }

    /// <summary>
    /// Sets the custom-handler key used by a custom action.
    /// </summary>
    /// <param name="handlerKey">The custom action handler key.</param>
    /// <returns>The same <see cref="AdjustmentPolicyBuilder" /> instance, enabling chained calls.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="handlerKey" /> is <see langword="null" />, empty, or white-space.
    /// </exception>
    public AdjustmentPolicyBuilder WithActionHandlerKey(string handlerKey)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(handlerKey);

        ActionHandlerKey = handlerKey;
        return this;
    }

    /// <summary>
    /// Sets the emission mode of the resulting occurrence.
    /// </summary>
    /// <param name="mode">The emission mode.</param>
    /// <returns>The same <see cref="AdjustmentPolicyBuilder" /> instance, enabling chained calls.</returns>
    public AdjustmentPolicyBuilder Emit(EmissionMode mode)
    {
        EmissionModeValue = mode;
        return this;
    }

    /// <summary>
    /// Sets the reason attached to the emitted occurrence.
    /// </summary>
    /// <param name="reason">The emission reason.</param>
    /// <returns>The same <see cref="AdjustmentPolicyBuilder" /> instance, enabling chained calls.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="reason" /> is <see langword="null" />.</exception>
    public AdjustmentPolicyBuilder WithReason(string reason)
    {
        ThrowHelper.ThrowIfNull(reason);

        EmissionReason = reason;
        return this;
    }

    /// <summary>
    /// Sets whether the emitted occurrence is a non-working day.
    /// </summary>
    /// <param name="value">The emission non-working flag.</param>
    /// <returns>The same <see cref="AdjustmentPolicyBuilder" /> instance, enabling chained calls.</returns>
    public AdjustmentPolicyBuilder EmitNonWorking(bool value = true)
    {
        EmissionNonWorking = value;
        return this;
    }

    /// <summary>
    /// Adds or replaces a custom handler parameter.
    /// </summary>
    /// <param name="key">The parameter key.</param>
    /// <param name="value">The parameter value.</param>
    /// <returns>The same <see cref="AdjustmentPolicyBuilder" /> instance, enabling chained calls.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="key" /> is <see langword="null" />, empty, or white-space.
    /// </exception>
    /// <exception cref="ArgumentNullException"><paramref name="value" /> is <see langword="null" />.</exception>
    public AdjustmentPolicyBuilder WithParameter(string key, string value)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(key);
        ThrowHelper.ThrowIfNull(value);

        _parameters[key] = value;
        return this;
    }

    /// <summary>
    /// Ensures the policy declares a trigger, an action, and an emission before it is serialized or built.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The trigger, action, or emission has not been configured.
    /// </exception>
    internal void EnsureComplete()
    {
        if (TriggerType is null || ActionType is null || EmissionModeValue is null)
            throw new InvalidOperationException(string.Format(System.Globalization.CultureInfo.CurrentCulture, BuilderResourceStrings.Op_Invalid_AdjustmentPolicyIncomplete, Id));
    }

    /// <summary>
    /// Sets the policy header values directly when reconstructing a builder from a parsed document.
    /// </summary>
    /// <param name="priority">The priority, or <see langword="null" />.</param>
    /// <param name="description">The description, or <see langword="null" />.</param>
    /// <param name="scope">The scope builder, or <see langword="null" />.</param>
    internal void SetParsedHeader(int? priority, string? description, AdjustmentScopeBuilder? scope)
    {
        Priority = priority;
        Description = description;
        Scope = scope;
    }

    /// <summary>
    /// Sets the trigger state directly when reconstructing a builder from a parsed document.
    /// </summary>
    /// <param name="type">The trigger type.</param>
    /// <param name="weekdays">The trigger weekdays.</param>
    /// <param name="month">The trigger month, or <see langword="null" />.</param>
    /// <param name="day">The trigger day, or <see langword="null" />.</param>
    /// <param name="weekOrdinal">The trigger week ordinal, or <see langword="null" />.</param>
    /// <param name="handlerKey">The trigger handler key, or <see langword="null" />.</param>
    internal void SetParsedTrigger(AdjustmentTrigger? type, IEnumerable<DayOfWeek> weekdays, int? month, int? day, WeekOrdinal? weekOrdinal, string? handlerKey)
    {
        TriggerType = type;
        _triggerWeekdays.Clear();
        _triggerWeekdays.AddRange(weekdays);
        TriggerMonth = month;
        TriggerDay = day;
        TriggerWeekOrdinal = weekOrdinal;
        TriggerHandlerKey = handlerKey;
    }

    /// <summary>
    /// Sets the action state directly when reconstructing a builder from a parsed document.
    /// </summary>
    /// <param name="type">The action type.</param>
    /// <param name="days">The action day offset, or <see langword="null" />.</param>
    /// <param name="dayOfWeek">The action target weekday, or <see langword="null" />.</param>
    /// <param name="maxSearchDays">The action search horizon, or <see langword="null" />.</param>
    /// <param name="skipWeekends">The action skip-weekends flag, or <see langword="null" />.</param>
    /// <param name="skipNonWorkingDates">The action skip-non-working-dates flag, or <see langword="null" />.</param>
    /// <param name="notableDateRef">The replacement concept reference, or <see langword="null" />.</param>
    /// <param name="ruleRef">The replacement rule reference, or <see langword="null" />.</param>
    /// <param name="handlerKey">The action handler key, or <see langword="null" />.</param>
    internal void SetParsedAction(AdjustmentAction? type, int? days, DayOfWeek? dayOfWeek, int? maxSearchDays, bool? skipWeekends, bool? skipNonWorkingDates, string? notableDateRef, string? ruleRef, string? handlerKey)
    {
        ActionType = type;
        ActionDays = days;
        ActionDayOfWeek = dayOfWeek;
        ActionMaxSearchDays = maxSearchDays;
        ActionSkipWeekends = skipWeekends;
        ActionSkipNonWorkingDates = skipNonWorkingDates;
        ActionNotableDateRef = notableDateRef;
        ActionRuleRef = ruleRef;
        ActionHandlerKey = handlerKey;
    }

    /// <summary>
    /// Sets the emission state directly when reconstructing a builder from a parsed document.
    /// </summary>
    /// <param name="mode">The emission mode.</param>
    /// <param name="reason">The emission reason, or <see langword="null" />.</param>
    /// <param name="nonWorking">The emission non-working flag, or <see langword="null" />.</param>
    internal void SetParsedEmission(EmissionMode? mode, string? reason, bool? nonWorking)
    {
        EmissionModeValue = mode;
        EmissionReason = reason;
        EmissionNonWorking = nonWorking;
    }

    /// <summary>
    /// Sets the custom handler parameters directly when reconstructing a builder from a parsed document.
    /// </summary>
    /// <param name="parameters">The parameter map.</param>
    internal void SetParsedParameters(IEnumerable<KeyValuePair<string, string>> parameters)
    {
        _parameters.Clear();
        foreach (KeyValuePair<string, string> pair in parameters)
            _parameters[pair.Key] = pair.Value;
    }

    /// <summary>
    /// Creates a deep copy of this policy builder.
    /// </summary>
    /// <returns>A new <see cref="AdjustmentPolicyBuilder" /> carrying the same configured state.</returns>
    internal AdjustmentPolicyBuilder Clone()
    {
        AdjustmentPolicyBuilder clone = new(Id);
        clone.SetParsedHeader(Priority, Description, Scope?.Clone());
        clone.SetParsedTrigger(TriggerType, _triggerWeekdays, TriggerMonth, TriggerDay, TriggerWeekOrdinal, TriggerHandlerKey);
        clone.SetParsedAction(ActionType, ActionDays, ActionDayOfWeek, ActionMaxSearchDays, ActionSkipWeekends, ActionSkipNonWorkingDates, ActionNotableDateRef, ActionRuleRef, ActionHandlerKey);
        clone.SetParsedEmission(EmissionModeValue, EmissionReason, EmissionNonWorking);
        clone.SetParsedParameters(_parameters);
        return clone;
    }
}
