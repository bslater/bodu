// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IndexedPriorityQueueTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

[TestClass]
public partial class IndexedPriorityQueueTests
{

    /// <summary>
    /// Asserts that the priorities of <paramref name="pairs"/> are non-decreasing under
    /// <see cref="Comparer{T}.Default"/>.
    /// </summary>
    /// <param name="pairs">The drained pairs to validate.</param>
    private static void AssertNonDecreasing<TElement, TPriority>(KeyValuePair<TElement, TPriority>[] pairs)
        where TElement : notnull
    {
        Comparer<TPriority> comparer = Comparer<TPriority>.Default;
        for (var i = 1; i < pairs.Length; i++)
            Assert.IsLessThanOrEqualTo(
                0, comparer.Compare(pairs[i - 1].Value, pairs[i].Value),
                $"Priorities not non-decreasing at index {i}: {pairs[i - 1].Value} > {pairs[i].Value}");
    }
    /// <summary>
    /// Drains the queue and returns every element-priority pair in dequeue order.
    /// </summary>
    /// <param name="queue">The queue to drain.</param>
    /// <returns>An array of pairs ordered by ascending priority.</returns>
    private static KeyValuePair<TElement, TPriority>[] DrainAll<TElement, TPriority>(
        IndexedPriorityQueue<TElement, TPriority> queue)
        where TElement : notnull
    {
        var list = new List<KeyValuePair<TElement, TPriority>>(queue.Count);
        while (queue.Count > 0)
            list.Add(queue.Dequeue());
        return list.ToArray();
    }

}
