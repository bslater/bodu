// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ITextFilterObserver.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Filtering;

/// <summary>
/// Receives a callback for every value a <see cref="TextFilter" /> evaluates, carrying the decision reached and the
/// pattern that decided it.
/// </summary>
/// <remarks>
/// <para>
/// Attach an implementation via <see cref="TextFilter.Observer" /> to see exactly which values matched, which were
/// rejected, and by which pattern — for diagnostics, sampling, or audit logging. When no observer is attached the
/// filter's evaluation path pays only a single null check.
/// </para>
/// <para>
/// The callback runs synchronously on the evaluating thread, once per evaluated value; exceptions it throws propagate
/// to the caller of the evaluating member. Implementations should be fast and must not re-enter the filter.
/// </para>
/// </remarks>
public interface ITextFilterObserver
{
    /// <summary>
    /// Called after the filter reaches a decision for one evaluated value.
    /// </summary>
    /// <param name="value">The value that was evaluated.</param>
    /// <param name="decision">The decision reached.</param>
    /// <param name="pattern">
    /// The pattern that decided the outcome, or <see langword="null" /> when the decision is
    /// <see cref="TextFilterDecision.IncludedByDefault" /> or <see cref="TextFilterDecision.NotIncluded" />.
    /// </param>
    void OnEvaluated(ReadOnlySpan<char> value, TextFilterDecision decision, TextFilterPattern? pattern);
}
