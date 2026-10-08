// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MonthSet.Parse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Bodu;

public readonly partial struct MonthSet
{
    /// <summary>
    /// Converts text in one of the forms <see cref="ToString(string, IFormatProvider)" /> writes into a
    /// <see cref="MonthSet" />, detecting the form.
    /// </summary>
    /// <param name="s">The text to convert, such as <c>"1,4,7,10"</c>.</param>
    /// <returns>The set the text describes.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="s" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="FormatException">
    /// Thrown when <paramref name="s" /> is not the binary form, the letter mask, or a list of values and ranges from 1
    /// to 12.
    /// </exception>
    /// <remarks>
    /// <para>
    /// A text of exactly 12 characters, each <c>0</c> or <c>1</c>, is the binary form, month 1 first. A text of 12
    /// characters, each a month's initial in its place or a placeholder, is the letter mask, January first, such as
    /// <c>"JFM________D"</c>: the initials are read in either case, and the placeholder may be <c>_</c>, <c>-</c>,
    /// <c>*</c> or a space, the same one throughout. Any other text is a comma-separated list of months and inclusive
    /// ranges: whitespace around the list and around each value is ignored, an empty or blank string is the empty set,
    /// values are unsigned decimal integers, a range <c>a-b</c> needs <c>a</c> no greater than <c>b</c>, and values may
    /// repeat or overlap.
    /// </para>
    /// <para>
    /// The forms are tested in that order, and the binary form and the letter mask read the text as given, so text made
    /// only of <c>0</c> and <c>1</c> is a list only when it is not 12 characters long, and a mask with whitespace
    /// around it is not a mask. <see cref="ParseExact(string, string)" /> with the <c>"G"</c> format reads any text as
    /// a list.
    /// </para>
    /// </remarks>
    public static MonthSet Parse(string s)
    {
        ThrowHelper.ThrowIfNull(s);

        return Parse(s.AsSpan());
    }

    /// <summary>
    /// Converts a span of characters in one of the forms <see cref="ToString(string, IFormatProvider)" /> writes into a
    /// <see cref="MonthSet" />, detecting the form.
    /// </summary>
    /// <param name="s">The characters to convert.</param>
    /// <returns>The set the characters describe.</returns>
    /// <exception cref="FormatException">
    /// Thrown when <paramref name="s" /> is neither the binary form, the letter mask, nor a list of values and ranges
    /// from 1 to 12.
    /// </exception>
    /// <remarks>
    /// The characters are read as <see cref="Parse(string)" /> reads a string.
    /// </remarks>
    public static MonthSet Parse(ReadOnlySpan<char> s)
    {
        CalendarValueSet.ParseFailure failure = CalendarValueSet.TryParseDetected(s, MinimumValue, MaximumValue, MonthLetters, out ulong bits);
        return failure == CalendarValueSet.ParseFailure.None
            ? new MonthSet(bits)
            : throw CalendarValueSet.CreateParseException(failure, s, position: 0, MinimumValue, MaximumValue);
    }

    /// <summary>
    /// Converts text in the specified format into a <see cref="MonthSet" />.
    /// </summary>
    /// <param name="s">The text to convert.</param>
    /// <param name="format">
    /// The format of <paramref name="s" />, one of those <see cref="ToString(string)" /> writes.
    /// </param>
    /// <returns>The set the text describes.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="s" /> or <paramref name="format" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="FormatException">
    /// Thrown when <paramref name="format" /> is not a supported format, or <paramref name="s" /> is not text in that
    /// format.
    /// </exception>
    /// <remarks>
    /// <c>"G"</c> reads a list of values and ranges as <see cref="Parse(string)" /> reads one, <c>"L"</c> a list of
    /// values without ranges, and <c>"B"</c>, <c>"0"</c>, <c>"1"</c> or <c>"01"</c> the binary form alone, 12
    /// characters with no whitespace. <c>"J"</c> reads the letter mask with any one placeholder throughout, and
    /// <c>"E"</c>, <c>"U"</c>, <c>"D"</c> or <c>"A"</c>, alone or after <c>J</c>, reads it with that placeholder alone.
    /// The format letters and the mask's initials are read in either case.
    /// </remarks>
    public static MonthSet ParseExact(string s, string format)
    {
        ThrowHelper.ThrowIfNull(s);
        ThrowHelper.ThrowIfNull(format);

        return ParseExact(s.AsSpan(), format.AsSpan());
    }

    /// <summary>
    /// Converts a span of characters in the specified format into a <see cref="MonthSet" />.
    /// </summary>
    /// <param name="s">The characters to convert.</param>
    /// <param name="format">
    /// The format of <paramref name="s" />, one of those <see cref="ToString(string)" /> writes.
    /// </param>
    /// <returns>The set the characters describe.</returns>
    /// <exception cref="FormatException">
    /// Thrown when <paramref name="format" /> is not a supported format, or <paramref name="s" /> is not text in that
    /// format.
    /// </exception>
    /// <remarks>
    /// The characters are read as <see cref="ParseExact(string, string)" /> reads a string.
    /// </remarks>
    public static MonthSet ParseExact(ReadOnlySpan<char> s, ReadOnlySpan<char> format)
    {
        if (!TryParseFormat(format, out TextFormat textFormat)) throw CalendarValueSet.CreateFormatStringException(format.ToString(), nameof(MonthSet));

        CalendarValueSet.ParseFailure failure = TryParseCore(s, textFormat, out ulong bits, out int position);
        return failure == CalendarValueSet.ParseFailure.None
            ? new MonthSet(bits)
            : throw CalendarValueSet.CreateParseException(failure, s, position, MinimumValue, MaximumValue);
    }

    /// <summary>
    /// Attempts to convert text in one of the forms <see cref="ToString(string, IFormatProvider)" /> writes into a
    /// <see cref="MonthSet" />, detecting the form.
    /// </summary>
    /// <param name="s">The text to convert, such as <c>"1,4,7,10"</c>.</param>
    /// <param name="result">
    /// When this method returns <see langword="true" />, the set the text describes; otherwise <see cref="Empty" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="s" /> is the binary form, the letter mask, or a list of values and
    /// ranges from 1 to 12; otherwise <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// The text is read as <see cref="Parse(string)" /> reads it.
    /// </remarks>
    public static bool TryParse([NotNullWhen(true)] string? s, out MonthSet result)
    {
        if (s is not null)
            return TryParse(s.AsSpan(), out result);

        result = default;
        return false;
    }

    /// <summary>
    /// Attempts to convert a span of characters in one of the forms <see cref="ToString(string, IFormatProvider)" />
    /// writes into a <see cref="MonthSet" />, detecting the form.
    /// </summary>
    /// <param name="s">The characters to convert.</param>
    /// <param name="result">
    /// When this method returns <see langword="true" />, the set the characters describe; otherwise
    /// <see cref="Empty" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="s" /> is the binary form, the letter mask, or a list of values and
    /// ranges from 1 to 12; otherwise <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// The characters are read as <see cref="Parse(string)" /> reads a string.
    /// </remarks>
    public static bool TryParse(ReadOnlySpan<char> s, out MonthSet result)
    {
        if (CalendarValueSet.TryParseDetected(s, MinimumValue, MaximumValue, MonthLetters, out ulong bits) == CalendarValueSet.ParseFailure.None)
        {
            result = new MonthSet(bits);
            return true;
        }

        result = default;
        return false;
    }

    /// <summary>
    /// Attempts to convert text in the specified format into a <see cref="MonthSet" />.
    /// </summary>
    /// <param name="s">The text to convert.</param>
    /// <param name="format">
    /// The format of <paramref name="s" />, one of those <see cref="ToString(string)" /> writes.
    /// </param>
    /// <param name="result">
    /// When this method returns <see langword="true" />, the set the text describes; otherwise <see cref="Empty" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="format" /> is a supported format and <paramref name="s" /> is text
    /// in it; otherwise <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// The text is read as <see cref="ParseExact(string, string)" /> reads it.
    /// </remarks>
    public static bool TryParseExact([NotNullWhen(true)] string? s, [NotNullWhen(true)] string? format, out MonthSet result)
    {
        if (s is not null && format is not null)
            return TryParseExact(s.AsSpan(), format.AsSpan(), out result);

        result = default;
        return false;
    }

    /// <summary>
    /// Attempts to convert a span of characters in the specified format into a <see cref="MonthSet" />.
    /// </summary>
    /// <param name="s">The characters to convert.</param>
    /// <param name="format">
    /// The format of <paramref name="s" />, one of those <see cref="ToString(string)" /> writes.
    /// </param>
    /// <param name="result">
    /// When this method returns <see langword="true" />, the set the characters describe; otherwise
    /// <see cref="Empty" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="format" /> is a supported format and <paramref name="s" /> is text
    /// in it; otherwise <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// The characters are read as <see cref="ParseExact(string, string)" /> reads a string.
    /// </remarks>
    public static bool TryParseExact(ReadOnlySpan<char> s, ReadOnlySpan<char> format, out MonthSet result)
    {
        if (TryParseFormat(format, out TextFormat textFormat)
            && TryParseCore(s, textFormat, out ulong bits, out _) == CalendarValueSet.ParseFailure.None)
        {
            result = new MonthSet(bits);
            return true;
        }

        result = default;
        return false;
    }

    /// <inheritdoc />
    static MonthSet IParsable<MonthSet>.Parse(string s, IFormatProvider? provider) =>
        Parse(s);

    /// <inheritdoc />
    static bool IParsable<MonthSet>.TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out MonthSet result) =>
        TryParse(s, out result);

    /// <inheritdoc />
    static MonthSet ISpanParsable<MonthSet>.Parse(ReadOnlySpan<char> s, IFormatProvider? provider) =>
        Parse(s);

    /// <inheritdoc />
    static bool ISpanParsable<MonthSet>.TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out MonthSet result) =>
        TryParse(s, out result);

    /// <summary>
    /// Attempts to read text in one form, without detecting it.
    /// </summary>
    /// <param name="text">The text to read.</param>
    /// <param name="format">The form of the text.</param>
    /// <param name="bits">
    /// When this method returns <see cref="CalendarValueSet.ParseFailure.None" />, the bits the text selects.
    /// </param>
    /// <param name="position">
    /// When this method returns <see cref="CalendarValueSet.ParseFailure.Character" />, the index of the first
    /// character that does not fit the form.
    /// </param>
    /// <returns>
    /// Why the text is not in the form, or <see cref="CalendarValueSet.ParseFailure.None" /> when it is.
    /// </returns>
    private static CalendarValueSet.ParseFailure TryParseCore(ReadOnlySpan<char> text, TextFormat format, out ulong bits, out int position) =>
        format.LetterMask
            ? CalendarValueSet.TryParseLetters(text, MonthLetters, first: 0, format.Placeholder, out bits, out position)
            : CalendarValueSet.TryParseExact(text, MinimumValue, MaximumValue, format.Form, out bits, out position);
}
