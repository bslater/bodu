// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DequeDebugViewContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Contracts;

namespace Bodu.Collections.Generic.Contracts;

/// <summary>
/// Drives <see cref="DebugViewContractTests{TCollection}" /> against <see cref="Deque{T}" />.
/// </summary>
[TestClass]
public sealed class DequeDebugViewContractTests
    : DebugViewContractTests<Deque<int>>
{
    /// <inheritdoc />
    protected override Deque<int> Create()
    {
        Deque<int> deque = new();
        deque.AddLast(1);
        deque.AddLast(2);
        deque.AddFirst(0);
        return deque;
    }
}
