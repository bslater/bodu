// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CircularBufferEnumeratorContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Contracts;

namespace Bodu.Collections.Generic.Contracts;

/// <summary>
/// Drives <see cref="EnumeratorContractTests{TEnumerable, TItem}" /> against
/// <see cref="CircularBuffer{T}" /> with <see cref="int" /> elements. The base exercises the shared
/// <see cref="IEnumerator{T}" /> contract; iteration-order specifics live in dedicated CircularBuffer
/// tests.
/// </summary>
[TestClass]
public sealed class CircularBufferEnumeratorContractTests
    : EnumeratorContractTests<CircularBuffer<int>, int>
{
    /// <inheritdoc />
    protected override CircularBuffer<int> Create(params int[] items)
    {
        CircularBuffer<int> buffer = new(capacity: Math.Max(16, items.Length));
        foreach (int item in items)
            buffer.Enqueue(item);
        return buffer;
    }

    /// <inheritdoc />
    protected override int CreateItem(int index) => 200 + index;
}
