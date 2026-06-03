// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ResolutionPolicy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.V2;

/// <summary>
/// Captures the resource-level policies that govern duplicate handling, collision handling, priority direction, and
/// observed-date range inclusion.
/// </summary>
/// <remarks>
/// <para>
/// The defaults mirror the recommended policy in the schema strategy: duplicates are an error, collisions keep all
/// occurrences, higher priority wins, and the observed (emitted) occurrence controls range inclusion.
/// </para>
/// </remarks>
public sealed class ResolutionPolicy
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResolutionPolicy" /> class.
    /// </summary>
    /// <param name="duplicatePolicy">The policy applied when two rules resolve to the same identity.</param>
    /// <param name="sameDayCollisionPolicy">
    /// The policy applied when distinct notable dates resolve to the same day.
    /// </param>
    /// <param name="spanCollisionPolicy">
    /// The policy applied when distinct notable dates resolve to overlapping spans.
    /// </param>
    /// <param name="priorityDirection">The direction in which numeric priority is interpreted.</param>
    /// <param name="observedDateRangePolicy">The occurrence that controls range-query inclusion.</param>
    public ResolutionPolicy(
        DuplicatePolicy duplicatePolicy = DuplicatePolicy.Error,
        CollisionPolicy sameDayCollisionPolicy = CollisionPolicy.KeepAll,
        CollisionPolicy spanCollisionPolicy = CollisionPolicy.KeepAll,
        PriorityDirection priorityDirection = PriorityDirection.HigherWins,
        ObservedDateRangePolicy observedDateRangePolicy = ObservedDateRangePolicy.ObservedOccurrenceControlsInclusion)
    {
        this.DuplicatePolicy = duplicatePolicy;
        this.SameDayCollisionPolicy = sameDayCollisionPolicy;
        this.SpanCollisionPolicy = spanCollisionPolicy;
        this.PriorityDirection = priorityDirection;
        this.ObservedDateRangePolicy = observedDateRangePolicy;
    }

    /// <summary>
    /// Gets the policy applied when two rules resolve to the same identity.
    /// </summary>
    /// <returns>The configured <see cref="V2.DuplicatePolicy" />.</returns>
    public DuplicatePolicy DuplicatePolicy { get; }

    /// <summary>
    /// Gets the policy applied when distinct notable dates resolve to the same day.
    /// </summary>
    /// <returns>The configured same-day <see cref="CollisionPolicy" />.</returns>
    public CollisionPolicy SameDayCollisionPolicy { get; }

    /// <summary>
    /// Gets the policy applied when distinct notable dates resolve to overlapping spans.
    /// </summary>
    /// <returns>The configured span <see cref="CollisionPolicy" />.</returns>
    public CollisionPolicy SpanCollisionPolicy { get; }

    /// <summary>
    /// Gets the direction in which numeric priority is interpreted.
    /// </summary>
    /// <returns>The configured <see cref="V2.PriorityDirection" />.</returns>
    public PriorityDirection PriorityDirection { get; }

    /// <summary>
    /// Gets the occurrence that controls whether a resolved notable date is included by a range query.
    /// </summary>
    /// <returns>The configured <see cref="V2.ObservedDateRangePolicy" />.</returns>
    public ObservedDateRangePolicy ObservedDateRangePolicy { get; }

    /// <summary>
    /// Gets a <see cref="ResolutionPolicy" /> populated with the recommended default values.
    /// </summary>
    /// <returns>A shared default policy instance.</returns>
    public static ResolutionPolicy Default { get; } = new ResolutionPolicy();
}
