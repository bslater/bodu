// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DayOfWeekSet.Parse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Bodu;

public readonly partial struct DayOfWeekSet
{
    /// <summary>
    /// Converts a seven-character mask into a <see cref="DayOfWeekSet" />, detecting the mask's form.
    /// </summary>
    /// <param name="s">The text to convert, such as <c>"_MTWTF_"</c> or <c>"0111110"</c>.</param>
    /// <returns>The set the text describes.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="s" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="FormatException">Thrown when <paramref name="s" /> is not a seven-character mask.</exception>
    /// <remarks>
    /// <para>
    /// The text has one character per day. A day's letter, in either case, selects the day, and <c>_</c>, <c>-</c>,
    /// <c>*</c> or a space leaves it out; one placeholder is used throughout. The letters show whether the mask starts
    /// on Sunday or on Monday, the first letter that fits only one of the two orders deciding. A mask that begins with
    /// <c>0</c> or <c>1</c> is the binary form, Sunday first, with <c>1</c> for a selected day.
    /// </para>
    /// <para>
    /// One mask fits both orders: <c>"______S"</c>, whose only letter is a final <c>S</c>, is Saturday Sunday first and
    /// Sunday Monday first. It is read Sunday first, as <see cref="ToString()" /> writes it; read Monday-first text
    /// with <see cref="ParseExact(string, string)" /> and the <c>"M"</c> format.
    /// </para>
    /// <para>
    /// Anything else, an abbreviated list of days such as <c>"MF"</c> included, is not a mask.
    /// </para>
    /// </remarks>
    public static DayOfWeekSet Parse(string s)
    {
        ThrowHelper.ThrowIfNull(s);

        return ParseCore(s, format: null);
    }

    /// <summary>
    /// Converts a seven-character mask in the specified format into a <see cref="DayOfWeekSet" />.
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
    /// Thrown when <paramref name="format" /> is not a supported format, or <paramref name="s" /> is not a mask in that
    /// format.
    /// </exception>
    /// <remarks>
    /// The format fixes the order of the days, and the placeholder for a day not selected when it names one: with
    /// <c>"S"</c> or <c>"M"</c> the placeholder is any of <c>_</c>, <c>-</c>, <c>*</c> or a space, used throughout. The
    /// letters of the days and of the format are read in either case.
    /// </remarks>
    public static DayOfWeekSet ParseExact(string s, string format)
    {
        ThrowHelper.ThrowIfNull(s);
        ThrowHelper.ThrowIfNull(format);
        if (!TryParseFormat(format, out TextFormat textFormat)) throw CalendarValueSet.CreateFormatStringException(format, nameof(DayOfWeekSet));

        return ParseCore(s, textFormat);
    }

    /// <summary>
    /// Attempts to convert a seven-character mask into a <see cref="DayOfWeekSet" />, detecting the mask's form.
    /// </summary>
    /// <param name="s">The text to convert.</param>
    /// <param name="result">
    /// When this method returns <see langword="true" />, the set the text describes; otherwise <see cref="Empty" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="s" /> is a seven-character mask; otherwise <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// The text is read as <see cref="Parse(string)" /> reads it.
    /// </remarks>
    public static bool TryParse([NotNullWhen(true)] string? s, out DayOfWeekSet result)
    {
        if (s is not null && TryParseCore(s, format: null, out ulong bits, out _) == CalendarValueSet.ParseFailure.None)
        {
            result = new DayOfWeekSet(bits);
            return true;
        }

        result = default;
        return false;
    }

    /// <summary>
    /// Attempts to convert a seven-character mask in the specified format into a <see cref="DayOfWeekSet" />.
    /// </summary>
    /// <param name="s">The text to convert.</param>
    /// <param name="format">
    /// The format of <paramref name="s" />, one of those <see cref="ToString(string)" /> writes.
    /// </param>
    /// <param name="result">
    /// When this method returns <see langword="true" />, the set the text describes; otherwise <see cref="Empty" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="format" /> is a supported format and <paramref name="s" /> is a
    /// mask in it; otherwise <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// The text is read as <see cref="ParseExact(string, string)" /> reads it.
    /// </remarks>
    public static bool TryParseExact([NotNullWhen(true)] string? s, [NotNullWhen(true)] string? format, out DayOfWeekSet result)
    {
        if (s is not null
            && format is not null
            && TryParseFormat(format, out TextFormat textFormat)
            && TryParseCore(s, textFormat, out ulong bits, out _) == CalendarValueSet.ParseFailure.None)
        {
            result = new DayOfWeekSet(bits);
            return true;
        }

        result = default;
        return false;
    }

    /// <inheritdoc />
    static DayOfWeekSet IParsable<DayOfWeekSet>.Parse(string s, IFormatProvider? provider) =>
        Parse(s);

    /// <inheritdoc />
    static bool IParsable<DayOfWeekSet>.TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out DayOfWeekSet result) =>
        TryParse(s, out result);

    /// <summary>
    /// Converts a mask into a set, or throws <see cref="FormatException" /> describing why it is not one.
    /// </summary>
    /// <param name="s">The text to convert.</param>
    /// <param name="format">The format of the text, or <see langword="null" /> to detect it.</param>
    /// <returns>The set the text describes.</returns>
    /// <exception cref="FormatException">
    /// Thrown when <paramref name="s" /> is not a seven-character mask in <paramref name="format" />.
    /// </exception>
    private static DayOfWeekSet ParseCore(string s, TextFormat? format)
    {
        CalendarValueSet.ParseFailure failure = TryParseCore(s, format, out ulong bits, out int position);

        return failure == CalendarValueSet.ParseFailure.None
            ? new DayOfWeekSet(bits)
            : throw CalendarValueSet.CreateParseException(failure, s, position, (int)DayOfWeek.Sunday, (int)DayOfWeek.Saturday);
    }

    /// <summary>
    /// Attempts to read a seven-character mask.
    /// </summary>
    /// <param name="text">The text to read.</param>
    /// <param name="format">The format of the text, or <see langword="null" /> to detect it.</param>
    /// <param name="bits">
    /// When this method returns <see cref="CalendarValueSet.ParseFailure.None" />, the bits the mask selects.
    /// </param>
    /// <param name="position">
    /// When this method returns <see cref="CalendarValueSet.ParseFailure.Character" />, the index of the first
    /// character that does not fit the mask.
    /// </param>
    /// <returns>
    /// Why the text is not a mask, or <see cref="CalendarValueSet.ParseFailure.None" /> when it is one.
    /// </returns>
    /// <remarks>
    /// Without a format, a mask that begins with <c>0</c> or <c>1</c> is binary, and the letters of any other decide
    /// whether it starts on Sunday or on Monday: the first letter that fits only one of the two orders, testing the
    /// Sunday-first letter first. Only the final position has the same letter in both orders, <c>S</c>, so a mask whose
    /// only letter is a final <c>S</c> is read Sunday first.
    /// </remarks>
    private static CalendarValueSet.ParseFailure TryParseCore(ReadOnlySpan<char> text, TextFormat? format, out ulong bits, out int position)
    {
        if (text.Length != MaskLength)
        {
            bits = 0;
            position = 0;
            return CalendarValueSet.ParseFailure.Length;
        }

        bool binary = format is TextFormat known ? known.Binary : (text[0] is '0' or '1');
        if (binary)
            return CalendarValueSet.TryParseBinary(text, MaskLength, out bits, out position);

        int? first = format is TextFormat named ? (named.MondayFirst ? 1 : 0) : null;
        return CalendarValueSet.TryParseLetters(text, DayLetters, first, format?.Placeholder, out bits, out position);
    }
}
