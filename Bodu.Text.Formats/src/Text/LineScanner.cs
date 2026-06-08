// ---------------------------------------------------------------------------------------------------------------
// <copyright file="LineScanner.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Formats;

/// <summary>
/// Provides shared, allocation-free character-cursor primitives over a <see cref="ReadOnlySpan{T}" /> of
/// <see cref="char" /> for the line-oriented text-format parsers that consume their input character by character.
/// </summary>
/// <remarks>
/// The helpers operate on the caller's own <c>remaining</c> span and 1-based <c>lineNumber</c> by reference so that a
/// parser <c>ref struct</c> can keep its existing fields while sharing a single, tested implementation of newline
/// handling. Line endings are recognised as <c>\r\n</c>, <c>\r</c>, or <c>\n</c>, and the line counter is incremented
/// once per terminator consumed.
/// </remarks>
internal static class LineScanner
{
    /// <summary>
    /// Consumes a single character from <paramref name="remaining" />, incrementing <paramref name="lineNumber" /> when
    /// the consumed character is a line feed (<c>'\n'</c>).
    /// </summary>
    /// <param name="remaining">The unconsumed source, advanced by one character when not empty.</param>
    /// <param name="lineNumber">The running 1-based line counter, incremented when a line feed is consumed.</param>
    public static void Advance(ref ReadOnlySpan<char> remaining, ref int lineNumber)
    {
        if (!remaining.IsEmpty)
        {
            if (remaining[0] == '\n')
                lineNumber++;

            remaining = remaining[1..];
        }
    }

    /// <summary>
    /// Advances <paramref name="remaining" /> to the end of the current line without consuming the line terminator.
    /// </summary>
    /// <param name="remaining">The unconsumed source, advanced to the next <c>'\r'</c> or <c>'\n'</c>.</param>
    public static void SkipToEndOfLine(ref ReadOnlySpan<char> remaining)
    {
        while (!remaining.IsEmpty && remaining[0] != '\n' && remaining[0] != '\r')
            remaining = remaining[1..];
    }

    /// <summary>
    /// Consumes a <c>\r\n</c>, <c>\r</c>, or <c>\n</c> line terminator from <paramref name="remaining" /> and
    /// increments <paramref name="lineNumber" /> accordingly. Does nothing when the cursor is not positioned on a line
    /// terminator.
    /// </summary>
    /// <param name="remaining">The unconsumed source, advanced past the line terminator when present.</param>
    /// <param name="lineNumber">The running 1-based line counter, incremented once per terminator consumed.</param>
    public static void SkipLineEnding(ref ReadOnlySpan<char> remaining, ref int lineNumber)
    {
        if (remaining.IsEmpty)
            return;

        if (remaining[0] == '\r')
        {
            remaining = remaining[1..];

            if (!remaining.IsEmpty && remaining[0] == '\n')
            {
                lineNumber++;
                remaining = remaining[1..];
            }
        }
        else if (remaining[0] == '\n')
        {
            lineNumber++;
            remaining = remaining[1..];
        }
    }
}
