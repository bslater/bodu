// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DayOfWeekSet.Formatting.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public readonly partial struct DayOfWeekSet
{
    /// <summary>The number of characters in a mask, one per day.</summary>
    private const int MaskLength = 7;

    /// <summary>The letter of each day, in <see cref="DayOfWeek" /> order.</summary>
    private const string DayLetters = "SMTWTFS";

    /// <summary>
    /// Returns the set as a seven-character mask, Sunday first, with each day's letter when it is selected and <c>_</c>
    /// when it is not.
    /// </summary>
    /// <returns>The mask, such as <c>"_MTWTF_"</c> for Monday to Friday.</returns>
    public override string ToString()
    {
        Span<char> buffer = stackalloc char[MaskLength];
        return new string(buffer[..Write(TextFormat.Default, buffer)]);
    }

    /// <summary>
    /// Returns the set as a seven-character mask in the specified format.
    /// </summary>
    /// <param name="format">The format, or <see langword="null" /> or empty for the default.</param>
    /// <returns>The mask.</returns>
    /// <exception cref="FormatException">Thrown when <paramref name="format" /> is not a supported format.</exception>
    /// <remarks>
    /// The formats are described at <see cref="ToString(string, IFormatProvider)" />.
    /// </remarks>
    public string ToString(string? format) =>
        ToString(format, formatProvider: null);

    /// <summary>
    /// Returns the set as a seven-character mask in the specified format.
    /// </summary>
    /// <param name="format">The format, or <see langword="null" /> or empty for the default.</param>
    /// <param name="formatProvider">Ignored; the mask does not depend on culture.</param>
    /// <returns>The mask.</returns>
    /// <exception cref="FormatException">Thrown when <paramref name="format" /> is not a supported format.</exception>
    /// <remarks>
    /// <para>
    /// The formats, whose letters are read in either case, are:
    /// </para>
    /// <list type="table">
    /// <listheader>
    /// <term>Format</term>
    /// <description>Mask</description>
    /// </listheader>
    /// <item>
    /// <term><c>S</c> or <c>G</c>, the default</term>
    /// <description>Sunday first, with <c>_</c> for a day not selected: <c>"_MTWTF_"</c>.</description>
    /// </item>
    /// <item>
    /// <term><c>M</c></term>
    /// <description>Monday first, with <c>_</c> for a day not selected: <c>"MTWTF__"</c>.</description>
    /// </item>
    /// <item>
    /// <term><c>E</c>, <c>U</c>, <c>D</c>, <c>A</c></term>
    /// <description>
    /// Sunday first, with a space, <c>_</c>, <c>-</c> or <c>*</c> respectively for a day not selected.
    /// </description>
    /// </item>
    /// <item>
    /// <term><c>S</c> or <c>M</c> followed by <c>E</c>, <c>U</c>, <c>D</c> or <c>A</c></term>
    /// <description>That order, with that placeholder: <c>"MD"</c> writes <c>"MTWTF--"</c>.</description>
    /// </item>
    /// <item>
    /// <term><c>B</c>, <c>0</c>, <c>1</c>, <c>01</c></term>
    /// <description>Binary, Sunday first, with <c>1</c> for a selected day: <c>"0111110"</c>.</description>
    /// </item>
    /// </list>
    /// </remarks>
    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        Span<char> buffer = stackalloc char[MaskLength];
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
        Span<char> buffer = stackalloc char[MaskLength];
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
        Span<char> buffer = stackalloc char[MaskLength];
        return CalendarValueSet.TryCopyUtf8(buffer[..Write(format, buffer)], utf8Destination, out bytesWritten);
    }

    /// <summary>
    /// Writes the set as text in a format.
    /// </summary>
    /// <param name="format">The format, or an empty span for the default.</param>
    /// <param name="buffer">The buffer to write to, <see cref="MaskLength" /> characters long.</param>
    /// <returns>The number of characters written.</returns>
    /// <exception cref="FormatException">Thrown when <paramref name="format" /> is not a supported format.</exception>
    private int Write(ReadOnlySpan<char> format, Span<char> buffer)
    {
        if (format.IsEmpty)
            return Write(TextFormat.Default, buffer);

        return TryParseFormat(format, out TextFormat textFormat)
            ? Write(textFormat, buffer)
            : throw CalendarValueSet.CreateFormatStringException(format.ToString(), nameof(DayOfWeekSet));
    }

    /// <summary>
    /// Attempts to read a format string.
    /// </summary>
    /// <param name="format">The format string, in either case.</param>
    /// <param name="result">When this method returns <see langword="true" />, the format it names.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="format" /> is a supported format; otherwise
    /// <see langword="false" />.
    /// </returns>
    private static bool TryParseFormat(ReadOnlySpan<char> format, out TextFormat result)
    {
        result = default;
        if (format.Length == 2 && format[0] == '0' && format[1] == '1')
        {
            result = TextFormat.BinaryForm;
            return true;
        }

        if (format.Length is < 1 or > 2)
            return false;

        char first = char.ToUpperInvariant(format[0]);
        if (format.Length == 1)
        {
            switch (first)
            {
                case 'B' or '0' or '1':
                    result = TextFormat.BinaryForm;
                    return true;

                case 'S' or 'M' or 'G':
                    result = new TextFormat(first == 'M', Placeholder: null, Binary: false);
                    return true;
            }

            // A placeholder letter on its own keeps the Sunday-first order.
            if (!CalendarValueSet.TryParsePlaceholder(first, out char sundayFirstPlaceholder))
                return false;

            result = new TextFormat(MondayFirst: false, sundayFirstPlaceholder, Binary: false);
            return true;
        }

        if (first is not ('S' or 'M') || !CalendarValueSet.TryParsePlaceholder(char.ToUpperInvariant(format[1]), out char placeholder))
            return false;

        result = new TextFormat(first == 'M', placeholder, Binary: false);
        return true;
    }

    /// <summary>
    /// Writes the set as a mask in a format.
    /// </summary>
    /// <param name="format">The format.</param>
    /// <param name="buffer">The buffer to write to, <see cref="MaskLength" /> characters long.</param>
    /// <returns>The number of characters written, seven.</returns>
    private int Write(TextFormat format, Span<char> buffer) =>
        format.Binary
            ? CalendarValueSet.WriteBinary(_bits, MaskLength, buffer)
            : CalendarValueSet.WriteLetters(_bits, DayLetters, format.MondayFirst ? 1 : 0, format.Placeholder ?? '_', buffer);
}
