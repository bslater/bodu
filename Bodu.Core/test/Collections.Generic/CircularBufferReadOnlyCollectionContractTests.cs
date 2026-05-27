// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CircularBufferReadOnlyCollectionContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------


namespace Bodu.Collections.Generic.Contracts;

/// <summary>
/// Drives <see cref="ReadOnlyCollectionContractTests{TCollection, TItem}" /> against
/// <see cref="CircularBuffer{T}" /> with <see cref="int" /> elements. <see cref="CircularBuffer{T}" />
/// inherits <see cref="IReadOnlyCollection{T}" /> via <c>RingBackedCollection&lt;T&gt;</c> but does not
/// implement the generic <see cref="ICollection{T}" />, so the read-only contract is the appropriate
/// surface to exercise.
/// </summary>
[TestClass]
public sealed class CircularBufferReadOnlyCollectionContractTests
    : ReadOnlyCollectionContractTests<CircularBuffer<int>, int>
{
    /// <inheritdoc />
    protected override CircularBuffer<int> CreateEmpty() => new(capacity: 16);

    /// <inheritdoc />
    protected override CircularBuffer<int> Create(params int[] items)
    {
        CircularBuffer<int> buffer = new(capacity: Math.Max(16, items.Length));
        foreach (int item in items)
            buffer.Enqueue(item);
        return buffer;
    }

    /// <inheritdoc />
    protected override int CreateItem(int index) => 100 + index;
}
