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

        return CalendarValueSet.TryParseNumericFormat(format, out CalendarValueSet.NumericForm form)
            ? CalendarValueSet.Format(_bits, MinimumValue, MaximumValue, form)
            : throw CalendarValueSet.CreateFormatStringException(format, nameof(DayOfMonthSet));
    }
}
