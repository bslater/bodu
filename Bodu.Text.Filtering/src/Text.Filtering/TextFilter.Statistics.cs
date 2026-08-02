// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TextFilter.Statistics.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Text.Filtering;

/// <content> The telemetry counters and the statistics snapshot surface. </content>
public sealed partial class TextFilter
{
    /// <summary>The per-pattern deciding-hit counters, indexed by pattern declaration index.</summary>
    /// <remarks>
    /// One contiguous array keeps the hot-path increment cache-friendly. Increments are deliberately plain (not
    /// interlocked) so evaluation stays cheap; see the thread-safety remarks on <see cref="TextFilter" />.
    /// </remarks>
    private readonly long[] _hitCounts;

    /// <summary>The total number of values evaluated.</summary>
    private long _itemsEvaluated;

    /// <summary>The number of values accepted (included by default or by an include pattern).</summary>
    private long _itemsAccepted;

    /// <summary>The number of values rejected by an exclude pattern.</summary>
    private long _itemsExcluded;

    /// <summary>The number of values rejected because no include pattern matched.</summary>
    private long _itemsNotIncluded;

    /// <summary>The number of regular-expression timeouts decided fail-safe.</summary>
    private long _regexTimeouts;

    /// <summary>The accumulated evaluation time in <see cref="Stopwatch" /> timestamp ticks.</summary>
    private long _evaluationTimestampTicks;

    /// <summary>
    /// Returns an immutable snapshot of the filter's accumulated statistics.
    /// </summary>
    /// <returns>The snapshot.</returns>
    public TextFilterStatistics GetStatistics()
    {
        var patterns = new TextFilterPatternStatistics[_patterns.Length];
        for (var i = 0; i < patterns.Length; i++)
            patterns[i] = new TextFilterPatternStatistics(_patterns[i], Volatile.Read(ref _hitCounts[i]));

        var ticks = Volatile.Read(ref _evaluationTimestampTicks);

        return new TextFilterStatistics(
            Volatile.Read(ref _itemsEvaluated),
            Volatile.Read(ref _itemsAccepted),
            Volatile.Read(ref _itemsExcluded),
            Volatile.Read(ref _itemsNotIncluded),
            Volatile.Read(ref _regexTimeouts),
            ticks == 0 ? TimeSpan.Zero : Stopwatch.GetElapsedTime(0, ticks),
            patterns);
    }

    /// <summary>
    /// Resets every statistics counter to zero.
    /// </summary>
    public void ResetStatistics()
    {
        Volatile.Write(ref _itemsEvaluated, 0);
        Volatile.Write(ref _itemsAccepted, 0);
        Volatile.Write(ref _itemsExcluded, 0);
        Volatile.Write(ref _itemsNotIncluded, 0);
        Volatile.Write(ref _regexTimeouts, 0);
        Volatile.Write(ref _evaluationTimestampTicks, 0);
        Array.Clear(_hitCounts);
    }

    /// <summary>
    /// Updates the scalar counters after one evaluation reaches its decision.
    /// </summary>
    /// <param name="decision">The decision reached.</param>
    /// <param name="timeoutCount">The number of regular-expression timeouts hit during the evaluation.</param>
    /// <param name="startTimestamp">
    /// The evaluation's starting <see cref="Stopwatch" /> timestamp, when timing is captured.
    /// </param>
    private void RecordDecision(TextFilterDecision decision, int timeoutCount, long startTimestamp)
    {
        _itemsEvaluated++;
        if (decision is TextFilterDecision.IncludedByDefault or TextFilterDecision.Included)
            _itemsAccepted++;
        else if (decision == TextFilterDecision.Excluded)
            _itemsExcluded++;
        else
            _itemsNotIncluded++;

        if (timeoutCount != 0)
            _regexTimeouts += timeoutCount;

        if (_captureTime)
            _evaluationTimestampTicks += Stopwatch.GetTimestamp() - startTimestamp;
    }
}
