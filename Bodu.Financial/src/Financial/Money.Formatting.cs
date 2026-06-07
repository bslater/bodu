// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Money.Formatting.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text;

namespace Bodu.Financial;

public readonly partial struct Money :
    IFormattable,
    ISpanFormattable,
    IUtf8SpanFormattable
{
    /// <summary>
    /// Returns the default string representation: ISO code followed by the amount at the currency's minor-unit
    /// precision under the current culture.
    /// </summary>
    /// <returns>The formatted string.</returns>
    public override string ToString() =>
        Format(default, CultureInfo.CurrentCulture, string.Empty);

    /// <summary>
    /// Returns a string representation using the supplied format specifier.
    /// </summary>
    /// <param name="format">The format specifier.</param>
    /// <returns>The formatted string.</returns>
    public string ToString(string? format) =>
        Format(format, CultureInfo.CurrentCulture, string.Empty);

    /// <summary>
    /// Returns a string representation using the supplied format specifier and culture.
    /// </summary>
    /// <param name="format">The format specifier.</param>
    /// <param name="formatProvider">The culture used to render the numeric component.</param>
    /// <returns>The formatted string.</returns>
    public string ToString(string? format, IFormatProvider? formatProvider) =>
        Format(format, formatProvider, string.Empty);

    /// <summary>
    /// Attempts to format this value into a character span.
    /// </summary>
    /// <param name="destination">The destination span.</param>
    /// <param name="charsWritten">The number of characters written.</param>
    /// <param name="format">The format specifier.</param>
    /// <param name="provider">The culture provider.</param>
    /// <returns><see langword="true" /> if the span was large enough.</returns>
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        var text = Format(format, provider, string.Empty);
        if (text.Length <= destination.Length)
        {
            text.AsSpan().CopyTo(destination);
            charsWritten = text.Length;
            return true;
        }

        charsWritten = 0;
        return false;
    }

    /// <summary>
    /// Attempts to format this value into a UTF-8 byte span.
    /// </summary>
    /// <param name="utf8Destination">The destination UTF-8 span.</param>
    /// <param name="bytesWritten">The number of bytes written.</param>
    /// <param name="format">The format specifier.</param>
    /// <param name="provider">The culture provider.</param>
    /// <returns><see langword="true" /> if the span was large enough.</returns>
    public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        var text = Format(format, provider, string.Empty);
        return Encoding.UTF8.TryGetBytes(text, utf8Destination, out bytesWritten);
    }

    /// <summary>
    /// Formats this amount according to the supplied specifier, mirroring the vocabulary of
    /// <see cref="Money{TCurrency}" />.
    /// </summary>
    /// <param name="format">
    /// The format specifier. Supported forms:
    /// <list type="bullet">
    /// <item>
    /// <description>
    /// <c>null</c>, <c>""</c>, or <c>"G"</c> — the ISO 4217 code followed by the amount with minor-unit precision and
    /// culture-aware grouping (e.g. <c>"USD 1,234.56"</c>).
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <c>"C"</c> — the culture's native currency format when its region currency matches <see cref="IsoCode" />, or
    /// the ISO code substituted into the culture's currency-position slot when they differ.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <c>"L"</c> — the amount followed by the currency's English-language name, sourced from
    /// <see cref="CurrencyRegistry" />. Falls back to the ISO-code form when the currency is not registered or has no
    /// English name.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <c>"R"</c> — invariant round-trip form (e.g. <c>"USD 1234.56"</c>); the supplied <paramref name="provider" /> is
    /// ignored and the <c>"~"</c> prefix and precision suffixes are rejected.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <c>"N"</c>, <c>"F"</c>, or <c>"D"</c> — bare numeric form with no currency designator. <c>"D"</c> is a
    /// Bodu-specific alias for <c>"F"</c>.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// Prefix <c>"~"</c> on <c>"C"</c>, <c>"G"</c>, or <c>"L"</c> — elide the currency designator entirely when the
    /// culture's region currency matches <see cref="IsoCode" />.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// Any specifier other than <c>"R"</c> with a numeric suffix (<c>"C4"</c>, <c>"L0"</c>) — explicit fractional-digit
    /// count overriding the currency's natural precision.
    /// </description>
    /// </item>
    /// </list>
    /// </param>
    /// <param name="provider">The culture used for the numeric portion.</param>
    /// <param name="magnitudeSuffix">
    /// The compact-magnitude suffix to append to the numeric portion, or <see cref="string.Empty" /> for none.
    /// </param>
    /// <returns>The formatted string.</returns>
    /// <exception cref="FormatException">The format specifier is not supported.</exception>
    internal string Format(ReadOnlySpan<char> format, IFormatProvider? provider, string magnitudeSuffix)
    {
        var isoCode = IsoCode;
        var minorUnits = MinorUnits;
        var englishName = CurrencyRegistry.TryGet(isoCode, out CurrencyInfo? info) && info is not null
            ? info.EnglishName
            : string.Empty;
        ParseSpecifier(format, minorUnits, out var specifier, out var decimals, out var elideIfMatched, out var hasPrecisionSuffix);

        if (specifier == 'R')
        {
            if (elideIfMatched || hasPrecisionSuffix)
                throw new FormatException(
                    string.Format(CultureInfo.CurrentCulture, FinancialResourceStrings.Format_Invalid_FormatSpecifier, format.ToString()));

            return string.Concat(
                isoCode,
                " ",
                _amount.ToString("F" + minorUnits.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture),
                magnitudeSuffix);
        }

        IFormatProvider effective = provider ?? CultureInfo.CurrentCulture;
        var decimalsSuffix = decimals.ToString(CultureInfo.InvariantCulture);

        switch (specifier)
        {
            case 'G':
                if (elideIfMatched && MoneyFormattingHelpers.CultureMatchesIsoCode(effective, isoCode))
                    return _amount.ToString("N" + decimalsSuffix, effective) + magnitudeSuffix;
                return $"{isoCode} {_amount.ToString("N" + decimalsSuffix, effective)}{magnitudeSuffix}";

            case 'C':
                var matchesC = MoneyFormattingHelpers.CultureMatchesIsoCode(effective, isoCode);
                if (elideIfMatched && matchesC)
                    return _amount.ToString("N" + decimalsSuffix, effective) + magnitudeSuffix;
                return matchesC
                    ? MoneyFormattingHelpers.FormatLocaleNative(_amount, decimals, magnitudeSuffix, effective)
                    : MoneyFormattingHelpers.FormatLocaleMismatch(_amount, isoCode, decimals, magnitudeSuffix, effective);

            case 'L':
                var matchesL = MoneyFormattingHelpers.CultureMatchesIsoCode(effective, isoCode);
                if (elideIfMatched && matchesL)
                    return _amount.ToString("N" + decimalsSuffix, effective) + magnitudeSuffix;
                if (string.IsNullOrEmpty(englishName))
                    return $"{isoCode} {_amount.ToString("N" + decimalsSuffix, effective)}{magnitudeSuffix}";
                return $"{_amount.ToString("N" + decimalsSuffix, effective)}{magnitudeSuffix} {englishName}";

            case 'N':
                return _amount.ToString("N" + decimalsSuffix, effective) + magnitudeSuffix;

            case 'F':
            case 'D':
                return _amount.ToString("F" + decimalsSuffix, effective) + magnitudeSuffix;

            default:
                throw new FormatException(
                    string.Format(CultureInfo.CurrentCulture, FinancialResourceStrings.Format_Invalid_FormatSpecifier, format.ToString()));
        }
    }

    /// <summary>
    /// Parses <paramref name="format" /> into its <c>"~"</c> prefix, specifier letter, and optional precision suffix.
    /// </summary>
    /// <param name="format">The format span to parse.</param>
    /// <param name="defaultDecimals">The decimal-place count to use when no precision suffix is supplied.</param>
    /// <param name="specifier">The upper-cased specifier letter.</param>
    /// <param name="decimals">The fractional-digit count to apply.</param>
    /// <param name="elideIfMatched"><see langword="true" /> when the format begins with <c>"~"</c>.</param>
    /// <param name="hasPrecisionSuffix">
    /// <see langword="true" /> when an explicit precision suffix was supplied in <paramref name="format" />.
    /// </param>
    /// <exception cref="FormatException">Thrown when the precision suffix is malformed.</exception>
    private static void ParseSpecifier(
        ReadOnlySpan<char> format,
        int defaultDecimals,
        out char specifier,
        out int decimals,
        out bool elideIfMatched,
        out bool hasPrecisionSuffix)
    {
        elideIfMatched = false;
        hasPrecisionSuffix = false;
        var cursor = 0;
        if (!format.IsEmpty && format[0] == '~')
        {
            elideIfMatched = true;
            cursor = 1;
        }

        if (cursor >= format.Length)
        {
            specifier = 'G';
            decimals = defaultDecimals;
            return;
        }

        specifier = char.ToUpperInvariant(format[cursor]);
        cursor++;

        if (cursor >= format.Length)
        {
            decimals = defaultDecimals;
            return;
        }

        ReadOnlySpan<char> precisionPart = format[cursor..];
        if (!int.TryParse(precisionPart, NumberStyles.None, CultureInfo.InvariantCulture, out decimals)
            || decimals < 0 || decimals > MaxDisplayPrecision)
        {
            throw new FormatException(
                string.Format(CultureInfo.CurrentCulture, FinancialResourceStrings.Format_Invalid_FormatSpecifier, format.ToString()));
        }

        hasPrecisionSuffix = true;
    }

    /// <summary>
    /// The maximum explicit display precision accepted by the format parser.
    /// </summary>
    private const int MaxDisplayPrecision = 28;

    /// <summary>
    /// Formats this amount using the supplied string specifier.
    /// </summary>
    /// <param name="format">The format specifier.</param>
    /// <param name="provider">The culture provider.</param>
    /// <param name="magnitudeSuffix">The compact-magnitude suffix, or <see cref="string.Empty" /> for none.</param>
    /// <returns>The formatted string.</returns>
    internal string Format(string? format, IFormatProvider? provider, string magnitudeSuffix) =>
        Format(format is null ? default : format.AsSpan(), provider, magnitudeSuffix);
}
