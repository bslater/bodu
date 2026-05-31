// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationSectionHeaderMode.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

/// <summary>
/// Controls how the reader treats non-whitespace text after the closing <c>]</c> of a section header.
/// </summary>
/// <remarks>
/// <para>
/// The parser identifies a section name as everything between the first <c>[</c> on the line and the last <c>]</c> on
/// the line, allowing <c>]</c> inside section names so EditorConfig pattern conventions remain usable. The handling of
/// any text that appears *after* the final <c>]</c> is a separate decision that this mode controls.
/// </para>
/// <para>
/// Under <see cref="Lenient" /> the parser accepts trailing content silently — useful when the document is authored
/// against a lenient EditorConfig dialect that ignores trailing words. Under <see cref="Strict" /> the parser emits
/// <see cref="ConfigurationDiagnosticCode.TrailingContentAfterSectionHeader" /> so authors learn about typos.
/// <see cref="AllowTrailingInlineComment" /> sits between the two: trailing comments (introduced by <c>#</c> or
/// <c>;</c>) are accepted, anything else still produces the diagnostic.
/// </para>
/// </remarks>
public enum ConfigurationSectionHeaderMode
{
    /// <summary>
    /// Trailing non-whitespace after the closing <c>]</c> is accepted silently. This matches the historical Bodu
    /// profile and is the default for documents authored against lenient dialects.
    /// </summary>
    Lenient = 0,

    /// <summary>
    /// Trailing content (whether word or comment-introducer) after the closing <c>]</c> emits
    /// <see cref="ConfigurationDiagnosticCode.TrailingContentAfterSectionHeader" />. Trailing whitespace is always
    /// permitted.
    /// </summary>
    Strict = 1,

    /// <summary>
    /// Trailing inline comments (introduced by <c>#</c> or <c>;</c>) after the closing <c>]</c> are accepted and
    /// discarded; any other trailing content emits
    /// <see cref="ConfigurationDiagnosticCode.TrailingContentAfterSectionHeader" />.
    /// </summary>
    AllowTrailingInlineComment = 2,
}
