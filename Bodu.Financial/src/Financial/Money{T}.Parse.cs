// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Money{T}.Parse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Financial;

public readonly partial struct Money<TCurrency> :
    IParsable<Money<TCurrency>>,
    ISpanParsable<Money<TCurrency>>
{
    /// <summary>
    /// Parses a monetary value from its string representation.
    /// </summary>
    /// <param name="s">
    /// The string to parse. See <see cref="TryParse(ReadOnlySpan{char}, IFormatProvider?, out Money{TCurrency})" /> for
    /// accepted forms.
    /// </param>
    /// <param name="provider">The culture used to parse the numeric component.</param>
    /// <returns>The parsed amount.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="s" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="FormatException">Thrown when <paramref name="s" /> is not a valid representation.</exception>
    public static Money<TCurrency> Parse(string s, IFormatProvider? provider)
    {
        ThrowHelper.ThrowIfNull(s);

        return Parse(s.AsSpan(), provider);
    }

    /// <summary>
    /// Parses a monetary value from its span representation.
    /// </summary>
    /// <param name="s">The span to parse.</param>
    /// <param name="provider">The culture used to parse the numeric component.</param>
    /// <returns>The parsed amount.</returns>
    /// <exception cref="FormatException">Thrown when <paramref name="s" /> is not a valid representation.</exception>
    public static Money<TCurrency> Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
    {
        return !TryParse(s, provider, out Money<TCurrency> result)
            ? throw new FormatException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    FinancialResourceStrings.Format_Invalid_MoneyOfTCurrencyString,
                    s.ToString(),
                    typeof(TCurrency).Name))
            : result;
    }

    /// <summary>
    /// Attempts to parse a monetary value from its string representation.
    /// </summary>
    /// <param name="s">The string to parse.</param>
    /// <param name="provider">The culture used to parse the numeric component.</param>
    /// <param name="result">
    /// When this method returns <see langword="true" />, the parsed amount; otherwise the default value.
    /// </param>
    /// <returns><see langword="true" /> when parsing succeeded; otherwise <see langword="false" />.</returns>
    public static bool TryParse(string? s, IFormatProvider? provider, out Money<TCurrency> result)
    {
        if (s is null)
        {
            result = default;
            return false;
        }

        return TryParse(s.AsSpan(), provider, out result);
    }

    /// <summary>
    /// Attempts to parse a monetary value from its span representation.
    /// </summary>
    /// <param name="s">
    /// The span to parse. Accepted forms are a bare decimal (<c>"19.99"</c>), the ISO code followed by a decimal (
    /// <c>"USD 19.99"</c>), or a decimal followed by the ISO code (<c>"19.99 USD"</c>). When an ISO code is present it
    /// must match <typeparamref name="TCurrency" />.<see cref="ICurrency.IsoCode" /> exactly, including case. Currency
    /// symbols such as <c>$</c> are not accepted because they are ambiguous across currencies.
    /// </param>
    /// <param name="provider">The culture used to parse the numeric component.</param>
    /// <param name="result">
    /// When this method returns <see langword="true" />, the parsed amount; otherwise the default value.
    /// </param>
    /// <returns><see langword="true" /> when parsing succeeded; otherwise <see langword="false" />.</returns>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// using System.Globalization;
    /// using Bodu.Financial;
    /// using Bodu.Financial.Currencies;
    ///
    /// // A bare decimal, or the ISO code at either end, all parse to the same value.
    /// var a = Money<USD>.Parse("19.99", CultureInfo.InvariantCulture);
    /// var b = Money<USD>.Parse("USD 19.99", CultureInfo.InvariantCulture);
    ///
    /// // TryParse rejects a mismatched ISO code instead of throwing.
    /// bool ok = Money<USD>.TryParse("EUR 19.99", CultureInfo.InvariantCulture, out _);   // false
    ///]]>
    /// </code>
    /// </example>
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Money<TCurrency> result)
    {
        result = default;

        ReadOnlySpan<char> trimmed = s.Trim();
        if (trimmed.IsEmpty)
            return false;

        string isoCode = CurrencyMetadata<TCurrency>.Value.IsoCode;
        ReadOnlySpan<char> numericPart;

        // Grammar:
        //   amount               → bare decimal
        //   isoCode WS+ amount   → ISO-prefixed decimal
        //   amount WS+ isoCode   → ISO-suffixed decimal
        // where WS is a single ASCII space (no tab, no NBSP). Currency symbols ($, ¥) are rejected — they are
        // ambiguous across currencies and consumers needing locale-aware parsing should pre-strip.
        if (TryStripIsoPrefix(trimmed, isoCode, out numericPart)
            || TryStripIsoSuffix(trimmed, isoCode, out numericPart))
        {
            // ISO-tagged form — proceed to numeric parse.
        }
        else if (HasIsoShapedToken(trimmed))
        {
            // Letters are present and they don't match the expected ISO code at either end (or the format is
            // otherwise non-grammatical, e.g. "USD19.99" without space, "$19.99", "NaN"). Reject.
            return false;
        }
        else
        {
            numericPart = trimmed;
        }

        if (numericPart.IsEmpty)
            return false;

        IFormatProvider effectiveProvider = provider ?? CultureInfo.CurrentCulture;
        if (!decimal.TryParse(
                numericPart,
                NumberStyles.Number | NumberStyles.AllowLeadingSign,
                effectiveProvider,
                out decimal amount))
        {
            return false;
        }

        result = new Money<TCurrency>(amount);
        return true;
    }

    /// <summary>
    /// Strips a leading <paramref name="isoCode" /> followed by a single space from <paramref name="trimmed" />,
    /// returning the remaining numeric portion.
    /// </summary>
    /// <param name="trimmed">The trimmed input span.</param>
    /// <param name="isoCode">The expected ISO code.</param>
    /// <param name="numericPart">The numeric portion when the prefix matched.</param>
    /// <returns><see langword="true" /> when the input had the expected ISO-prefix shape.</returns>
    private static bool TryStripIsoPrefix(ReadOnlySpan<char> trimmed, string isoCode, out ReadOnlySpan<char> numericPart)
    {
        if (trimmed.Length > isoCode.Length
            && trimmed.StartsWith(isoCode, StringComparison.Ordinal)
            && trimmed[isoCode.Length] == ' ')
        {
            numericPart = trimmed[(isoCode.Length + 1)..];
            return true;
        }

        numericPart = default;
        return false;
    }

    /// <summary>
    /// Strips a trailing <paramref name="isoCode" /> preceded by a single space from <paramref name="trimmed" />.
    /// </summary>
    /// <param name="trimmed">The trimmed input span.</param>
    /// <param name="isoCode">The expected ISO code.</param>
    /// <param name="numericPart">The numeric portion when the suffix matched.</param>
    /// <returns><see langword="true" /> when the input had the expected ISO-suffix shape.</returns>
    private static bool TryStripIsoSuffix(ReadOnlySpan<char> trimmed, string isoCode, out ReadOnlySpan<char> numericPart)
    {
        if (trimmed.Length > isoCode.Length
            && trimmed.EndsWith(isoCode, StringComparison.Ordinal)
            && trimmed[^(isoCode.Length + 1)] == ' ')
        {
            numericPart = trimmed[..^(isoCode.Length + 1)];
            return true;
        }

        numericPart = default;
        return false;
    }

    /// <summary>
    /// Determines whether <paramref name="s" /> contains any character that is not a digit, sign, or number-format
    /// glyph permitted by <see cref="NumberStyles.Number" />. Used as a fast rejection check — the production parser
    /// delegates to <see cref="decimal.TryParse(ReadOnlySpan{char}, NumberStyles, IFormatProvider, out decimal)" /> to
    /// make the final accept/reject decision on the numeric portion.
    /// </summary>
    /// <param name="s">The text to scan.</param>
    /// <returns><see langword="true" /> if any letter or symbol is present.</returns>
    private static bool HasIsoShapedToken(ReadOnlySpan<char> s)
    {
        foreach (char c in s)
        {
            if (char.IsLetter(c))
                return true;
            if (c is '$' or '£' or '¥' or '€' or '¤')
                return true;
        }

        return false;
    }
}
