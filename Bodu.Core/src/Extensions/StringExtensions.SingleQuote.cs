// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.SingleQuote.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class StringExtensions
{
    /// <summary>
    /// Returns <paramref name="value" /> wrapped in straight single-quote characters (<c>'…'</c>).
    /// </summary>
    /// <param name="value">The string to wrap. Must not be <see langword="null" />.</param>
    /// <returns>The single-quoted string.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// This method does not escape embedded apostrophes.
    /// </remarks>
    public static string SingleQuote(this string value)
    {
        ThrowHelper.ThrowIfNull(value);

        return "'" + value + "'";
    }
}
