// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DequeNonGenericCollectionContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Contracts;

namespace Bodu.Collections.Generic.Contracts;

/// <summary>
/// Drives <see cref="NonGenericCollectionContractTests{TCollection}" /> against <see cref="Deque{T}" />.
/// </summary>
[TestClass]
public sealed class DequeNonGenericCollectionContractTests
    : NonGenericCollectionContractTests<Deque<int>>
{
    /// <inheritdoc />
    protected override Deque<int> CreateEmpty() => new();

    /// <inheritdoc />
    protected override Deque<int> Create()
    {
        Deque<int> deque = new();
        deque.AddLast(1);
        deque.AddLast(2);
        deque.AddLast(3);
        return deque;
    }
}
