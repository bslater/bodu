// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.Quote.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class StringExtensions
{
    /// <summary>
    /// Returns <paramref name="value" /> wrapped in straight double-quote characters (<c>"…"</c>).
    /// </summary>
    /// <param name="value">The string to wrap. Must not be <see langword="null" />.</param>
    /// <returns>The double-quoted string.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// This method does not escape embedded quote characters. For CSV-style escaping use the helpers in the
    /// <c>Bodu.Text.Delimited</c> namespace.
    /// </remarks>
    public static string Quote(this string value)
    {
        ThrowHelper.ThrowIfNull(value);

        return "\"" + value + "\"";
    }
}
