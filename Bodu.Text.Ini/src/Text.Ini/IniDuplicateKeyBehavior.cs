// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniDuplicateKeyBehavior.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Ini;

/// <summary>
/// Specifies how the INI document model resolves a key that appears more than once within the same section.
/// </summary>
public enum IniDuplicateKeyBehavior
{
    /// <summary>
    /// The last occurrence of the key wins.
    /// </summary>
    LastWins = 0,

    /// <summary>
    /// The first occurrence of the key wins.
    /// </summary>
    FirstWins,

    /// <summary>
    /// A duplicate key throws <see cref="IniFormatException" />.
    /// </summary>
    Disallowed,
}
