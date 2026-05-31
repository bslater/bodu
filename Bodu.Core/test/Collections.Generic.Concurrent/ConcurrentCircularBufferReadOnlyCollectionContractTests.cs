// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentCircularBufferReadOnlyCollectionContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Collections.Generic.Contracts;

namespace Bodu.Collections.Generic.Concurrent.Contracts;

/// <summary>
/// Drives <see cref="ReadOnlyCollectionContractTests{TCollection, TItem}" /> against
/// <see cref="ConcurrentCircularBuffer{T}" />. The type parameter is constrained to
/// <c>class?</c>, so the contract is exercised with <see cref="string" /> elements.
/// </summary>
[TestClass]
public sealed class ConcurrentCircularBufferReadOnlyCollectionContractTests
    : ReadOnlyCollectionContractTests<ConcurrentCircularBuffer<string>, string>
{
    /// <inheritdoc />
    protected override ConcurrentCircularBuffer<string> CreateEmpty() => new(capacity: 16);

    /// <inheritdoc />
    protected override ConcurrentCircularBuffer<string> Create(params string[] items)
    {
        ConcurrentCircularBuffer<string> buffer = new(capacity: Math.Max(16, items.Length));
        foreach (string item in items)
            buffer.Enqueue(item);
        return buffer;
    }

    /// <inheritdoc />
    protected override string CreateItem(int index) => $"item-{index}";
}
