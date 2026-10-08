// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationMissingPathRootMode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

/// <summary>
/// Selects what <see cref="ConfigurationExtensions.Resolve(IniDocumentBase, string?, ConfigurationResolveOptions?)" />
/// does when it receives no target path and no root is known: no <see cref="ConfigurationResolveOptions.PathRoot" />
/// was supplied and the document was not loaded from a file.
/// </summary>
/// <remarks>
/// Strict EditorConfig semantics require every glob to be evaluated relative to a known directory. In-memory scenarios
/// such as unit tests rarely have a meaningful root; permitting an empty root makes
/// <see cref="ConfigurationDocument.Parse(string, ConfigurationParseOptions?)" /> useful end-to-end without forcing
/// every test to supply a path context. A document loaded with
/// <see cref="ConfigurationDocument.Load(string, ConfigurationParseOptions?)" /> is rooted at the directory of its
/// file, so this mode never applies to it.
/// </remarks>
public enum ConfigurationMissingPathRootMode
{
    /// <summary>
    /// Resolve against an empty root: with no target path no section applies, and the view holds only what the preamble
    /// contributes. This is the default for the <see cref="ConfigurationProfile.Bodu" /> and
    /// <see cref="ConfigurationProfile.Relaxed" /> profiles.
    /// </summary>
    UseEmptyRoot = 0,

    /// <summary>
    /// Throw <see cref="InvalidOperationException" />. This is the default for the
    /// <see cref="ConfigurationProfile.EditorConfigCompatible" /> and <see cref="ConfigurationProfile.Strict" />
    /// profiles.
    /// </summary>
    Throw = 1,
}
