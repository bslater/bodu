// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalendarValueSet.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Numerics;
using System.Text;

namespace Bodu;

/// <summary>
/// Provides the bit handling and the text form shared by the numeric calendar value sets: <see cref="MonthSet" />,
/// <see cref="DayOfMonthSet" />, <see cref="HourSet" />, <see cref="MinuteSet" />, and <see cref="SecondSet" />.
/// </summary>
/// <remarks>
/// <para>
/// Each set holds its values in the low bits of a <see cref="ulong" />, bit <c>n</c> selecting the value
/// <c>minimum + n</c>, so a set of up to 64 consecutive values needs no allocation and every set operation is a single
/// bitwise instruction.
/// </para>
/// <para>
/// The text form is a comma-separated list in ascending order, with each run of two or more consecutive values written
/// as an inclusive range: <c>"1-3,12"</c> for January to March and December. The empty set is the empty string.
/// </para>
/// </remarks>
internal static class CalendarValueSet
{
    /// <summary>
    /// Returns the mask of the low bits that select the values of a domain.
    /// </summary>
    /// <param name="minimum">The smallest value in the domain.</param>
    /// <param name="maximum">The largest value in the domain.</param>
    /// <returns>
    /// A mask with one bit set for each value from <paramref name="minimum" /> to <paramref name="maximum" />.
    /// </returns>
    internal static ulong DomainMask(int minimum, int maximum)
    {
        int count = maximum - minimum + 1;
        return count == 64 ? ulong.MaxValue : (1UL << count) - 1;
    }

    /// <summary>
    /// Builds the bits that select the specified values.
    /// </summary>
    /// <param name="values">The values to select, or <see langword="null" /> for none.</param>
    /// <param name="minimum">The smallest value in the domain.</param>
    /// <param name="maximum">The largest value in the domain.</param>
    /// <param name="paramName">The name of the parameter that supplied <paramref name="values" />.</param>
    /// <returns>The bits that select every value in <paramref name="values" />.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when a value is less than <paramref name="minimum" /> or greater than <paramref name="maximum" />.
    /// </exception>
    internal static ulong FromValues(int[]? values, int minimum, int maximum, string paramName)
    {
        if (values is null)
            return 0;

        ulong bits = 0;
        foreach (int value in values)
        {
            ThrowHelper.ThrowIfOutOfRange(value, minimum, maximum, paramName: paramName);
            bits |= 1UL << (value - minimum);
        }

        return bits;
    }

    /// <summary>
    /// Writes the values a set's bits select as a comma-separated list, each run of two or more consecutive values as
    /// an inclusive range.
    /// </summary>
    /// <param name="bits">The set's bits.</param>
    /// <param name="minimum">The value bit zero selects.</param>
    /// <returns>The canonical text form, or the empty string when no bit is set.</returns>
    internal static string Format(ulong bits, int minimum)
    {
        if (bits == 0)
            return string.Empty;

        var builder = new StringBuilder();
        while (bits != 0)
        {
            int start = BitOperations.TrailingZeroCount(bits);
            int run = BitOperations.TrailingZeroCount(~(bits >> start));
            if (builder.Length > 0)
                builder.Append(',');

            builder.Append((minimum + start).ToString(CultureInfo.InvariantCulture));
            if (run > 1)
                builder.Append('-').Append((minimum + start + run - 1).ToString(CultureInfo.InvariantCulture));

            // Clear the run just written; a run that reaches bit 63 leaves nothing above it.
            bits = start + run >= 64 ? 0 : bits & ~((1UL << (start + run)) - 1);
        }

        return builder.ToString();
    }

    /// <summary>
    /// Attempts to read a comma-separated list of values and inclusive ranges.
    /// </summary>
    /// <param name="text">The text to read.</param>
    /// <param name="minimum">The smallest value in the domain.</param>
    /// <param name="maximum">The largest value in the domain.</param>
    /// <param name="bits">
    /// When this method returns <see langword="true" />, the bits that select the values read.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="text" /> is a valid list; otherwise <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// Whitespace around the list and around each value is ignored, and an empty or blank text is the empty set. Values
    /// are unsigned decimal integers; a range <c>a-b</c> needs <c>a</c> no greater than <c>b</c>; values may repeat and
    /// ranges may overlap.
    /// </remarks>
    internal static bool TryParse(ReadOnlySpan<char> text, int minimum, int maximum, out ulong bits)
    {
        bits = 0;
        text = text.Trim();
        if (text.IsEmpty)
            return true;

        while (true)
        {
            int comma = text.IndexOf(',');
            ReadOnlySpan<char> item = comma < 0 ? text : text[..comma];
            if (!TryParseItem(item, minimum, maximum, ref bits))
            {
                bits = 0;
                return false;
            }

            if (comma < 0)
                return true;

            text = text[(comma + 1)..];
        }
    }

    /// <summary>
    /// Attempts to read one list item, a value or an inclusive range, and select its values.
    /// </summary>
    /// <param name="item">The item text.</param>
    /// <param name="minimum">The smallest value in the domain.</param>
    /// <param name="maximum">The largest value in the domain.</param>
    /// <param name="bits">The bits to add the item's values to.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="item" /> is a valid value or range; otherwise
    /// <see langword="false" />.
    /// </returns>
    private static bool TryParseItem(ReadOnlySpan<char> item, int minimum, int maximum, ref ulong bits)
    {
        int dash = item.IndexOf('-');
        if (dash < 0)
        {
            if (!TryParseValue(item, minimum, maximum, out int value))
                return false;

            bits |= 1UL << (value - minimum);
            return true;
        }

        if (!TryParseValue(item[..dash], minimum, maximum, out int low)
            || !TryParseValue(item[(dash + 1)..], minimum, maximum, out int high)
            || low > high)
        {
            return false;
        }

        ulong range = DomainMask(low, high) << (low - minimum);
        bits |= range;
        return true;
    }

    /// <summary>
    /// Attempts to read one value of the domain.
    /// </summary>
    /// <param name="text">The value text, possibly surrounded by whitespace.</param>
    /// <param name="minimum">The smallest value in the domain.</param>
    /// <param name="maximum">The largest value in the domain.</param>
    /// <param name="value">When this method returns <see langword="true" />, the value read.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="text" /> is an unsigned decimal integer in the domain; otherwise
    /// <see langword="false" />.
    /// </returns>
    private static bool TryParseValue(ReadOnlySpan<char> text, int minimum, int maximum, out int value) =>
        int.TryParse(text.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out value)
        && value >= minimum
        && value <= maximum;
}
