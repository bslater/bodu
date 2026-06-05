// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ResolutionPolicyBuilder.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar.RangeResolution;

namespace Bodu.Globalization.Calendar.Builder;

/// <summary>
/// Provides a fluent surface for configuring the document-level <c>ResolutionPolicy</c>, which controls duplicate
/// handling, same-day and span collisions, priority direction, observed-date range inclusion, and the working week.
/// </summary>
public sealed class ResolutionPolicyBuilder
{
    /// <summary>
    /// The configured duplicate-resolution policy, or <see langword="null" /> when the schema default applies.
    /// </summary>
    private DuplicatePolicy? _duplicatePolicy;

    /// <summary>
    /// The configured same-day collision policy, or <see langword="null" /> when the schema default applies.
    /// </summary>
    private CollisionPolicy? _sameDayCollisionPolicy;

    /// <summary>
    /// The configured span collision policy, or <see langword="null" /> when the schema default applies.
    /// </summary>
    private CollisionPolicy? _spanCollisionPolicy;

    /// <summary>
    /// The configured priority direction, or <see langword="null" /> when the schema default applies.
    /// </summary>
    private PriorityDirection? _priorityDirection;

    /// <summary>
    /// The configured observed-date range policy, or <see langword="null" /> when the schema default applies.
    /// </summary>
    private ObservedDateRangePolicy? _observedDateRangePolicy;

    /// <summary>
    /// The configured working week, or <see langword="null" /> when the schema default applies.
    /// </summary>
    private WeekPattern? _workingWeek;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResolutionPolicyBuilder" /> class with no configured values.
    /// </summary>
    internal ResolutionPolicyBuilder()
    {
    }

    /// <summary>
    /// Gets the configured duplicate-resolution policy.
    /// </summary>
    /// <returns>The duplicate policy, or <see langword="null" /> when unset.</returns>
    internal DuplicatePolicy? DuplicatePolicy =>
        this._duplicatePolicy;

    /// <summary>
    /// Gets the configured same-day collision policy.
    /// </summary>
    /// <returns>The same-day collision policy, or <see langword="null" /> when unset.</returns>
    internal CollisionPolicy? SameDayCollisionPolicy =>
        this._sameDayCollisionPolicy;

    /// <summary>
    /// Gets the configured span collision policy.
    /// </summary>
    /// <returns>The span collision policy, or <see langword="null" /> when unset.</returns>
    internal CollisionPolicy? SpanCollisionPolicy =>
        this._spanCollisionPolicy;

    /// <summary>
    /// Gets the configured priority direction.
    /// </summary>
    /// <returns>The priority direction, or <see langword="null" /> when unset.</returns>
    internal PriorityDirection? PriorityDirection =>
        this._priorityDirection;

    /// <summary>
    /// Gets the configured observed-date range policy.
    /// </summary>
    /// <returns>The observed-date range policy, or <see langword="null" /> when unset.</returns>
    internal ObservedDateRangePolicy? ObservedDateRangePolicy =>
        this._observedDateRangePolicy;

    /// <summary>
    /// Gets the configured working week.
    /// </summary>
    /// <returns>The working-week pattern, or <see langword="null" /> when unset.</returns>
    internal WeekPattern? WorkingWeek =>
        this._workingWeek;

    /// <summary>
    /// Sets the policy that governs how duplicate concept definitions are reconciled.
    /// </summary>
    /// <param name="policy">The duplicate-resolution policy.</param>
    /// <returns>The same <see cref="ResolutionPolicyBuilder" /> instance, enabling chained calls.</returns>
    public ResolutionPolicyBuilder WithDuplicatePolicy(DuplicatePolicy policy)
    {
        this._duplicatePolicy = policy;
        return this;
    }

    /// <summary>
    /// Sets the policy that governs how multiple occurrences resolving to the same day are reconciled.
    /// </summary>
    /// <param name="policy">The same-day collision policy.</param>
    /// <returns>The same <see cref="ResolutionPolicyBuilder" /> instance, enabling chained calls.</returns>
    public ResolutionPolicyBuilder WithSameDayCollisionPolicy(CollisionPolicy policy)
    {
        this._sameDayCollisionPolicy = policy;
        return this;
    }

    /// <summary>
    /// Sets the policy that governs how overlapping multi-day spans are reconciled.
    /// </summary>
    /// <param name="policy">The span collision policy.</param>
    /// <returns>The same <see cref="ResolutionPolicyBuilder" /> instance, enabling chained calls.</returns>
    public ResolutionPolicyBuilder WithSpanCollisionPolicy(CollisionPolicy policy)
    {
        this._spanCollisionPolicy = policy;
        return this;
    }

    /// <summary>
    /// Sets the direction in which numeric priority is interpreted when resolving collisions.
    /// </summary>
    /// <param name="direction">The priority direction.</param>
    /// <returns>The same <see cref="ResolutionPolicyBuilder" /> instance, enabling chained calls.</returns>
    public ResolutionPolicyBuilder WithPriorityDirection(PriorityDirection direction)
    {
        this._priorityDirection = direction;
        return this;
    }

    /// <summary>
    /// Sets the policy that decides whether an actual or observed occurrence controls inclusion in a date range.
    /// </summary>
    /// <param name="policy">The observed-date range policy.</param>
    /// <returns>The same <see cref="ResolutionPolicyBuilder" /> instance, enabling chained calls.</returns>
    public ResolutionPolicyBuilder WithObservedDateRangePolicy(ObservedDateRangePolicy policy)
    {
        this._observedDateRangePolicy = policy;
        return this;
    }

    /// <summary>
    /// Sets the working week used for non-working-day adjustments and working-day arithmetic.
    /// </summary>
    /// <param name="workingWeek">
    /// The working-week pattern, serialized as a seven-character Sunday-first binary string.
    /// </param>
    /// <returns>The same <see cref="ResolutionPolicyBuilder" /> instance, enabling chained calls.</returns>
    public ResolutionPolicyBuilder WithWorkingWeek(WeekPattern workingWeek)
    {
        this._workingWeek = workingWeek;
        return this;
    }

    /// <summary>
    /// Creates a deep copy of this builder.
    /// </summary>
    /// <returns>A new <see cref="ResolutionPolicyBuilder" /> carrying the same configured values.</returns>
    internal ResolutionPolicyBuilder Clone() =>
        new()
        {
            _duplicatePolicy = this._duplicatePolicy,
            _sameDayCollisionPolicy = this._sameDayCollisionPolicy,
            _spanCollisionPolicy = this._spanCollisionPolicy,
            _priorityDirection = this._priorityDirection,
            _observedDateRangePolicy = this._observedDateRangePolicy,
            _workingWeek = this._workingWeek,
        };

    /// <summary>
    /// Determines whether any value has been configured on this builder.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when at least one policy value is set; otherwise, <see langword="false" />.
    /// </returns>
    internal bool HasAnyValue() =>
        this._duplicatePolicy is not null
        || this._sameDayCollisionPolicy is not null
        || this._spanCollisionPolicy is not null
        || this._priorityDirection is not null
        || this._observedDateRangePolicy is not null
        || this._workingWeek is not null;

    /// <summary>
    /// Sets the configured values directly when reconstructing a builder from a parsed document.
    /// </summary>
    /// <param name="duplicatePolicy">The duplicate policy, or <see langword="null" />.</param>
    /// <param name="sameDayCollisionPolicy">The same-day collision policy, or <see langword="null" />.</param>
    /// <param name="spanCollisionPolicy">The span collision policy, or <see langword="null" />.</param>
    /// <param name="priorityDirection">The priority direction, or <see langword="null" />.</param>
    /// <param name="observedDateRangePolicy">The observed-date range policy, or <see langword="null" />.</param>
    /// <param name="workingWeek">The working week, or <see langword="null" />.</param>
    internal void SetParsedValues(
        DuplicatePolicy? duplicatePolicy,
        CollisionPolicy? sameDayCollisionPolicy,
        CollisionPolicy? spanCollisionPolicy,
        PriorityDirection? priorityDirection,
        ObservedDateRangePolicy? observedDateRangePolicy,
        WeekPattern? workingWeek)
    {
        this._duplicatePolicy = duplicatePolicy;
        this._sameDayCollisionPolicy = sameDayCollisionPolicy;
        this._spanCollisionPolicy = spanCollisionPolicy;
        this._priorityDirection = priorityDirection;
        this._observedDateRangePolicy = observedDateRangePolicy;
        this._workingWeek = workingWeek;
    }
}
