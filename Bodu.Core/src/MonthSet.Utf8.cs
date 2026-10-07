// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MonthSet.Utf8.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public readonly partial struct MonthSet
{
    /// <summary>
    /// Converts UTF-8 text in one of the forms <see cref="ToString(string, IFormatProvider)" /> writes into a
    /// <see cref="MonthSet" />, detecting the form.
    /// </summary>
    /// <param name="utf8Text">The UTF-8 text to convert.</param>
    /// <returns>The set the text describes.</returns>
    /// <exception cref="FormatException">
    /// Thrown when <paramref name="utf8Text" /> is not valid UTF-8, or is neither the binary form, the letter mask, nor
    /// a list of values and ranges from 1 to 12.
    /// </exception>
    /// <remarks>
    /// The text is decoded and read as <see cref="Parse(string)" /> reads a string.
    /// </remarks>
    public static MonthSet Parse(ReadOnlySpan<byte> utf8Text) =>
        CalendarValueSet.ParseUtf8<MonthSet>(utf8Text);

    /// <summary>
    /// Attempts to convert UTF-8 text in one of the forms <see cref="ToString(string, IFormatProvider)" /> writes into
    /// a <see cref="MonthSet" />, detecting the form.
    /// </summary>
    /// <param name="utf8Text">The UTF-8 text to convert.</param>
    /// <param name="result">
    /// When this method returns <see langword="true" />, the set the text describes; otherwise <see cref="Empty" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="utf8Text" /> is valid UTF-8 for the binary form, the letter mask,
    /// or a list of values and ranges from 1 to 12; otherwise <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// The text is decoded and read as <see cref="Parse(string)" /> reads a string.
    /// </remarks>
    public static bool TryParse(ReadOnlySpan<byte> utf8Text, out MonthSet result) =>
        CalendarValueSet.TryParseUtf8(utf8Text, out result);

    /// <inheritdoc />
    static MonthSet IUtf8SpanParsable<MonthSet>.Parse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider) =>
        Parse(utf8Text);

    /// <inheritdoc />
    static bool IUtf8SpanParsable<MonthSet>.TryParse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider, out MonthSet result) =>
        TryParse(utf8Text, out result);
}
