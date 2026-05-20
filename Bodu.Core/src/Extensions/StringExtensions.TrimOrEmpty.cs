// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.TrimOrEmpty.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class StringExtensions
{
    /// <summary>
    /// Returns the trimmed form of <paramref name="value" />, or <see cref="string.Empty" /> when
    /// <paramref name="value" /> is <see langword="null" />.
    /// </summary>
    /// <param name="value">The string to trim.</param>
    /// <returns>
    /// <see cref="string.Empty" /> when <paramref name="value" /> is <see langword="null" />; otherwise the result of
    /// <see cref="string.Trim()" /> applied to <paramref name="value" />.
    /// </returns>
    /// <remarks>
    /// Avoids the common mistake of writing <c>value?.Trim() ?? string.Empty</c> manually — the <see langword="null" />
    /// propagation precedes the trim call so callers can safely chain further string operations on the result.
    /// </remarks>
    public static string TrimOrEmpty(this string? value) =>
        value is null ? string.Empty : value.Trim();
}
