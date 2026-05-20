// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.NullIfEmpty.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class StringExtensions
{
    /// <summary>
    /// Returns <see langword="null" /> when <paramref name="value" /> is <see langword="null" /> or the empty string;
    /// otherwise returns <paramref name="value" /> unchanged.
    /// </summary>
    /// <param name="value">The string to evaluate.</param>
    /// <returns>
    /// <see langword="null" /> when <paramref name="value" /> is <see langword="null" /> or <see cref="string.Empty" />
    /// ; otherwise the original <paramref name="value" />.
    /// </returns>
    /// <remarks>
    /// Useful for collapsing the "missing" and "blank" cases into a single <see langword="null" /> sentinel — for
    /// example, <c>request.Title.NullIfEmpty() ?? defaultTitle</c>.
    /// </remarks>
    public static string? NullIfEmpty(this string? value) =>
        string.IsNullOrEmpty(value) ? null : value;
}
