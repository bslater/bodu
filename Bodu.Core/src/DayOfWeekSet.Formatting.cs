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
    public override string ToString() =>
        Format(TextFormat.Default);

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
        if (string.IsNullOrEmpty(format))
            return Format(TextFormat.Default);

        return TryParseFormat(format, out TextFormat textFormat)
            ? Format(textFormat)
            : throw CalendarValueSet.CreateFormatStringException(format, nameof(DayOfWeekSet));
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
    /// <returns>The seven-character mask.</returns>
    private string Format(TextFormat format) =>
        format.Binary
            ? CalendarValueSet.FormatBinary(_bits, MaskLength)
            : CalendarValueSet.FormatLetters(_bits, DayLetters, format.MondayFirst ? 1 : 0, format.Placeholder ?? '_');
}
