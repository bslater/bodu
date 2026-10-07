// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MonthSet.Formatting.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public readonly partial struct MonthSet
{
    /// <summary>The letter of each month, January first.</summary>
    private const string MonthLetters = "JFMAMJJASOND";

    /// <summary>
    /// Returns the canonical text form of the set: the selected months in ascending order, separated by commas, each
    /// run of two or more consecutive months written as an inclusive range.
    /// </summary>
    /// <returns>The text form, such as <c>"1-3,12"</c>, or the empty string for the empty set.</returns>
    public override string ToString() =>
        CalendarValueSet.Format(_bits, MinimumValue);

    /// <summary>
    /// Returns the set as text in the specified format.
    /// </summary>
    /// <param name="format">The format, or <see langword="null" /> or empty for the default.</param>
    /// <returns>The text.</returns>
    /// <exception cref="FormatException">Thrown when <paramref name="format" /> is not a supported format.</exception>
    /// <remarks>
    /// The formats are described at <see cref="ToString(string, IFormatProvider)" />.
    /// </remarks>
    public string ToString(string? format) =>
        ToString(format, formatProvider: null);

    /// <summary>
    /// Returns the set as text in the specified format.
    /// </summary>
    /// <param name="format">The format, or <see langword="null" /> or empty for the default.</param>
    /// <param name="formatProvider">Ignored; the text does not depend on culture.</param>
    /// <returns>The text.</returns>
    /// <exception cref="FormatException">Thrown when <paramref name="format" /> is not a supported format.</exception>
    /// <remarks>
    /// <para>
    /// The formats, whose letters are read in either case, are:
    /// </para>
    /// <list type="table">
    /// <listheader>
    /// <term>Format</term>
    /// <description>Text</description>
    /// </listheader>
    /// <item>
    /// <term><c>G</c>, the default</term>
    /// <description>
    /// The months in ascending order, separated by commas, each run of two or more consecutive months as an inclusive
    /// range: <c>"1-3,12"</c>.
    /// </description>
    /// </item>
    /// <item>
    /// <term><c>L</c></term>
    /// <description>Every selected month listed, without ranges: <c>"1,2,3,12"</c>.</description>
    /// </item>
    /// <item>
    /// <term><c>B</c>, <c>0</c>, <c>1</c>, <c>01</c></term>
    /// <description>
    /// Binary, one character per month from 1 to 12, 1 first, with <c>1</c> for a selected month: <c>"111000000001"</c>.
    /// </description>
    /// </item>
    /// <item>
    /// <term><c>J</c></term>
    /// <description>
    /// A twelve-character letter mask, January first, with each selected month's initial and <c>_</c> for a month not
    /// selected: <c>"JFM________D"</c>.
    /// </description>
    /// </item>
    /// <item>
    /// <term><c>E</c>, <c>U</c>, <c>D</c>, <c>A</c></term>
    /// <description>
    /// The letter mask with a space, <c>_</c>, <c>-</c> or <c>*</c> respectively for a month not selected.
    /// </description>
    /// </item>
    /// <item>
    /// <term><c>J</c> followed by <c>E</c>, <c>U</c>, <c>D</c> or <c>A</c></term>
    /// <description>The letter mask with that placeholder: <c>"JD"</c> writes <c>"JFM--------D"</c>.</description>
    /// </item>
    /// </list>
    /// </remarks>
    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        if (string.IsNullOrEmpty(format))
            return ToString();

        Span<char> buffer = stackalloc char[CalendarValueSet.MaxTextLength];
        return new string(buffer[..Write(format, buffer)]);
    }

    /// <summary>
    /// Attempts to write the set as text in the specified format into a span of characters.
    /// </summary>
    /// <param name="destination">The span to write to.</param>
    /// <param name="charsWritten">
    /// When this method returns <see langword="true" />, the number of characters written; otherwise 0.
    /// </param>
    /// <param name="format">The format, or an empty span for the default.</param>
    /// <param name="provider">Ignored; the text does not depend on culture.</param>
    /// <returns>
    /// <see langword="true" /> when the text fits in <paramref name="destination" />; otherwise
    /// <see langword="false" />, and <paramref name="destination" /> is left unchanged.
    /// </returns>
    /// <exception cref="FormatException">Thrown when <paramref name="format" /> is not a supported format.</exception>
    /// <remarks>
    /// The text is the one <see cref="ToString(string, IFormatProvider)" /> returns for the same format.
    /// </remarks>
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format = default, IFormatProvider? provider = null)
    {
        Span<char> buffer = stackalloc char[CalendarValueSet.MaxTextLength];
        return CalendarValueSet.TryCopy(buffer[..Write(format, buffer)], destination, out charsWritten);
    }

    /// <summary>
    /// Attempts to write the set as UTF-8 text in the specified format into a span of bytes.
    /// </summary>
    /// <param name="utf8Destination">The span to write to.</param>
    /// <param name="bytesWritten">
    /// When this method returns <see langword="true" />, the number of bytes written; otherwise 0.
    /// </param>
    /// <param name="format">The format, or an empty span for the default.</param>
    /// <param name="provider">Ignored; the text does not depend on culture.</param>
    /// <returns>
    /// <see langword="true" /> when the text fits in <paramref name="utf8Destination" />; otherwise
    /// <see langword="false" />, and <paramref name="utf8Destination" /> is left unchanged.
    /// </returns>
    /// <exception cref="FormatException">Thrown when <paramref name="format" /> is not a supported format.</exception>
    /// <remarks>
    /// The text is the one <see cref="ToString(string, IFormatProvider)" /> returns for the same format. It is ASCII,
    /// so each character is one byte.
    /// </remarks>
    public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format = default, IFormatProvider? provider = null)
    {
        Span<char> buffer = stackalloc char[CalendarValueSet.MaxTextLength];
        return CalendarValueSet.TryCopyUtf8(buffer[..Write(format, buffer)], utf8Destination, out bytesWritten);
    }

    /// <summary>
    /// Writes the set as text in a format.
    /// </summary>
    /// <param name="format">The format, or an empty span for the default.</param>
    /// <param name="buffer">
    /// The buffer to write to, <see cref="CalendarValueSet.MaxTextLength" /> characters long.
    /// </param>
    /// <returns>The number of characters written.</returns>
    /// <exception cref="FormatException">Thrown when <paramref name="format" /> is not a supported format.</exception>
    private int Write(ReadOnlySpan<char> format, Span<char> buffer)
    {
        if (format.IsEmpty)
            return CalendarValueSet.WriteList(_bits, MinimumValue, ranges: true, buffer);

        return TryParseFormat(format, out TextFormat textFormat)
            ? Write(textFormat, buffer)
            : throw CalendarValueSet.CreateFormatStringException(format.ToString(), nameof(MonthSet));
    }

    /// <summary>
    /// Attempts to read a format string.
    /// </summary>
    /// <param name="format">The format string, in either case.</param>
    /// <param name="result">When this method returns <see langword="true" />, the form it names.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="format" /> is a supported format; otherwise
    /// <see langword="false" />.
    /// </returns>
    private static bool TryParseFormat(ReadOnlySpan<char> format, out TextFormat result)
    {
        if (CalendarValueSet.TryParseNumericFormat(format, out CalendarValueSet.NumericForm form))
        {
            result = new TextFormat(form, LetterMask: false, Placeholder: null);
            return true;
        }

        result = default;
        if (format.Length is < 1 or > 2)
            return false;

        // The letter mask: J on its own, a placeholder letter on its own, or J followed by a placeholder letter.
        char first = char.ToUpperInvariant(format[0]);
        char? placeholder = null;
        if (format.Length == 2)
        {
            if (first != 'J' || !CalendarValueSet.TryParsePlaceholder(char.ToUpperInvariant(format[1]), out char named))
                return false;

            placeholder = named;
        }
        else if (first != 'J')
        {
            if (!CalendarValueSet.TryParsePlaceholder(first, out char named))
                return false;

            placeholder = named;
        }

        result = new TextFormat(CalendarValueSet.NumericForm.List, LetterMask: true, placeholder);
        return true;
    }

    /// <summary>
    /// Writes the set as text in a form.
    /// </summary>
    /// <param name="format">The form.</param>
    /// <param name="buffer">
    /// The buffer to write to, <see cref="CalendarValueSet.MaxTextLength" /> characters long.
    /// </param>
    /// <returns>The number of characters written.</returns>
    private int Write(TextFormat format, Span<char> buffer) =>
        format.LetterMask
            ? CalendarValueSet.WriteLetters(_bits, MonthLetters, first: 0, format.Placeholder ?? '_', buffer)
            : CalendarValueSet.Write(_bits, MinimumValue, MaximumValue, format.Form, buffer);
}
