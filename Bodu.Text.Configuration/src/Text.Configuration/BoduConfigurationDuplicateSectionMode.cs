// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationDuplicateSectionMode.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

/// <summary>
/// Selects how the reader treats two section headers with the same pattern in the same document.
/// </summary>
/// <remarks>
/// EditorConfig precedence relies on physical section order: if <c>[*.cs]</c> appears twice, the second one
/// wins for any property re-stated within it. Preserving the duplicate sections is therefore the default;
/// merging is opt-in.
/// </remarks>
public enum BoduConfigurationDuplicateSectionMode
{
    /// <summary>
    /// Retain every section as authored. Duplicate patterns produce separate ordered sections; EditorConfig
    /// precedence (last-wins) governs the resolved value. This is the default.
    /// </summary>
    Preserve = 0,

    /// <summary>
    /// Treat a repeat of a previously seen section pattern as a recoverable error. A diagnostic is emitted;
    /// under <see cref="BoduConfigurationDiagnosticMode.Throw" /> a <see cref="BoduConfigurationParseException" />
    /// is raised.
    /// </summary>
    Reject = 1,

    /// <summary>
    /// Merge adjacent duplicate sections during parse. Properties from the second section are appended to the
    /// first; sections with different patterns between them remain unaffected.
    /// </summary>
    MergeAdjacent = 2,

    /// <summary>
    /// Merge every duplicate of a section pattern into the first occurrence regardless of position. Note that
    /// this changes the resolved precedence relative to <see cref="Preserve" /> when other sections appear
    /// between duplicates.
    /// </summary>
    MergeAll = 3,
}
