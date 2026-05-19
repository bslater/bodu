// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationWriteOptions.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Text.Configuration;

/// <summary>
/// Controls how a configuration document is emitted by
/// <see cref="BoduConfigurationDocument.Save(Bodu.Text.Formats.IniDocument, string, BoduConfigurationWriteOptions?)" />
/// and related methods.
/// </summary>
public sealed partial class BoduConfigurationWriteOptions
{
    /// <summary>
    /// Gets the behaviour profile this option bag represents.
    /// </summary>
    /// <returns>The selected profile.</returns>
    public BoduConfigurationProfile Profile { get; init; } = BoduConfigurationProfile.Bodu;

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
