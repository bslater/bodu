// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Money.ParseOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Financial.Currencies;

namespace Bodu.Financial;

public readonly partial struct Money
{
    /// <summary>
    /// Parses a <see cref="Money" /> using the supplied <paramref name="options" />.
    /// </summary>
    /// <param name="s">The text to parse.</param>
    /// <param name="options">The parse options controlling mode and culture.</param>
    /// <returns>The parsed value.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="s" /> or <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="FormatException">The input is not valid under <paramref name="options" />.</exception>
    /// <remarks>
    /// In every mode, text carrying more fractional digits than the currency's registered minor units parses to a value
    /// reporting that finer scale from <see cref="MinorUnits" />, as <see cref="Parse(string, IFormatProvider?)" />
    /// does.
    /// </remarks>
    public static Money Parse(string s, MoneyParseOptions options)
    {
        ThrowHelper.ThrowIfNull(s);
        ThrowHelper.ThrowIfNull(options);

        return TryParse(s.AsSpan(), options, out Money result)
            ? result
            : throw new FormatException(
                string.Format(CultureInfo.CurrentCulture, FinancialResourceStrings.Format_Invalid_MoneyString, s));
    }

    /// <summary>
    /// Attempts to parse a <see cref="Money" /> using the supplied <paramref name="options" />.
    /// </summary>
    /// <param name="s">The text to parse.</param>
    /// <param name="options">The parse options controlling mode and culture.</param>
    /// <param name="result">
    /// When this method returns <see langword="true" />, the parsed value; otherwise the default.
    /// </param>
    /// <returns><see langword="true" /> on success.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="options" />.<see cref="MoneyParseOptions.Mode" /> is not a defined value.
    /// </exception>
    public static bool TryParse(ReadOnlySpan<char> s, MoneyParseOptions options, out Money result)
    {
        ThrowHelper.ThrowIfNull(options);
        ThrowHelper.ThrowIfEnumValueIsUndefined(options.Mode);

        result = default;

        // The round-trip form is exact: no trimming, no culture, and only the shape the "R" specifier writes.
        if (options.Mode == MoneyParseMode.RoundTripOnly)
            return TryParseRoundTrip(s, out result);

        IFormatProvider provider = options.FormatProvider ?? CultureInfo.CurrentCulture;

        ReadOnlySpan<char> trimmed = s.Trim();
        if (trimmed.IsEmpty)
            return false;

        if (TryExtractIso(trimmed, out ReadOnlySpan<char> numericPart, out string? iso))
        {
            if (options.Mode == MoneyParseMode.LenientImport)
                iso = iso.ToUpperInvariant();

            return TryCompose(numericPart, iso, provider, out result);
        }

        // No embedded ISO code: only culture-aware parsing can resolve a currency from a symbol.
        if (options.Mode == MoneyParseMode.CultureAware
            && TryExtractSymbol(trimmed, out ReadOnlySpan<char> symbolNumeric, out string? symbol))
        {
            ICurrencyLookup lookup = options.CurrencyLookup ?? new CurrencyLookupService();
            if (lookup.TryBySymbol(symbol, out IReadOnlyList<CurrencyInfo> matches) && matches.Count == 1)
                return TryCompose(symbolNumeric, matches[0].IsoCode, provider, out result);
        }

        return false;
    }

    /// <summary>
    /// Parses the invariant round-trip form written by the <c>"R"</c> format specifier: an uppercase ISO code, one
    /// space, and an amount in invariant fixed-point notation, with nothing before or after.
    /// </summary>
    /// <param name="s">The untrimmed input span.</param>
    /// <param name="result">The constructed value.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="s" /> is in the round-trip form for a registered currency.
    /// </returns>
    private static bool TryParseRoundTrip(ReadOnlySpan<char> s, out Money result)
    {
        result = default;

        if (s.Length < 5 || s[3] != ' ' || !IsUppercaseAscii(s[0]) || !IsUppercaseAscii(s[1]) || !IsUppercaseAscii(s[2]))
            return false;

        ReadOnlySpan<char> numericPart = s[4..];
        return IsRoundTripNumber(numericPart) && TryCompose(numericPart, s[..3].ToString(), CultureInfo.InvariantCulture, out result);
    }

    /// <summary>
    /// Determines whether <paramref name="numericPart" /> is in the invariant fixed-point notation the <c>"R"</c>
    /// format specifier writes: an optional leading minus sign, one or more digits, and optionally a decimal point
    /// followed by one or more digits.
    /// </summary>
    /// <param name="numericPart">The numeric span.</param>
    /// <returns><see langword="true" /> when <paramref name="numericPart" /> has the fixed-point shape.</returns>
    private static bool IsRoundTripNumber(ReadOnlySpan<char> numericPart)
    {
        int index = !numericPart.IsEmpty && numericPart[0] == '-' ? 1 : 0;
        int integerStart = index;
        while (index < numericPart.Length && char.IsAsciiDigit(numericPart[index]))
            index++;

        if (index == integerStart)
            return false;

        if (index == numericPart.Length)
            return true;

        if (numericPart[index] != '.')
            return false;

        int fractionStart = ++index;
        while (index < numericPart.Length && char.IsAsciiDigit(numericPart[index]))
            index++;

        return index > fractionStart && index == numericPart.Length;
    }

    /// <summary>
    /// Extracts the numeric portion and three-letter ISO code from <paramref name="trimmed" /> when it is in prefix or
    /// suffix form.
    /// </summary>
    /// <param name="trimmed">The trimmed input span.</param>
    /// <param name="numericPart">The numeric portion of the input.</param>
    /// <param name="iso">The detected ISO code.</param>
    /// <returns><see langword="true" /> when an ISO code was located.</returns>
    private static bool TryExtractIso(ReadOnlySpan<char> trimmed, out ReadOnlySpan<char> numericPart, out string iso)
    {
        // ISO prefix: "USD 19.99".
        if (trimmed.Length >= 5 && trimmed[3] == ' '
            && IsAsciiLetter(trimmed[0]) && IsAsciiLetter(trimmed[1]) && IsAsciiLetter(trimmed[2]))
        {
            iso = trimmed[..3].ToString();
            numericPart = trimmed[4..].TrimStart();
            return true;
        }

        // ISO suffix: "19.99 USD".
        if (trimmed.Length >= 5 && trimmed[^4] == ' '
            && IsAsciiLetter(trimmed[^3]) && IsAsciiLetter(trimmed[^2]) && IsAsciiLetter(trimmed[^1]))
        {
            iso = trimmed[^3..].ToString();
            numericPart = trimmed[..^4].TrimEnd();
            return true;
        }

        numericPart = default;
        iso = string.Empty;
        return false;
    }

    /// <summary>
    /// Extracts a leading or trailing currency symbol and the remaining numeric portion.
    /// </summary>
    /// <param name="trimmed">The trimmed input span.</param>
    /// <param name="numericPart">The numeric portion of the input.</param>
    /// <param name="symbol">The detected symbol.</param>
    /// <returns><see langword="true" /> when a symbol was located.</returns>
    private static bool TryExtractSymbol(ReadOnlySpan<char> trimmed, out ReadOnlySpan<char> numericPart, out string symbol)
    {
        int start = 0;
        while (start < trimmed.Length && IsSymbolChar(trimmed[start]))
            start++;

        if (start > 0)
        {
            symbol = trimmed[..start].ToString();
            numericPart = trimmed[start..].TrimStart();
            return true;
        }

        int end = trimmed.Length;
        while (end > 0 && IsSymbolChar(trimmed[end - 1]))
            end--;

        if (end < trimmed.Length)
        {
            symbol = trimmed[end..].ToString();
            numericPart = trimmed[..end].TrimEnd();
            return true;
        }

        numericPart = default;
        symbol = string.Empty;
        return false;
    }

    /// <summary>
    /// Parses the numeric portion and constructs the value when the currency is registered.
    /// </summary>
    /// <param name="numericPart">The numeric span.</param>
    /// <param name="iso">The ISO code.</param>
    /// <param name="provider">The culture provider.</param>
    /// <param name="result">The constructed value.</param>
    /// <returns><see langword="true" /> on success.</returns>
    private static bool TryCompose(
        ReadOnlySpan<char> numericPart,
        string iso,
        IFormatProvider provider,
        out Money result)
    {
        result = default;

        if (numericPart.IsEmpty || iso.Length != 3
            || !IsUppercaseAscii(iso[0]) || !IsUppercaseAscii(iso[1]) || !IsUppercaseAscii(iso[2]))
        {
            return false;
        }

        if (!decimal.TryParse(numericPart, NumberStyles.Number | NumberStyles.AllowLeadingSign, provider, out decimal amount))
            return false;

        if (CurrencyResolution.Contains(iso) && CurrencyInfo.TryGetCurrencyCode(iso, out CurrencyCode code))
        {
            result = FromParsedAmount(amount, code);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Determines whether a character is an ASCII letter (either case).
    /// </summary>
    /// <param name="c">The character to test.</param>
    /// <returns><see langword="true" /> when <paramref name="c" /> is in [A-Za-z].</returns>
    private static bool IsAsciiLetter(char c) =>
        c is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z');

    /// <summary>
    /// Determines whether a character can form part of a currency symbol (neither digit, letter, nor white space).
    /// </summary>
    /// <param name="c">The character to test.</param>
    /// <returns><see langword="true" /> when <paramref name="c" /> is a symbol character.</returns>
    private static bool IsSymbolChar(char c) =>
        !char.IsDigit(c) && !char.IsWhiteSpace(c) && c is not ('-' or '+' or '.' or ',' or '(' or ')');
}
