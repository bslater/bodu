// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalTreeGenericDebugViewContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Contracts;

/// <summary>
/// Drives <see cref="DebugViewContractTests{TCollection}" /> against <see cref="IntervalTree{TKey, TValue}" />.
/// Asserts the standard Bodu debugger-display contract — DebuggerDisplay, DebuggerTypeProxy, and an
/// instance-constructible proxy — is present and wired up correctly.
/// </summary>
[TestClass]
public sealed class IntervalTreeGenericDebugViewContractTests
    : DebugViewContractTests<IntervalTree<int, string>>
{
    /// <inheritdoc />
    protected override IntervalTree<int, string> Create()
    {
        var tree = new IntervalTree<int, string>();
        tree.Add(1, 5, "a");
        tree.Add(3, 8, "b");
        tree.Add(3, 8, "c");
        return tree;
    }
}
