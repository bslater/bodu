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
/// Provides the bit handling and the text forms shared by the calendar value sets: <see cref="DayOfWeekSet" />,
/// <see cref="MonthSet" />, <see cref="DayOfMonthSet" />, <see cref="HourSet" />, <see cref="MinuteSet" />, and
/// <see cref="SecondSet" />.
/// </summary>
/// <remarks>
/// <para>
/// Each set holds its values in the low bits of a <see cref="ulong" />, bit <c>n</c> selecting the value
/// <c>minimum + n</c>, so a set of up to 64 consecutive values needs no allocation and every set operation is a single
/// bitwise instruction.
/// </para>
/// <para>
/// The numeric sets' text form is a comma-separated list in ascending order, with each run of two or more consecutive
/// values written as an inclusive range: <c>"1-3,12"</c> for January to March and December. The empty set is the empty
/// string.
/// </para>
/// <para>
/// Every set also has a binary form, one character per value of the domain, lowest first, with <c>1</c> for a selected
/// value; and a set whose values have letters, such as the days of the week, has a letter mask, the value's letter when
/// it is selected and a placeholder when it is not.
/// </para>
/// </remarks>
internal static partial class CalendarValueSet
{
    /// <summary>
    /// Returns the mask of the low bits of a domain of the specified size.
    /// </summary>
    /// <param name="count">The number of values in the domain, from 0 to 64.</param>
    /// <returns>A mask with the low <paramref name="count" /> bits set.</returns>
    /// <remarks>
    /// A 64-value domain is handled on its own, because a shift by 64 bits is a shift by none, so
    /// <c>(1UL &lt;&lt; 64) - 1</c> would be zero rather than every bit.
    /// </remarks>
    internal static ulong CreateMask(int count) =>
        count == 64 ? ulong.MaxValue : (1UL << count) - 1;

    /// <summary>
    /// Returns the mask of the low bits that select the values of a domain.
    /// </summary>
    /// <param name="minimum">The smallest value in the domain.</param>
    /// <param name="maximum">The largest value in the domain.</param>
    /// <returns>
    /// A mask with one bit set for each value from <paramref name="minimum" /> to <paramref name="maximum" />.
    /// </returns>
    internal static ulong DomainMask(int minimum, int maximum) =>
        CreateMask(maximum - minimum + 1);

    /// <summary>
    /// Writes a set's bits in the binary form: one character per value of the domain, lowest first, <c>1</c> for a
    /// selected value and <c>0</c> otherwise.
    /// </summary>
    /// <param name="bits">The set's bits.</param>
    /// <param name="width">The number of values in the domain.</param>
    /// <returns>The binary form, <paramref name="width" /> characters long.</returns>
    internal static string FormatBinary(ulong bits, int width) =>
        string.Create(width, bits, static (span, bits) =>
        {
            for (int i = 0; i < span.Length; i++)
                span[i] = ((bits >> i) & 1) != 0 ? '1' : '0';
        });

    /// <summary>
    /// Attempts to read the binary form: exactly one <c>0</c> or <c>1</c> per value of the domain, lowest first.
    /// </summary>
    /// <param name="text">The text to read.</param>
    /// <param name="width">The number of values in the domain.</param>
    /// <param name="bits">When this method returns <see cref="ParseFailure.None" />, the bits the text selects.</param>
    /// <param name="position">
    /// When this method returns <see cref="ParseFailure.Character" />, the index of the first character that is neither
    /// <c>0</c> nor <c>1</c>.
    /// </param>
    /// <returns>Why the text is not in the binary form, or <see cref="ParseFailure.None" /> when it is.</returns>
    internal static ParseFailure TryParseBinary(ReadOnlySpan<char> text, int width, out ulong bits, out int position)
    {
        bits = 0;
        position = 0;
        if (text.Length != width)
            return ParseFailure.Length;

        ulong selected = 0;
        for (int i = 0; i < width; i++)
        {
            char c = text[i];
            if (c == '1')
            {
                selected |= 1UL << i;
            }
            else if (c != '0')
            {
                position = i;
                return ParseFailure.Character;
            }
        }

        bits = selected;
        return ParseFailure.None;
    }

    /// <summary>
    /// Determines whether a text has the shape of the binary form: exactly one <c>0</c> or <c>1</c> per value of the
    /// domain.
    /// </summary>
    /// <param name="text">The text to test, untrimmed.</param>
    /// <param name="width">The number of values in the domain.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="text" /> is <paramref name="width" /> characters, each <c>0</c> or
    /// <c>1</c>; otherwise <see langword="false" />.
    /// </returns>
    internal static bool IsBinary(ReadOnlySpan<char> text, int width) =>
        text.Length == width && !text.ContainsAnyExcept('0', '1');

    /// <summary>
    /// Writes a set's bits as a letter mask: one character per value, the value's letter when it is selected and the
    /// placeholder when it is not.
    /// </summary>
    /// <param name="bits">The set's bits.</param>
    /// <param name="letters">The letter of each value, in bit order.</param>
    /// <param name="first">
    /// The index of the value written first; the others follow in bit order, wrapping around.
    /// </param>
    /// <param name="placeholder">The character for a value not selected.</param>
    /// <returns>The mask, one character per letter.</returns>
    internal static string FormatLetters(ulong bits, string letters, int first, char placeholder) =>
        string.Create(letters.Length, (Bits: bits, Letters: letters, First: first, Placeholder: placeholder), static (span, state) =>
        {
            for (int i = 0; i < span.Length; i++)
            {
                int index = (state.First + i) % span.Length;
                span[i] = ((state.Bits >> index) & 1) != 0 ? state.Letters[index] : state.Placeholder;
            }
        });

    /// <summary>
    /// Attempts to read a letter mask: one character per value, the value's letter, in either case, when it is selected
    /// and a placeholder when it is not, the same placeholder throughout.
    /// </summary>
    /// <param name="text">The text to read.</param>
    /// <param name="letters">The upper-case letter of each value, in bit order.</param>
    /// <param name="first">
    /// The index of the value written first, or <see langword="null" /> to choose between 0 and 1 by the first letter
    /// that fits only one of them.
    /// </param>
    /// <param name="placeholder">
    /// The placeholder, or <see langword="null" /> to take the first of <c>_</c>, <c>-</c>, <c>*</c> or a space that
    /// the text uses.
    /// </param>
    /// <param name="bits">When this method returns <see cref="ParseFailure.None" />, the bits the mask selects.</param>
    /// <param name="position">
    /// When this method returns <see cref="ParseFailure.Character" />, the index of the first character that fits
    /// neither the value at its position nor the placeholder.
    /// </param>
    /// <returns>Why the text is not a letter mask, or <see cref="ParseFailure.None" /> when it is one.</returns>
    internal static ParseFailure TryParseLetters(
        ReadOnlySpan<char> text,
        ReadOnlySpan<char> letters,
        int? first,
        char? placeholder,
        out ulong bits,
        out int position)
    {
        bits = 0;
        position = 0;
        int width = letters.Length;
        if (text.Length != width)
            return ParseFailure.Length;

        ulong selected = 0;
        for (int i = 0; i < width; i++)
        {
            char c = text[i];

            // Without a placeholder from the caller, the first one in the text fixes it for the rest.
            if (placeholder is null && c is '_' or '-' or '*' or ' ')
                placeholder = c;

            // Without a first value from the caller, the first letter that fits only one of the two orders decides it.
            char letter = char.ToUpperInvariant(c);
            if (first is null)
            {
                if (letter == letters[i])
                    first = 0;
                else if (letter == letters[(i + 1) % width])
                    first = 1;
            }

            int index = ((first ?? 0) + i) % width;
            if (letter == letters[index])
            {
                selected |= 1UL << index;
            }
            else if (c != placeholder)
            {
                position = i;
                return ParseFailure.Character;
            }
        }

        bits = selected;
        return ParseFailure.None;
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
