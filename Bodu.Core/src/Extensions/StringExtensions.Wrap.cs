// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.Wrap.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class StringExtensions
{
    /// <summary>
    /// Returns <paramref name="value" /> with <paramref name="prefix" /> prepended and <paramref name="suffix" />
    /// appended.
    /// </summary>
    /// <param name="value">The string to wrap. Must not be <see langword="null" />.</param>
    /// <param name="prefix">The string to prepend. Must not be <see langword="null" />.</param>
    /// <param name="suffix">The string to append. Must not be <see langword="null" />.</param>
    /// <returns>The wrapped string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any argument is <see langword="null" />.</exception>
    public static string Wrap(this string value, string prefix, string suffix)
    {
        ThrowHelper.ThrowIfNull(value);
        ThrowHelper.ThrowIfNull(prefix);
        ThrowHelper.ThrowIfNull(suffix);

        return prefix + value + suffix;
    }
}
