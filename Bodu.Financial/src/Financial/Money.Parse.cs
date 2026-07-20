// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Money.Parse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Financial.Currencies;

namespace Bodu.Financial;

public readonly partial struct Money
    : IParsable<Money>,
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
    /// <remarks>
    /// Text carrying more fractional digits than the currency's registered minor units — a unit price such as
    /// <c>"USD 12.345678"</c> — parses to a value reporting that finer scale from <see cref="MinorUnits" />, making
    /// this method the inverse of the round-trip (<c>"R"</c>) format. Text at or below the registered precision parses
    /// at the registry precision, unchanged from earlier behaviour.
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// using System.Globalization;
    /// using Bodu.Financial;
    ///
    /// // The ISO code may lead or trail the amount; it selects the runtime currency.
    /// var a = Money.Parse("USD 19.99", CultureInfo.InvariantCulture);
    /// var b = Money.Parse("19.99 USD", CultureInfo.InvariantCulture);   // equal to a
    ///
    /// // A bare amount has no currency and fails to parse.
    /// bool ok = Money.TryParse("19.99", CultureInfo.InvariantCulture, out _);   // false
    ///]]>
    /// </code>
    /// </example>
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
                string.Format(CultureInfo.CurrentCulture, FinancialResourceStrings.Format_Invalid_MoneyString, s.ToString()))
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

        // ISO prefix: "USD 19.99" — three-letter code, whitespace, amount. Any Unicode whitespace separates the code
        // from the amount, so localized output using a non-breaking space (as many number formatters emit) parses.
        if (trimmed.Length >= 5 && char.IsWhiteSpace(trimmed[3])
            && IsUppercaseAscii(trimmed[0]) && IsUppercaseAscii(trimmed[1]) && IsUppercaseAscii(trimmed[2]))
        {
            string iso = trimmed[..3].ToString();
            ReadOnlySpan<char> numericPart = trimmed[4..].TrimStart();
            return TryComposeWithCulture(numericPart, iso, provider, out result);
        }

        // ISO suffix: "19.99 USD".
        if (trimmed.Length >= 5 && char.IsWhiteSpace(trimmed[^4])
            && IsUppercaseAscii(trimmed[^3]) && IsUppercaseAscii(trimmed[^2]) && IsUppercaseAscii(trimmed[^1]))
        {
            string iso = trimmed[^3..].ToString();
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

        // The strict parser only yields a value for currencies the ambient lookup knows and that map to a stored
        // CurrencyCode; an unmapped code is a parse failure here rather than an exception.
        if (!CurrencyResolution.Contains(iso) || !CurrencyInfo.TryGetCurrencyCode(iso, out CurrencyCode code))
            return false;

        IFormatProvider effective = provider ?? CultureInfo.CurrentCulture;
        if (!decimal.TryParse(numericPart, NumberStyles.Number | NumberStyles.AllowLeadingSign, effective, out decimal amount))
            return false;

        // The parsed decimal preserves the printed fractional-digit count as its scale. When the text carries more
        // fractional digits than the currency's registered minor units - a unit price such as "USD 12.345678" - the
        // value is constructed at that finer scale so parsing is the inverse of the round-trip ("R") format; the
        // ordinary registry-precision path would silently round the sub-minor-unit digits away. Text at or below the
        // registered precision keeps the historical behaviour and reports the registry precision.
        result = amount.Scale > CurrencyInfo.FromCurrencyCode(code).MinorUnits
            ? FromExplicitScale(amount, code, amount.Scale)
            : new Money(amount, code);
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
