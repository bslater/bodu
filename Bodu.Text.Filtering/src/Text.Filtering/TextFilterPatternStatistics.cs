// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TextFilterPatternStatistics.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Filtering;

/// <summary>
/// Represents the accumulated statistics of one pattern within a <see cref="TextFilterStatistics" /> snapshot.
/// </summary>
public sealed class TextFilterPatternStatistics
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TextFilterPatternStatistics" /> class.
    /// </summary>
    /// <param name="pattern">The pattern the statistics describe.</param>
    /// <param name="hitCount">The number of evaluations this pattern decided.</param>
    internal TextFilterPatternStatistics(TextFilterPattern pattern, long hitCount)
    {
        Pattern = pattern;
        HitCount = hitCount;
    }

    /// <summary>
    /// Gets the pattern the statistics describe.
    /// </summary>
    public TextFilterPattern Pattern { get; }

    /// <summary>
    /// Gets the number of evaluations this pattern decided — the include that admitted the value or the exclude that
    /// rejected it. Patterns that matched without deciding the outcome are not counted.
    /// </summary>
    public long HitCount { get; }
}
