// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniDuplicateSectionBehavior.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Ini;

/// <summary>
/// Defines how the INI parser resolves a section name that appears more than once in the source.
/// </summary>
/// <remarks>
/// The <see cref="Merge" /> value (the default) preserves the historical INI behaviour: entries from later occurrences
/// are merged into the first occurrence. <see cref="MergeAll" /> is an explicit alias for this value.
/// <see cref="MergeAdjacent" /> and <see cref="Preserve" /> implement the finer-grained EditorConfig-style semantics
/// required by <c>Bodu.Text.Configuration</c>.
/// </remarks>
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

    /// <summary>
    /// Retain duplicate sections as separate entries in source order so that downstream consumers can apply their own
    /// precedence rules (for example, EditorConfig's last-section-wins).
    /// </summary>
    Preserve = 2,

    /// <summary>
    /// Merge a duplicate section into the immediately preceding section only when the two share the same name and no
    /// other section appears between them; otherwise, retain the section as a separate occurrence.
    /// </summary>
    MergeAdjacent = 3,

    /// <summary>
    /// Explicit alias for <see cref="Merge" />: merge every duplicate occurrence into the first regardless of position.
    /// Provided so callers selecting from a finer-grained set of behaviours can opt in by name.
    /// </summary>
    MergeAll = Merge,
}
