// ---------------------------------------------------------------------------------------------------------------
// <copyright file="QuotedPrintableEncodingOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

/// <summary>
/// Configures how <see cref="QuotedPrintable" /> produces Quoted-Printable output: the line-break treatment, the soft
/// line-wrap column, and the newline sequence.
/// </summary>
/// <remarks>
/// <para>
/// <c>default(QuotedPrintableEncodingOptions)</c> behaves as <see cref="QuotedPrintableEncodingMode.Binary" /> with a
/// 76-character line limit and a CRLF newline: a zero <see cref="MaxLineLength" /> is normalized to <c>76</c> and a
/// <see langword="null" /> <see cref="NewLine" /> to <c>"\r\n"</c>. A zero line length therefore means the RFC default,
/// not unlimited output.
/// </para>
/// </remarks>
public readonly record struct QuotedPrintableEncodingOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QuotedPrintableEncodingOptions" /> class.
    /// </summary>
    /// <param name="mode">The line-break treatment.</param>
    /// <param name="maxLineLength">The maximum encoded line length, excluding the newline delimiter.</param>
    /// <param name="newLine">The newline sequence, or <see langword="null" /> for CRLF.</param>
    public QuotedPrintableEncodingOptions(
        QuotedPrintableEncodingMode mode = QuotedPrintableEncodingMode.Binary,
        int maxLineLength = 76,
        string? newLine = null)
    {
        Mode = mode;
        MaxLineLength = maxLineLength;
        NewLine = newLine;
    }

    /// <summary>
    /// Gets the default options: <see cref="QuotedPrintableEncodingMode.Binary" />, a 76-character line limit, and a
    /// CRLF newline.
    /// </summary>
    /// <value>The default Quoted-Printable encoding options.</value>
    public static QuotedPrintableEncodingOptions Default =>
        new(QuotedPrintableEncodingMode.Binary, 76, "\r\n");

    /// <summary>
    /// Gets canonical MIME text options: <see cref="QuotedPrintableEncodingMode.Text" />, a 76-character line limit,
    /// and a CRLF newline.
    /// </summary>
    /// <value>The canonical MIME text encoding options.</value>
    public static QuotedPrintableEncodingOptions Text =>
        new(QuotedPrintableEncodingMode.Text, 76, "\r\n");

    /// <summary>
    /// Gets the line-break treatment applied to the source.
    /// </summary>
    /// <value>The encoding mode.</value>
    public QuotedPrintableEncodingMode Mode { get; init; }

    /// <summary>
    /// Gets the maximum number of characters per encoded line, excluding the trailing newline delimiter.
    /// </summary>
    /// <value>The line length, where <c>0</c> selects the RFC default of <c>76</c>.</value>
    public int MaxLineLength { get; init; }

    /// <summary>
    /// Gets the newline sequence inserted for soft and hard line breaks.
    /// </summary>
    /// <value>The newline string, or <see langword="null" /> to select CRLF.</value>
    /// <remarks>
    /// Only <c>"\r\n"</c> and <c>"\n"</c> are accepted. CRLF is the MIME canonical line ending; <c>"\n"</c> is a
    /// non-canonical convenience profile for local text processing and should not be used for MIME transport. Output
    /// using <c>"\n"</c> round-trips only when decoded with
    /// <see cref="QuotedPrintableDecodingOptions.AllowBareLineFeed" />.
    /// </remarks>
    public string? NewLine { get; init; }
}
