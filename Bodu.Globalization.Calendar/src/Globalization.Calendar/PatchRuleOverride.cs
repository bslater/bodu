// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PatchRuleOverride.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Represents an override operation that patches exactly one rule, identified by its notable-date and rule ids.
/// </summary>
/// <remarks>
/// <para>
/// Each patch field is optional; a <see langword="null" /> field leaves the corresponding rule value unchanged. The
/// patch cannot clear a value back to <see langword="null" /> in this first cut.
/// </para>
/// </remarks>
internal sealed class PatchRuleOverride
    : NotableDateRuleOverride
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PatchRuleOverride" /> class.
    /// </summary>
    /// <param name="notableDateRef">The identifier of the targeted notable-date concept.</param>
    /// <param name="ruleRef">The identifier of the rule to patch.</param>
    /// <param name="priority">The replacement priority, or <see langword="null" /> to leave unchanged.</param>
    /// <param name="category">The replacement category, or <see langword="null" /> to leave unchanged.</param>
    /// <param name="nonWorking">
    /// The replacement non-working-day flag, or <see langword="null" /> to leave unchanged.
    /// </param>
    /// <param name="duration">The replacement duration, or <see langword="null" /> to leave unchanged.</param>
    /// <param name="applicability">
    /// The replacement applicability, or <see langword="null" /> to leave unchanged.
    /// </param>
    /// <param name="strategy">The replacement single-date strategy, or <see langword="null" /> to leave unchanged.</param>
    /// <param name="recurrence">The replacement recurrence, or <see langword="null" /> to leave unchanged.</param>
    /// <param name="adjustmentPolicyRefs">
    /// The replacement adjustment references, or <see langword="null" /> to leave unchanged.
    /// </param>
    /// <param name="tags">The replacement tags, or <see langword="null" /> to leave unchanged.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="notableDateRef" /> or <paramref name="ruleRef" /> is <see langword="null" />.
    /// </exception>
    public PatchRuleOverride(
        string notableDateRef,
        string ruleRef,
        int? priority,
        NotableDateCategory? category,
        bool? nonWorking,
        NotableDateDurationDefinition? duration,
        RuleApplicability? applicability,
        IDateCalculationStrategy? strategy,
        IDateRecurrenceStrategy? recurrence,
        IReadOnlyList<string>? adjustmentPolicyRefs,
        IReadOnlyList<string>? tags)
        : base(notableDateRef)
    {
        ThrowHelper.ThrowIfNull(ruleRef);

        RuleRef = ruleRef;
        Priority = priority;
        Category = category;
        NonWorking = nonWorking;
        Duration = duration;
        Applicability = applicability;
        Strategy = strategy;
        Recurrence = recurrence;
        AdjustmentPolicyRefs = adjustmentPolicyRefs;
        Tags = tags;
    }

    /// <summary>
    /// Gets the identifier of the rule to patch.
    /// </summary>
    /// <value>The targeted rule id.</value>
    public string RuleRef { get; }

    /// <summary>
    /// Gets the replacement priority.
    /// </summary>
    /// <value>The priority, or <see langword="null" /> to leave unchanged.</value>
    public int? Priority { get; }

    /// <summary>
    /// Gets the replacement category.
    /// </summary>
    /// <value>The category, or <see langword="null" /> to leave unchanged.</value>
    public NotableDateCategory? Category { get; }

    /// <summary>
    /// Gets the replacement non-working-day flag.
    /// </summary>
    /// <value>The flag, or <see langword="null" /> to leave unchanged.</value>
    public bool? NonWorking { get; }

    /// <summary>
    /// Gets the replacement duration.
    /// </summary>
    /// <value>The duration definition, or <see langword="null" /> to leave unchanged.</value>
    public NotableDateDurationDefinition? Duration { get; }

    /// <summary>
    /// Gets the replacement applicability.
    /// </summary>
    /// <value>The applicability, or <see langword="null" /> to leave unchanged.</value>
    public RuleApplicability? Applicability { get; }

    /// <summary>
    /// Gets the replacement single-date strategy.
    /// </summary>
    /// <value>The strategy, or <see langword="null" /> to leave unchanged.</value>
    public IDateCalculationStrategy? Strategy { get; }

    /// <summary>
    /// Gets the replacement recurrence.
    /// </summary>
    /// <value>The recurrence, or <see langword="null" /> to leave unchanged.</value>
    public IDateRecurrenceStrategy? Recurrence { get; }

    /// <summary>
    /// Gets the replacement adjustment references.
    /// </summary>
    /// <value>The references, or <see langword="null" /> to leave unchanged.</value>
    public IReadOnlyList<string>? AdjustmentPolicyRefs { get; }

    /// <summary>
    /// Gets the replacement tags.
    /// </summary>
    /// <value>The tags, or <see langword="null" /> to leave unchanged.</value>
    public IReadOnlyList<string>? Tags { get; }
}
