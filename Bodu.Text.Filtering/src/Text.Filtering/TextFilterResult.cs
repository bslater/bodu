// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TextFilterResult.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Filtering;

/// <summary>
/// Represents the outcome of evaluating a single value against a <see cref="TextFilter" />: the decision reached and
/// the pattern that decided it, if any.
/// </summary>
public readonly struct TextFilterResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TextFilterResult" /> struct.
    /// </summary>
    /// <param name="decision">The decision reached for the value.</param>
    /// <param name="pattern">
    /// The pattern that decided the outcome, or <see langword="null" /> when no pattern was involved.
    /// </param>
    internal TextFilterResult(TextFilterDecision decision, TextFilterPattern? pattern)
    {
        Decision = decision;
        Pattern = pattern;
    }

    /// <summary>
    /// Gets the decision reached for the value.
    /// </summary>
    public TextFilterDecision Decision { get; }

    /// <summary>
    /// Gets the pattern that decided the outcome.
    /// </summary>
    /// <value>
    /// The deciding pattern, or <see langword="null" /> when the decision is
    /// <see cref="TextFilterDecision.IncludedByDefault" /> or <see cref="TextFilterDecision.NotIncluded" />. In
    /// <see cref="TextFilterEvaluationMode.AnyMatch" /> mode, when several patterns could have matched, the reported
    /// pattern is one of them (the cheapest to evaluate), not necessarily the first declared; in
    /// <see cref="TextFilterEvaluationMode.LastMatchWins" /> mode it is exactly the last matching rule.
    /// </value>
    public TextFilterPattern? Pattern { get; }

    /// <summary>
    /// Gets a value indicating whether the value was accepted by the filter.
    /// </summary>
    /// <value>
    /// <see langword="true" /> when <see cref="Decision" /> is <see cref="TextFilterDecision.IncludedByDefault" /> or
    /// <see cref="TextFilterDecision.Included" />; otherwise <see langword="false" />.
    /// </value>
    public bool IsMatch =>
        Decision is TextFilterDecision.IncludedByDefault or TextFilterDecision.Included;
}
