// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.RemovePrefix.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class StringExtensions
{
    /// <summary>
    /// Returns <paramref name="value" /> with a single leading occurrence of <paramref name="prefix" /> removed when
    /// present.
    /// </summary>
    /// <param name="value">The string to inspect. Must not be <see langword="null" />.</param>
    /// <param name="prefix">The prefix to remove. Must not be <see langword="null" />.</param>
    /// <param name="comparison">
    /// The comparison rule used to detect <paramref name="prefix" /> at the start of <paramref name="value" />.
    /// Defaults to <see cref="StringComparison.Ordinal" />.
    /// </param>
    /// <returns>
    /// <paramref name="value" /> without the leading <paramref name="prefix" /> when present; otherwise the original
    /// instance.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> or <paramref name="prefix" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// Removes at most one occurrence of <paramref name="prefix" />. Use the BCL
    /// <see cref="string.TrimStart(char[])" /> when greedy removal of character-level prefixes is required.
    /// </remarks>
    public static string RemovePrefix(
        this string value,
        string prefix,
        StringComparison comparison = StringComparison.Ordinal)
    {
        ThrowHelper.ThrowIfNull(value);
        ThrowHelper.ThrowIfNull(prefix);

        return prefix.Length == 0 ? value : value.StartsWith(prefix, comparison) ? value[prefix.Length..] : value;
    }
}
