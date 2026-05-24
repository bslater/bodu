// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IndexedPriorityQueueDebugView.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Collections.Generic;

/// <summary>
/// Provides a debugger-friendly view of the contents of an <see cref="IndexedPriorityQueue{TElement, TPriority}" />.
/// </summary>
/// <typeparam name="TElement">Specifies the element type of the underlying queue.</typeparam>
/// <typeparam name="TPriority">Specifies the priority type of the underlying queue.</typeparam>
/// <remarks>
/// The view materializes the queue's element-priority pairs into an array sorted by priority so that the debugger
/// surfaces the dequeue order rather than the heap-storage order. The underlying queue is not mutated by this view.
/// </remarks>
internal sealed class IndexedPriorityQueueDebugView<TElement, TPriority>
    where TElement : notnull
{
    /// <summary>
    /// The queue instance being inspected.
    /// </summary>
    private readonly IndexedPriorityQueue<TElement, TPriority> _queue;

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexedPriorityQueueDebugView{TElement, TPriority}" /> class.
    /// </summary>
    /// <param name="queue">The queue instance to debug. Must not be <see langword="null" />.</param>
    /// <exception cref="ArgumentNullException"><paramref name="queue" /> is <see langword="null" />.</exception>
    public IndexedPriorityQueueDebugView(IndexedPriorityQueue<TElement, TPriority> queue)
    {
        _queue = queue ?? throw new ArgumentNullException(nameof(queue));
    }

    /// <summary>
    /// Gets the queue's element-priority pairs sorted in dequeue (priority) order for display in the debugger.
    /// </summary>
    /// <returns>An array of pairs ordered ascending by the queue's configured comparer.</returns>
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public KeyValuePair<TElement, TPriority>[] Items =>
        [.. _queue.OrderBy(pair => pair.Value, _queue.Comparer)];
}
