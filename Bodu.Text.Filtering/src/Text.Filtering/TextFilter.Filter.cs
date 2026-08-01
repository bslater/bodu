// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TextFilter.Filter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Filtering;

/// <content> The sequence-filtering surface: deferred <see cref="Filter" /> and eager <see cref="FilterToList" />.
/// </content>
public sealed partial class TextFilter
{
    /// <summary>
    /// Filters a sequence, yielding the values the filter accepts in their source order.
    /// </summary>
    /// <param name="source">The sequence to filter. Must not be <see langword="null" />.</param>
    /// <returns>A deferred sequence of the accepted values.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// Evaluation is deferred: values are evaluated — and statistics updated — as the result is enumerated.
    /// <see langword="null" /> elements are dropped without being evaluated, since no pattern can match them.
    /// </remarks>
    public IEnumerable<string> Filter(IEnumerable<string> source)
    {
        ThrowHelper.ThrowIfNull(source);

        return FilterIterator(source);
    }

    /// <summary>
    /// Filters an indexed list eagerly, returning the accepted values in their source order.
    /// </summary>
    /// <param name="source">The list to filter. Must not be <see langword="null" />.</param>
    /// <returns>A new list containing the accepted values.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// This is the bulk-throughput surface: a tight indexed loop with the result list pre-sized to the source count.
    /// <see langword="null" /> elements are dropped without being evaluated.
    /// </remarks>
    public List<string> FilterToList(IReadOnlyList<string> source)
    {
        ThrowHelper.ThrowIfNull(source);

        var results = new List<string>(source.Count);
        for (var i = 0; i < source.Count; i++)
        {
            var value = source[i];
            if (value is not null && EvaluateCore(value.AsSpan()).IsMatch)
                results.Add(value);
        }

        return results;
    }

    /// <summary>
    /// Enumerates a source sequence and yields the values the filter accepts.
    /// </summary>
    /// <param name="source">The sequence to filter.</param>
    /// <returns>The accepted values.</returns>
    private IEnumerable<string> FilterIterator(IEnumerable<string> source)
    {
        foreach (var value in source)
        {
            if (value is not null && EvaluateCore(value.AsSpan()).IsMatch)
                yield return value;
        }
    }
}
