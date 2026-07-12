// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentEvictingDictionaryDebugViewContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent.Contracts;

/// <summary>
/// Drives <see cref="DebugViewContractTests{TCollection}" /> against
/// <see cref="ConcurrentEvictingDictionary{TKey, TValue}" />. Asserts the standard Bodu debugger-display contract —
/// DebuggerDisplay, DebuggerTypeProxy, and an instance-constructible proxy — is present and wired up correctly.
/// </summary>
[TestClass]
public sealed class ConcurrentEvictingDictionaryDebugViewContractTests
    : DebugViewContractTests<ConcurrentEvictingDictionary<string, int>>
{
    /// <inheritdoc />
    protected override ConcurrentEvictingDictionary<string, int> Create()
    {
        ConcurrentEvictingDictionary<string, int> dictionary = new(capacity: 128);
        dictionary.Add("a", 1);
        dictionary.Add("b", 2);
        dictionary.Add("c", 3);
        return dictionary;
    }
}
