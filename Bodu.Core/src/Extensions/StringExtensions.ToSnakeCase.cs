// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.ToSnakeCase.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Bodu.Extensions;

public static partial class StringExtensions
{
    /// <summary>
    /// Converts <paramref name="value" /> to <c>snake_case</c>: lower-cased words joined by underscores.
    /// </summary>
    /// <param name="value">The string to convert. Must not be <see langword="null" />.</param>
    /// <returns>The <c>snake_case</c> form of <paramref name="value" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// Word boundaries follow <see cref="EnumerateWords(string, WordCasingOptions)" /> using
    /// <see cref="WordCasingOptions.Default" />. Every word, including acronyms, is lower-cased.
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// "UserAccountId".ToSnakeCase();  // "user_account_id"
    ///]]>
    /// </code>
    /// </example>
    public static string ToSnakeCase(this string value) =>
        ToSnakeCase(value, WordCasingOptions.Default);

    /// <summary>
    /// Converts <paramref name="value" /> to <c>snake_case</c> under the supplied <paramref name="options" />.
    /// </summary>
    /// <param name="value">The string to convert. Must not be <see langword="null" />.</param>
    /// <param name="options">
    /// The acronym, mixed-case, and culture configuration. Must not be <see langword="null" />.
    /// </param>
    /// <returns>The <c>snake_case</c> form of <paramref name="value" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> or <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// Every word, including acronyms, is lower-cased before joining.
    /// </remarks>
    public static string ToSnakeCase(this string value, WordCasingOptions options) =>
        JoinLowerWords(value, options, '_');
}
