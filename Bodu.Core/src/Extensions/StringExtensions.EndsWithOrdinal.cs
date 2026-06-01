// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.EndsWithOrdinal.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class StringExtensions
{
    /// <summary>
    /// Returns a value indicating whether <paramref name="value" /> ends with <paramref name="valueToFind" /> under
    /// <see cref="StringComparison.Ordinal" />.
    /// </summary>
    /// <param name="value">The string to inspect. Must not be <see langword="null" />.</param>
    /// <param name="valueToFind">The suffix to locate. Must not be <see langword="null" />.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="value" /> ends with <paramref name="valueToFind" /> under ordinal
    /// comparison; otherwise <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> or <paramref name="valueToFind" /> is <see langword="null" />.
    /// </exception>
    public static bool EndsWithOrdinal(this string value, string valueToFind)
    {
        ThrowHelper.ThrowIfNull(value);
        ThrowHelper.ThrowIfNull(valueToFind);

        return value.EndsWith(valueToFind, StringComparison.Ordinal);
    }
}
