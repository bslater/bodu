// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RuleStaticProfile.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.RangeResolution;

/// <summary>
/// Captures the static, year-independent characteristics of a <see cref="NotableDateRule" /> used by the chronological
/// range-resolution pipeline to scope per-request processing.
/// </summary>
/// <remarks>
/// <para>
/// A profile records the rule's processing tier, transitive root anchor (when offset-derived), the base offset relative
/// to that anchor, and the minimum and maximum day deltas that any observance adjustment can produce. Profiles are
/// built once at service construction by <see cref="RuleStaticAnalysis" /> and re-used across every range-resolution
/// request.
/// </para>
/// <para>
/// <see cref="MinObservedReach" /> and <see cref="MaxObservedReach" /> express the day-delta envelope of the rule's
/// observable date space relative to the rule's <em>own anchor date</em>. They include every adjustment's worst-case
/// shift and the duration of the observable span. The pipeline uses this envelope to decide which civil years and which
/// rules can possibly contribute to a requested chronological window.
/// </para>
/// </remarks>
internal sealed record RuleStaticProfile(
    NotableDateRule Rule,
    RuleTier Tier,
    NotableDateRuleIdentity? RootAnchorIdentity,
    int OffsetFromRoot,
    int MinObservedReach,
    int MaxObservedReach)
{
    /// <summary>
    /// Gets the canonical name of the rule's transitive root anchor, for display and grouping only.
    /// </summary>
    /// <remarks>
    /// Execution paths (anchor materialization, cache lookup, anchor-year planning) bind on the full
    /// <see cref="RootAnchorIdentity" /> so that same-name variants do not collapse; this projection is provided for
    /// diagnostics and human-readable assertions where the canonical name is sufficient.
    /// </remarks>
    /// <returns>The root anchor's canonical name, or <see langword="null" /> when the rule has no offset root.</returns>
    public string? RootAnchorRuleName => RootAnchorIdentity?.Name;

    /// <summary>
    /// Gets a value indicating whether this rule depends transitively on an algorithmic anchor.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when <see cref="Tier" /> is <see cref="RuleTier.Algorithmic" /> or
    /// <see cref="RuleTier.OffsetFromAlgorithmic" />; otherwise <see langword="false" />.
    /// </returns>
    public bool DependsOnAlgorithmicAnchor =>
        Tier is RuleTier.Algorithmic or RuleTier.OffsetFromAlgorithmic;

    /// <summary>
    /// Gets the inclusive upper bound on how far an observed date for this rule can move in the positive direction
    /// relative to its own anchor.
    /// </summary>
    /// <returns>A non-negative day count expressing the rule's maximum forward shift.</returns>
    public int MaxForwardReach => MaxObservedReach;

    /// <summary>
    /// Gets the inclusive lower bound on how far an observed date for this rule can move in the negative direction
    /// relative to its own anchor.
    /// </summary>
    /// <returns>A non-positive day count expressing the rule's maximum backward shift.</returns>
    public int MinBackwardReach => MinObservedReach;
}
