// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.StartsWithOrdinal.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class StringExtensions
{
    /// <summary>
    /// Returns a value indicating whether <paramref name="value" /> begins with <paramref name="valueToFind" /> under
    /// <see cref="StringComparison.Ordinal" />.
    /// </summary>
    /// <param name="value">The string to inspect. Must not be <see langword="null" />.</param>
    /// <param name="valueToFind">The prefix to locate. Must not be <see langword="null" />.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="value" /> starts with <paramref name="valueToFind" /> under ordinal
    /// comparison; otherwise <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> or <paramref name="valueToFind" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// Provides an explicit-Ordinal alternative to the BCL <see cref="string.StartsWith(string)" /> overload, which
    /// defaults to <see cref="StringComparison.CurrentCulture" /> and can produce surprising results across locales.
    /// </remarks>
    public static bool StartsWithOrdinal(this string value, string valueToFind)
    {
        ThrowHelper.ThrowIfNull(value);
        ThrowHelper.ThrowIfNull(valueToFind);

        return value.StartsWith(valueToFind, StringComparison.Ordinal);
    }
}
