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
    /// Initializes a new instance of the <see cref="ResolutionPolicyBuilder" /> class with no configured values.
    /// </summary>
    internal ResolutionPolicyBuilder()
    {
    }

    /// <summary>
    /// Gets the configured duplicate-resolution policy.
    /// </summary>
    /// <value>The duplicate policy, or <see langword="null" /> when unset.</value>
    internal DuplicatePolicy? DuplicatePolicy { get; private set; }

    /// <summary>
    /// Gets the configured same-day collision policy.
    /// </summary>
    /// <value>The same-day collision policy, or <see langword="null" /> when unset.</value>
    internal CollisionPolicy? SameDayCollisionPolicy { get; private set; }

    /// <summary>
    /// Gets the configured span collision policy.
    /// </summary>
    /// <value>The span collision policy, or <see langword="null" /> when unset.</value>
    internal CollisionPolicy? SpanCollisionPolicy { get; private set; }

    /// <summary>
    /// Gets the configured priority direction.
    /// </summary>
    /// <value>The priority direction, or <see langword="null" /> when unset.</value>
    internal PriorityDirection? PriorityDirection { get; private set; }

    /// <summary>
    /// Gets the configured observed-date range policy.
    /// </summary>
    /// <value>The observed-date range policy, or <see langword="null" /> when unset.</value>
    internal ObservedDateRangePolicy? ObservedDateRangePolicy { get; private set; }

    /// <summary>
    /// Gets the configured working week.
    /// </summary>
    /// <value>The working-week pattern, or <see langword="null" /> when unset.</value>
    internal DayOfWeekSet? WorkingWeek { get; private set; }

    /// <summary>
    /// Gets the configured category precedence, highest-winning first.
    /// </summary>
    /// <value>The category precedence, or <see langword="null" /> when unset.</value>
    internal IReadOnlyList<NotableDateCategory>? CategoryPrecedence { get; private set; }

    /// <summary>
    /// Sets the policy the document declares for duplicate concept definitions. Reserved: the engine stores the value
    /// but does not apply it yet.
    /// </summary>
    /// <param name="policy">The duplicate-resolution policy.</param>
    /// <returns>The same <see cref="ResolutionPolicyBuilder" /> instance, enabling chained calls.</returns>
    /// <remarks>
    /// The value is written to the document and carried by the built resource, but duplicate concept and rule
    /// identifiers are load errors whatever it is; see <see cref="DuplicatePolicy" />. Implementing it is tracked by
    /// <see href="https://github.com/bslater/bodu/issues/1119">issue #1119</see>.
    /// </remarks>
    public ResolutionPolicyBuilder WithDuplicatePolicy(DuplicatePolicy policy)
    {
        DuplicatePolicy = policy;
        return this;
    }

    /// <summary>
    /// Sets the policy that governs how multiple occurrences resolving to the same emitted date are reconciled.
    /// </summary>
    /// <param name="policy">The same-day collision policy.</param>
    /// <returns>The same <see cref="ResolutionPolicyBuilder" /> instance, enabling chained calls.</returns>
    public ResolutionPolicyBuilder WithSameDayCollisionPolicy(CollisionPolicy policy)
    {
        SameDayCollisionPolicy = policy;
        return this;
    }

    /// <summary>
    /// Sets the policy the document declares for overlapping multi-day spans. Reserved: the engine stores the value but
    /// does not apply it yet.
    /// </summary>
    /// <param name="policy">The span collision policy.</param>
    /// <returns>The same <see cref="ResolutionPolicyBuilder" /> instance, enabling chained calls.</returns>
    /// <remarks>
    /// The value is written to the document and carried by the built resource, but overlapping spans are never
    /// reconciled. Implementing it is tracked by
    /// <see href="https://github.com/bslater/bodu/issues/1119">issue #1119</see>.
    /// </remarks>
    public ResolutionPolicyBuilder WithSpanCollisionPolicy(CollisionPolicy policy)
    {
        SpanCollisionPolicy = policy;
        return this;
    }

    /// <summary>
    /// Sets the direction in which numeric priority is interpreted when resolving collisions.
    /// </summary>
    /// <param name="direction">The priority direction.</param>
    /// <returns>The same <see cref="ResolutionPolicyBuilder" /> instance, enabling chained calls.</returns>
    public ResolutionPolicyBuilder WithPriorityDirection(PriorityDirection direction)
    {
        PriorityDirection = direction;
        return this;
    }

    /// <summary>
    /// Sets the policy the document declares for whether an actual or observed occurrence controls inclusion in a date
    /// range. Reserved: the engine stores the value but does not apply it yet.
    /// </summary>
    /// <param name="policy">The observed-date range policy.</param>
    /// <returns>The same <see cref="ResolutionPolicyBuilder" /> instance, enabling chained calls.</returns>
    /// <remarks>
    /// The value is written to the document and carried by the built resource, but a range query always includes an
    /// occurrence whose emitted span intersects the window; see <see cref="ObservedDateRangePolicy" />. Implementing it
    /// is tracked by <see href="https://github.com/bslater/bodu/issues/1119">issue #1119</see>.
    /// </remarks>
    public ResolutionPolicyBuilder WithObservedDateRangePolicy(ObservedDateRangePolicy policy)
    {
        ObservedDateRangePolicy = policy;
        return this;
    }

    /// <summary>
    /// Sets the working week used for non-working-day adjustments and working-day arithmetic.
    /// </summary>
    /// <param name="workingWeek">
    /// The working-week pattern, serialized as a seven-character Sunday-first binary string.
    /// </param>
    /// <returns>The same <see cref="ResolutionPolicyBuilder" /> instance, enabling chained calls.</returns>
    public ResolutionPolicyBuilder WithWorkingWeek(DayOfWeekSet workingWeek)
    {
        WorkingWeek = workingWeek;
        return this;
    }

    /// <summary>
    /// Sets the category precedence applied by <see cref="CollisionPolicy.CategoryPriority" />, ordered highest-winning
    /// first.
    /// </summary>
    /// <param name="precedence">The categories in descending precedence order.</param>
    /// <returns>The same <see cref="ResolutionPolicyBuilder" /> instance, enabling chained calls.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="precedence" /> is <see langword="null" />.</exception>
    public ResolutionPolicyBuilder WithCategoryPrecedence(params NotableDateCategory[] precedence)
    {
        ThrowHelper.ThrowIfNull(precedence);

        CategoryPrecedence = (NotableDateCategory[])precedence.Clone();
        return this;
    }

    /// <summary>
    /// Creates a deep copy of this builder.
    /// </summary>
    /// <returns>A new <see cref="ResolutionPolicyBuilder" /> carrying the same configured values.</returns>
    internal ResolutionPolicyBuilder Clone() =>
        new()
        {
            DuplicatePolicy = DuplicatePolicy,
            SameDayCollisionPolicy = SameDayCollisionPolicy,
            SpanCollisionPolicy = SpanCollisionPolicy,
            PriorityDirection = PriorityDirection,
            ObservedDateRangePolicy = ObservedDateRangePolicy,
            WorkingWeek = WorkingWeek,
            CategoryPrecedence = CategoryPrecedence,
        };

    /// <summary>
    /// Determines whether any value has been configured on this builder.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when at least one policy value is set; otherwise, <see langword="false" />.
    /// </returns>
    internal bool HasAnyValue() =>
        DuplicatePolicy is not null
        || SameDayCollisionPolicy is not null
        || SpanCollisionPolicy is not null
        || PriorityDirection is not null
        || ObservedDateRangePolicy is not null
        || WorkingWeek is not null
        || CategoryPrecedence is not null;

    /// <summary>
    /// Sets the configured values directly when reconstructing a builder from a parsed document.
    /// </summary>
    /// <param name="duplicatePolicy">The duplicate policy, or <see langword="null" />.</param>
    /// <param name="sameDayCollisionPolicy">The same-day collision policy, or <see langword="null" />.</param>
    /// <param name="spanCollisionPolicy">The span collision policy, or <see langword="null" />.</param>
    /// <param name="priorityDirection">The priority direction, or <see langword="null" />.</param>
    /// <param name="observedDateRangePolicy">The observed-date range policy, or <see langword="null" />.</param>
    /// <param name="workingWeek">The working week, or <see langword="null" />.</param>
    /// <param name="categoryPrecedence">The category precedence, or <see langword="null" />.</param>
    internal void SetParsedValues(
        DuplicatePolicy? duplicatePolicy,
        CollisionPolicy? sameDayCollisionPolicy,
        CollisionPolicy? spanCollisionPolicy,
        PriorityDirection? priorityDirection,
        ObservedDateRangePolicy? observedDateRangePolicy,
        DayOfWeekSet? workingWeek,
        IReadOnlyList<NotableDateCategory>? categoryPrecedence = null)
    {
        DuplicatePolicy = duplicatePolicy;
        SameDayCollisionPolicy = sameDayCollisionPolicy;
        SpanCollisionPolicy = spanCollisionPolicy;
        PriorityDirection = priorityDirection;
        ObservedDateRangePolicy = observedDateRangePolicy;
        WorkingWeek = workingWeek;
        CategoryPrecedence = categoryPrecedence;
    }
}
