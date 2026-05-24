// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.KeepDigits.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class StringExtensions
{
    /// <summary>
    /// Returns <paramref name="value" /> filtered down to its Unicode digit characters.
    /// </summary>
    /// <param name="value">The string to filter. Must not be <see langword="null" />.</param>
    /// <returns>A new string containing only the digit characters from <paramref name="value" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// Membership is determined via <see cref="char.IsDigit(char)" />, which recognises every Unicode digit (Nd)
    /// character — not just ASCII <c>0–9</c>.
    /// </remarks>
    public static string KeepDigits(this string value) =>
        value.KeepWhere(char.IsDigit);
}
