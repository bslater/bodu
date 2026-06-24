// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CaseInsensitiveCharComparer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Trees;

/// <summary>
/// Provides a case-insensitive equality comparer for characters, used to exercise culture-insensitive trie matching
/// in tests.
/// </summary>
internal sealed class CaseInsensitiveCharComparer : IEqualityComparer<char>
{
    /// <inheritdoc />
    public bool Equals(char x, char y) =>
        char.ToUpperInvariant(x) == char.ToUpperInvariant(y);

    /// <inheritdoc />
    public int GetHashCode(char obj) =>
        char.ToUpperInvariant(obj).GetHashCode();
}
