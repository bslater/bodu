// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.StartsWithOrdinalIgnoreCase.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class StringExtensions
{
    /// <summary>
    /// Returns a value indicating whether <paramref name="value" /> begins with <paramref name="valueToFind" /> under
    /// <see cref="StringComparison.OrdinalIgnoreCase" />.
    /// </summary>
    /// <param name="value">The string to inspect. Must not be <see langword="null" />.</param>
    /// <param name="valueToFind">The prefix to locate. Must not be <see langword="null" />.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="value" /> starts with <paramref name="valueToFind" /> under
    /// case-insensitive ordinal comparison; otherwise <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> or <paramref name="valueToFind" /> is <see langword="null" />.
    /// </exception>
    public static bool StartsWithOrdinalIgnoreCase(this string value, string valueToFind)
    {
        ThrowHelper.ThrowIfNull(value);
        ThrowHelper.ThrowIfNull(valueToFind);

        return value.StartsWith(valueToFind, StringComparison.OrdinalIgnoreCase);
    }
}
