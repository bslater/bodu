// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.HasText.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class StringExtensions
{
    /// <summary>
    /// Returns a value indicating whether <paramref name="value" /> is non-<see langword="null" />, non-empty, and
    /// contains at least one character that is not white space.
    /// </summary>
    /// <param name="value">The string to inspect.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="value" /> contains at least one non-white-space character;
    /// <see langword="false" /> when <paramref name="value" /> is <see langword="null" />, empty, or consists solely of
    /// white-space characters.
    /// </returns>
    /// <remarks>
    /// Provides the positive form of <see cref="string.IsNullOrWhiteSpace(string)" /> so call sites can express "has
    /// meaningful text" affirmatively in LINQ predicates and conditional expressions.
    /// </remarks>
    public static bool HasText(this string? value) =>
        !string.IsNullOrWhiteSpace(value);
}
