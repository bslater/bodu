// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.ToSafeFileName.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Extensions;

public static partial class StringExtensions
{
    /// <summary>The platform's invalid file-name characters as a vectorised search set, computed once per process (<see cref="Path.GetInvalidFileNameChars" /> allocates a fresh array on every call).</summary>
    private static readonly System.Buffers.SearchValues<char> s_invalidFileNameChars =
        System.Buffers.SearchValues.Create(Path.GetInvalidFileNameChars());

    /// <summary>
    /// Returns <paramref name="value" /> with every character that <see cref="Path.GetInvalidFileNameChars" /> reports
    /// as invalid for a file name replaced by an underscore.
    /// </summary>
    /// <param name="value">The candidate file name to sanitise.</param>
    /// <returns>
    /// A string in which each invalid character has been replaced by <c>'_'</c> — the original instance when nothing
    /// required replacement. Returns <c>"_"</c> when the input is empty so that the result is never itself an empty
    /// file name.
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

        // Vectorised scan: the common all-valid input returns the original instance with no allocation.
        int firstInvalid = value.AsSpan().IndexOfAny(s_invalidFileNameChars);
        if (firstInvalid < 0) return value;

        return string.Create(value.Length, (value, firstInvalid), static (destination, state) =>
        {
            (string source, int first) = state;
            source.AsSpan(0, first).CopyTo(destination);

            for (int i = first; i < source.Length; i++)
            {
                char c = source[i];
                destination[i] = s_invalidFileNameChars.Contains(c) ? '_' : c;
            }
        });
    }
}
