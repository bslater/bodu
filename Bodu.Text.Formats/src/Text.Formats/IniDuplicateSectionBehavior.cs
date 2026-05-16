// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniDuplicateSectionBehavior.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Formats;

/// <summary>
/// Defines how the INI parser resolves a section name that appears more than once in the source.
/// </summary>
public enum IniDuplicateSectionBehavior
{

    /// <summary>
    /// Entries from later occurrences of a section are merged into the first occurrence. The active
    /// <see cref="IniDuplicateKeyBehavior" /> governs any key conflicts introduced by the merge.
    /// </summary>
    Merge = 0,

    /// <summary>
    /// Duplicate section names are not permitted. A duplicate section header causes an
    /// <see cref="IniFormatException" /> to be thrown.
    /// </summary>
    Disallowed = 1,

}
