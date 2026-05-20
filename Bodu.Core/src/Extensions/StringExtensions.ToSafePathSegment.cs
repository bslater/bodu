// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.ToSafePathSegment.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Text;

namespace Bodu.Extensions;

public static partial class StringExtensions
{
    /// <summary>
    /// Returns <paramref name="value" /> with every character that <see cref="Path.GetInvalidFileNameChars" /> reports
    /// as invalid for a single path segment (plus the platform path separators) replaced by an underscore.
    /// </summary>
    /// <param name="value">The candidate path segment to sanitise.</param>
    /// <returns>
    /// A new string in which each invalid character — including <see cref="Path.DirectorySeparatorChar" /> and
    /// <see cref="Path.AltDirectorySeparatorChar" /> — has been replaced by <c>'_'</c>. Returns <c>"_"</c> when the
    /// input is empty so that the result is never itself an empty segment.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// Use this when composing a path from user input where each component must be a single segment without embedded
    /// separators. For a full file name (no embedded separators required as a rule), prefer
    /// <see cref="ToSafeFileName(string)" /> — the two differ only in whether path separators are stripped.
    /// </remarks>
    public static string ToSafePathSegment(this string value)
    {
        ThrowHelper.ThrowIfNull(value);

        if (value.Length == 0) return "_";

        char[] invalid = Path.GetInvalidFileNameChars();
        StringBuilder builder = new(value.Length);
        foreach (char c in value)
        {
            bool isInvalid = Array.IndexOf(invalid, c) >= 0
                || c == Path.DirectorySeparatorChar
                || c == Path.AltDirectorySeparatorChar;
            builder.Append(isInvalid ? '_' : c);
        }

        return builder.ToString();
    }
}
