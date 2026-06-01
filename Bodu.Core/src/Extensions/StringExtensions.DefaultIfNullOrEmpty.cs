// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.DefaultIfNullOrEmpty.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class StringExtensions
{
    /// <summary>
    /// Returns <paramref name="defaultValue" /> when <paramref name="value" /> is <see langword="null" /> or empty;
    /// otherwise returns <paramref name="value" /> unchanged.
    /// </summary>
    /// <param name="value">The string to evaluate.</param>
    /// <param name="defaultValue">
    /// The fallback returned when <paramref name="value" /> is <see langword="null" /> or empty. Must not be
    /// <see langword="null" />.
    /// </param>
    /// <returns>
    /// <paramref name="value" /> when it contains at least one character; otherwise <paramref name="defaultValue" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="defaultValue" /> is <see langword="null" />.
    /// </exception>
    public static string DefaultIfNullOrEmpty(this string? value, string defaultValue)
    {
        ThrowHelper.ThrowIfNull(defaultValue);

        return string.IsNullOrEmpty(value) ? defaultValue : value;
    }
}
