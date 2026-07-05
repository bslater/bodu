// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NavigableDictionaryDebugViewContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Contracts;

/// <summary>
/// Drives <see cref="DebugViewContractTests{TCollection}" /> against
/// <see cref="NavigableDictionary{TKey, TValue}" />. Asserts the standard Bodu debugger-display contract —
/// DebuggerDisplay, DebuggerTypeProxy, and an instance-constructible proxy — is present and wired up correctly.
/// </summary>
[TestClass]
public sealed class NavigableDictionaryDebugViewContractTests
    : DebugViewContractTests<NavigableDictionary<int, string>>
{
    /// <inheritdoc />
    protected override NavigableDictionary<int, string> Create()
    {
        var dictionary = new NavigableDictionary<int, string>();
        dictionary.Add(3, "three");
        dictionary.Add(1, "one");
        dictionary.Add(2, "two");
        return dictionary;
    }
}
