// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DuplicateKeyPolicy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text;

/// <summary>
/// Specifies how a key-based text-format parser resolves a key that appears more than once in the source. Shared by
/// the formats whose duplicate-key semantics are identical (DotEnv and INI).
/// </summary>
public enum DuplicateKeyPolicy
{
    /// <summary>
    /// The last occurrence of a duplicated key wins; earlier values are discarded. This is the default.
    /// </summary>
    LastWins = 0,

    /// <summary>
    /// The first occurrence of a duplicated key is retained; subsequent occurrences are silently ignored.
    /// </summary>
    FirstWins = 1,

    /// <summary>
    /// Any duplicated key causes a <see cref="TextFormatException" /> to be thrown.
    /// </summary>
    Disallowed = 2,
}
