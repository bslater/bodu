// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CircularBufferNonGenericCollectionContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------


namespace Bodu.Collections.Generic.Contracts;

/// <summary>
/// Drives <see cref="NonGenericCollectionContractTests{TCollection}" /> against
/// <see cref="CircularBuffer{T}" />, exercising the non-generic <see cref="System.Collections.ICollection" />
/// surface that <c>RingBackedCollection&lt;T&gt;</c> implements (<c>Count</c>, <c>SyncRoot</c>,
/// <c>IsSynchronized</c>, <c>CopyTo</c>).
/// </summary>
[TestClass]
public sealed class CircularBufferNonGenericCollectionContractTests
    : NonGenericCollectionContractTests<CircularBuffer<int>>
{
    /// <inheritdoc />
    protected override CircularBuffer<int> CreateEmpty() => new(capacity: 16);

    /// <inheritdoc />
    protected override CircularBuffer<int> Create()
    {
        CircularBuffer<int> buffer = new(capacity: 8);
        buffer.Enqueue(1);
        buffer.Enqueue(2);
        buffer.Enqueue(3);
        return buffer;
    }
}
