// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Money.Parse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Financial;

public readonly partial struct Money :
    IParsable<Money>,
    ISpanParsable<Money>
{
    /// <summary>
    /// Parses a <see cref="Money" /> from <c>"&lt;ISO&gt; &lt;amount&gt;"</c> or <c>"&lt;amount&gt; &lt;ISO&gt;"</c>.
    /// </summary>
    /// <param name="s">The text to parse.</param>
    /// <param name="provider">The culture used to interpret the numeric component.</param>
    /// <returns>The parsed value.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="s" /> is <see langword="null" />.</exception>
    /// <exception cref="FormatException">The input is not a valid <see cref="Money" /> representation.</exception>
    public static Money Parse(string s, IFormatProvider? provider)
    {
        ThrowHelper.ThrowIfNull(s);
        return Parse(s.AsSpan(), provider);
    }

    /// <summary>
    /// Parses a <see cref="Money" /> from a span representation.
    /// </summary>
    /// <param name="s">The text to parse.</param>
    /// <param name="provider">The culture used to interpret the numeric component.</param>
    /// <returns>The parsed value.</returns>
    /// <exception cref="FormatException">The input is not a valid representation.</exception>
    public static Money Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
    {
        return !TryParse(s, provider, out Money result)
            ? throw new FormatException(
                string.Format(CultureInfo.InvariantCulture, FinancialResourceStrings.Format_Invalid_MoneyString, s.ToString()))
            : result;
    }

    /// <summary>
    /// Attempts to parse a <see cref="Money" /> from a string.
    /// </summary>
    /// <param name="s">The text to parse.</param>
    /// <param name="provider">The culture provider.</param>
    /// <param name="result">
    /// When this method returns <see langword="true" />, the parsed value; otherwise the default.
    /// </param>
    /// <returns><see langword="true" /> on success.</returns>
    public static bool TryParse(string? s, IFormatProvider? provider, out Money result)
    {
        if (s is null)
        {
            result = default;
            return false;
        }

        return TryParse(s.AsSpan(), provider, out result);
    }

    /// <summary>
    /// Attempts to parse a <see cref="Money" /> from a span.
    /// </summary>
    /// <param name="s">The text to parse.</param>
    /// <param name="provider">The culture provider.</param>
    /// <param name="result">The parsed value or default.</param>
    /// <returns><see langword="true" /> on success.</returns>
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Money result)
    {
        result = default;

        ReadOnlySpan<char> trimmed = s.Trim();
        if (trimmed.IsEmpty)
            return false;

        // ISO prefix: "USD 19.99" — three-letter code, space, amount.
        if (trimmed.Length >= 5 && trimmed[3] == ' '
            && IsUppercaseAscii(trimmed[0]) && IsUppercaseAscii(trimmed[1]) && IsUppercaseAscii(trimmed[2]))
        {
            var iso = trimmed[..3].ToString();
            ReadOnlySpan<char> numericPart = trimmed[4..].TrimStart();
            return TryComposeWithCulture(numericPart, iso, provider, out result);
        }

        // ISO suffix: "19.99 USD".
        if (trimmed.Length >= 5 && trimmed[^4] == ' '
            && IsUppercaseAscii(trimmed[^3]) && IsUppercaseAscii(trimmed[^2]) && IsUppercaseAscii(trimmed[^1]))
        {
            var iso = trimmed[^3..].ToString();
            ReadOnlySpan<char> numericPart = trimmed[..^4].TrimEnd();
            return TryComposeWithCulture(numericPart, iso, provider, out result);
        }

        // Bare decimal with no currency — cannot construct without an ISO code.
        return false;
    }

    /// <summary>
    /// Parses the numeric portion and constructs the <see cref="Money" /> when both parts are valid.
    /// </summary>
    /// <param name="numericPart">The numeric span.</param>
    /// <param name="iso">The ISO code.</param>
    /// <param name="provider">The culture provider.</param>
    /// <param name="result">The constructed value.</param>
    /// <returns><see langword="true" /> on success.</returns>
    private static bool TryComposeWithCulture(ReadOnlySpan<char> numericPart, string iso, IFormatProvider? provider, out Money result)
    {
        result = default;
        if (numericPart.IsEmpty) return false;

        // The strict parser only yields a value for currencies registered in CurrencyRegistry; an unregistered code is
        // a parse failure here (it cannot be constructed under the default reject policy) rather than an exception.
        if (!CurrencyRegistry.Contains(iso))
            return false;

        IFormatProvider effective = provider ?? CultureInfo.CurrentCulture;
        if (!decimal.TryParse(numericPart, NumberStyles.Number | NumberStyles.AllowLeadingSign, effective, out var amount))
            return false;

        result = new Money(amount, iso);
        return true;
    }

    /// <summary>
    /// Determines whether a character is an ASCII uppercase letter.
    /// </summary>
    /// <param name="c">The character to test.</param>
    /// <returns><see langword="true" /> when <paramref name="c" /> is in [A-Z].</returns>
    private static bool IsUppercaseAscii(char c) =>
        c is >= 'A' and <= 'Z';
}
