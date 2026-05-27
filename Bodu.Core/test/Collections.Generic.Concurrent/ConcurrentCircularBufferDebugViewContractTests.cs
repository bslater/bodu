// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentCircularBufferDebugViewContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Collections.Generic.Contracts;

namespace Bodu.Collections.Generic.Concurrent.Contracts;

/// <summary>
/// Drives <see cref="DebugViewContractTests{TCollection}" /> against
/// <see cref="ConcurrentCircularBuffer{T}" />.
/// </summary>
[TestClass]
public sealed class ConcurrentCircularBufferDebugViewContractTests
    : DebugViewContractTests<ConcurrentCircularBuffer<string>>
{
    /// <inheritdoc />
    protected override ConcurrentCircularBuffer<string> Create()
    {
        ConcurrentCircularBuffer<string> buffer = new(capacity: 8);
        buffer.Enqueue("alpha");
        buffer.Enqueue("beta");
        buffer.Enqueue("gamma");
        return buffer;
    }
}
