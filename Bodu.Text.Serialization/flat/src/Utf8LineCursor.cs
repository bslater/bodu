// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8LineCursor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization;

/// <summary>
/// Byte-oriented CR, LF and CRLF cursor operations, and the byte-order mark, shared by the DotEnv, INI and Delimited
/// readers.
/// </summary>
/// <remarks>
/// A line ends at a LF, a CR LF pair or a lone CR. The scans search with <see cref="MemoryExtensions" />'s vectorized
/// <c>IndexOfAny</c> rather than a byte at a time.
/// </remarks>
internal static class Utf8LineCursor
{
    /// <summary>
    /// Gets the UTF-8 byte-order mark.
    /// </summary>
    internal static ReadOnlySpan<byte> ByteOrderMark => [0xEF, 0xBB, 0xBF];

    /// <summary>
    /// Gets the length of the UTF-8 byte-order mark that starts the source, if one does.
    /// </summary>
    /// <param name="source">The UTF-8 source bytes.</param>
    /// <returns>The length of the mark when the source starts with all of it; otherwise zero.</returns>
    internal static int ByteOrderMarkLength(ReadOnlySpan<byte> source) =>
        source.StartsWith(ByteOrderMark) ? ByteOrderMark.Length : 0;

    /// <summary>
    /// Determines whether a byte is a CR or a LF.
    /// </summary>
    /// <param name="value">The byte to test.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="value" /> is a CR or a LF; otherwise <see langword="false" />.
    /// </returns>
    internal static bool IsLineEnding(byte value) =>
        value is (byte)'\r' or (byte)'\n';

    /// <summary>
    /// Finds the next CR or LF byte starting at the specified position.
    /// </summary>
    /// <param name="source">The UTF-8 source bytes.</param>
    /// <param name="position">The position from which to scan.</param>
    /// <returns>
    /// The newline position, or the source length if none remains; <paramref name="position" /> itself when it is at or
    /// past the end of the source.
    /// </returns>
    internal static int EndOfLine(ReadOnlySpan<byte> source, int position)
    {
        if (position >= source.Length)
            return position;

        int index = source[position..].IndexOfAny((byte)'\r', (byte)'\n');
        return index < 0 ? source.Length : position + index;
    }

    /// <summary>
    /// Finds the next occurrence of a stop byte, a CR or a LF, starting at the specified position.
    /// </summary>
    /// <param name="source">The UTF-8 source bytes.</param>
    /// <param name="position">The position from which to scan.</param>
    /// <param name="stop">The byte that ends the scan as a line ending does.</param>
    /// <returns>
    /// The position of the first such byte, or the source length if none remains; <paramref name="position" /> itself
    /// when it is at or past the end of the source.
    /// </returns>
    internal static int EndOfLineOr(ReadOnlySpan<byte> source, int position, byte stop)
    {
        if (position >= source.Length)
            return position;

        int index = source[position..].IndexOfAny(stop, (byte)'\r', (byte)'\n');
        return index < 0 ? source.Length : position + index;
    }

    /// <summary>
    /// Determines whether the byte at a position ends a line inside text that keeps its line breaks: a LF, or a CR that
    /// no LF follows, so that a CR LF pair counts once, at its LF.
    /// </summary>
    /// <param name="source">The UTF-8 source bytes.</param>
    /// <param name="position">The position of the byte to test, within the source.</param>
    /// <returns><see langword="true" /> when the byte ends a line; otherwise <see langword="false" />.</returns>
    internal static bool EndsLine(ReadOnlySpan<byte> source, int position) =>
        source[position] == (byte)'\n'
        || (source[position] == (byte)'\r' && (position + 1 == source.Length || source[position + 1] != (byte)'\n'));

    /// <summary>
    /// Consumes a CR, LF or CRLF sequence, advancing the cursor past the terminator.
    /// </summary>
    /// <param name="source">The UTF-8 source bytes.</param>
    /// <param name="position">The cursor advanced when a line terminator is present.</param>
    /// <returns>One when a line terminator was consumed; otherwise zero.</returns>
    internal static int ConsumeLineEnding(ReadOnlySpan<byte> source, ref int position)
    {
        if (position >= source.Length)
            return 0;

        if (source[position] == (byte)'\r')
        {
            position++;
            if (position < source.Length && source[position] == (byte)'\n')
                position++;
            return 1;
        }

        if (source[position] == (byte)'\n')
        {
            position++;
            return 1;
        }

        return 0;
    }

    /// <summary>
    /// Finds the start of the line that holds a position: just past the last CR or LF before it, or, on the first line,
    /// just past a byte-order mark that starts the source.
    /// </summary>
    /// <param name="source">The UTF-8 source bytes.</param>
    /// <param name="position">The position, within the source.</param>
    /// <returns>The position of the first byte of the line.</returns>
    internal static int LineStart(ReadOnlySpan<byte> source, int position)
    {
        int lineStart = source[..position].LastIndexOfAny((byte)'\r', (byte)'\n') + 1;
        return lineStart == 0 ? ByteOrderMarkLength(source) : lineStart;
    }
}
