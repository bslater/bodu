// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MinuteSet.Parse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Bodu;

public readonly partial struct MinuteSet
{
    /// <summary>
    /// Converts a comma-separated list of minutes and inclusive ranges into a <see cref="MinuteSet" />.
    /// </summary>
    /// <param name="s">The text to convert, such as <c>"0,15,30,45"</c>.</param>
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
    public static MinuteSet Parse(string s)
    {
        ThrowHelper.ThrowIfNull(s);

        return TryParse(s, out MinuteSet result)
            ? result
            : throw new FormatException(
                string.Format(CultureInfo.CurrentCulture, ResourceStrings.Format_Invalid_CalendarValueList, s, MinimumValue, MaximumValue));
    }

    /// <summary>
    /// Attempts to convert a comma-separated list of minutes and inclusive ranges into a <see cref="MinuteSet" />.
    /// </summary>
    /// <param name="s">The text to convert, such as <c>"0,15,30,45"</c>.</param>
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
    public static bool TryParse([NotNullWhen(true)] string? s, out MinuteSet result)
    {
        if (s is not null && CalendarValueSet.TryParse(s, MinimumValue, MaximumValue, out ulong bits))
        {
            result = new MinuteSet(bits);
            return true;
        }

        result = default;
        return false;
    }

    /// <inheritdoc />
    static MinuteSet IParsable<MinuteSet>.Parse(string s, IFormatProvider? provider) =>
        Parse(s);

    /// <inheritdoc />
    static bool IParsable<MinuteSet>.TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out MinuteSet result) =>
        TryParse(s, out result);

    /// <summary>
    /// Returns the canonical text form of the set: the selected minutes in ascending order, separated by commas, each
    /// run of two or more consecutive minutes written as an inclusive range.
    /// </summary>
    /// <returns>The text form, such as <c>"0,15,30,45"</c>, or the empty string for the empty set.</returns>
    public override string ToString() =>
        CalendarValueSet.Format(_bits, MinimumValue);
}
