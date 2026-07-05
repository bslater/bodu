// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BitSetDebugViewContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Contracts;

/// <summary>
/// Drives <see cref="DebugViewContractTests{TCollection}" /> against <see cref="BitSet" />. Asserts the standard Bodu
/// debugger-display contract — DebuggerDisplay, DebuggerTypeProxy, and an instance-constructible proxy — is present
/// and wired up correctly.
/// </summary>
[TestClass]
public sealed class BitSetDebugViewContractTests
    : DebugViewContractTests<BitSet>
{
    /// <inheritdoc />
    protected override BitSet Create()
    {
        var bits = new BitSet();
        bits.Set(1);
        bits.Set(64);
        bits.Set(200);
        return bits;
    }
}
