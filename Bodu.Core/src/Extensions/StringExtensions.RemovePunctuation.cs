// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.RemovePunctuation.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class StringExtensions
{
    /// <summary>
    /// Returns <paramref name="value" /> with every Unicode punctuation character removed.
    /// </summary>
    /// <param name="value">The string to inspect. Must not be <see langword="null" />.</param>
    /// <returns>A new string with punctuation characters stripped.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// Membership is determined via <see cref="char.IsPunctuation(char)" /> which covers the Unicode punctuation
    /// categories (Pc, Pd, Pe, Pf, Pi, Po, Ps).
    /// </remarks>
    public static string RemovePunctuation(this string value) =>
        value.RemoveWhere(char.IsPunctuation);
}
