// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DayOfMonthSet.Formatting.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public readonly partial struct DayOfMonthSet
{
    /// <summary>
    /// Returns the canonical text form of the set: the selected days in ascending order, separated by commas, each run
    /// of two or more consecutive days written as an inclusive range.
    /// </summary>
    /// <returns>The text form, such as <c>"1,15,30-31"</c>, or the empty string for the empty set.</returns>
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
    /// The days in ascending order, separated by commas, each run of two or more consecutive days as an inclusive
    /// range: <c>"1,15,30-31"</c>.
    /// </description>
    /// </item>
    /// <item>
    /// <term><c>L</c></term>
    /// <description>Every selected day listed, without ranges: <c>"1,15,30,31"</c>.</description>
    /// </item>
    /// <item>
    /// <term><c>B</c>, <c>0</c>, <c>1</c>, <c>01</c></term>
    /// <description>
    /// Binary, one character per day from 1 to 31, 1 first, with <c>1</c> for a selected day:
    /// <c>"1000000000000010000000000000011"</c>.
    /// </description>
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

        return CalendarValueSet.TryParseNumericFormat(format, out CalendarValueSet.NumericForm form)
            ? CalendarValueSet.Write(_bits, MinimumValue, MaximumValue, form, buffer)
            : throw CalendarValueSet.CreateFormatStringException(format.ToString(), nameof(DayOfMonthSet));
    }
}
