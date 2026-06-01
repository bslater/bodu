// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.ReplaceOrdinalIgnoreCase.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class StringExtensions
{
    /// <summary>
    /// Returns <paramref name="value" /> with every occurrence of <paramref name="oldValue" /> replaced by
    /// <paramref name="newValue" /> using case-insensitive ordinal comparison.
    /// </summary>
    /// <param name="value">The string to search. Must not be <see langword="null" />.</param>
    /// <param name="oldValue">The substring to replace. Must not be <see langword="null" /> or empty.</param>
    /// <param name="newValue">The replacement substring. May be <see langword="null" /> or empty.</param>
    /// <returns>A new string with each case-insensitive occurrence of <paramref name="oldValue" /> replaced.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> or <paramref name="oldValue" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="oldValue" /> is the empty string.</exception>
    public static string ReplaceOrdinalIgnoreCase(this string value, string oldValue, string? newValue)
    {
        ThrowHelper.ThrowIfNull(value);
        ThrowHelper.ThrowIfNull(oldValue);
        return oldValue.Length == 0
            ? throw new ArgumentException("oldValue must not be empty.", nameof(oldValue))
            : value.Replace(oldValue, newValue, StringComparison.OrdinalIgnoreCase);
    }
}
