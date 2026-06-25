// ---------------------------------------------------------------------------------------------------------------
// <copyright file="LinkedDictionaryDebugViewContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Contracts;

/// <summary>
/// Drives <see cref="DebugViewContractTests{TCollection}" /> against
/// <see cref="LinkedDictionary{TKey, TValue}" />. Asserts the standard Bodu debugger-display contract —
/// DebuggerDisplay, DebuggerTypeProxy, and an instance-constructible proxy — is present and wired up correctly.
/// </summary>
[TestClass]
public sealed class LinkedDictionaryDebugViewContractTests
    : DebugViewContractTests<LinkedDictionary<int, string>>
{
    /// <inheritdoc />
    protected override LinkedDictionary<int, string> Create()
    {
        LinkedDictionary<int, string> map = new();
        map[1] = "one";
        map[2] = "two";
        map[3] = "three";
        return map;
    }
}
