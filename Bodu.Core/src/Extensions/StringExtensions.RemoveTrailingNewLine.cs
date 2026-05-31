// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.RemoveTrailingNewLine.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class StringExtensions
{
    /// <summary>
    /// Returns <paramref name="value" /> with a single trailing line ending removed when present.
    /// </summary>
    /// <param name="value">The string to trim. Must not be <see langword="null" />.</param>
    /// <returns>
    /// <paramref name="value" /> with the last <c>"\r\n"</c>, <c>"\n"</c>, or <c>"\r"</c> sequence removed. Returns the
    /// original instance unchanged when <paramref name="value" /> does not end with a line terminator.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// Only one trailing line terminator is removed. Repeated trailing newlines are intentionally preserved — use
    /// <see cref="string.TrimEnd(char[])" /> with <c>'\r','\n'</c> when greedy stripping is required.
    /// </remarks>
    public static string RemoveTrailingNewLine(this string value)
    {
        ThrowHelper.ThrowIfNull(value);

        return value.EndsWith("\r\n", StringComparison.Ordinal)
            ? value[..^2]
            : value.Length > 0 && (value[^1] == '\n' || value[^1] == '\r') ? value[..^1] : value;
    }
}
