// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.Brace.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class StringExtensions
{
    /// <summary>
    /// Returns <paramref name="value" /> wrapped in curly-brace characters (<c>{…}</c>).
    /// </summary>
    /// <param name="value">The string to wrap. Must not be <see langword="null" />.</param>
    /// <returns>The braced string.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    public static string Brace(this string value)
    {
        ThrowHelper.ThrowIfNull(value);

        return "{" + value + "}";
    }
}
