// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.RemoveSuffix.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class StringExtensions
{
    /// <summary>
    /// Returns <paramref name="value" /> with a single trailing occurrence of <paramref name="suffix" /> removed when
    /// present.
    /// </summary>
    /// <param name="value">The string to inspect. Must not be <see langword="null" />.</param>
    /// <param name="suffix">The suffix to remove. Must not be <see langword="null" />.</param>
    /// <param name="comparison">
    /// The comparison rule used to detect <paramref name="suffix" /> at the end of <paramref name="value" />. Defaults
    /// to <see cref="StringComparison.Ordinal" />.
    /// </param>
    /// <returns>
    /// <paramref name="value" /> without the trailing <paramref name="suffix" /> when present; otherwise the original
    /// instance.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> or <paramref name="suffix" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// Removes at most one occurrence of <paramref name="suffix" />. Use the BCL <see cref="string.TrimEnd(char[])" />
    /// when greedy removal of character-level suffixes is required.
    /// </remarks>
    public static string RemoveSuffix(
        this string value,
        string suffix,
        StringComparison comparison = StringComparison.Ordinal)
    {
        ThrowHelper.ThrowIfNull(value);
        ThrowHelper.ThrowIfNull(suffix);

        return suffix.Length == 0
            ? value
            : value.EndsWith(suffix, comparison)
            ? value[..^suffix.Length]
            : value;
    }
}
