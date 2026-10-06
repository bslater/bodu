// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HourSet.Formatting.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public readonly partial struct HourSet
{
    /// <summary>
    /// Returns the canonical text form of the set: the selected hours in ascending order, separated by commas, each run
    /// of two or more consecutive hours written as an inclusive range.
    /// </summary>
    /// <returns>The text form, such as <c>"9-17"</c>, or the empty string for the empty set.</returns>
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
    /// The hours in ascending order, separated by commas, each run of two or more consecutive hours as an inclusive
    /// range: <c>"9-17"</c>.
    /// </description>
    /// </item>
    /// <item>
    /// <term><c>L</c></term>
    /// <description>Every selected hour listed, without ranges: <c>"9,10,11,12,13,14,15,16,17"</c>.</description>
    /// </item>
    /// <item>
    /// <term><c>B</c>, <c>0</c>, <c>1</c>, <c>01</c></term>
    /// <description>
    /// Binary, one character per hour from 0 to 23, 0 first, with <c>1</c> for a selected hour:
    /// <c>"000000000111111111000000"</c>.
    /// </description>
    /// </item>
    /// </list>
    /// </remarks>
    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        if (string.IsNullOrEmpty(format))
            return ToString();

        return CalendarValueSet.TryParseNumericFormat(format, out CalendarValueSet.NumericForm form)
            ? CalendarValueSet.Format(_bits, MinimumValue, MaximumValue, form)
            : throw CalendarValueSet.CreateFormatStringException(format, nameof(HourSet));
    }
}
