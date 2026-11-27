// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateSourcePosition.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using System.Xml;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Identifies a place in the text of a notable-date document by its line and the character on that line, both counted
/// from one.
/// </summary>
/// <param name="LineNumber">The line, counted from one.</param>
/// <param name="LinePosition">The character on the line, counted from one.</param>
internal readonly record struct NotableDateSourcePosition(int LineNumber, int LinePosition)
{
    /// <summary>
    /// Returns the position that the line information of an XML node records, or <see langword="null" /> when the node
    /// carries none.
    /// </summary>
    /// <param name="node">The element or attribute, loaded with line information.</param>
    /// <returns>The node's position, or <see langword="null" />.</returns>
    public static NotableDateSourcePosition? Of(IXmlLineInfo node) =>
        node.HasLineInfo() ? FromLineInfo(node.LineNumber, node.LinePosition) : null;

    /// <summary>
    /// Returns the position that an XML or schema exception reports, or <see langword="null" /> when it reports none.
    /// </summary>
    /// <param name="lineNumber">The line the exception reports, or zero when it reports none.</param>
    /// <param name="linePosition">The position on the line the exception reports.</param>
    /// <returns>The reported position, or <see langword="null" />.</returns>
    public static NotableDateSourcePosition? FromLineInfo(int lineNumber, int linePosition) =>
        lineNumber > 0 ? new NotableDateSourcePosition(lineNumber, Math.Max(linePosition, 1)) : null;

    /// <summary>
    /// Returns the position in <paramref name="json" /> that a JSON reader failure reports, or <see langword="null" />
    /// when it reports none.
    /// </summary>
    /// <param name="json">The JSON text the reader failed on.</param>
    /// <param name="exception">The failure.</param>
    /// <returns>The reported position, or <see langword="null" />.</returns>
    /// <remarks>
    /// System.Text.Json counts lines from zero, ending each at a line feed, and reports the offset within the line in
    /// UTF-8 bytes. The offset is converted to a position in UTF-16 characters by walking the line, as an XML position
    /// counts them; a leading byte order mark, which the JSON parser drops before it reads, is skipped the same way.
    /// </remarks>
    public static NotableDateSourcePosition? FromJson(string json, JsonException exception)
    {
        if (exception.LineNumber is not long line || exception.BytePositionInLine is not long bytes)
            return null;

        int start = json.Length > 0 && json[0] == '\uFEFF' ? 1 : 0;
        for (long skipped = 0; skipped < line; skipped++)
        {
            int feed = json.IndexOf('\n', start);
            if (feed < 0)
                return null;

            start = feed + 1;
        }

        // Each UTF-16 character stands for the UTF-8 bytes that encode it; a surrogate pair's four bytes split two and
        // two between its halves.
        int position = 0;
        long consumed = 0;
        for (int i = start; i < json.Length && consumed < bytes && json[i] != '\n'; i++)
        {
            char c = json[i];
            consumed += c < 0x80 ? 1 : c < 0x800 || char.IsSurrogate(c) ? 2 : 3;
            position++;
        }

        return new NotableDateSourcePosition((int)(line + 1), position + 1);
    }
}
