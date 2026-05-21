// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.KeepLetters.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class StringExtensions
{
    /// <summary>
    /// Returns <paramref name="value" /> filtered down to its Unicode letter characters.
    /// </summary>
    /// <param name="value">The string to filter. Must not be <see langword="null" />.</param>
    /// <returns>A new string containing only the letter characters from <paramref name="value" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// Membership is determined via <see cref="char.IsLetter(char)" />, which recognises every Unicode letter category
    /// (Lu, Ll, Lt, Lm, Lo). ASCII digits, punctuation, whitespace, and control characters are stripped.
    /// </remarks>
    public static string KeepLetters(this string value) =>
        value.KeepWhere(char.IsLetter);
}
