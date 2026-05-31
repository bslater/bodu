// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.KeepWhere.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Extensions;

public static partial class StringExtensions
{
    /// <summary>
    /// Returns <paramref name="value" /> filtered down to the characters for which <paramref name="predicate" />
    /// returns <see langword="true" />.
    /// </summary>
    /// <param name="value">The string to filter. Must not be <see langword="null" />.</param>
    /// <param name="predicate">The selector evaluated for each character. Must not be <see langword="null" />.</param>
    /// <returns>
    /// A new string containing only the characters where <paramref name="predicate" /> returned <see langword="true" />
    /// . When every character is kept, the original instance is returned.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> or <paramref name="predicate" /> is <see langword="null" />.
    /// </exception>
    public static string KeepWhere(this string value, Func<char, bool> predicate)
    {
        ThrowHelper.ThrowIfNull(value);
        ThrowHelper.ThrowIfNull(predicate);

        if (value.Length == 0) return value;

        StringBuilder? builder = null;
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (predicate(c))
            {
                builder?.Append(c);
            }
            else
            {
                builder ??= new StringBuilder(value.Length).Append(value, 0, i);
            }
        }

        return builder is null ? value : builder.ToString();
    }
}
