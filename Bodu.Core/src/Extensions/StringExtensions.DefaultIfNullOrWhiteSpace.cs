// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.DefaultIfNullOrWhiteSpace.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class StringExtensions
{
    /// <summary>
    /// Returns <paramref name="defaultValue" /> when <paramref name="value" /> is <see langword="null" />, empty, or
    /// white-space-only; otherwise returns <paramref name="value" /> unchanged.
    /// </summary>
    /// <param name="value">The string to evaluate.</param>
    /// <param name="defaultValue">
    /// The fallback returned when <paramref name="value" /> is <see langword="null" />, empty, or contains only
    /// white-space characters. Must not be <see langword="null" />.
    /// </param>
    /// <returns>
    /// <paramref name="value" /> when it contains at least one non-white-space character; otherwise
    /// <paramref name="defaultValue" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="defaultValue" /> is <see langword="null" />.
    /// </exception>
    public static string DefaultIfNullOrWhiteSpace(this string? value, string defaultValue)
    {
        ThrowHelper.ThrowIfNull(defaultValue);

        return string.IsNullOrWhiteSpace(value) ? defaultValue : value;
    }
}
