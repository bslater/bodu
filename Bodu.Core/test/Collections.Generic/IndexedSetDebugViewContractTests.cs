// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IndexedSetDebugViewContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Contracts;

namespace Bodu.Collections.Generic.Contracts;

/// <summary>
/// Drives <see cref="DebugViewContractTests{TCollection}" /> against <see cref="IndexedSet{T}" />.
/// Asserts the standard Bodu debugger-display contract — the <c>DebuggerDisplayAttribute</c>,
/// <c>DebuggerTypeProxyAttribute</c>, and instance-constructible debug-view proxy — is present and
/// wired up correctly.
/// </summary>
[TestClass]
public sealed class IndexedSetDebugViewContractTests
    : DebugViewContractTests<IndexedSet<int>>
{
    /// <inheritdoc />
    protected override IndexedSet<int> Create()
    {
        IndexedSet<int> set = new();
        set.Add(1);
        set.Add(2);
        set.Add(3);
        return set;
    }
}
