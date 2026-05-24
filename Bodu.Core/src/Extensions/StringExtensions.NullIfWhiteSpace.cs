// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.NullIfWhiteSpace.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class StringExtensions
{
    /// <summary>
    /// Returns <see langword="null" /> when <paramref name="value" /> is <see langword="null" />, empty, or consists
    /// solely of white-space characters; otherwise returns <paramref name="value" /> unchanged.
    /// </summary>
    /// <param name="value">The string to evaluate.</param>
    /// <returns>
    /// <see langword="null" /> when <paramref name="value" /> is <see langword="null" />, empty, or white-space-only;
    /// otherwise the original <paramref name="value" />.
    /// </returns>
    /// <remarks>
    /// Useful for input sanitisation pipelines where whitespace-only entries should be treated as missing data.
    /// </remarks>
    public static string? NullIfWhiteSpace(this string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;
}
