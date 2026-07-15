// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRule.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Represents one way of calculating a notable-date concept: an applicability scope, exactly one occurrence source (a
/// single-date calculation strategy or a recurrence strategy), an optional duration, and the adjustment policies that
/// transform its occurrences.
/// </summary>
/// <remarks>
/// <para>
/// A rule is identified within its notable-date concept by its <see cref="Id" />. Optional <see cref="Category" />,
/// <see cref="NonWorking" />, and <see cref="Duration" /> values override the inherited defaults of the parent concept;
/// when <see langword="null" /> the parent value applies.
/// </para>
/// <para>
/// A rule declares exactly one occurrence source: either a single-date <see cref="Strategy" /> that yields at most one
/// occurrence per year, or a <see cref="Recurrence" /> strategy that yields many occurrences within a window. Exactly
/// one of the two is non-<see langword="null" />.
/// </para>
/// </remarks>
/// <seealso cref="NotableDateDefinition" /> <seealso href="../guides/calendar/rule-reference.html">NotableDateRule and
/// adjustment-policy reference (guide)</seealso> <seealso href="../guides/calendar/rule-authoring.html">Authoring
/// notable date rules (guide)</seealso>
public sealed class NotableDateRule
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotableDateRule" /> class with a single-date calculation strategy.
    /// </summary>
    /// <param name="id">The stable identifier of the rule within its notable-date concept.</param>
    /// <param name="priority">The selection priority of the rule.</param>
    /// <param name="category">
    /// The category override, or <see langword="null" /> to inherit the concept's category.
    /// </param>
    /// <param name="nonWorking">
    /// The non-working-day override, or <see langword="null" /> to inherit the concept's default.
    /// </param>
    /// <param name="duration">
    /// The duration override, or <see langword="null" /> to inherit the concept's default.
    /// </param>
    /// <param name="applicability">The territory, calendar, and year applicability of the rule.</param>
    /// <param name="strategy">The single-date calculation strategy of the rule.</param>
    /// <param name="adjustmentPolicyRefs">The identifiers of the adjustment policies applied to the rule.</param>
    /// <param name="tags">The rule-specific tags.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="id" />, <paramref name="applicability" />, <paramref name="strategy" />,
    /// <paramref name="adjustmentPolicyRefs" />, or <paramref name="tags" /> is <see langword="null" />.
    /// </exception>
    public NotableDateRule(
        string id,
        int priority,
        NotableDateCategory? category,
        bool? nonWorking,
        NotableDateDurationDefinition? duration,
        RuleApplicability applicability,
        IDateCalculationStrategy strategy,
        IEnumerable<string> adjustmentPolicyRefs,
        IEnumerable<string> tags)
        : this(id, priority, category, nonWorking, duration, applicability, strategy, null, adjustmentPolicyRefs, tags)
    {
        ThrowHelper.ThrowIfNull(strategy);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotableDateRule" /> class with a recurrence strategy.
    /// </summary>
    /// <param name="id">The stable identifier of the rule within its notable-date concept.</param>
    /// <param name="priority">The selection priority of the rule.</param>
    /// <param name="category">
    /// The category override, or <see langword="null" /> to inherit the concept's category.
    /// </param>
    /// <param name="nonWorking">
    /// The non-working-day override, or <see langword="null" /> to inherit the concept's default.
    /// </param>
    /// <param name="duration">
    /// The duration override, or <see langword="null" /> to inherit the concept's default.
    /// </param>
    /// <param name="applicability">The territory, calendar, and year applicability of the rule.</param>
    /// <param name="recurrence">The recurrence strategy of the rule.</param>
    /// <param name="adjustmentPolicyRefs">The identifiers of the adjustment policies applied to the rule.</param>
    /// <param name="tags">The rule-specific tags.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="id" />, <paramref name="applicability" />, <paramref name="recurrence" />,
    /// <paramref name="adjustmentPolicyRefs" />, or <paramref name="tags" /> is <see langword="null" />.
    /// </exception>
    public NotableDateRule(
        string id,
        int priority,
        NotableDateCategory? category,
        bool? nonWorking,
        NotableDateDurationDefinition? duration,
        RuleApplicability applicability,
        IDateRecurrenceStrategy recurrence,
        IEnumerable<string> adjustmentPolicyRefs,
        IEnumerable<string> tags)
        : this(id, priority, category, nonWorking, duration, applicability, null, recurrence, adjustmentPolicyRefs, tags)
    {
        ThrowHelper.ThrowIfNull(recurrence);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotableDateRule" /> class, storing whichever occurrence source is
    /// supplied.
    /// </summary>
    /// <param name="id">The stable identifier of the rule within its notable-date concept.</param>
    /// <param name="priority">The selection priority of the rule.</param>
    /// <param name="category">The category override, or <see langword="null" /> to inherit the concept's category.</param>
    /// <param name="nonWorking">The non-working-day override, or <see langword="null" /> to inherit the default.</param>
    /// <param name="duration">The duration override, or <see langword="null" /> to inherit the default.</param>
    /// <param name="applicability">The territory, calendar, and year applicability of the rule.</param>
    /// <param name="strategy">The single-date strategy, or <see langword="null" /> when a recurrence is used.</param>
    /// <param name="recurrence">The recurrence strategy, or <see langword="null" /> when a single-date strategy is used.</param>
    /// <param name="adjustmentPolicyRefs">The identifiers of the adjustment policies applied to the rule.</param>
    /// <param name="tags">The rule-specific tags.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="id" />, <paramref name="applicability" />, <paramref name="adjustmentPolicyRefs" />, or
    /// <paramref name="tags" /> is <see langword="null" />.
    /// </exception>
    private NotableDateRule(
        string id,
        int priority,
        NotableDateCategory? category,
        bool? nonWorking,
        NotableDateDurationDefinition? duration,
        RuleApplicability applicability,
        IDateCalculationStrategy? strategy,
        IDateRecurrenceStrategy? recurrence,
        IEnumerable<string> adjustmentPolicyRefs,
        IEnumerable<string> tags)
    {
        ThrowHelper.ThrowIfNull(id);
        ThrowHelper.ThrowIfNull(applicability);
        ThrowHelper.ThrowIfNull(adjustmentPolicyRefs);
        ThrowHelper.ThrowIfNull(tags);

        Id = id;
        Priority = priority;
        Category = category;
        NonWorking = nonWorking;
        Duration = duration;
        Applicability = applicability;
        Strategy = strategy;
        Recurrence = recurrence;
        AdjustmentPolicyRefs = [.. adjustmentPolicyRefs];
        Tags = [.. tags];
    }

    /// <summary>
    /// Gets the stable identifier of the rule within its notable-date concept.
    /// </summary>
    /// <value>The rule id.</value>
    public string Id { get; }

    /// <summary>
    /// Gets the selection priority of the rule.
    /// </summary>
    /// <value>The numeric priority.</value>
    public int Priority { get; }

    /// <summary>
    /// Gets the category override of the rule.
    /// </summary>
    /// <value>The category, or <see langword="null" /> when the concept's category is inherited.</value>
    public NotableDateCategory? Category { get; }

    /// <summary>
    /// Gets the non-working-day override of the rule.
    /// </summary>
    /// <value>The flag, or <see langword="null" /> when the concept's default is inherited.</value>
    public bool? NonWorking { get; }

    /// <summary>
    /// Gets the duration override of the rule.
    /// </summary>
    /// <value>
    /// The duration definition, or <see langword="null" /> when the concept's default duration is inherited.
    /// </value>
    public NotableDateDurationDefinition? Duration { get; }

    /// <summary>
    /// Gets the explicit fixed-duration override of the rule, in days.
    /// </summary>
    /// <value>
    /// The fixed duration in days when the rule declares a fixed duration; otherwise <see langword="null" /> (the
    /// concept default is inherited, or the rule uses a calculated end-date duration).
    /// </value>
    public int? DurationDays =>
        (Duration as FixedDurationDefinition)?.Days;

    /// <summary>
    /// Gets the territory, calendar, and year applicability of the rule.
    /// </summary>
    /// <value>The <see cref="RuleApplicability" />.</value>
    public RuleApplicability Applicability { get; }

    /// <summary>
    /// Gets the single-date calculation strategy of the rule.
    /// </summary>
    /// <value>
    /// The <see cref="IDateCalculationStrategy" />, or <see langword="null" /> when the rule uses a recurrence source.
    /// </value>
    public IDateCalculationStrategy? Strategy { get; }

    /// <summary>
    /// Gets the recurrence strategy of the rule.
    /// </summary>
    /// <value>
    /// The <see cref="IDateRecurrenceStrategy" />, or <see langword="null" /> when the rule uses a single-date
    /// strategy.
    /// </value>
    public IDateRecurrenceStrategy? Recurrence { get; }

    /// <summary>
    /// Gets the identifiers of the adjustment policies applied to the rule.
    /// </summary>
    /// <value>The referenced policy ids; empty when the rule has no adjustments.</value>
    public IReadOnlyList<string> AdjustmentPolicyRefs { get; }

    /// <summary>
    /// Gets the rule-specific tags.
    /// </summary>
    /// <value>The tags; empty when the rule declares none.</value>
    public IReadOnlyList<string> Tags { get; }
}
