// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.Unwrap.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class StringExtensions
{
    /// <summary>
    /// Returns <paramref name="value" /> with <paramref name="prefix" /> and <paramref name="suffix" /> removed if both
    /// are present at their respective ends.
    /// </summary>
    /// <param name="value">The string to unwrap. Must not be <see langword="null" />.</param>
    /// <param name="prefix">The expected prefix. Must not be <see langword="null" />.</param>
    /// <param name="suffix">The expected suffix. Must not be <see langword="null" />.</param>
    /// <param name="comparison">The string comparison used for prefix and suffix matching.</param>
    /// <returns>
    /// The unwrapped string when both ends match; otherwise <paramref name="value" /> unchanged. Unwrapping requires
    /// both ends to match — partial matches are left intact so the operation is reversible against
    /// <see cref="Wrap(string, string, string)" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when any string argument is <see langword="null" />.</exception>
    public static string Unwrap(
        this string value,
        string prefix,
        string suffix,
        StringComparison comparison = StringComparison.Ordinal)
    {
        ThrowHelper.ThrowIfNull(value);
        ThrowHelper.ThrowIfNull(prefix);
        ThrowHelper.ThrowIfNull(suffix);

        if (value.Length < prefix.Length + suffix.Length) return value;
        if (!value.StartsWith(prefix, comparison)) return value;
        if (!value.EndsWith(suffix, comparison)) return value;

        var inner = value.Length - prefix.Length - suffix.Length;
        return value.Substring(prefix.Length, inner);
    }
}
