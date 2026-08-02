// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TextFilterOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Filtering;

/// <summary>
/// Provides configuration for building a <see cref="TextFilter" />.
/// </summary>
/// <remarks>
/// Options are read once when the filter is built; changing an options instance afterwards has no effect on filters
/// already built from it. Invalid option values are reported by
/// <see cref="TextFilter.Build(System.Collections.Generic.IEnumerable{TextFilterPattern}, TextFilterOptions?)" />
/// rather than by the property setters.
/// </remarks>
public sealed class TextFilterOptions
{
    /// <summary>
    /// Gets the evaluation mode that determines how patterns combine into a decision.
    /// </summary>
    /// <value>The evaluation mode. The default is <see cref="TextFilterEvaluationMode.AnyMatch" />.</value>
    public TextFilterEvaluationMode Mode { get; init; } = TextFilterEvaluationMode.AnyMatch;

    /// <summary>
    /// Gets a value indicating whether patterns match case-insensitively by default.
    /// </summary>
    /// <value>
    /// <see langword="true" /> to match ordinally ignoring case; <see langword="false" /> to match ordinally and
    /// case-sensitively. The default is <see langword="true" />. Individual patterns can override this via
    /// <see cref="TextFilterPattern.IgnoreCase" />.
    /// </value>
    /// <remarks>
    /// Comparison is always ordinal — culture-sensitive comparison is not supported, because the engine's optimized
    /// matchers operate on raw character values.
    /// </remarks>
    public bool IgnoreCase { get; init; } = true;

    /// <summary>
    /// Gets the match timeout applied to regular-expression patterns.
    /// </summary>
    /// <value>
    /// The per-match timeout. The default is 100 milliseconds. Must be positive or
    /// <see cref="System.Text.RegularExpressions.Regex.InfiniteMatchTimeout" />.
    /// </value>
    /// <remarks>
    /// The timeout is a guard against pathologically backtracking regular expressions. The filter prefers the
    /// linear-time <see cref="System.Text.RegularExpressions.RegexOptions.NonBacktracking" /> engine, under which the
    /// timeout is effectively unreachable; it applies to patterns that fall back to the backtracking engine. A
    /// timed-out pattern fails safe: a timed-out include does not admit the value, and a timed-out exclude still
    /// rejects it.
    /// </remarks>
    public TimeSpan RegexMatchTimeout { get; init; } = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Gets a value indicating whether the filter accumulates evaluation time in its statistics.
    /// </summary>
    /// <value>
    /// <see langword="true" /> to measure and accumulate the time spent evaluating values, exposed via
    /// <see cref="TextFilterStatistics.EvaluationTime" />; otherwise <see langword="false" />. The default is
    /// <see langword="false" />.
    /// </value>
    public bool CaptureEvaluationTime { get; init; }
}
