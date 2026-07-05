// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TableDebugViewContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Contracts;

/// <summary>
/// Drives <see cref="DebugViewContractTests{TCollection}" /> against <see cref="Table{TRow, TColumn, TValue}" />.
/// Asserts the standard Bodu debugger-display contract — DebuggerDisplay, DebuggerTypeProxy, and an
/// instance-constructible proxy — is present and wired up correctly.
/// </summary>
[TestClass]
public sealed class TableDebugViewContractTests
    : DebugViewContractTests<Table<string, string, int>>
{
    /// <inheritdoc />
    protected override Table<string, string, int> Create()
    {
        var table = new Table<string, string, int>();
        table.Add("r1", "c1", 1);
        table.Add("r1", "c2", 2);
        table.Add("r2", "c1", 3);
        return table;
    }
}
