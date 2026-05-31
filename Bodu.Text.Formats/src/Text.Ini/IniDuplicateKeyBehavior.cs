// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniDuplicateKeyBehavior.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Ini;

/// <summary>
/// Defines how the INI parser resolves a key that appears more than once within the same section.
/// </summary>
public enum IniDuplicateKeyBehavior
{
    /// <summary>
    /// The last occurrence of a duplicated key wins; earlier occurrences are silently replaced.
    /// </summary>
    LastWins = 0,

    /// <summary>
    /// The first occurrence of a duplicated key wins; subsequent occurrences are silently ignored.
    /// </summary>
    FirstWins = 1,

    /// <summary>
    /// Duplicate keys are not permitted. A duplicate key causes an <see cref="IniFormatException" /> to be thrown.
    /// </summary>
    Disallowed = 2,
}
