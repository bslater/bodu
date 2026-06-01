// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.ToSafeFileName.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Extensions;

public static partial class StringExtensions
{
    /// <summary>
    /// Returns <paramref name="value" /> with every character that <see cref="Path.GetInvalidFileNameChars" /> reports
    /// as invalid for a file name replaced by an underscore.
    /// </summary>
    /// <param name="value">The candidate file name to sanitise.</param>
    /// <returns>
    /// A new string in which each invalid character has been replaced by <c>'_'</c>. Returns <c>"_"</c> when the input
    /// is empty so that the result is never itself an empty file name.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// The invalid character set is platform-dependent — Windows reports more invalid characters than POSIX systems do.
    /// Callers writing files for a known target platform should construct the invalid set explicitly rather than
    /// relying on the current platform's defaults.
    /// </remarks>
    public static string ToSafeFileName(this string value)
    {
        ThrowHelper.ThrowIfNull(value);

        if (value.Length == 0) return "_";

        var invalid = Path.GetInvalidFileNameChars();
        StringBuilder builder = new(value.Length);
        foreach (var c in value)
        {
            builder.Append(Array.IndexOf(invalid, c) >= 0 ? '_' : c);
        }

        return builder.ToString();
    }
}
