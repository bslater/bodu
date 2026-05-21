// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.RemovePunctuation.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

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
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// "Hello, world!".RemovePunctuation();  // "Hello world"
    ///]]>
    /// </code>
    /// </example>
    public static string RemovePunctuation(this string value) =>
        value.RemoveWhere(char.IsPunctuation);
}
