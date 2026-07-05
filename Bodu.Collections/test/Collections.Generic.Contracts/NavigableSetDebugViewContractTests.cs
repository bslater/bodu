// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NavigableSetDebugViewContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Contracts;

/// <summary>
/// Drives <see cref="DebugViewContractTests{TCollection}" /> against <see cref="NavigableSet{T}" />. Asserts the
/// standard Bodu debugger-display contract — DebuggerDisplay, DebuggerTypeProxy, and an instance-constructible proxy
/// — is present and wired up correctly.
/// </summary>
[TestClass]
public sealed class NavigableSetDebugViewContractTests
    : DebugViewContractTests<NavigableSet<int>>
{
    /// <inheritdoc />
    protected override NavigableSet<int> Create()
    {
        var set = new NavigableSet<int>();
        set.Add(3);
        set.Add(1);
        set.Add(2);
        return set;
    }
}
