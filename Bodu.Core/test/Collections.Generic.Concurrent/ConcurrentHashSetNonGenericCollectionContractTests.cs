// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentHashSetNonGenericCollectionContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Collections.Generic.Contracts;

namespace Bodu.Collections.Generic.Concurrent.Contracts;

/// <summary>
/// Drives <see cref="NonGenericCollectionContractTests{TCollection}" /> against
/// <see cref="ConcurrentHashSet{T}" />, exercising the non-generic
/// <see cref="System.Collections.ICollection" /> surface (<c>Count</c>, <c>SyncRoot</c>,
/// <c>IsSynchronized</c>, <c>CopyTo</c>). Set semantics are covered separately by
/// <c>ConcurrentHashSetSetContractTests</c>; concurrency stress lives in
/// <c>ConcurrentHashSetTests._Stress.cs</c>.
/// </summary>
[TestClass]
public sealed class ConcurrentHashSetNonGenericCollectionContractTests
    : NonGenericCollectionContractTests<ConcurrentHashSet<int>>
{
    /// <inheritdoc />
    /// <remarks>ConcurrentHashSet intentionally throws <see cref="NotSupportedException" /> from <c>SyncRoot</c> to prevent callers taking the same lock used internally.</remarks>
    protected override bool SyncRootSupported => false;

    /// <inheritdoc />
    protected override ConcurrentHashSet<int> CreateEmpty() => new();

    /// <inheritdoc />
    protected override ConcurrentHashSet<int> Create()
    {
        ConcurrentHashSet<int> set = new();
        set.Add(1);
        set.Add(2);
        set.Add(3);
        return set;
    }
}
