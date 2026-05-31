// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IndexedPriorityQueueDebugViewContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------


namespace Bodu.Collections.Generic.Contracts;

/// <summary>
/// Drives <see cref="DebugViewContractTests{TCollection}" /> against
/// <see cref="IndexedPriorityQueue{TElement, TPriority}" />. Asserts the standard Bodu
/// debugger-display contract — DebuggerDisplay, DebuggerTypeProxy, and an instance-constructible
/// proxy — is present and wired up correctly.
/// </summary>
[TestClass]
public sealed class IndexedPriorityQueueDebugViewContractTests
    : DebugViewContractTests<IndexedPriorityQueue<string, int>>
{
    /// <inheritdoc />
    protected override IndexedPriorityQueue<string, int> Create()
    {
        IndexedPriorityQueue<string, int> queue = new();
        queue.Enqueue("low", 10);
        queue.Enqueue("hi", 1);
        queue.Enqueue("mid", 5);
        return queue;
    }
}
