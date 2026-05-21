// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.RemoveControlCharacters.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class StringExtensions
{
    /// <summary>
    /// Returns <paramref name="value" /> with every Unicode control character removed.
    /// </summary>
    /// <param name="value">The string to inspect. Must not be <see langword="null" />.</param>
    /// <returns>A new string with control characters stripped.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// Membership is determined via <see cref="char.IsControl(char)" /> which covers ASCII C0 control codes
    /// (U+0000–U+001F), DEL (U+007F), and the C1 control range (U+0080–U+009F). Tab, CR and LF count as control
    /// characters and are also removed.
    /// </remarks>
    public static string RemoveControlCharacters(this string value) =>
        value.RemoveWhere(char.IsControl);
}
