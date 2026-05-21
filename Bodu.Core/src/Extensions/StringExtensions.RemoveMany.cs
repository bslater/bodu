// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.RemoveMany.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class StringExtensions
{
    /// <summary>
    /// Returns <paramref name="value" /> with every occurrence of each entry in <paramref name="valuesToRemove" />
    /// removed under ordinal comparison, applied sequentially in array order.
    /// </summary>
    /// <param name="value">The string to inspect. Must not be <see langword="null" />.</param>
    /// <param name="valuesToRemove">
    /// The substrings to remove. Must not be <see langword="null" />; individual entries must not be
    /// <see langword="null" /> or empty.
    /// </param>
    /// <returns>
    /// A new string with all matches removed. When <paramref name="valuesToRemove" /> is empty, the original instance
    /// is returned unchanged.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> or <paramref name="valuesToRemove" /> is <see langword="null" />, or when
    /// any element of <paramref name="valuesToRemove" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when any element of <paramref name="valuesToRemove" /> is the empty string.
    /// </exception>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// "(555) 123-4567".RemoveMany("(", ")", " ", "-");  // "5551234567"
    ///]]>
    /// </code>
    /// </example>
    public static string RemoveMany(this string value, params string[] valuesToRemove)
    {
        ThrowHelper.ThrowIfNull(value);
        ThrowHelper.ThrowIfNull(valuesToRemove);

        if (valuesToRemove.Length == 0) return value;

        string current = value;
        for (int i = 0; i < valuesToRemove.Length; i++)
        {
            string item = valuesToRemove[i];
            ThrowHelper.ThrowIfNull(item);
            if (item.Length == 0) throw new ArgumentException(ResourceStrings.Arg_Invalid_EmptyCollectionElement, nameof(valuesToRemove));
            current = current.Replace(item, string.Empty, StringComparison.Ordinal);
        }

        return current;
    }
}
