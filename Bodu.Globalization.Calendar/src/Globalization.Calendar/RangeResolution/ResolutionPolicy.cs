// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ResolutionPolicy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.RangeResolution;

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
    /// <param name="workingWeek">
    /// The working week that defines which weekdays are working days, or <see langword="null" /> for the default
    /// Monday-to-Friday working week (a Saturday and Sunday weekend).
    /// </param>
    public ResolutionPolicy(
        DuplicatePolicy duplicatePolicy = DuplicatePolicy.Error,
        CollisionPolicy sameDayCollisionPolicy = CollisionPolicy.KeepAll,
        CollisionPolicy spanCollisionPolicy = CollisionPolicy.KeepAll,
        PriorityDirection priorityDirection = PriorityDirection.HigherWins,
        ObservedDateRangePolicy observedDateRangePolicy = ObservedDateRangePolicy.ObservedOccurrenceControlsInclusion,
        WeekPattern? workingWeek = null)
    {
        this.DuplicatePolicy = duplicatePolicy;
        this.SameDayCollisionPolicy = sameDayCollisionPolicy;
        this.SpanCollisionPolicy = spanCollisionPolicy;
        this.PriorityDirection = priorityDirection;
        this.ObservedDateRangePolicy = observedDateRangePolicy;
        this.WorkingWeek = workingWeek ?? WeekPattern.MondayToFriday;
    }

    /// <summary>
    /// Gets the policy applied when two rules resolve to the same identity.
    /// </summary>
    /// <returns>The configured <see cref="DuplicatePolicy" />.</returns>
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
    /// <returns>The configured <see cref="PriorityDirection" />.</returns>
    public PriorityDirection PriorityDirection { get; }

    /// <summary>
    /// Gets the occurrence that controls whether a resolved notable date is included by a range query.
    /// </summary>
    /// <returns>The configured <see cref="ObservedDateRangePolicy" />.</returns>
    public ObservedDateRangePolicy ObservedDateRangePolicy { get; }

    /// <summary>
    /// Gets the working week that defines which weekdays are working days, and therefore which are weekend
    /// (non-working) days for weekend-sensitive triggers and working-day searches.
    /// </summary>
    /// <returns>
    /// The configured working-week pattern; <see cref="WeekPattern.MondayToFriday" /> when the resource leaves it
    /// unspecified.
    /// </returns>
    public WeekPattern WorkingWeek { get; }

    /// <summary>
    /// Gets a <see cref="ResolutionPolicy" /> populated with the recommended default values.
    /// </summary>
    /// <returns>A shared default policy instance.</returns>
    public static ResolutionPolicy Default { get; } = new ResolutionPolicy();
}
