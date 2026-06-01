// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Money.Formatting.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text;

namespace Bodu.Financial;

public readonly partial struct Money<TCurrency> :
    IFormattable,
    ISpanFormattable,
    IUtf8SpanFormattable
{
    /// <summary>
    /// Returns the default string representation: the ISO 4217 code followed by the amount with the currency's
    /// minor-unit precision, using thousand separators from the current culture.
    /// </summary>
    /// <returns>A string such as <c>"USD 1,234.56"</c>, <c>"JPY 100"</c>, or <c>"BHD 12.345"</c>.</returns>
    public override string ToString() =>
        Format(default, CultureInfo.CurrentCulture);

    /// <summary>
    /// Returns a string representation of this amount using the supplied format specifier.
    /// </summary>
    /// <param name="format">
    /// The format specifier; see <see cref="Format(ReadOnlySpan{char}, IFormatProvider?)" /> for the supported
    /// vocabulary.
    /// </param>
    /// <returns>The formatted representation.</returns>
    /// <exception cref="FormatException">
    /// Thrown when <paramref name="format" /> is not a supported specifier.
    /// </exception>
    public string ToString(string? format) =>
        Format(format, CultureInfo.CurrentCulture);

    /// <summary>
    /// Returns a string representation of this amount using the supplied format specifier and culture.
    /// </summary>
    /// <param name="format">The format specifier.</param>
    /// <param name="formatProvider">The culture used to render the numeric component.</param>
    /// <returns>The formatted representation.</returns>
    /// <exception cref="FormatException">
    /// Thrown when <paramref name="format" /> is not a supported specifier.
    /// </exception>
    public string ToString(string? format, IFormatProvider? formatProvider) =>
        Format(format, formatProvider);

    /// <summary>
    /// Attempts to format this amount into the provided character span.
    /// </summary>
    /// <param name="destination">The span that receives the formatted characters.</param>
    /// <param name="charsWritten">When this method returns, contains the number of characters written.</param>
    /// <param name="format">The format specifier.</param>
    /// <param name="provider">The culture used to render the numeric component.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="destination" /> was large enough; otherwise
    /// <see langword="false" />.
    /// </returns>
    /// <exception cref="FormatException">
    /// Thrown when <paramref name="format" /> is not a supported specifier.
    /// </exception>
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        var text = Format(format, provider);
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
    /// Attempts to format this amount into the provided UTF-8 byte span.
    /// </summary>
    /// <param name="utf8Destination">The span that receives the formatted UTF-8 bytes.</param>
    /// <param name="bytesWritten">When this method returns, contains the number of bytes written.</param>
    /// <param name="format">The format specifier.</param>
    /// <param name="provider">The culture used to render the numeric component.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="utf8Destination" /> was large enough; otherwise
    /// <see langword="false" />.
    /// </returns>
    /// <exception cref="FormatException">
    /// Thrown when <paramref name="format" /> is not a supported specifier.
    /// </exception>
    public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        var text = Format(format, provider);
        return Encoding.UTF8.TryGetBytes(text, utf8Destination, out bytesWritten);
    }

    /// <summary>
    /// Formats this amount according to the supplied specifier.
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
    /// <c>"C"</c> — the culture's native currency format when its region currency matches <typeparamref name="TCurrency" />
    /// (e.g. <c>"$1,234.56"</c> in en-US for USD, <c>"19,99 €"</c> in fr-FR for EUR), or the ISO code substituted into the
    /// culture's currency-position slot when they differ (e.g. <c>"JPY 1,234"</c> in en-US for JPY).
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <c>"L"</c> — the amount followed by the currency's English-language name (e.g.
    /// <c>"1,234.56 Australian Dollar"</c>). Falls back to the ISO-code form when the currency has no English name
    /// supplied.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <c>"R"</c> — invariant round-trip form: the ISO code followed by the amount under
    /// <see cref="CultureInfo.InvariantCulture" /> with no grouping (e.g. <c>"USD 1234.56"</c>). The supplied
    /// <paramref name="provider" /> is ignored so the output round-trips through
    /// <see cref="Money{TCurrency}.Parse(string, IFormatProvider?)" /> when invariant culture is supplied to the parser.
    /// The <c>R</c> specifier rejects the <c>"~"</c> prefix and explicit precision suffixes.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <c>"N"</c>, <c>"F"</c>, or <c>"D"</c> — bare numeric form with no currency designator. <c>"N"</c> includes
    /// culture-aware grouping; <c>"F"</c> and <c>"D"</c> do not. <c>"D"</c> is a Bodu-specific alias for <c>"F"</c>.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// Prefix <c>"~"</c> on <c>"C"</c>, <c>"G"</c>, or <c>"L"</c> — elide the currency designator entirely when the
    /// culture's region currency matches <typeparamref name="TCurrency" />, while keeping the designator when the
    /// currencies differ. For example, <c>"~C"</c> renders <c>Money&lt;USD&gt;(19.99m)</c> as <c>"19.99"</c> in en-US
    /// but as <c>"JPY 1,234"</c> for a <c>Money&lt;JPY&gt;</c> value in the same culture.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// Any specifier other than <c>"R"</c> with a numeric suffix (<c>"C4"</c>, <c>"L0"</c>, <c>"~C2"</c>) — explicit
    /// fractional-digit count overriding the currency's natural precision.
    /// </description>
    /// </item>
    /// </list>
    /// </param>
    /// <param name="provider">
    /// The culture used to render the numeric component. Ignored when <paramref name="format" /> is <c>"R"</c>.
    /// </param>
    /// <returns>The formatted representation.</returns>
    /// <exception cref="FormatException">
    /// Thrown when <paramref name="format" /> is not a supported specifier.
    /// </exception>
    private string Format(ReadOnlySpan<char> format, IFormatProvider? provider) =>
        FormatScaled(_amount, magnitudeSuffix: string.Empty, format, provider);

    /// <summary>
    /// Formats <paramref name="amount" /> under the supplied specifier, appending <paramref name="magnitudeSuffix" />
    /// (such as <c>"K"</c> or <c>"M"</c>) immediately after the numeric portion when supplied.
    /// </summary>
    /// <param name="amount">
    /// The decimal value to render. Compact-formatting extensions pass a scaled value here so the magnitude suffix can
    /// be attached to a culture-correctly positioned numeric portion.
    /// </param>
    /// <param name="magnitudeSuffix">
    /// The compact-magnitude suffix to append to the numeric portion, or <see cref="string.Empty" /> when no suffix is
    /// required.
    /// </param>
    /// <param name="format">The format specifier; see <see cref="Format(ReadOnlySpan{char}, IFormatProvider?)" />.</param>
    /// <param name="provider">The culture used for the numeric component.</param>
    /// <returns>The formatted representation with the suffix embedded in the numeric position.</returns>
    /// <exception cref="FormatException">Thrown when <paramref name="format" /> is not a supported specifier.</exception>
    internal static string FormatScaled(
        decimal amount,
        string magnitudeSuffix,
        ReadOnlySpan<char> format,
        IFormatProvider? provider)
    {
        CurrencyMetadataDescriptor metadata = CurrencyMetadata<TCurrency>.Value;
        ParseSpecifier(format, metadata.MinorUnits, out var specifier, out var decimals, out var elideIfMatched, out var hasPrecisionSuffix);

        if (specifier == 'R')
        {
            if (elideIfMatched || hasPrecisionSuffix)
            {
                throw new FormatException(
                    string.Format(CultureInfo.InvariantCulture, FinancialResourceStrings.Format_Invalid_FormatSpecifier, format.ToString()));
            }

            return string.Concat(
                metadata.IsoCode,
                " ",
                amount.ToString("F" + metadata.MinorUnits.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture),
                magnitudeSuffix);
        }

        IFormatProvider effectiveProvider = provider ?? CultureInfo.CurrentCulture;
        var decimalsSuffix = decimals.ToString(CultureInfo.InvariantCulture);

        switch (specifier)
        {
            case 'G':
                if (elideIfMatched && MoneyFormattingHelpers.CultureMatchesIsoCode(effectiveProvider, metadata.IsoCode))
                    return amount.ToString("N" + decimalsSuffix, effectiveProvider) + magnitudeSuffix;
                return string.Concat(metadata.IsoCode, " ", amount.ToString("N" + decimalsSuffix, effectiveProvider), magnitudeSuffix);

            case 'C':
                var matchesC = MoneyFormattingHelpers.CultureMatchesIsoCode(effectiveProvider, metadata.IsoCode);
                if (elideIfMatched && matchesC)
                    return amount.ToString("N" + decimalsSuffix, effectiveProvider) + magnitudeSuffix;
                return matchesC
                    ? MoneyFormattingHelpers.FormatLocaleNative(amount, decimals, magnitudeSuffix, effectiveProvider)
                    : MoneyFormattingHelpers.FormatLocaleMismatch(amount, metadata.IsoCode, decimals, magnitudeSuffix, effectiveProvider);

            case 'L':
                var matchesL = MoneyFormattingHelpers.CultureMatchesIsoCode(effectiveProvider, metadata.IsoCode);
                if (elideIfMatched && matchesL)
                    return amount.ToString("N" + decimalsSuffix, effectiveProvider) + magnitudeSuffix;
                if (string.IsNullOrEmpty(metadata.EnglishName))
                    return string.Concat(metadata.IsoCode, " ", amount.ToString("N" + decimalsSuffix, effectiveProvider), magnitudeSuffix);
                return string.Concat(amount.ToString("N" + decimalsSuffix, effectiveProvider), magnitudeSuffix, " ", metadata.EnglishName);

            case 'N':
                return amount.ToString("N" + decimalsSuffix, effectiveProvider) + magnitudeSuffix;

            case 'F':
            case 'D':
                return amount.ToString("F" + decimalsSuffix, effectiveProvider) + magnitudeSuffix;

            default:
                throw new FormatException(
                    string.Format(CultureInfo.InvariantCulture, FinancialResourceStrings.Format_Invalid_FormatSpecifier, format.ToString()));
        }
    }

    /// <summary>
    /// Parses <paramref name="format" /> into its <c>"~"</c> prefix, specifier letter, and optional precision suffix.
    /// </summary>
    /// <param name="format">The format span to parse.</param>
    /// <param name="defaultDecimals">
    /// The decimal-place count to use when no explicit precision suffix is supplied — typically the currency's natural
    /// <see cref="ICurrency.MinorUnits" /> precision.
    /// </param>
    /// <param name="specifier">The upper-cased specifier letter ('G', 'C', 'L', 'R', 'N', 'F', 'D').</param>
    /// <param name="decimals">The fractional-digit count to apply.</param>
    /// <param name="elideIfMatched">
    /// <see langword="true" /> when the format begins with <c>"~"</c>, indicating the designator should be elided when
    /// the culture's region currency matches.
    /// </param>
    /// <param name="hasPrecisionSuffix">
    /// <see langword="true" /> when an explicit precision suffix was supplied in <paramref name="format" />.
    /// </param>
    /// <exception cref="FormatException">
    /// Thrown when the precision suffix is malformed (non-numeric, negative, or exceeds
    /// <see cref="MaxDisplayPrecision" />).
    /// </exception>
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
                string.Format(CultureInfo.InvariantCulture, FinancialResourceStrings.Format_Invalid_FormatSpecifier, format.ToString()));
        }

        hasPrecisionSuffix = true;
    }

    /// <summary>
    /// The maximum explicit display precision accepted by the format parser, matching <see cref="decimal" />'s 28-digit
    /// native precision. Precisions above this either produce nonsense output or, for pathological values like
    /// <see cref="int.MaxValue" />, exhaust resources in the underlying <c>decimal.ToString</c> call.
    /// </summary>
    private const int MaxDisplayPrecision = 28;

    /// <summary>
    /// Formats this amount using the supplied string specifier, delegating to the span-based implementation.
    /// </summary>
    /// <param name="format">The format specifier.</param>
    /// <param name="provider">The culture used to render the numeric component.</param>
    /// <returns>The formatted representation.</returns>
    /// <exception cref="FormatException">
    /// Thrown when <paramref name="format" /> is not a supported specifier.
    /// </exception>
    private string Format(string? format, IFormatProvider? provider) =>
        Format(format is null ? default : format.AsSpan(), provider);

}
