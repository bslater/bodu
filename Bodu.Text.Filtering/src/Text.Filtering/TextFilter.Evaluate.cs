// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TextFilter.Evaluate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Text.Filtering;

/// <content> The evaluation surface: <see cref="IsMatch(string)" />, <see cref="Evaluate" />,
/// <see cref="GetMatchingPatterns" />, and the shared evaluation core. </content>
public sealed partial class TextFilter
{
    /// <summary>
    /// Determines whether the filter accepts the specified value.
    /// </summary>
    /// <param name="value">The value to evaluate. Must not be <see langword="null" />.</param>
    /// <returns><see langword="true" /> when the value is accepted; otherwise <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value" /> is <see langword="null" />.</exception>
    public bool IsMatch(string value)
    {
        ThrowHelper.ThrowIfNull(value);

        return EvaluateCore(value.AsSpan()).IsMatch;
    }

    /// <summary>
    /// Determines whether the filter accepts the specified value.
    /// </summary>
    /// <param name="value">The value to evaluate.</param>
    /// <returns><see langword="true" /> when the value is accepted; otherwise <see langword="false" />.</returns>
    public bool IsMatch(ReadOnlySpan<char> value) =>
        EvaluateCore(value).IsMatch;

    /// <summary>
    /// Evaluates the specified value and reports the decision together with the pattern that decided it.
    /// </summary>
    /// <param name="value">The value to evaluate.</param>
    /// <returns>The decision and, when a pattern decided the outcome, that pattern.</returns>
    public TextFilterResult Evaluate(ReadOnlySpan<char> value) =>
        EvaluateCore(value);

    /// <summary>
    /// Returns every declared pattern that matches the specified value, in declaration order.
    /// </summary>
    /// <param name="value">The value to test.</param>
    /// <returns>The matching patterns; empty when none match.</returns>
    /// <remarks>
    /// This diagnostic surface tests all patterns without short-circuiting — unlike <see cref="Evaluate" />, which
    /// stops at the deciding pattern — and does not update the filter's statistics or invoke the observer. A pattern
    /// whose brace alternation expanded into several matchers is reported once. Regular-expression timeouts follow the
    /// same fail-safe rule as evaluation (a timed-out exclude reports as matching).
    /// </remarks>
    public IReadOnlyList<TextFilterPattern> GetMatchingPatterns(ReadOnlySpan<char> value)
    {
        var matched = new bool[_patterns.Length];
        var timeoutCount = 0;

        var group = _mode == TextFilterEvaluationMode.AnyMatch ? _includes : _ordered;
        MarkMatches(group, value, matched, ref timeoutCount);
        if (_mode == TextFilterEvaluationMode.AnyMatch)
            MarkMatches(_excludes, value, matched, ref timeoutCount);

        var results = new List<TextFilterPattern>();
        for (var i = 0; i < matched.Length; i++)
        {
            if (matched[i])
                results.Add(_patterns[i]);
        }

        return results;
    }

    /// <summary>
    /// Tests every compiled entry of one group against a value and marks the declaration slots that matched.
    /// </summary>
    /// <param name="group">The compiled entries to test.</param>
    /// <param name="value">The value to test.</param>
    /// <param name="matched">The per-declaration-slot match flags to update.</param>
    /// <param name="timeoutCount">Accumulates regular-expression timeouts.</param>
    private static void MarkMatches(CompiledPattern[] group, ReadOnlySpan<char> value, bool[] matched, ref int timeoutCount)
    {
        foreach (var entry in group)
        {
            if (!matched[entry.SourceIndex] && entry.IsMatch(value, ref timeoutCount))
                matched[entry.SourceIndex] = true;
        }
    }

    /// <summary>
    /// Evaluates one value: reaches the decision for the configured mode, updates the statistics counters, and notifies
    /// the observer.
    /// </summary>
    /// <param name="value">The value to evaluate.</param>
    /// <returns>The decision and deciding pattern.</returns>
    private TextFilterResult EvaluateCore(ReadOnlySpan<char> value)
    {
        var start = _captureTime ? Stopwatch.GetTimestamp() : 0L;
        var timeoutCount = 0;

        TextFilterDecision decision;
        var decidingIndex = -1;
        if (_mode == TextFilterEvaluationMode.AnyMatch)
            decision = EvaluateAnyMatch(value, ref decidingIndex, ref timeoutCount);
        else
            decision = EvaluateLastMatchWins(value, ref decidingIndex, ref timeoutCount);

        TextFilterPattern? deciding = null;
        if (decidingIndex >= 0)
        {
            // The compiled entry carries only the declaration index; resolve it to the declared pattern once, and
            // credit the hit to that slot — brace-expanded alternatives share their pattern's slot by design.
            deciding = _patterns[decidingIndex];
            _hitCounts[decidingIndex]++;
        }

        RecordDecision(decision, timeoutCount, start);

        // Snapshot the observer property once so a concurrent detach cannot null-ref between check and call; when no
        // observer is attached this whole hook costs one field read and a predictable branch.
        var observer = Observer;
        observer?.OnEvaluated(value, decision, deciding);

        return new TextFilterResult(decision, deciding);
    }

    /// <summary>
    /// Reaches the decision for a value under <see cref="TextFilterEvaluationMode.AnyMatch" /> semantics, scanning each
    /// group cheapest-strategy-first and short-circuiting on the first match.
    /// </summary>
    /// <param name="value">The value to evaluate.</param>
    /// <param name="decidingIndex">Receives the declaration index of the deciding pattern, or stays <c>-1</c>.</param>
    /// <param name="timeoutCount">Accumulates regular-expression timeouts.</param>
    /// <returns>The decision.</returns>
    private TextFilterDecision EvaluateAnyMatch(ReadOnlySpan<char> value, ref int decidingIndex, ref int timeoutCount)
    {
        // Includes run before excludes deliberately: with a non-empty include group the (usually large) fraction of
        // values matching no include exits here without ever touching the excludes, and with an empty include group
        // the _includeAll flag skips the loop entirely — so this order is never worse than excludes-first. Both
        // groups are pre-sorted cheapest-strategy-first, which is safe because set matching is an
        // order-independent OR.
        var includeIndex = -1;
        if (!_includeAll)
        {
            var includes = _includes;
            for (var i = 0; i < includes.Length; i++)
            {
                if (includes[i].IsMatch(value, ref timeoutCount))
                {
                    includeIndex = i;
                    break;
                }
            }

            // With a non-empty include group and no include hit, the excludes can never matter.
            if (includeIndex < 0)
                return TextFilterDecision.NotIncluded;
        }

        var excludes = _excludes;
        for (var i = 0; i < excludes.Length; i++)
        {
            if (excludes[i].IsMatch(value, ref timeoutCount))
            {
                decidingIndex = excludes[i].SourceIndex;
                return TextFilterDecision.Excluded;
            }
        }

        // No exclude vetoed: the earlier include hit (if any) is the deciding pattern; otherwise the value passed
        // purely because the include set is empty and no pattern gets the credit.
        if (includeIndex >= 0)
        {
            decidingIndex = _includes[includeIndex].SourceIndex;
            return TextFilterDecision.Included;
        }

        return TextFilterDecision.IncludedByDefault;
    }

    /// <summary>
    /// Reaches the decision for a value under <see cref="TextFilterEvaluationMode.LastMatchWins" /> semantics by
    /// scanning the rules last-to-first and letting the first hit decide.
    /// </summary>
    /// <param name="value">The value to evaluate.</param>
    /// <param name="decidingIndex">Receives the declaration index of the deciding pattern, or stays <c>-1</c>.</param>
    /// <param name="timeoutCount">Accumulates regular-expression timeouts.</param>
    /// <returns>The decision.</returns>
    private TextFilterDecision EvaluateLastMatchWins(ReadOnlySpan<char> value, ref int decidingIndex, ref int timeoutCount)
    {
        // "Last matching rule wins" evaluated directly: scan from the last rule toward the first and stop at the
        // first hit — the same trick git uses for .gitignore. Rule order is semantic here, so unlike AnyMatch these
        // entries are stored in declaration order and never cost-sorted.
        var ordered = _ordered;
        for (var i = ordered.Length - 1; i >= 0; i--)
        {
            if (ordered[i].IsMatch(value, ref timeoutCount))
            {
                decidingIndex = ordered[i].SourceIndex;
                return ordered[i].IsExclude ? TextFilterDecision.Excluded : TextFilterDecision.Included;
            }
        }

        return TextFilterDecision.IncludedByDefault;
    }
}
