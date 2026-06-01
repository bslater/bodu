// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyFormattingHelpers.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Financial;

/// <summary>
/// Shared formatting helpers consumed by <see cref="Money{TCurrency}" /> and <see cref="Money" /> to keep their
/// locale-aware <c>C</c>-specifier output consistent without duplicating the pattern-substitution machinery.
/// </summary>
internal static class MoneyFormattingHelpers
{
    /// <summary>
    /// The positional templates corresponding to <see cref="NumberFormatInfo.CurrencyPositivePattern" /> values 0–3.
    /// </summary>
    private static readonly string[] CurrencyPositivePatterns = ["$n", "n$", "$ n", "n $"];

    /// <summary>
    /// The positional templates corresponding to <see cref="NumberFormatInfo.CurrencyNegativePattern" /> values 0–15,
    /// matching the documented BCL pattern table.
    /// </summary>
    private static readonly string[] CurrencyNegativePatterns =
    [
        "($n)", "-$n", "$-n", "$n-", "(n$)", "-n$", "n-$", "n$-",
        "-n $", "-$ n", "n $-", "$ n-", "$ -n", "n- $", "($ n)", "(n $)",
    ];

    /// <summary>
    /// Renders <paramref name="amount" /> using the culture's native <c>"C"</c> currency format, overriding the
    /// culture's <see cref="NumberFormatInfo.CurrencyDecimalDigits" /> with the explicitly requested fractional-digit
    /// count and embedding <paramref name="magnitudeSuffix" /> in the numeric portion when supplied.
    /// </summary>
    /// <param name="amount">The decimal value to render.</param>
    /// <param name="decimals">The fractional-digit count to use.</param>
    /// <param name="magnitudeSuffix">
    /// The compact-magnitude suffix (e.g. <c>"K"</c>, <c>"M"</c>), or <see cref="string.Empty" /> for none.
    /// </param>
    /// <param name="provider">The culture used to render the amount.</param>
    /// <returns>The locale-formatted amount with the culture's native currency symbol.</returns>
    internal static string FormatLocaleNative(decimal amount, int decimals, string magnitudeSuffix, IFormatProvider provider)
    {
        var source = NumberFormatInfo.GetInstance(provider);
        var nfi = (NumberFormatInfo)source.Clone();
        nfi.CurrencyDecimalDigits = decimals;
        if (magnitudeSuffix.Length == 0)
            return amount.ToString("C", nfi);

        return ApplyCurrencyPattern(amount, decimals, nfi.CurrencySymbol, magnitudeSuffix, nfi);
    }

    /// <summary>
    /// Renders <paramref name="amount" /> with <paramref name="isoCode" /> substituted for the culture's currency
    /// symbol, honouring both <see cref="NumberFormatInfo.CurrencyPositivePattern" /> and
    /// <see cref="NumberFormatInfo.CurrencyNegativePattern" /> via a cloned <see cref="NumberFormatInfo" />.
    /// </summary>
    /// <param name="amount">The decimal value to render.</param>
    /// <param name="isoCode">The validated ISO code to substitute as the currency symbol.</param>
    /// <param name="decimals">The decimal-place count.</param>
    /// <param name="magnitudeSuffix">
    /// The compact-magnitude suffix (e.g. <c>"K"</c>, <c>"M"</c>), or <see cref="string.Empty" /> for none.
    /// </param>
    /// <param name="provider">The culture used to render the numeric portion.</param>
    /// <returns>An unambiguous representation pairing the locale's number formatting with the ISO code.</returns>
    /// <remarks>
    /// The mismatch path keeps the locale's decimal and grouping separators (so 1,234.56 in en-US becomes 1.234,56 in
    /// de-DE) and uses the locale's positive- and negative-currency patterns — for example, en-US's negative pattern
    /// <c>($n)</c> applies to a mismatched-currency negative as <c>(JPY 1,234)</c>.
    /// </remarks>
    internal static string FormatLocaleMismatch(decimal amount, string isoCode, int decimals, string magnitudeSuffix, IFormatProvider provider)
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

        if (magnitudeSuffix.Length == 0)
            return amount.ToString("C", nfi);

        return ApplyCurrencyPattern(amount, decimals, nfi.CurrencySymbol, magnitudeSuffix, nfi);
    }

    /// <summary>
    /// Determines whether <paramref name="provider" /> is a region-bound <see cref="CultureInfo" /> whose
    /// <see cref="RegionInfo.ISOCurrencySymbol" /> equals <paramref name="isoCode" />.
    /// </summary>
    /// <param name="provider">The format provider to interrogate.</param>
    /// <param name="isoCode">The ISO code to compare against the culture's region currency.</param>
    /// <returns>
    /// <see langword="true" /> when the culture's region currency matches; otherwise <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// Neutral cultures (<c>"en"</c>, <c>"fr"</c>) and the invariant culture have no region currency and therefore
    /// never match. A non-<see cref="CultureInfo" /> provider — for example, a bare <see cref="NumberFormatInfo" /> —
    /// also never matches because it carries no region context.
    /// </remarks>
    internal static bool CultureMatchesIsoCode(IFormatProvider? provider, string isoCode)
    {
        if (provider is not CultureInfo culture || culture.IsNeutralCulture)
            return false;

        try
        {
            var region = new RegionInfo(culture.Name);
            return string.Equals(region.ISOCurrencySymbol, isoCode, StringComparison.Ordinal);
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    /// <summary>
    /// Builds a currency-formatted string by combining a culture-formatted numeric portion (plus a compact magnitude
    /// suffix) with <paramref name="symbol" /> placed according to <paramref name="nfi" />'s positive- and
    /// negative-currency patterns.
    /// </summary>
    /// <param name="amount">The decimal value to render.</param>
    /// <param name="decimals">The fractional-digit count.</param>
    /// <param name="symbol">The currency symbol to embed in the pattern slot.</param>
    /// <param name="magnitudeSuffix">The compact-magnitude suffix appended to the numeric portion.</param>
    /// <param name="nfi">The culture's number-format info, supplying decimal/group separators and patterns.</param>
    /// <returns>The composed currency string with the magnitude suffix attached to the numeric portion.</returns>
    private static string ApplyCurrencyPattern(decimal amount, int decimals, string symbol, string magnitudeSuffix, NumberFormatInfo nfi)
    {
        var decimalsSuffix = decimals.ToString(CultureInfo.InvariantCulture);
        var numberPart = Math.Abs(amount).ToString("N" + decimalsSuffix, nfi) + magnitudeSuffix;
        var pattern = amount < 0
            ? CurrencyNegativePatterns[nfi.CurrencyNegativePattern]
            : CurrencyPositivePatterns[nfi.CurrencyPositivePattern];

        // Substitute the number placeholder first (the formatted number never contains an ASCII '$'), then the symbol —
        // if we substituted the symbol first, a symbol containing the literal letter 'n' (rare but possible for
        // user-defined symbols) would be scrambled by the subsequent number substitution.
        return pattern.Replace("n", numberPart, StringComparison.Ordinal).Replace("$", symbol, StringComparison.Ordinal);
    }
}
