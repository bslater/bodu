// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.EnsureTrailingNewLine.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class StringExtensions
{
    /// <summary>
    /// Returns <paramref name="value" /> guaranteed to end with <paramref name="newline" />, appending the terminator
    /// only when it is not already present.
    /// </summary>
    /// <param name="value">The string to terminate. Must not be <see langword="null" />.</param>
    /// <param name="newline">
    /// The terminator to ensure. Defaults to <c>"\n"</c> (LF). Must not be <see langword="null" /> or empty.
    /// </param>
    /// <returns>
    /// <paramref name="value" /> when it already ends with <paramref name="newline" />; otherwise a new string equal to
    /// <paramref name="value" /> with <paramref name="newline" /> appended.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> or <paramref name="newline" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="newline" /> is the empty string.</exception>
    /// <remarks>
    /// Comparison is ordinal — only the exact byte sequence specified by <paramref name="newline" /> counts as already
    /// terminated. A value ending in <c>"\r\n"</c> is not considered to end in <c>"\n"</c> for the purposes of this
    /// method.
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// "line".EnsureTrailingNewLine();    // "line\n"
    /// "line\n".EnsureTrailingNewLine();  // "line\n" (already terminated)
    ///]]>
    /// </code>
    /// </example>
    public static string EnsureTrailingNewLine(this string value, string newline = "\n")
    {
        ThrowHelper.ThrowIfNull(value);
        ThrowHelper.ThrowIfNull(newline);
        return newline.Length == 0
            ? throw new ArgumentException(ResourceStrings.Arg_Invalid_StringIsEmpty, nameof(newline))
            : value.EndsWith(newline, StringComparison.Ordinal) ? value : value + newline;
    }
}
