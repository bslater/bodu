// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EvictingDictionaryDebugViewContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Contracts;

namespace Bodu.Collections.Generic.Contracts;

/// <summary>
/// Drives <see cref="DebugViewContractTests{TCollection}" /> against
/// <see cref="EvictingDictionary{TKey, TValue}" />. Asserts the standard Bodu debugger-display
/// contract — DebuggerDisplay, DebuggerTypeProxy, and an instance-constructible proxy — is present
/// and wired up correctly.
/// </summary>
[TestClass]
public sealed class EvictingDictionaryDebugViewContractTests
    : DebugViewContractTests<EvictingDictionary<int, string>>
{
    /// <inheritdoc />
    protected override EvictingDictionary<int, string> Create()
    {
        EvictingDictionary<int, string> map = new(capacity: 4);
        map[1] = "one";
        map[2] = "two";
        map[3] = "three";
        return map;
    }
}
