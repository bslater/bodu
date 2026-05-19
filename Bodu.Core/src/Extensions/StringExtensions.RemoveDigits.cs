// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.RemoveDigits.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class StringExtensions
{
    /// <summary>
    /// Returns <paramref name="value" /> with every Unicode digit character removed.
    /// </summary>
    /// <param name="value">The string to inspect. Must not be <see langword="null" />.</param>
    /// <returns>
    /// A new string with digit characters stripped.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    public static string RemoveDigits(this string value) =>
        value.RemoveWhere(char.IsDigit);
}
