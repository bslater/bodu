// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CircularBufferDebugViewContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------


namespace Bodu.Collections.Generic.Contracts;

/// <summary>
/// Drives <see cref="DebugViewContractTests{TCollection}" /> against <see cref="CircularBuffer{T}" />.
/// Asserts the standard Bodu debugger-display contract — the <c>DebuggerDisplayAttribute</c>,
/// <c>DebuggerTypeProxyAttribute</c>, and instance-constructible debug-view proxy — is present and wired
/// up correctly.
/// </summary>
[TestClass]
public sealed class CircularBufferDebugViewContractTests
    : DebugViewContractTests<CircularBuffer<int>>
{
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
