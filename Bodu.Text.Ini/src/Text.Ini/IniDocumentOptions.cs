// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniDocumentOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Ini;

/// <summary>
/// Provides configuration for the INI document model, controlling how duplicate sections and duplicate keys are
/// resolved when a document is materialized.
/// </summary>
/// <remarks>
/// The source-order <see cref="Bodu.Text.Ini.Reader.Utf8IniReader" /> surfaces the document verbatim and never applies
/// these policies; they take effect when a document is materialized into the mutable
/// <see cref="Bodu.Text.Ini.Nodes.IniNode" /> tree or the read-only <see cref="Bodu.Text.Ini.Document.IniDocument" />.
/// </remarks>
public readonly struct IniDocumentOptions
{
    /// <summary>
    /// Gets the default document options (duplicate sections merge, the last duplicate key wins).
    /// </summary>
    /// <value>The default options.</value>
    public static IniDocumentOptions Default => default;

    /// <summary>
    /// Gets the policy applied when a section name appears more than once.
    /// </summary>
    /// <value>The duplicate-section policy; the default is <see cref="IniDuplicateSectionBehavior.Merge" />.</value>
    public IniDuplicateSectionBehavior DuplicateSectionBehavior { get; init; }

    /// <summary>
    /// Gets the policy applied when a key appears more than once within the same section.
    /// </summary>
    /// <value>The duplicate-key policy; the default is <see cref="IniDuplicateKeyBehavior.LastWins" />.</value>
    public IniDuplicateKeyBehavior DuplicateKeyBehavior { get; init; }
}
