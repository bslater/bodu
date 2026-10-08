// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalendarValueSet.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
    /// <summary>The length of a buffer that holds a set's text in any form.</summary>
    /// <remarks>
    /// The longest text is a list of every value of a 64-value domain whose values have up to three digits: 64 values
    /// and 63 commas, at most 255 characters. The sets' own domains have at most 60 values of at most two digits.
    /// </remarks>
    internal const int MaxTextLength = 256;

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
    /// Writes the values a set's bits select as a comma-separated list, every value written out.
    /// </summary>
    /// <param name="bits">The set's bits.</param>
    /// <param name="minimum">The value bit zero selects.</param>
    /// <returns>The list without ranges, or the empty string when no bit is set.</returns>
    internal static string FormatValues(ulong bits, int minimum)
    {
        Span<char> buffer = stackalloc char[MaxTextLength];
        return new string(buffer[..WriteList(bits, minimum, ranges: false, buffer)]);
    }

    /// <summary>
    /// Writes a numeric set's bits in one of the forms every numeric set shares.
    /// </summary>
    /// <param name="bits">The set's bits.</param>
    /// <param name="minimum">The smallest value in the domain.</param>
    /// <param name="maximum">The largest value in the domain.</param>
    /// <param name="form">The form to write.</param>
    /// <returns>The text.</returns>
    internal static string Format(ulong bits, int minimum, int maximum, NumericForm form)
    {
        Span<char> buffer = stackalloc char[MaxTextLength];
        return new string(buffer[..Write(bits, minimum, maximum, form, buffer)]);
    }

    /// <summary>
    /// Writes a numeric set's bits in one of the forms every numeric set shares.
    /// </summary>
    /// <param name="bits">The set's bits.</param>
    /// <param name="minimum">The smallest value in the domain.</param>
    /// <param name="maximum">The largest value in the domain.</param>
    /// <param name="form">The form to write.</param>
    /// <param name="buffer">The buffer to write to, at least <see cref="MaxTextLength" /> characters long.</param>
    /// <returns>The number of characters written.</returns>
    internal static int Write(ulong bits, int minimum, int maximum, NumericForm form, Span<char> buffer) =>
        form switch
        {
            NumericForm.Values => WriteList(bits, minimum, ranges: false, buffer),
            NumericForm.Binary => WriteBinary(bits, maximum - minimum + 1, buffer),
            _ => WriteList(bits, minimum, ranges: true, buffer),
        };

    /// <summary>
    /// Copies a set's text to a span of characters, or nothing when it does not fit.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="destination">The span to copy to.</param>
    /// <param name="charsWritten">
    /// When this method returns <see langword="true" />, the length of <paramref name="text" />; otherwise 0.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="text" /> fits in <paramref name="destination" />; otherwise
    /// <see langword="false" />, with <paramref name="destination" /> unchanged.
    /// </returns>
    internal static bool TryCopy(ReadOnlySpan<char> text, Span<char> destination, out int charsWritten)
    {
        if (!text.TryCopyTo(destination))
        {
            charsWritten = 0;
            return false;
        }

        charsWritten = text.Length;
        return true;
    }

    /// <summary>
    /// Copies a set's text to a span of UTF-8 bytes, or nothing when it does not fit.
    /// </summary>
    /// <param name="text">The text, which is ASCII, so one byte per character.</param>
    /// <param name="utf8Destination">The span to copy to.</param>
    /// <param name="bytesWritten">
    /// When this method returns <see langword="true" />, the length of <paramref name="text" />; otherwise 0.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="text" /> fits in <paramref name="utf8Destination" />; otherwise
    /// <see langword="false" />, with <paramref name="utf8Destination" /> unchanged.
    /// </returns>
    internal static bool TryCopyUtf8(ReadOnlySpan<char> text, Span<byte> utf8Destination, out int bytesWritten)
    {
        if (utf8Destination.Length < text.Length)
        {
            bytesWritten = 0;
            return false;
        }

        OperationStatus status = Ascii.FromUtf16(text, utf8Destination, out bytesWritten);
        Debug.Assert(status == OperationStatus.Done, "A set's text is ASCII.");
        return true;
    }

    /// <summary>
    /// Converts UTF-8 text into a set by decoding it and reading the characters as the set's character parser does.
    /// </summary>
    /// <typeparam name="TSet">The type of set.</typeparam>
    /// <param name="utf8Text">The UTF-8 text.</param>
    /// <returns>The set the text describes.</returns>
    /// <exception cref="FormatException">
    /// Thrown when the decoded text is not a set; an invalid UTF-8 sequence decodes to U+FFFD, which no form accepts.
    /// </exception>
    internal static TSet ParseUtf8<TSet>(ReadOnlySpan<byte> utf8Text)
        where TSet : ISpanParsable<TSet>
    {
        char[]? rented = null;
        Span<char> text = utf8Text.Length <= MaxTextLength
            ? stackalloc char[MaxTextLength]
            : rented = ArrayPool<char>.Shared.Rent(utf8Text.Length);

        try
        {
            return TSet.Parse(text[..Decode(utf8Text, text)], provider: null);
        }
        finally
        {
            if (rented is not null)
                ArrayPool<char>.Shared.Return(rented);
        }
    }

    /// <summary>
    /// Attempts to convert UTF-8 text into a set by decoding it and reading the characters as the set's character
    /// parser does.
    /// </summary>
    /// <typeparam name="TSet">The type of set.</typeparam>
    /// <param name="utf8Text">The UTF-8 text.</param>
    /// <param name="result">When this method returns <see langword="true" />, the set the text describes.</param>
    /// <returns>
    /// <see langword="true" /> when the decoded text is a set; otherwise <see langword="false" />, which includes text
    /// that is not valid UTF-8.
    /// </returns>
    internal static bool TryParseUtf8<TSet>(ReadOnlySpan<byte> utf8Text, [MaybeNullWhen(false)] out TSet result)
        where TSet : ISpanParsable<TSet>
    {
        char[]? rented = null;
        Span<char> text = utf8Text.Length <= MaxTextLength
            ? stackalloc char[MaxTextLength]
            : rented = ArrayPool<char>.Shared.Rent(utf8Text.Length);

        try
        {
            return TSet.TryParse(text[..Decode(utf8Text, text)], provider: null, out result);
        }
        finally
        {
            if (rented is not null)
                ArrayPool<char>.Shared.Return(rented);
        }
    }

    /// <summary>
    /// Decodes UTF-8 text, replacing each invalid sequence with U+FFFD.
    /// </summary>
    /// <param name="utf8Text">The UTF-8 text.</param>
    /// <param name="text">
    /// The buffer to decode into, at least as many characters long as <paramref name="utf8Text" /> has bytes, which
    /// UTF-8 never exceeds.
    /// </param>
    /// <returns>The number of characters decoded.</returns>
    private static int Decode(ReadOnlySpan<byte> utf8Text, Span<char> text) =>
        Encoding.UTF8.GetChars(utf8Text, text);

    /// <summary>
    /// Attempts to read a format string that names one of the forms every numeric set shares.
    /// </summary>
    /// <param name="format">The format string, in either case.</param>
    /// <param name="form">When this method returns <see langword="true" />, the form it names.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="format" /> is <c>G</c>, <c>L</c>, <c>B</c>, <c>0</c>, <c>1</c> or
    /// <c>01</c>; otherwise <see langword="false" />.
    /// </returns>
    internal static bool TryParseNumericFormat(ReadOnlySpan<char> format, out NumericForm form)
    {
        form = NumericForm.List;
        if (format.Length == 2)
        {
            if (format[0] != '0' || format[1] != '1')
                return false;

            form = NumericForm.Binary;
            return true;
        }

        if (format.Length != 1)
            return false;

        switch (char.ToUpperInvariant(format[0]))
        {
            case 'G':
                form = NumericForm.List;
                return true;

            case 'L':
                form = NumericForm.Values;
                return true;

            case 'B' or '0' or '1':
                form = NumericForm.Binary;
                return true;

            default:
                return false;
        }
    }

    /// <summary>
    /// Attempts to read a numeric set's text in one form, without detecting it.
    /// </summary>
    /// <param name="text">The text to read.</param>
    /// <param name="minimum">The smallest value in the domain.</param>
    /// <param name="maximum">The largest value in the domain.</param>
    /// <param name="form">The form of the text.</param>
    /// <param name="bits">When this method returns <see cref="ParseFailure.None" />, the bits the text selects.</param>
    /// <param name="position">
    /// When this method returns <see cref="ParseFailure.Character" />, the index of the first character that does not
    /// fit the form.
    /// </param>
    /// <returns>Why the text is not in the form, or <see cref="ParseFailure.None" /> when it is.</returns>
    internal static ParseFailure TryParseExact(
        ReadOnlySpan<char> text,
        int minimum,
        int maximum,
        NumericForm form,
        out ulong bits,
        out int position)
    {
        if (form == NumericForm.Binary)
            return TryParseBinary(text, maximum - minimum + 1, out bits, out position);

        position = 0;
        return TryParse(text, minimum, maximum, allowRanges: form == NumericForm.List, out bits)
            ? ParseFailure.None
            : ParseFailure.List;
    }

    /// <summary>
    /// Attempts to read a numeric set's text, detecting its form: the binary form when the text, as given, has one
    /// <c>0</c> or <c>1</c> per value of the domain, and otherwise a list of values and ranges.
    /// </summary>
    /// <param name="text">The text to read.</param>
    /// <param name="minimum">The smallest value in the domain.</param>
    /// <param name="maximum">The largest value in the domain.</param>
    /// <param name="bits">When this method returns <see cref="ParseFailure.None" />, the bits the text selects.</param>
    /// <returns>
    /// <see cref="ParseFailure.None" /> when the text is in either form; otherwise <see cref="ParseFailure.List" />,
    /// since a text that is not the binary form is read as a list.
    /// </returns>
    internal static ParseFailure TryParseDetected(ReadOnlySpan<char> text, int minimum, int maximum, out ulong bits) =>
        TryParseDetected(text, minimum, maximum, letters: null, out bits);

    /// <summary>
    /// Attempts to read the text of a numeric set whose values may also have letters, detecting its form: the binary
    /// form, then the letter mask, then a list of values and ranges.
    /// </summary>
    /// <param name="text">The text to read.</param>
    /// <param name="minimum">The smallest value in the domain.</param>
    /// <param name="maximum">The largest value in the domain.</param>
    /// <param name="letters">
    /// The upper-case letter of each value, in bit order, or <see langword="null" /> when the set has no letter mask.
    /// </param>
    /// <param name="bits">When this method returns <see cref="ParseFailure.None" />, the bits the text selects.</param>
    /// <returns>
    /// <see cref="ParseFailure.None" /> when the text is in any of the forms; otherwise
    /// <see cref="ParseFailure.List" />, since a text that is neither the binary form nor a letter mask is read as a
    /// list.
    /// </returns>
    /// <remarks>
    /// The binary form and the letter mask are read as given, without trimming. The letter mask starts at the first
    /// value and may use any one placeholder throughout.
    /// </remarks>
    internal static ParseFailure TryParseDetected(ReadOnlySpan<char> text, int minimum, int maximum, string? letters, out ulong bits)
    {
        int width = maximum - minimum + 1;
        if (IsBinary(text, width))
            return TryParseBinary(text, width, out bits, out _);

        if (letters is not null && TryParseLetters(text, letters, first: 0, placeholder: null, out bits, out _) == ParseFailure.None)
            return ParseFailure.None;

        return TryParse(text, minimum, maximum, out bits) ? ParseFailure.None : ParseFailure.List;
    }

    /// <summary>
    /// Attempts to read the letter that names a letter mask's placeholder in a format string.
    /// </summary>
    /// <param name="letter">The upper-case letter: <c>E</c>, <c>U</c>, <c>D</c> or <c>A</c>.</param>
    /// <param name="placeholder">
    /// When this method returns <see langword="true" />, the placeholder: a space, <c>_</c>, <c>-</c> or <c>*</c>.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="letter" /> names a placeholder; otherwise <see langword="false" />.
    /// </returns>
    internal static bool TryParsePlaceholder(char letter, out char placeholder)
    {
        placeholder = letter switch
        {
            'E' => ' ',
            'U' => '_',
            'D' => '-',
            'A' => '*',
            _ => '\0',
        };

        return placeholder != '\0';
    }

    /// <summary>
    /// Creates the exception that describes why a text is not a set.
    /// </summary>
    /// <param name="failure">Why the text is not a set; not <see cref="ParseFailure.None" />.</param>
    /// <param name="text">The text.</param>
    /// <param name="position">
    /// For <see cref="ParseFailure.Character" />, the index of the character that does not fit.
    /// </param>
    /// <param name="minimum">The smallest value in the domain.</param>
    /// <param name="maximum">The largest value in the domain.</param>
    /// <returns>The exception to throw.</returns>
    internal static FormatException CreateParseException(ParseFailure failure, ReadOnlySpan<char> text, int position, int minimum, int maximum) =>
        failure switch
        {
            ParseFailure.Length => new FormatException(
                string.Format(CultureInfo.CurrentCulture, ResourceStrings.Format_Invalid_StringLength, maximum - minimum + 1)),
            ParseFailure.Character => new FormatException(
                string.Format(CultureInfo.CurrentCulture, ResourceStrings.Format_Invalid_Character, text[position], position + 1)),
            _ => new FormatException(
                string.Format(CultureInfo.CurrentCulture, ResourceStrings.Format_Invalid_CalendarValueList, text.ToString(), minimum, maximum)),
        };

    /// <summary>
    /// Creates the exception that reports a format string a set does not support.
    /// </summary>
    /// <param name="format">The format string.</param>
    /// <param name="typeName">The name of the set's type.</param>
    /// <returns>The exception to throw.</returns>
    internal static FormatException CreateFormatStringException(string format, string typeName) =>
        new(string.Format(CultureInfo.CurrentCulture, ResourceStrings.Format_Invalid_CalendarValueSetFormat, format, typeName));

    /// <summary>
    /// Writes a set's bits in the binary form: one character per value of the domain, lowest first, <c>1</c> for a
    /// selected value and <c>0</c> otherwise.
    /// </summary>
    /// <param name="bits">The set's bits.</param>
    /// <param name="width">The number of values in the domain.</param>
    /// <returns>The binary form, <paramref name="width" /> characters long.</returns>
    internal static string FormatBinary(ulong bits, int width) =>
        string.Create(width, bits, static (span, bits) => WriteBinary(bits, span.Length, span));

    /// <summary>
    /// Writes a set's bits in the binary form: one character per value of the domain, lowest first, <c>1</c> for a
    /// selected value and <c>0</c> otherwise.
    /// </summary>
    /// <param name="bits">The set's bits.</param>
    /// <param name="width">The number of values in the domain.</param>
    /// <param name="buffer">The buffer to write to, at least <paramref name="width" /> characters long.</param>
    /// <returns>The number of characters written, <paramref name="width" />.</returns>
    internal static int WriteBinary(ulong bits, int width, Span<char> buffer)
    {
        for (int i = 0; i < width; i++)
            buffer[i] = ((bits >> i) & 1) != 0 ? '1' : '0';

        return width;
    }

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
        string.Create(
            letters.Length,
            (Bits: bits, Letters: letters, First: first, Placeholder: placeholder),
            static (span, state) => WriteLetters(state.Bits, state.Letters, state.First, state.Placeholder, span));

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
    /// <param name="buffer">The buffer to write to, at least as long as <paramref name="letters" />.</param>
    /// <returns>The number of characters written, one per letter.</returns>
    internal static int WriteLetters(ulong bits, ReadOnlySpan<char> letters, int first, char placeholder, Span<char> buffer)
    {
        for (int i = 0; i < letters.Length; i++)
        {
            int index = (first + i) % letters.Length;
            buffer[i] = ((bits >> index) & 1) != 0 ? letters[index] : placeholder;
        }

        return letters.Length;
    }

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
        Span<char> buffer = stackalloc char[MaxTextLength];
        return new string(buffer[..WriteList(bits, minimum, ranges: true, buffer)]);
    }

    /// <summary>
    /// Writes the values a set's bits select as a comma-separated list in ascending order.
    /// </summary>
    /// <param name="bits">The set's bits.</param>
    /// <param name="minimum">
    /// The value bit zero selects, from 0 to 936, so that no value has more than three digits.
    /// </param>
    /// <param name="ranges">
    /// Whether each run of two or more consecutive values is written as an inclusive range, rather than every value.
    /// </param>
    /// <param name="buffer">The buffer to write to, at least <see cref="MaxTextLength" /> characters long.</param>
    /// <returns>The number of characters written, 0 when no bit is set.</returns>
    internal static int WriteList(ulong bits, int minimum, bool ranges, Span<char> buffer)
    {
        Debug.Assert(minimum is >= 0 and <= 999 - 63, "Every value fits in three digits.");

        int length = 0;
        while (bits != 0)
        {
            int start = BitOperations.TrailingZeroCount(bits);
            int run = ranges ? BitOperations.TrailingZeroCount(~(bits >> start)) : 1;
            if (length > 0)
                buffer[length++] = ',';

            length += WriteValue(minimum + start, buffer[length..]);
            if (run > 1)
            {
                buffer[length++] = '-';
                length += WriteValue(minimum + start + run - 1, buffer[length..]);
            }

            // Clear the values just written; a run that reaches bit 63 leaves nothing above it.
            bits = start + run >= 64 ? 0 : bits & ~((1UL << (start + run)) - 1);
        }

        return length;
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
    internal static bool TryParse(ReadOnlySpan<char> text, int minimum, int maximum, out ulong bits) =>
        TryParse(text, minimum, maximum, allowRanges: true, out bits);

    /// <summary>
    /// Attempts to read a comma-separated list of values and, when allowed, inclusive ranges.
    /// </summary>
    /// <param name="text">The text to read.</param>
    /// <param name="minimum">The smallest value in the domain.</param>
    /// <param name="maximum">The largest value in the domain.</param>
    /// <param name="allowRanges">Whether an item may be an inclusive range <c>a-b</c>.</param>
    /// <param name="bits">
    /// When this method returns <see langword="true" />, the bits that select the values read.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="text" /> is a valid list; otherwise <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// The text is read as <see cref="TryParse(ReadOnlySpan{char}, int, int, out ulong)" /> reads it, except that a
    /// range is not a valid item when <paramref name="allowRanges" /> is <see langword="false" />.
    /// </remarks>
    internal static bool TryParse(ReadOnlySpan<char> text, int minimum, int maximum, bool allowRanges, out ulong bits)
    {
        bits = 0;
        text = text.Trim();
        if (text.IsEmpty)
            return true;

        while (true)
        {
            int comma = text.IndexOf(',');
            ReadOnlySpan<char> item = comma < 0 ? text : text[..comma];
            if (!TryParseItem(item, minimum, maximum, allowRanges, ref bits))
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
    /// <param name="allowRanges">Whether the item may be an inclusive range.</param>
    /// <param name="bits">The bits to add the item's values to.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="item" /> is a valid value or range; otherwise
    /// <see langword="false" />.
    /// </returns>
    private static bool TryParseItem(ReadOnlySpan<char> item, int minimum, int maximum, bool allowRanges, ref ulong bits)
    {
        int dash = item.IndexOf('-');
        if (dash >= 0 && !allowRanges)
            return false;

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
    /// Writes one value as an unsigned decimal integer.
    /// </summary>
    /// <param name="value">The value, from 0 to 999.</param>
    /// <param name="buffer">The buffer to write to, at least three characters long.</param>
    /// <returns>The number of characters written.</returns>
    private static int WriteValue(int value, Span<char> buffer)
    {
        bool written = value.TryFormat(buffer, out int length, provider: CultureInfo.InvariantCulture);
        Debug.Assert(written, "The buffer holds every value.");
        return length;
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
