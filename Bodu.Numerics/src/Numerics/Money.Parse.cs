// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Money.Parse.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Numerics;

public readonly partial struct Money<TCurrency> :
    IParsable<Money<TCurrency>>,
    ISpanParsable<Money<TCurrency>>
{
    /// <summary>
    /// Parses a monetary value from its string representation.
    /// </summary>
    /// <param name="s">The string to parse. See <see cref="TryParse(ReadOnlySpan{char}, IFormatProvider?, out Money{TCurrency})" /> for accepted forms.</param>
    /// <param name="provider">The culture used to parse the numeric component.</param>
    /// <returns>The parsed amount.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="s" /> is <see langword="null" />.</exception>
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
        if (!TryParse(s, provider, out Money<TCurrency> result))
            throw new FormatException($"The input '{s.ToString()}' is not a valid Money<{typeof(TCurrency).Name}> representation.");

        return result;
    }

    /// <summary>
    /// Attempts to parse a monetary value from its string representation.
    /// </summary>
    /// <param name="s">The string to parse.</param>
    /// <param name="provider">The culture used to parse the numeric component.</param>
    /// <param name="result">When this method returns <see langword="true" />, the parsed amount; otherwise the default value.</param>
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
    /// The span to parse. Accepted forms are a bare decimal (<c>"19.99"</c>), the ISO code followed by a decimal
    /// (<c>"USD 19.99"</c>), or a decimal followed by the ISO code (<c>"19.99 USD"</c>). When an ISO code is
    /// present it must match <typeparamref name="TCurrency" />.<see cref="ICurrency.IsoCode" /> exactly, including
    /// case. Currency symbols such as <c>$</c> are not accepted because they are ambiguous across currencies.
    /// </param>
    /// <param name="provider">The culture used to parse the numeric component.</param>
    /// <param name="result">When this method returns <see langword="true" />, the parsed amount; otherwise the default value.</param>
    /// <returns><see langword="true" /> when parsing succeeded; otherwise <see langword="false" />.</returns>
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Money<TCurrency> result)
    {
        result = default;

        ReadOnlySpan<char> trimmed = s.Trim();
        if (trimmed.IsEmpty)
            return false;

        string isoCode = TCurrency.IsoCode;
        ReadOnlySpan<char> numericPart = trimmed;

        // ISO prefix: "USD 19.99".
        if (trimmed.Length > isoCode.Length
            && trimmed.StartsWith(isoCode, StringComparison.Ordinal)
            && char.IsWhiteSpace(trimmed[isoCode.Length]))
        {
            numericPart = trimmed[(isoCode.Length + 1)..].TrimStart();
        }
        else if (trimmed.Length > isoCode.Length
            && trimmed.EndsWith(isoCode, StringComparison.Ordinal)
            && char.IsWhiteSpace(trimmed[^(isoCode.Length + 1)]))
        {
            // ISO suffix: "19.99 USD".
            numericPart = trimmed[..^(isoCode.Length + 1)].TrimEnd();
        }
        else if (ContainsLetter(trimmed))
        {
            // Any letters present that aren't the matching ISO code → reject.
            return false;
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
    /// Determines whether <paramref name="s" /> contains any ASCII letter.
    /// </summary>
    /// <param name="s">The text to scan.</param>
    /// <returns><see langword="true" /> if any letter is present; otherwise <see langword="false" />.</returns>
    private static bool ContainsLetter(ReadOnlySpan<char> s)
    {
        foreach (char c in s)
        {
            if (char.IsLetter(c))
                return true;
        }

        return false;
    }
}
