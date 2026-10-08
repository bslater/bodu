// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SecondSet.Parse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Bodu;

public readonly partial struct SecondSet
{
    /// <summary>
    /// Converts text in one of the forms <see cref="ToString(string, IFormatProvider)" /> writes into a
    /// <see cref="SecondSet" />, detecting the form.
    /// </summary>
    /// <param name="s">The text to convert, such as <c>"0,30"</c>.</param>
    /// <returns>The set the text describes.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="s" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="FormatException">
    /// Thrown when <paramref name="s" /> is neither the binary form nor a list of values and ranges from 0 to 59.
    /// </exception>
    /// <remarks>
    /// <para>
    /// A text of exactly 60 characters, each <c>0</c> or <c>1</c>, is the binary form, second 0 first. Any other text
    /// is a comma-separated list of seconds and inclusive ranges: whitespace around the list and around each value is
    /// ignored, an empty or blank string is the empty set, values are unsigned decimal integers, a range <c>a-b</c>
    /// needs <c>a</c> no greater than <c>b</c>, and values may repeat or overlap.
    /// </para>
    /// <para>
    /// The binary test comes first and reads the text as given, so text made only of <c>0</c> and <c>1</c> is a list
    /// only when it is not 60 characters long. <see cref="ParseExact(string, string)" /> with the <c>"G"</c> format
    /// reads any text as a list.
    /// </para>
    /// </remarks>
    public static SecondSet Parse(string s)
    {
        ThrowHelper.ThrowIfNull(s);

        return Parse(s.AsSpan());
    }

    /// <summary>
    /// Converts a span of characters in one of the forms <see cref="ToString(string, IFormatProvider)" /> writes into a
    /// <see cref="SecondSet" />, detecting the form.
    /// </summary>
    /// <param name="s">The characters to convert.</param>
    /// <returns>The set the characters describe.</returns>
    /// <exception cref="FormatException">
    /// Thrown when <paramref name="s" /> is neither the binary form nor a list of values and ranges from 0 to 59.
    /// </exception>
    /// <remarks>
    /// The characters are read as <see cref="Parse(string)" /> reads a string.
    /// </remarks>
    public static SecondSet Parse(ReadOnlySpan<char> s)
    {
        CalendarValueSet.ParseFailure failure = CalendarValueSet.TryParseDetected(s, MinimumValue, MaximumValue, out ulong bits);
        return failure == CalendarValueSet.ParseFailure.None
            ? new SecondSet(bits)
            : throw CalendarValueSet.CreateParseException(failure, s, position: 0, MinimumValue, MaximumValue);
    }

    /// <summary>
    /// Converts text in the specified format into a <see cref="SecondSet" />.
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
    /// values without ranges, and <c>"B"</c>, <c>"0"</c>, <c>"1"</c> or <c>"01"</c> the binary form alone, 60
    /// characters with no whitespace. The format letters are read in either case.
    /// </remarks>
    public static SecondSet ParseExact(string s, string format)
    {
        ThrowHelper.ThrowIfNull(s);
        ThrowHelper.ThrowIfNull(format);

        return ParseExact(s.AsSpan(), format.AsSpan());
    }

    /// <summary>
    /// Converts a span of characters in the specified format into a <see cref="SecondSet" />.
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
    public static SecondSet ParseExact(ReadOnlySpan<char> s, ReadOnlySpan<char> format)
    {
        if (!CalendarValueSet.TryParseNumericFormat(format, out CalendarValueSet.NumericForm form)) throw CalendarValueSet.CreateFormatStringException(format.ToString(), nameof(SecondSet));

        CalendarValueSet.ParseFailure failure = CalendarValueSet.TryParseExact(s, MinimumValue, MaximumValue, form, out ulong bits, out int position);
        return failure == CalendarValueSet.ParseFailure.None
            ? new SecondSet(bits)
            : throw CalendarValueSet.CreateParseException(failure, s, position, MinimumValue, MaximumValue);
    }

    /// <summary>
    /// Attempts to convert text in one of the forms <see cref="ToString(string, IFormatProvider)" /> writes into a
    /// <see cref="SecondSet" />, detecting the form.
    /// </summary>
    /// <param name="s">The text to convert, such as <c>"0,30"</c>.</param>
    /// <param name="result">
    /// When this method returns <see langword="true" />, the set the text describes; otherwise <see cref="Empty" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="s" /> is the binary form or a list of values and ranges from 0 to
    /// 59; otherwise <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// The text is read as <see cref="Parse(string)" /> reads it.
    /// </remarks>
    public static bool TryParse([NotNullWhen(true)] string? s, out SecondSet result)
    {
        if (s is not null)
            return TryParse(s.AsSpan(), out result);

        result = default;
        return false;
    }

    /// <summary>
    /// Attempts to convert a span of characters in one of the forms <see cref="ToString(string, IFormatProvider)" />
    /// writes into a <see cref="SecondSet" />, detecting the form.
    /// </summary>
    /// <param name="s">The characters to convert.</param>
    /// <param name="result">
    /// When this method returns <see langword="true" />, the set the characters describe; otherwise
    /// <see cref="Empty" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="s" /> is the binary form or a list of values and ranges from 0 to
    /// 59; otherwise <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// The characters are read as <see cref="Parse(string)" /> reads a string.
    /// </remarks>
    public static bool TryParse(ReadOnlySpan<char> s, out SecondSet result)
    {
        if (CalendarValueSet.TryParseDetected(s, MinimumValue, MaximumValue, out ulong bits) == CalendarValueSet.ParseFailure.None)
        {
            result = new SecondSet(bits);
            return true;
        }

        result = default;
        return false;
    }

    /// <summary>
    /// Attempts to convert text in the specified format into a <see cref="SecondSet" />.
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
    public static bool TryParseExact([NotNullWhen(true)] string? s, [NotNullWhen(true)] string? format, out SecondSet result)
    {
        if (s is not null && format is not null)
            return TryParseExact(s.AsSpan(), format.AsSpan(), out result);

        result = default;
        return false;
    }

    /// <summary>
    /// Attempts to convert a span of characters in the specified format into a <see cref="SecondSet" />.
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
    public static bool TryParseExact(ReadOnlySpan<char> s, ReadOnlySpan<char> format, out SecondSet result)
    {
        if (CalendarValueSet.TryParseNumericFormat(format, out CalendarValueSet.NumericForm form)
            && CalendarValueSet.TryParseExact(s, MinimumValue, MaximumValue, form, out ulong bits, out _) == CalendarValueSet.ParseFailure.None)
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

    /// <inheritdoc />
    static SecondSet ISpanParsable<SecondSet>.Parse(ReadOnlySpan<char> s, IFormatProvider? provider) =>
        Parse(s);

    /// <inheritdoc />
    static bool ISpanParsable<SecondSet>.TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out SecondSet result) =>
        TryParse(s, out result);
}
