// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Money.Formatting.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
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
    /// <c>null</c>, <c>""</c>, <c>"G"</c>, or <c>"C"</c> — the ISO 4217 code followed by the amount with minor-unit
    /// precision and culture-aware grouping (e.g. <c>"USD 1,234.56"</c>).
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <c>"L"</c> — locale-aware: the culture's native currency format when its currency matches
    /// <typeparamref name="TCurrency" /> (e.g. <c>"$1,234.56"</c> in en-US for USD, <c>"19,99 €"</c> in fr-FR for EUR),
    /// or the ISO code substituted into the locale's currency position when they differ (e.g. <c>"JPY 1,234"</c> in
    /// en-US for JPY).
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <c>"N"</c>, <c>"F"</c>, or <c>"D"</c> — bare numeric form with no currency designator. <c>"N"</c> includes
    /// culture-aware grouping; <c>"F"</c> and <c>"D"</c> do not.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// Prefix <c>"~"</c> on <c>"C"</c>, <c>"G"</c>, or <c>"L"</c> — elide the currency designator entirely when the
    /// culture's currency matches <typeparamref name="TCurrency" />, while keeping the explicit ISO code when the
    /// currencies differ. For example, <c>"~C"</c> renders <c>Money&lt;USD&gt;(19.99m)</c> as <c>"19.99"</c> in en-US
    /// but as <c>"JPY 1,234"</c> for a <c>Money&lt;JPY&gt;</c> value in the same culture.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// Any specifier with a numeric suffix (<c>"C4"</c>, <c>"L0"</c>, <c>"~C2"</c>) — explicit fractional-digit count
    /// overriding the currency's natural precision.
    /// </description>
    /// </item>
    /// </list>
    /// </param>
    /// <param name="provider">The culture used to render the numeric component.</param>
    /// <returns>The formatted representation.</returns>
    /// <exception cref="FormatException">
    /// Thrown when <paramref name="format" /> is not a supported specifier.
    /// </exception>
    private string Format(ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        var elideIfMatched = false;
        var cursor = 0;
        if (!format.IsEmpty && format[0] == '~')
        {
            elideIfMatched = true;
            cursor = 1;
        }

        CurrencyMetadataDescriptor metadata = CurrencyMetadata<TCurrency>.Value;
        char specifier;
        int decimals;
        if (cursor >= format.Length)
        {
            specifier = 'G';
            decimals = metadata.MinorUnits;
        }
        else
        {
            specifier = char.ToUpperInvariant(format[cursor]);
            cursor++;

            if (cursor >= format.Length)
            {
                decimals = metadata.MinorUnits;
            }
            else
            {
                ReadOnlySpan<char> precisionPart = format[cursor..];
                if (!int.TryParse(precisionPart, NumberStyles.None, CultureInfo.InvariantCulture, out decimals)
                    || decimals < 0 || decimals > MaxDisplayPrecision)
                {
                    throw new FormatException(
                        string.Format(CultureInfo.InvariantCulture, FinancialResourceStrings.Format_Invalid_FormatSpecifier, format.ToString()));
                }
            }
        }

        IFormatProvider effectiveProvider = provider ?? CultureInfo.CurrentCulture;
        var decimalsSuffix = decimals.ToString(CultureInfo.InvariantCulture);

        switch (specifier)
        {
            case 'G':
            case 'C':
                if (elideIfMatched && CultureMatchesCurrency(effectiveProvider))
                    return _amount.ToString("N" + decimalsSuffix, effectiveProvider);
                return string.Concat(metadata.IsoCode, " ", _amount.ToString("N" + decimalsSuffix, effectiveProvider));

            case 'L':
                var matches = CultureMatchesCurrency(effectiveProvider);
                if (elideIfMatched && matches)
                    return _amount.ToString("N" + decimalsSuffix, effectiveProvider);
                return matches
                    ? FormatLocaleNative(decimals, effectiveProvider)
                    : FormatLocaleMismatch(metadata.IsoCode, decimals, effectiveProvider);

            case 'N':
                return _amount.ToString("N" + decimalsSuffix, effectiveProvider);

            case 'F':
            case 'D':
                return _amount.ToString("F" + decimalsSuffix, effectiveProvider);

            default:
                throw new FormatException(
                    string.Format(CultureInfo.InvariantCulture, FinancialResourceStrings.Format_Invalid_FormatSpecifier, format.ToString()));
        }
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

    /// <summary>
    /// Renders the amount using the culture's native <c>"C"</c> currency format, overriding the culture's
    /// <see cref="NumberFormatInfo.CurrencyDecimalDigits" /> with the explicitly requested fractional-digit count.
    /// </summary>
    /// <param name="decimals">The fractional-digit count to use.</param>
    /// <param name="provider">The culture used to render the amount.</param>
    /// <returns>The locale-formatted amount with the culture's native currency symbol.</returns>
    private string FormatLocaleNative(int decimals, IFormatProvider provider)
    {
        var source = NumberFormatInfo.GetInstance(provider);
        var nfi = (NumberFormatInfo)source.Clone();
        nfi.CurrencyDecimalDigits = decimals;
        return _amount.ToString("C", nfi);
    }

    /// <summary>
    /// Renders the amount with the ISO 4217 code substituted for the culture's currency symbol, honouring both the
    /// culture's <see cref="NumberFormatInfo.CurrencyPositivePattern" /> and
    /// <see cref="NumberFormatInfo.CurrencyNegativePattern" /> via a cloned <see cref="NumberFormatInfo" />.
    /// </summary>
    /// <param name="isoCode">The validated ISO code to substitute as the currency symbol.</param>
    /// <param name="decimals">The decimal-place count.</param>
    /// <param name="provider">The culture used to render the numeric portion.</param>
    /// <returns>An unambiguous representation pairing the locale's number formatting with the ISO code.</returns>
    /// <remarks>
    /// The mismatch path keeps the locale's decimal and grouping separators (so 1,234.56 in en-US becomes 1.234,56 in
    /// de-DE) and uses the locale's positive- and negative-currency patterns — for example, en-US's negative-pattern
    /// <c>($n)</c> applies to a mismatched-currency negative as <c>(JPY 1,234)</c>.
    /// </remarks>
    private string FormatLocaleMismatch(string isoCode, int decimals, IFormatProvider provider)
    {
        var source = NumberFormatInfo.GetInstance(provider);
        var nfi = (NumberFormatInfo)source.Clone();
        nfi.CurrencyDecimalDigits = decimals;

        var isoBeforeAmount = source.CurrencyPositivePattern is 0 or 2;
        if (isoBeforeAmount)
        {
            nfi.CurrencySymbol = isoCode + " ";
            nfi.CurrencyPositivePattern = 0;
            nfi.CurrencyNegativePattern = source.CurrencyNegativePattern switch
            {
                14 => 0,    // ($ n) → ($n) with space in symbol
                9 => 1,     // -$ n → -$n
                12 => 2,    // $ -n → $-n
                11 => 3,    // $ n- → $n-
                0 or 1 or 2 or 3 => source.CurrencyNegativePattern,
                _ => 1,     // sensible default when the locale's negative pattern is amount-first
            };
        }
        else
        {
            nfi.CurrencySymbol = " " + isoCode;
            nfi.CurrencyPositivePattern = 1;
            nfi.CurrencyNegativePattern = source.CurrencyNegativePattern switch
            {
                15 => 4,    // (n $) → (n$)
                8 => 5,     // -n $ → -n$
                13 => 6,    // n- $ → n-$
                10 => 7,    // n $- → n$-
                4 or 5 or 6 or 7 => source.CurrencyNegativePattern,
                _ => 5,     // sensible default when the locale's negative pattern is amount-last
            };
        }

        return _amount.ToString("C", nfi);
    }

    /// <summary>
    /// Determines whether the supplied culture's region currency equals <typeparamref name="TCurrency" />.
    /// </summary>
    /// <param name="provider">
    /// The format provider to interrogate; must be a <see cref="CultureInfo" /> for a match to be possible.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="provider" /> is a region-bound <see cref="CultureInfo" /> whose
    /// <see cref="RegionInfo.ISOCurrencySymbol" /> equals <c>TCurrency.IsoCode</c>; otherwise <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// Neutral cultures (those without a region component, such as <c>"en"</c> or <c>"fr"</c>) and the invariant
    /// culture have no region currency and therefore never match. A non-<see cref="CultureInfo" /> provider — for
    /// example, a bare <see cref="NumberFormatInfo" /> — also never matches because it carries no region context.
    /// </remarks>
    private static bool CultureMatchesCurrency(IFormatProvider provider)
    {
        if (provider is not CultureInfo culture || culture.IsNeutralCulture)
            return false;

        try
        {
            var region = new RegionInfo(culture.Name);
            return string.Equals(region.ISOCurrencySymbol, CurrencyMetadata<TCurrency>.Value.IsoCode, StringComparison.Ordinal);
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
