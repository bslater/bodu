// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyValue.Parse.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Numerics;

public readonly partial struct MoneyValue :
    IParsable<MoneyValue>,
    ISpanParsable<MoneyValue>
{
    /// <summary>
    /// Parses a <see cref="MoneyValue" /> from <c>"&lt;ISO&gt; &lt;amount&gt;"</c> or
    /// <c>"&lt;amount&gt; &lt;ISO&gt;"</c>.
    /// </summary>
    /// <param name="s">The text to parse.</param>
    /// <param name="provider">The culture used to interpret the numeric component.</param>
    /// <returns>The parsed value.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="s" /> is <see langword="null" />.</exception>
    /// <exception cref="FormatException">The input is not a valid <see cref="MoneyValue" /> representation.</exception>
    public static MoneyValue Parse(string s, IFormatProvider? provider)
    {
        ThrowHelper.ThrowIfNull(s);
        return Parse(s.AsSpan(), provider);
    }

    /// <summary>
    /// Parses a <see cref="MoneyValue" /> from a span representation.
    /// </summary>
    /// <param name="s">The text to parse.</param>
    /// <param name="provider">The culture used to interpret the numeric component.</param>
    /// <returns>The parsed value.</returns>
    /// <exception cref="FormatException">The input is not a valid representation.</exception>
    public static MoneyValue Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
    {
        if (!TryParse(s, provider, out MoneyValue result))
            throw new FormatException($"The input '{s.ToString()}' is not a valid MoneyValue representation.");
        return result;
    }

    /// <summary>
    /// Attempts to parse a <see cref="MoneyValue" /> from a string.
    /// </summary>
    /// <param name="s">The text to parse.</param>
    /// <param name="provider">The culture provider.</param>
    /// <param name="result">When this method returns <see langword="true" />, the parsed value; otherwise the default.</param>
    /// <returns><see langword="true" /> on success.</returns>
    public static bool TryParse(string? s, IFormatProvider? provider, out MoneyValue result)
    {
        if (s is null)
        {
            result = default;
            return false;
        }

        return TryParse(s.AsSpan(), provider, out result);
    }

    /// <summary>
    /// Attempts to parse a <see cref="MoneyValue" /> from a span.
    /// </summary>
    /// <param name="s">The text to parse.</param>
    /// <param name="provider">The culture provider.</param>
    /// <param name="result">The parsed value or default.</param>
    /// <returns><see langword="true" /> on success.</returns>
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out MoneyValue result)
    {
        result = default;

        ReadOnlySpan<char> trimmed = s.Trim();
        if (trimmed.IsEmpty)
            return false;

        // ISO prefix: "USD 19.99" — three-letter code, space, amount.
        if (trimmed.Length >= 5 && trimmed[3] == ' '
            && IsUppercaseAscii(trimmed[0]) && IsUppercaseAscii(trimmed[1]) && IsUppercaseAscii(trimmed[2]))
        {
            string iso = trimmed[..3].ToString();
            ReadOnlySpan<char> numericPart = trimmed[4..].TrimStart();
            return TryComposeWithCulture(numericPart, iso, provider, out result);
        }

        // ISO suffix: "19.99 USD".
        if (trimmed.Length >= 5 && trimmed[^4] == ' '
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
    /// Parses the numeric portion and constructs the <see cref="MoneyValue" /> when both parts are valid.
    /// </summary>
    /// <param name="numericPart">The numeric span.</param>
    /// <param name="iso">The ISO code.</param>
    /// <param name="provider">The culture provider.</param>
    /// <param name="result">The constructed value.</param>
    /// <returns><see langword="true" /> on success.</returns>
    private static bool TryComposeWithCulture(ReadOnlySpan<char> numericPart, string iso, IFormatProvider? provider, out MoneyValue result)
    {
        result = default;
        if (numericPart.IsEmpty) return false;

        IFormatProvider effective = provider ?? CultureInfo.CurrentCulture;
        if (!decimal.TryParse(numericPart, NumberStyles.Number | NumberStyles.AllowLeadingSign, effective, out decimal amount))
            return false;

        result = new MoneyValue(amount, iso);
        return true;
    }

    /// <summary>
    /// Determines whether a character is an ASCII uppercase letter.
    /// </summary>
    /// <param name="c">The character to test.</param>
    /// <returns><see langword="true" /> when <paramref name="c" /> is in [A-Z].</returns>
    private static bool IsUppercaseAscii(char c) =>
        c >= 'A' && c <= 'Z';
}
