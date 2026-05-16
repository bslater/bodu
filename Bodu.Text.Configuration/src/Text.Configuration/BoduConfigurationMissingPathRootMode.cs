// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationMissingPathRootMode.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

/// <summary>
/// Selects how <see cref="BoduConfigurationExtensions.Resolve(Bodu.Text.Formats.IniDocument, string?, BoduConfigurationResolveOptions?)" /> reacts
/// when the document was parsed from a string and no
/// <see cref="BoduConfigurationResolveOptions.PathRoot" /> was supplied.
/// </summary>
/// <remarks>
/// Strict EditorConfig semantics require every glob to be evaluated relative to a known directory. In-memory
/// scenarios such as unit tests rarely have a meaningful root; permitting an empty root makes
/// <see cref="BoduConfigurationDocument.Parse(string, BoduConfigurationParseOptions?)" /> useful end-to-end
/// without forcing every test to supply a path context.
/// </remarks>
public enum BoduConfigurationMissingPathRootMode
{
    /// <summary>
    /// Use an empty path root. Patterns without <c>/</c> match at any depth; patterns with <c>/</c> are
    /// anchored to the root of the supplied target path. This is the default for the
    /// <see cref="BoduConfigurationProfile.Bodu" /> profile.
    /// </summary>
    UseEmptyRoot = 0,

    /// <summary>
    /// Throw <see cref="InvalidOperationException" /> when no path root has been supplied. This is the default
    /// for the <see cref="BoduConfigurationProfile.EditorConfigCompatible" /> profile.
    /// </summary>
    Throw = 1,
}
