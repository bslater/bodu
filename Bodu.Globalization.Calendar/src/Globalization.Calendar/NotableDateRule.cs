// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRule.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Represents one way of calculating a notable-date concept: an applicability scope, exactly one calculation strategy,
/// and the adjustment policies that transform its occurrences.
/// </summary>
/// <remarks>
/// <para>
/// A rule is identified within its notable-date concept by its <see cref="Id" />. Optional <see cref="Category" />,
/// <see cref="NonWorking" />, and <see cref="DurationDays" /> values override the inherited defaults of the parent
/// concept; when <see langword="null" /> the parent value applies.
/// </para>
/// </remarks>
/// <seealso cref="NotableDateDefinition" /> <seealso href="../guides/calendar/rule-reference.html">NotableDateRule and
/// adjustment-policy reference (guide)</seealso> <seealso href="../guides/calendar/rule-authoring.html">Authoring
/// notable date rules (guide)</seealso>
public sealed class NotableDateRule
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotableDateRule" /> class.
    /// </summary>
    /// <param name="id">The stable identifier of the rule within its notable-date concept.</param>
    /// <param name="priority">The selection priority of the rule.</param>
    /// <param name="category">
    /// The category override, or <see langword="null" /> to inherit the concept's category.
    /// </param>
    /// <param name="nonWorking">
    /// The non-working-day override, or <see langword="null" /> to inherit the concept's default.
    /// </param>
    /// <param name="durationDays">
    /// The duration override, or <see langword="null" /> to inherit the concept's default.
    /// </param>
    /// <param name="applicability">The territory, calendar, and year applicability of the rule.</param>
    /// <param name="strategy">The single calculation strategy of the rule.</param>
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
        int? durationDays,
        RuleApplicability applicability,
        IDateCalculationStrategy strategy,
        IEnumerable<string> adjustmentPolicyRefs,
        IEnumerable<string> tags)
    {
        ThrowHelper.ThrowIfNull(id);
        ThrowHelper.ThrowIfNull(applicability);
        ThrowHelper.ThrowIfNull(strategy);
        ThrowHelper.ThrowIfNull(adjustmentPolicyRefs);
        ThrowHelper.ThrowIfNull(tags);

        Id = id;
        Priority = priority;
        Category = category;
        NonWorking = nonWorking;
        DurationDays = durationDays;
        Applicability = applicability;
        Strategy = strategy;
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
    /// <value>The duration in days, or <see langword="null" /> when the concept's default is inherited.</value>
    public int? DurationDays { get; }

    /// <summary>
    /// Gets the territory, calendar, and year applicability of the rule.
    /// </summary>
    /// <value>The <see cref="RuleApplicability" />.</value>
    public RuleApplicability Applicability { get; }

    /// <summary>
    /// Gets the single calculation strategy of the rule.
    /// </summary>
    /// <value>The <see cref="IDateCalculationStrategy" />.</value>
    public IDateCalculationStrategy Strategy { get; }

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
