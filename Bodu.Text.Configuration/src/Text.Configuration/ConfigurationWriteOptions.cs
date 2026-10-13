// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationWriteOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Text.Configuration;

/// <summary>
/// Controls how a configuration document is emitted by
/// <see cref="ConfigurationDocument.Save(IniDocumentBase, string, ConfigurationWriteOptions?)" /> and related methods.
/// </summary>
/// <remarks>
/// <para>
/// The writer aims to be round-trip preserving by default: <see cref="PreserveComments" /> and
/// <see cref="WriteInlineComments" /> retain commentary from the original document, and
/// <see cref="InsertBlankLineBetweenSections" /> keeps section blocks visually separated. Override these when emitting
/// a freshly built document, or when generating a minimized variant for transport.
/// </para>
/// <para>
/// <see cref="Profile" /> only labels the option set: the writer reads the individual properties. Start from a preset
/// or <see cref="For(ConfigurationProfile)" /> to take a profile's formatting choices, and set the properties that
/// differ.
/// </para>
/// <para>
/// <see cref="Encoding" /> defaults to UTF-8 without a byte-order mark to match the EditorConfig file convention;
/// supply an alternative explicitly when interoperating with a host that expects a BOM or a different code page.
/// <see cref="NewLine" /> defaults to a single LF - switch to <c>"\r\n"</c> for hosts that require CRLF on output.
/// Every property is <c>init</c>-only, so instances are safe to cache and share across threads.
/// </para>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// ConfigurationDocument.Save(document, "team.boduconfig", new ConfigurationWriteOptions
/// {
///     KeyValueSeparator = " = ",
///     PreserveComments = true,
///     InsertBlankLineBetweenSections = true,
/// });
///]]>
/// </code>
/// </example>
/// </remarks>
public sealed partial class ConfigurationWriteOptions
{
    /// <summary>
    /// Gets the profile this option set was created from. The default is <see cref="ConfigurationProfile.Bodu" />.
    /// </summary>
    /// <value>The profile the option set is labelled with.</value>
    /// <remarks>
    /// The property is informational: the writer does not read it, and setting it changes no other property. Use
    /// <see cref="For(ConfigurationProfile)" /> or a preset to select a profile's formatting choices.
    /// </remarks>
    public ConfigurationProfile Profile { get; init; } = ConfigurationProfile.Bodu;

    /// <summary>
    /// Gets the encoding used when writing to a byte stream or file. The default is UTF-8 without BOM.
    /// </summary>
    /// <value>The output encoding.</value>
    public Encoding Encoding { get; init; } = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    /// <summary>
    /// Gets the line-ending sequence to emit between lines.
    /// </summary>
    /// <value>The line terminator string.</value>
    public string NewLine { get; init; } = "\n";

    /// <summary>
    /// Gets the string used to separate keys and values (typically <c> = </c>).
    /// </summary>
    /// <value>The separator string.</value>
    public string KeyValueSeparator { get; init; } = " = ";

    /// <summary>
    /// Gets a comment prefix character. The writer does not use it: every <see cref="IniComment" /> is written with its
    /// own <see cref="IniComment.Prefix" />, and the writer emits no comment that lacks one.
    /// </summary>
    /// <value>The comment prefix; it has no effect on the output.</value>
    public char CommentPrefix { get; init; } = '#';

    /// <summary>
    /// Gets a value indicating whether existing leading comments should be preserved in the output.
    /// </summary>
    /// <value><see langword="true" /> when comments are preserved.</value>
    public bool PreserveComments { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether inline comments should be emitted on property lines.
    /// </summary>
    /// <value><see langword="true" /> when inline comments are emitted.</value>
    public bool WriteInlineComments { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether a blank line is inserted between sections.
    /// </summary>
    /// <value><see langword="true" /> to insert a blank line.</value>
    public bool InsertBlankLineBetweenSections { get; init; } = true;
}
