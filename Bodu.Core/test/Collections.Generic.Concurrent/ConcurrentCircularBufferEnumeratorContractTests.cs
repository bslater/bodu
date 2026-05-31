// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentCircularBufferEnumeratorContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Collections.Generic.Contracts;

namespace Bodu.Collections.Generic.Concurrent.Contracts;

/// <summary>
/// Drives <see cref="EnumeratorContractTests{TEnumerable, TItem}" /> against
/// <see cref="ConcurrentCircularBuffer{T}" /> with <see cref="string" /> elements (the type parameter is
/// constrained to <c>class?</c>).
/// </summary>
[TestClass]
public sealed class ConcurrentCircularBufferEnumeratorContractTests
    : EnumeratorContractTests<ConcurrentCircularBuffer<string>, string>
{
    /// <inheritdoc />
    protected override ConcurrentCircularBuffer<string> Create(params string[] items)
    {
        ConcurrentCircularBuffer<string> buffer = new(capacity: Math.Max(16, items.Length));
        foreach (var item in items)
            buffer.Enqueue(item);
        return buffer;
    }

    /// <inheritdoc />
    protected override string CreateItem(int index) => $"enum-{index}";
}
