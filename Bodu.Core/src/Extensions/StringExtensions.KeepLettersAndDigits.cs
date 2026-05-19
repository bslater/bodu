// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.KeepLettersAndDigits.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class StringExtensions
{
    /// <summary>
    /// Returns <paramref name="value" /> filtered down to its Unicode letter and digit characters.
    /// </summary>
    /// <param name="value">The string to filter. Must not be <see langword="null" />.</param>
    /// <returns>
    /// A new string containing only the letter and digit characters from <paramref name="value" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    public static string KeepLettersAndDigits(this string value) =>
        value.KeepWhere(char.IsLetterOrDigit);
}
