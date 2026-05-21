// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationMissingPathRootMode.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

/// <summary>
/// Selects how
/// <see cref="ConfigurationExtensions.Resolve(Bodu.Text.Ini.IniDocument, string?, ConfigurationResolveOptions?)" />
/// reacts when the document was parsed from a string and no <see cref="ConfigurationResolveOptions.PathRoot" /> was
/// supplied.
/// </summary>
/// <remarks>
/// Strict EditorConfig semantics require every glob to be evaluated relative to a known directory. In-memory scenarios
/// such as unit tests rarely have a meaningful root; permitting an empty root makes
/// <see cref="ConfigurationDocument.Parse(string, ConfigurationParseOptions?)" /> useful end-to-end without forcing
/// every test to supply a path context.
/// </remarks>
public enum ConfigurationMissingPathRootMode
{
    /// <summary>
    /// Use an empty path root. Patterns without <c>/</c> match at any depth; patterns with <c>/</c> are anchored to the
    /// root of the supplied target path. This is the default for the <see cref="ConfigurationProfile.Bodu" /> profile.
    /// </summary>
    UseEmptyRoot = 0,

    /// <summary>
    /// Throw <see cref="InvalidOperationException" /> when no path root has been supplied. This is the default for the
    /// <see cref="ConfigurationProfile.EditorConfigCompatible" /> profile.
    /// </summary>
    Throw = 1,
}
