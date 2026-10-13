// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8LineCursor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization;

/// <summary>
/// Byte-oriented CR, LF and CRLF cursor operations shared by INI and DotEnv readers.
/// </summary>
internal static class Utf8LineCursor
{
    /// <summary>
    /// Finds the next CR or LF byte starting at the specified position.
    /// </summary>
    /// <param name="source">The UTF-8 source bytes.</param>
    /// <param name="position">The position from which to scan.</param>
    /// <returns>The newline position, or the source length if none remains.</returns>
    internal static int EndOfLine(ReadOnlySpan<byte> source, int position)
    {
        while (position < source.Length && source[position] is not ((byte)'\r' or (byte)'\n'))
            position++;
        return position;
    }

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
}
