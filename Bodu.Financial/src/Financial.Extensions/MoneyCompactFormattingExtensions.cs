// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyCompactFormattingExtensions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Financial.Extensions;

/// <summary>
/// Provides compact-notation formatting (<c>"$1.2K"</c>, <c>"€1.5M"</c>, <c>"USD 2.3B"</c>) for
/// <see cref="Money{TCurrency}" /> and <see cref="Money" /> values, on top of the standard format-specifier vocabulary.
/// </summary>
/// <remarks>
/// <para>
/// Compact formatting scales the amount by an order-of-magnitude factor (one thousand, one million, one billion, or one
/// trillion) and appends a single-letter magnitude suffix (<c>K</c>, <c>M</c>, <c>B</c>, or <c>T</c>) to the numeric
/// portion of the output. The chosen format specifier (<c>C</c>, <c>G</c>, <c>L</c>, <c>N</c>, <c>F</c>, <c>D</c>)
/// still drives where the currency designator appears, so the suffix attaches to the numeric portion in the correct
/// culture position — for example, <c>"$1.2K"</c> in en-US for USD versus <c>"1,2K €"</c> in fr-FR for EUR.
/// </para>
/// <para>
/// The <c>R</c> specifier is not compatible with compact formatting because the round-trip form is meant to parse back
/// to the original value; combining the two would silently lose precision. Compact extensions therefore reject the
/// <c>R</c> specifier and the <c>"~"</c> prefix on <c>R</c>.
/// </para>
/// <para>
/// Magnitude selection is by absolute value: values below 1,000 keep their natural scale, values from 1,000 use
/// <c>K</c>, from 1,000,000 use <c>M</c>, from 1,000,000,000 use <c>B</c>, and from 1,000,000,000,000 use <c>T</c>.
/// Values above the trillion threshold continue to use <c>T</c> (so <c>5,000,000,000,000</c> becomes <c>5T</c>).
/// </para>
/// </remarks>
public static partial class MoneyCompactFormattingExtensions
{
    /// <summary>The default fractional-digit count for compact-notation output when none is supplied.</summary>
    private const int DefaultCompactPrecision = 1;

    /// <summary>The thresholds at which the magnitude suffix switches, paired with their suffix letter. Ordered from largest to smallest so the first match wins.</summary>
    private static readonly (decimal Scale, string Suffix)[] s_thresholds =
    [
        (1_000_000_000_000m, "T"),
        (1_000_000_000m, "B"),
        (1_000_000m, "M"),
        (1_000m, "K"),
    ];

    /// <summary>
    /// Selects the magnitude scale and suffix for <paramref name="amount" />, returning <c>(1m, "")</c> for values
    /// below one thousand in magnitude.
    /// </summary>
    /// <param name="amount">The raw decimal amount.</param>
    /// <returns>
    /// A tuple of <c>(scaled, suffix)</c> where <c>scaled</c> is the amount divided by the chosen scale factor and
    /// <c>suffix</c> is the magnitude letter (or <see cref="string.Empty" />).
    /// </returns>
    private static (decimal Scaled, string Suffix) Scale(decimal amount)
    {
        decimal abs = Math.Abs(amount);
        foreach ((decimal scale, string? suffix) in s_thresholds)
        {
            if (abs >= scale)
                return (amount / scale, suffix);
        }

        return (amount, string.Empty);
    }

    /// <summary>
    /// Returns a format span with the explicit <paramref name="precision" /> suffix appended when the amount was
    /// scaled, stripping any precision suffix already present in <paramref name="format" /> so the
    /// <paramref name="precision" /> argument wins. When the amount was not scaled, the caller's format is returned
    /// unchanged so the underlying value uses the currency's natural precision.
    /// </summary>
    /// <param name="format">The caller-supplied format specifier, or <see langword="null" /> for the default.</param>
    /// <param name="precision">The fractional-digit count to apply when scaling occurred.</param>
    /// <param name="scaled">Whether the amount was scaled (and therefore needs an explicit precision override).</param>
    /// <returns>A character span describing the format to pass to the underlying formatter.</returns>
    private static ReadOnlySpan<char> BuildFormatWithPrecision(string? format, int precision, bool scaled)
    {
        if (!scaled)
            return format is null ? default : format.AsSpan();

        string prefix = StripPrecisionSuffix(format ?? "G");
        return (prefix + precision.ToString(CultureInfo.InvariantCulture)).AsSpan();
    }

    /// <summary>
    /// Removes a trailing decimal-digit precision suffix from <paramref name="format" />, leaving the optional
    /// <c>"~"</c> prefix and specifier letter intact.
    /// </summary>
    /// <param name="format">The format specifier, possibly including a precision suffix.</param>
    /// <returns>The format with any trailing digits removed.</returns>
    private static string StripPrecisionSuffix(string format)
    {
        int end = format.Length;
        while (end > 0 && format[end - 1] >= '0' && format[end - 1] <= '9')
            end--;
        return end == format.Length ? format : format[..end];
    }

    /// <summary>
    /// Rejects the <c>R</c> specifier (with or without the <c>"~"</c> prefix) because the invariant round-trip form
    /// cannot be combined with compact notation without silently dropping precision.
    /// </summary>
    /// <param name="format">The caller-supplied format specifier.</param>
    /// <exception cref="FormatException">Thrown when the format begins with <c>R</c> or <c>~R</c>.</exception>
    private static void RejectRoundTripSpecifier(string? format)
    {
        if (format is null)
            return;

        ReadOnlySpan<char> span = format.AsSpan();
        int cursor = 0;
        if (cursor < span.Length && span[cursor] == '~')
            cursor++;

        if (cursor < span.Length && char.ToUpperInvariant(span[cursor]) == 'R')
        {
            throw new FormatException(
                string.Format(CultureInfo.CurrentCulture, FinancialResourceStrings.Format_Invalid_FormatSpecifier, format));
        }
    }
}
