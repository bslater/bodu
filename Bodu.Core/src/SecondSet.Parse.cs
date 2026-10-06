// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SecondSet.Parse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Bodu;

public readonly partial struct SecondSet
{
    /// <summary>
    /// Converts a comma-separated list of seconds and inclusive ranges into a <see cref="SecondSet" />.
    /// </summary>
    /// <param name="s">The text to convert, such as <c>"0,30"</c>.</param>
    /// <returns>The set the text describes.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="s" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="FormatException">
    /// Thrown when <paramref name="s" /> is not a list of values and ranges from 0 to 59.
    /// </exception>
    /// <remarks>
    /// Whitespace around the list and around each value is ignored, and an empty or blank string is the empty set.
    /// Values are unsigned decimal integers, a range <c>a-b</c> needs <c>a</c> no greater than <c>b</c>, and values may
    /// repeat or overlap.
    /// </remarks>
    public static SecondSet Parse(string s)
    {
        ThrowHelper.ThrowIfNull(s);

        return TryParse(s, out SecondSet result)
            ? result
            : throw new FormatException(
                string.Format(CultureInfo.CurrentCulture, ResourceStrings.Format_Invalid_CalendarValueList, s, MinimumValue, MaximumValue));
    }

    /// <summary>
    /// Attempts to convert a comma-separated list of seconds and inclusive ranges into a <see cref="SecondSet" />.
    /// </summary>
    /// <param name="s">The text to convert, such as <c>"0,30"</c>.</param>
    /// <param name="result">
    /// When this method returns <see langword="true" />, the set the text describes; otherwise <see cref="Empty" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="s" /> is a list of values and ranges from 0 to 59; otherwise
    /// <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// The text is read as <see cref="Parse(string)" /> reads it.
    /// </remarks>
    public static bool TryParse([NotNullWhen(true)] string? s, out SecondSet result)
    {
        if (s is not null && CalendarValueSet.TryParse(s, MinimumValue, MaximumValue, out ulong bits))
        {
            result = new SecondSet(bits);
            return true;
        }

        result = default;
        return false;
    }

    /// <inheritdoc />
    static SecondSet IParsable<SecondSet>.Parse(string s, IFormatProvider? provider) =>
        Parse(s);

    /// <inheritdoc />
    static bool IParsable<SecondSet>.TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out SecondSet result) =>
        TryParse(s, out result);

    /// <summary>
    /// Returns the canonical text form of the set: the selected seconds in ascending order, separated by commas, each
    /// run of two or more consecutive seconds written as an inclusive range.
    /// </summary>
    /// <returns>The text form, such as <c>"0,30"</c>, or the empty string for the empty set.</returns>
    public override string ToString() =>
        CalendarValueSet.Format(_bits, MinimumValue);
}
