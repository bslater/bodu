// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Money.ParseOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Financial;

public readonly partial struct Money
{
    /// <summary>
    /// Parses a <see cref="Money" /> using the supplied <paramref name="options" />.
    /// </summary>
    /// <param name="s">The text to parse.</param>
    /// <param name="options">The parse options controlling mode, culture, and unknown-currency handling.</param>
    /// <returns>The parsed value.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="s" /> or <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="FormatException">The input is not valid under <paramref name="options" />.</exception>
    public static Money Parse(string s, MoneyParseOptions options)
    {
        ThrowHelper.ThrowIfNull(s);
        ThrowHelper.ThrowIfNull(options);

        return TryParse(s.AsSpan(), options, out Money result)
            ? result
            : throw new FormatException(
                string.Format(CultureInfo.InvariantCulture, FinancialResourceStrings.Format_Invalid_MoneyString, s));
    }

    /// <summary>
    /// Attempts to parse a <see cref="Money" /> using the supplied <paramref name="options" />.
    /// </summary>
    /// <param name="s">The text to parse.</param>
    /// <param name="options">The parse options controlling mode, culture, and unknown-currency handling.</param>
    /// <param name="result">When this method returns <see langword="true" />, the parsed value; otherwise the default.</param>
    /// <returns><see langword="true" /> on success.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="options" />.<see cref="MoneyParseOptions.Mode" /> is not a defined value.
    /// </exception>
    public static bool TryParse(ReadOnlySpan<char> s, MoneyParseOptions options, out Money result)
    {
        ThrowHelper.ThrowIfNull(options);
        FinancialThrowHelper.ThrowIfMoneyParseModeUndefined(options.Mode);

        result = default;

        IFormatProvider provider = options.FormatProvider
            ?? (options.Mode == MoneyParseMode.RoundTripOnly ? CultureInfo.InvariantCulture : CultureInfo.CurrentCulture);

        ReadOnlySpan<char> trimmed = s.Trim();
        if (trimmed.IsEmpty)
            return false;

        if (TryExtractIso(trimmed, out ReadOnlySpan<char> numericPart, out var iso))
        {
            if (options.Mode == MoneyParseMode.LenientImport)
                iso = iso.ToUpperInvariant();

            return TryComposeWithPolicy(numericPart, iso, provider, options.UnknownCurrency, out result);
        }

        // No embedded ISO code: only culture-aware parsing can resolve a currency from a symbol.
        if (options.Mode == MoneyParseMode.CultureAware
            && TryExtractSymbol(trimmed, out ReadOnlySpan<char> symbolNumeric, out var symbol))
        {
            ICurrencyLookup lookup = options.CurrencyLookup ?? new CurrencyLookupService();
            if (lookup.TryBySymbol(symbol, out IReadOnlyList<CurrencyInfo> matches) && matches.Count == 1)
                return TryComposeWithPolicy(symbolNumeric, matches[0].IsoCode, provider, options.UnknownCurrency, out result);
        }

        return false;
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
        var start = 0;
        while (start < trimmed.Length && IsSymbolChar(trimmed[start]))
            start++;

        if (start > 0)
        {
            symbol = trimmed[..start].ToString();
            numericPart = trimmed[start..].TrimStart();
            return true;
        }

        var end = trimmed.Length;
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
    /// Parses the numeric portion and constructs the value, honouring the unknown-currency policy.
    /// </summary>
    /// <param name="numericPart">The numeric span.</param>
    /// <param name="iso">The ISO code.</param>
    /// <param name="provider">The culture provider.</param>
    /// <param name="unknownCurrency">The policy applied when the currency is unregistered.</param>
    /// <param name="result">The constructed value.</param>
    /// <returns><see langword="true" /> on success.</returns>
    private static bool TryComposeWithPolicy(
        ReadOnlySpan<char> numericPart,
        string iso,
        IFormatProvider provider,
        UnknownCurrencyPolicy unknownCurrency,
        out Money result)
    {
        result = default;

        if (numericPart.IsEmpty || iso.Length != 3
            || !IsUppercaseAscii(iso[0]) || !IsUppercaseAscii(iso[1]) || !IsUppercaseAscii(iso[2]))
        {
            return false;
        }

        if (!decimal.TryParse(numericPart, NumberStyles.Number | NumberStyles.AllowLeadingSign, provider, out var amount))
            return false;

        if (CurrencyRegistry.Contains(iso))
        {
            result = new Money(amount, iso);
            return true;
        }

        // Unregistered: AllowUnscaled is the only policy that yields a value without a caller-supplied scale.
        if (unknownCurrency == UnknownCurrencyPolicy.AllowUnscaled)
        {
            result = Money.From(amount, iso, UnknownCurrencyPolicy.AllowUnscaled);
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
