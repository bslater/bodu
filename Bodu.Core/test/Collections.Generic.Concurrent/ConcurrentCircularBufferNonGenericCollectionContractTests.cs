// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentCircularBufferNonGenericCollectionContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Collections.Generic.Contracts;

namespace Bodu.Collections.Generic.Concurrent.Contracts;

/// <summary>
/// Drives <see cref="NonGenericCollectionContractTests{TCollection}" /> against
/// <see cref="ConcurrentCircularBuffer{T}" />, exercising the non-generic
/// <see cref="System.Collections.ICollection" /> surface (<c>Count</c>, <c>SyncRoot</c>,
/// <c>IsSynchronized</c>, <c>CopyTo</c>). Concurrency stress lives in
/// <c>ConcurrentCircularBufferTests._Stress.cs</c> (regression/stress tier).
/// </summary>
[TestClass]
public sealed class ConcurrentCircularBufferNonGenericCollectionContractTests
    : NonGenericCollectionContractTests<ConcurrentCircularBuffer<string>>
{
    /// <inheritdoc />
    /// <remarks>ConcurrentCircularBuffer intentionally throws <see cref="NotSupportedException" /> from <c>SyncRoot</c> to prevent callers taking the same lock used internally.</remarks>
    protected override bool SyncRootSupported => false;

    /// <inheritdoc />
    protected override ConcurrentCircularBuffer<string> CreateEmpty() => new(capacity: 16);

    /// <inheritdoc />
    protected override ConcurrentCircularBuffer<string> Create()
    {
        ConcurrentCircularBuffer<string> buffer = new(capacity: 8);
        buffer.Enqueue("a");
        buffer.Enqueue("b");
        buffer.Enqueue("c");
        return buffer;
    }
}
