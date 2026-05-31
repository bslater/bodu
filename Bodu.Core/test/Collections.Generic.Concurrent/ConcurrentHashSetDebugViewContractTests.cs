// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentHashSetDebugViewContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Collections.Generic.Contracts;

namespace Bodu.Collections.Generic.Concurrent.Contracts;

/// <summary>
/// Drives <see cref="DebugViewContractTests{TCollection}" /> against <see cref="ConcurrentHashSet{T}" />.
/// Asserts the standard Bodu debugger-display contract — DebuggerDisplay, DebuggerTypeProxy, and an
/// instance-constructible proxy — is present and wired up correctly.
/// </summary>
[TestClass]
public sealed class ConcurrentHashSetDebugViewContractTests
    : DebugViewContractTests<ConcurrentHashSet<int>>
{
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
