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
/// <seealso cref="CollisionPolicy" /> <seealso cref="DuplicatePolicy" />
/// <seealso href="../guides/calendar/identity-and-resolution.html">Rule identity, priority, and observed-date
/// resolution (guide)</seealso>
public sealed class ResolutionPolicy
{
    /// <summary>The built-in category precedence, highest-winning first, used when a resource does not author its own and to fill any categories a partial authored list omits. <see cref="NotableDateCategory.None" /> is intentionally excluded so an uncategorized occurrence never wins a category collision.</summary>
    private static readonly NotableDateCategory[] s_defaultCategoryPrecedence =
    [
        NotableDateCategory.PublicHoliday,
        NotableDateCategory.BankHoliday,
        NotableDateCategory.Remembrance,
        NotableDateCategory.Religious,
        NotableDateCategory.Civic,
        NotableDateCategory.Seasonal,
        NotableDateCategory.Cultural,
        NotableDateCategory.School,
        NotableDateCategory.Regional,
        NotableDateCategory.Observance,
        NotableDateCategory.Other,
    ];

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
    /// <param name="categoryPrecedence">
    /// The category precedence, highest-winning first, applied by <see cref="CollisionPolicy.CategoryPriority" />, or
    /// <see langword="null" /> for the built-in precedence. A partial list is completed with the remaining categories
    /// in built-in order so every ranked category retains a position.
    /// </param>
    public ResolutionPolicy(
        DuplicatePolicy duplicatePolicy = DuplicatePolicy.Error,
        CollisionPolicy sameDayCollisionPolicy = CollisionPolicy.KeepAll,
        CollisionPolicy spanCollisionPolicy = CollisionPolicy.KeepAll,
        PriorityDirection priorityDirection = PriorityDirection.HigherWins,
        ObservedDateRangePolicy observedDateRangePolicy = ObservedDateRangePolicy.ObservedOccurrenceControlsInclusion,
        WeekPattern? workingWeek = null,
        IReadOnlyList<NotableDateCategory>? categoryPrecedence = null)
    {
        DuplicatePolicy = duplicatePolicy;
        SameDayCollisionPolicy = sameDayCollisionPolicy;
        SpanCollisionPolicy = spanCollisionPolicy;
        PriorityDirection = priorityDirection;
        ObservedDateRangePolicy = observedDateRangePolicy;
        WorkingWeek = workingWeek ?? WeekPattern.MondayToFriday;
        CategoryPrecedence = CompleteCategoryPrecedence(categoryPrecedence);
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
    /// Gets the category precedence applied by <see cref="CollisionPolicy.CategoryPriority" />, highest-winning first.
    /// </summary>
    /// <returns>
    /// The effective category precedence; every ranked category appears exactly once, with any categories an authored
    /// list omits appended in built-in order.
    /// </returns>
    public IReadOnlyList<NotableDateCategory> CategoryPrecedence { get; }

    /// <summary>
    /// Gets a <see cref="ResolutionPolicy" /> populated with the recommended default values.
    /// </summary>
    /// <returns>A shared default policy instance.</returns>
    public static ResolutionPolicy Default { get; } = new ResolutionPolicy();

    /// <summary>
    /// Completes an authored category precedence by appending the built-in categories it omits, preserving the authored
    /// order for the categories it supplies.
    /// </summary>
    /// <param name="authored">The authored precedence, or <see langword="null" /> for the built-in precedence.</param>
    /// <returns>The completed precedence, highest-winning first.</returns>
    private static IReadOnlyList<NotableDateCategory> CompleteCategoryPrecedence(IReadOnlyList<NotableDateCategory>? authored)
    {
        if (authored is null || authored.Count == 0)
            return s_defaultCategoryPrecedence;

        List<NotableDateCategory> completed = new(s_defaultCategoryPrecedence.Length);
        HashSet<NotableDateCategory> seen = new();

        foreach (NotableDateCategory category in authored)
        {
            if (seen.Add(category))
                completed.Add(category);
        }

        foreach (NotableDateCategory category in s_defaultCategoryPrecedence)
        {
            if (seen.Add(category))
                completed.Add(category);
        }

        return completed;
    }
}
