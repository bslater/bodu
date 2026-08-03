// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TextFilterStatistics.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Filtering;

/// <summary>
/// Represents an immutable snapshot of a <see cref="TextFilter" />'s accumulated evaluation statistics, obtained via
/// <see cref="TextFilter.GetStatistics" />.
/// </summary>
/// <remarks>
/// Counter totals reconcile: <see cref="ItemsEvaluated" /> equals <see cref="ItemsAccepted" /> +
/// <see cref="ItemsExcluded" /> + <see cref="ItemsNotIncluded" />. Under concurrent evaluation the counters may
/// undercount (they are deliberately not interlocked to keep the evaluation path cheap); matching results themselves
/// are always exact.
/// </remarks>
public sealed class TextFilterStatistics
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TextFilterStatistics" /> class.
    /// </summary>
    /// <param name="itemsEvaluated">The total number of values evaluated.</param>
    /// <param name="itemsAccepted">The number of values accepted.</param>
    /// <param name="itemsExcluded">The number of values rejected by an exclude pattern.</param>
    /// <param name="itemsNotIncluded">The number of values rejected because no include pattern matched.</param>
    /// <param name="regexTimeouts">The number of fail-safe decisions taken after a regular-expression timeout.</param>
    /// <param name="evaluationTime">The accumulated evaluation time, when capture is enabled.</param>
    /// <param name="patterns">The per-pattern statistics, in pattern declaration order.</param>
    internal TextFilterStatistics(
        long itemsEvaluated,
        long itemsAccepted,
        long itemsExcluded,
        long itemsNotIncluded,
        long regexTimeouts,
        TimeSpan evaluationTime,
        IReadOnlyList<TextFilterPatternStatistics> patterns)
    {
        ItemsEvaluated = itemsEvaluated;
        ItemsAccepted = itemsAccepted;
        ItemsExcluded = itemsExcluded;
        ItemsNotIncluded = itemsNotIncluded;
        RegexTimeouts = regexTimeouts;
        EvaluationTime = evaluationTime;
        Patterns = patterns;
    }

    /// <summary>
    /// Gets the total number of values evaluated since the filter was built or its statistics were last reset.
    /// </summary>
    public long ItemsEvaluated { get; }

    /// <summary>
    /// Gets the number of evaluated values that were accepted — the sum of the
    /// <see cref="TextFilterDecision.IncludedByDefault" /> and <see cref="TextFilterDecision.Included" /> outcomes.
    /// </summary>
    public long ItemsAccepted { get; }

    /// <summary>
    /// Gets the number of evaluated values rejected by an exclude pattern (the
    /// <see cref="TextFilterDecision.Excluded" /> outcome).
    /// </summary>
    public long ItemsExcluded { get; }

    /// <summary>
    /// Gets the number of evaluated values rejected because include patterns exist and none matched (the
    /// <see cref="TextFilterDecision.NotIncluded" /> outcome).
    /// </summary>
    public long ItemsNotIncluded { get; }

    /// <summary>
    /// Gets the number of pattern evaluations that timed out in the regular-expression engine and were decided
    /// fail-safe (a timed-out include treated as not matching; a timed-out exclude treated as matching).
    /// </summary>
    public long RegexTimeouts { get; }

    /// <summary>
    /// Gets the accumulated time spent evaluating values.
    /// </summary>
    /// <value>
    /// The accumulated evaluation time, or <see cref="TimeSpan.Zero" /> when
    /// <see cref="TextFilterOptions.CaptureEvaluationTime" /> is disabled.
    /// </value>
    public TimeSpan EvaluationTime { get; }

    /// <summary>
    /// Gets the per-pattern statistics, ordered as the patterns were declared.
    /// </summary>
    public IReadOnlyList<TextFilterPatternStatistics> Patterns { get; }
}
