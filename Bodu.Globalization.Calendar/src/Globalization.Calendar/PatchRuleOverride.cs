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
    /// <param name="durationDays">The replacement duration, or <see langword="null" /> to leave unchanged.</param>
    /// <param name="applicability">
    /// The replacement applicability, or <see langword="null" /> to leave unchanged.
    /// </param>
    /// <param name="strategy">The replacement strategy, or <see langword="null" /> to leave unchanged.</param>
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
        int? durationDays,
        RuleApplicability? applicability,
        IDateCalculationStrategy? strategy,
        IReadOnlyList<string>? adjustmentPolicyRefs,
        IReadOnlyList<string>? tags)
        : base(notableDateRef)
    {
        ThrowHelper.ThrowIfNull(ruleRef);

        RuleRef = ruleRef;
        Priority = priority;
        Category = category;
        NonWorking = nonWorking;
        DurationDays = durationDays;
        Applicability = applicability;
        Strategy = strategy;
        AdjustmentPolicyRefs = adjustmentPolicyRefs;
        Tags = tags;
    }

    /// <summary>
    /// Gets the identifier of the rule to patch.
    /// </summary>
    /// <returns>The targeted rule id.</returns>
    public string RuleRef { get; }

    /// <summary>
    /// Gets the replacement priority.
    /// </summary>
    /// <returns>The priority, or <see langword="null" /> to leave unchanged.</returns>
    public int? Priority { get; }

    /// <summary>
    /// Gets the replacement category.
    /// </summary>
    /// <returns>The category, or <see langword="null" /> to leave unchanged.</returns>
    public NotableDateCategory? Category { get; }

    /// <summary>
    /// Gets the replacement non-working-day flag.
    /// </summary>
    /// <returns>The flag, or <see langword="null" /> to leave unchanged.</returns>
    public bool? NonWorking { get; }

    /// <summary>
    /// Gets the replacement duration.
    /// </summary>
    /// <returns>The duration, or <see langword="null" /> to leave unchanged.</returns>
    public int? DurationDays { get; }

    /// <summary>
    /// Gets the replacement applicability.
    /// </summary>
    /// <returns>The applicability, or <see langword="null" /> to leave unchanged.</returns>
    public RuleApplicability? Applicability { get; }

    /// <summary>
    /// Gets the replacement strategy.
    /// </summary>
    /// <returns>The strategy, or <see langword="null" /> to leave unchanged.</returns>
    public IDateCalculationStrategy? Strategy { get; }

    /// <summary>
    /// Gets the replacement adjustment references.
    /// </summary>
    /// <returns>The references, or <see langword="null" /> to leave unchanged.</returns>
    public IReadOnlyList<string>? AdjustmentPolicyRefs { get; }

    /// <summary>
    /// Gets the replacement tags.
    /// </summary>
    /// <returns>The tags, or <see langword="null" /> to leave unchanged.</returns>
    public IReadOnlyList<string>? Tags { get; }
}
