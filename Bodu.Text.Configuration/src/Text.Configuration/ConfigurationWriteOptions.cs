// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationWriteOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Text.Configuration;

/// <summary>
/// Controls how a configuration document is emitted by
/// <see cref="ConfigurationDocument.Save(Bodu.Text.Ini.IniDocumentBase, string, ConfigurationWriteOptions?)" /> and
/// related methods.
/// </summary>
/// <remarks>
/// <para>
/// The writer aims to be round-trip preserving by default: <see cref="PreserveComments" /> and
/// <see cref="WriteInlineComments" /> retain commentary from the original document, and
/// <see cref="InsertBlankLineBetweenSections" /> keeps section blocks visually separated. Override these when emitting
/// a freshly built document, or when generating a minimized variant for transport.
/// </para>
/// <para>
/// <see cref="Profile" /> selects the default formatting envelope (Bodu, EditorConfig-compatible, or strict). The
/// remaining properties override individual aspects of that envelope without abandoning the rest of its defaults.
/// </para>
/// <para>
/// <see cref="Encoding" /> defaults to UTF-8 without a byte-order mark to match the EditorConfig file convention;
/// supply an alternative explicitly when interoperating with a host that expects a BOM or a different code page.
/// <see cref="NewLine" /> defaults to a single LF — switch to <c>"\r\n"</c> for hosts that require CRLF on output.
/// Every property is <c>init</c>-only, so instances are safe to cache and share across threads.
/// </para>
/// </remarks>
public sealed partial class ConfigurationWriteOptions
{
    /// <summary>
    /// Gets the behaviour profile this option bag represents.
    /// </summary>
    /// <returns>The selected profile.</returns>
    public ConfigurationProfile Profile { get; init; } = ConfigurationProfile.Bodu;

    /// <summary>
    /// Gets the encoding used when writing to a byte stream or file. The default is UTF-8 without BOM.
    /// </summary>
    /// <returns>The output encoding.</returns>
    public Encoding Encoding { get; init; } = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    /// <summary>
    /// Gets the line-ending sequence to emit between lines.
    /// </summary>
    /// <returns>The line terminator string.</returns>
    public string NewLine { get; init; } = "\n";

    /// <summary>
    /// Gets the string used to separate keys and values (typically <c> = </c>).
    /// </summary>
    /// <returns>The separator string.</returns>
    public string KeyValueSeparator { get; init; } = " = ";

    /// <summary>
    /// Gets the comment prefix character used when emitting new comments.
    /// </summary>
    /// <returns>The comment prefix.</returns>
    public char CommentPrefix { get; init; } = '#';

    /// <summary>
    /// Gets a value indicating whether existing leading comments should be preserved in the output.
    /// </summary>
    /// <returns><see langword="true" /> when comments are preserved.</returns>
    public bool PreserveComments { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether inline comments should be emitted on property lines.
    /// </summary>
    /// <returns><see langword="true" /> when inline comments are emitted.</returns>
    public bool WriteInlineComments { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether a blank line is inserted between sections.
    /// </summary>
    /// <returns><see langword="true" /> to insert a blank line.</returns>
    public bool InsertBlankLineBetweenSections { get; init; } = true;
}
