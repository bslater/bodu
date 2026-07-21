// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.ToSafePathSegment.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Extensions;

public static partial class StringExtensions
{
    /// <summary>
    /// The platform's invalid file-name characters plus both directory separators as a vectorised search set, computed
    /// once per process.
    /// </summary>
    private static readonly System.Buffers.SearchValues<char> s_invalidPathSegmentChars =
        System.Buffers.SearchValues.Create([.. Path.GetInvalidFileNameChars(), Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar]);

    /// <summary>
    /// Returns <paramref name="value" /> with every character that <see cref="Path.GetInvalidFileNameChars" /> reports
    /// as invalid for a single path segment (plus the platform path separators) replaced by an underscore.
    /// </summary>
    /// <param name="value">The candidate path segment to sanitise.</param>
    /// <returns>
    /// A string in which each invalid character — including <see cref="Path.DirectorySeparatorChar" /> and
    /// <see cref="Path.AltDirectorySeparatorChar" /> — has been replaced by <c>'_'</c>; the original instance when
    /// nothing required replacement. Returns <c>"_"</c> when the input is empty so that the result is never itself an
    /// empty segment.
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

        // Vectorised scan: the common all-valid input returns the original instance with no allocation.
        int firstInvalid = value.AsSpan().IndexOfAny(s_invalidPathSegmentChars);
        if (firstInvalid < 0) return value;

        return string.Create(value.Length, (value, firstInvalid), static (destination, state) =>
        {
            (string source, int first) = state;
            source.AsSpan(0, first).CopyTo(destination);

            for (int i = first; i < source.Length; i++)
            {
                char c = source[i];
                destination[i] = s_invalidPathSegmentChars.Contains(c) ? '_' : c;
            }
        });
    }
}
