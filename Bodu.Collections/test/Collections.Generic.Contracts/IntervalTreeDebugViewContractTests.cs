// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalTreeDebugViewContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Contracts;

/// <summary>
/// Drives <see cref="DebugViewContractTests{TCollection}" /> against <see cref="IntervalTree{T}" />. Asserts the
/// standard Bodu debugger-display contract — DebuggerDisplay, DebuggerTypeProxy, and an instance-constructible proxy
/// — is present and wired up correctly.
/// </summary>
[TestClass]
public sealed class IntervalTreeDebugViewContractTests
    : DebugViewContractTests<IntervalTree<int>>
{
    /// <inheritdoc />
    protected override IntervalTree<int> Create()
    {
        var tree = new IntervalTree<int>();
        tree.Add(1, 5);
        tree.Add(3, 8);
        tree.Add(3, 8);
        return tree;
    }
}
