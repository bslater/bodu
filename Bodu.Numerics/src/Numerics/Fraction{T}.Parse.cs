// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Fraction{T}.Parse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Numerics;

namespace Bodu.Numerics;

public readonly partial struct Fraction<T> :
    IParsable<Fraction<T>>,
    ISpanParsable<Fraction<T>>
{
    /// <summary>Maps a Unicode vulgar-fraction glyph to its proper-fraction numerator and denominator.</summary>
    private static readonly Dictionary<char, (int Numerator, int Denominator)> s_vulgarGlyphs = new()
    {
        ['½'] = (1, 2),
        ['⅓'] = (1, 3),
        ['⅔'] = (2, 3),
        ['¼'] = (1, 4),
        ['¾'] = (3, 4),
        ['⅕'] = (1, 5),
        ['⅖'] = (2, 5),
        ['⅗'] = (3, 5),
        ['⅘'] = (4, 5),
        ['⅙'] = (1, 6),
        ['⅚'] = (5, 6),
        ['⅐'] = (1, 7),
        ['⅛'] = (1, 8),
        ['⅜'] = (3, 8),
        ['⅝'] = (5, 8),
        ['⅞'] = (7, 8),
        ['⅑'] = (1, 9),
        ['⅒'] = (1, 10),
    };

    /// <summary>
    /// Parses a string into a <see cref="Fraction{T}" />.
    /// </summary>
    /// <param name="s">The string to parse.</param>
    /// <returns>The rational value the string represents.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="s" /> is <see langword="null" />.</exception>
    /// <exception cref="FormatException">Thrown if <paramref name="s" /> is not a valid fraction.</exception>
    public static Fraction<T> Parse(string s) =>
        Parse(s, null);

    /// <summary>
    /// Parses a string into a <see cref="Fraction{T}" /> using the specified culture.
    /// </summary>
    /// <param name="s">The string to parse.</param>
    /// <param name="provider">The culture used to interpret the numeric components.</param>
    /// <returns>The rational value the string represents.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="s" /> is <see langword="null" />.</exception>
    /// <exception cref="FormatException">Thrown if <paramref name="s" /> is not a valid fraction.</exception>
    public static Fraction<T> Parse(string s, IFormatProvider? provider)
    {
        ThrowHelper.ThrowIfNull(s);

        return Parse(s.AsSpan(), provider);
    }

    /// <summary>
    /// Parses a character span into a <see cref="Fraction{T}" /> using the specified culture.
    /// </summary>
    /// <param name="s">The span to parse.</param>
    /// <param name="provider">The culture used to interpret the numeric components.</param>
    /// <returns>The rational value the span represents.</returns>
    /// <exception cref="FormatException">Thrown if <paramref name="s" /> is not a valid fraction.</exception>
    public static Fraction<T> Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
    {
        return TryParseCore(s, provider, out Fraction<T> result)
            ? result
            : throw new FormatException(
                string.Format(CultureInfo.CurrentCulture, NumericsResourceStrings.Format_Invalid_FractionText, s.ToString()));
    }

    /// <summary>
    /// Attempts to parse a string into a <see cref="Fraction{T}" />.
    /// </summary>
    /// <param name="s">The string to parse.</param>
    /// <param name="result">When this method returns, contains the parsed value, or zero on failure.</param>
    /// <returns><see langword="true" /> if parsing succeeded; otherwise, <see langword="false" />.</returns>
    public static bool TryParse(string? s, out Fraction<T> result) =>
        TryParse(s, null, out result);

    /// <summary>
    /// Attempts to parse a string into a <see cref="Fraction{T}" /> using the specified culture.
    /// </summary>
    /// <param name="s">The string to parse.</param>
    /// <param name="provider">The culture used to interpret the numeric components.</param>
    /// <param name="result">When this method returns, contains the parsed value, or zero on failure.</param>
    /// <returns><see langword="true" /> if parsing succeeded; otherwise, <see langword="false" />.</returns>
    public static bool TryParse(string? s, IFormatProvider? provider, out Fraction<T> result)
    {
        if (s is null)
        {
            result = default;
            return false;
        }

        return TryParseCore(s.AsSpan(), provider, out result);
    }

    /// <summary>
    /// Attempts to parse a character span into a <see cref="Fraction{T}" /> using the specified culture.
    /// </summary>
    /// <param name="s">The span to parse.</param>
    /// <param name="provider">The culture used to interpret the numeric components.</param>
    /// <param name="result">When this method returns, contains the parsed value, or zero on failure.</param>
    /// <returns><see langword="true" /> if parsing succeeded; otherwise, <see langword="false" />.</returns>
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Fraction<T> result) =>
        TryParseCore(s, provider, out result);

    /// <summary>
    /// Performs the shared parsing logic for every string and span entry point.
    /// </summary>
    /// <param name="s">The text to parse.</param>
    /// <param name="provider">The culture used to interpret the numeric components.</param>
    /// <param name="result">When this method returns, contains the parsed value, or zero on failure.</param>
    /// <returns><see langword="true" /> if parsing succeeded; otherwise, <see langword="false" />.</returns>
    private static bool TryParseCore(ReadOnlySpan<char> s, IFormatProvider? provider, out Fraction<T> result)
    {
        result = default;

        s = s.Trim();
        if (s.IsEmpty)
            return false;

        bool percent = s[^1] == '%';
        if (percent)
        {
            s = s[..^1].Trim();
            if (s.IsEmpty)
                return false;
        }

        bool negative = false;
        if (s[0] == '-')
        {
            negative = true;
            s = s[1..].TrimStart();
        }
        else if (s[0] == '+')
        {
            s = s[1..].TrimStart();
        }

        if (s.IsEmpty)
            return false;

        if (!TryParseMagnitude(s, provider, out BigInteger numerator, out BigInteger denominator))
            return false;

        if (denominator.IsZero)
            return false;

        if (negative)
            numerator = -numerator;

        if (percent)
            denominator *= 100;

        try
        {
            result = FromBigInteger(numerator, denominator);
            return true;
        }
        catch (OverflowException)
        {
            return false;
        }
    }

    /// <summary>
    /// Parses the unsigned magnitude of a fraction expressed as an integer, a ratio, a mixed number, or a Unicode
    /// vulgar fraction.
    /// </summary>
    /// <param name="s">The non-empty, sign-stripped text to parse.</param>
    /// <param name="provider">The culture used to interpret the numeric components.</param>
    /// <param name="numerator">When this method returns, contains the parsed numerator.</param>
    /// <param name="denominator">When this method returns, contains the parsed denominator.</param>
    /// <returns><see langword="true" /> if the magnitude was parsed; otherwise, <see langword="false" />.</returns>
    private static bool TryParseMagnitude(ReadOnlySpan<char> s, IFormatProvider? provider, out BigInteger numerator, out BigInteger denominator)
    {
        numerator = BigInteger.Zero;
        denominator = BigInteger.One;

        if (s_vulgarGlyphs.TryGetValue(s[^1], out (int Numerator, int Denominator) glyph))
        {
            ReadOnlySpan<char> wholeText = s[..^1].Trim();
            BigInteger whole = BigInteger.Zero;
            if (!wholeText.IsEmpty && !BigInteger.TryParse(wholeText, NumberStyles.None, provider, out whole))
                return false;

            denominator = glyph.Denominator;
            numerator = (whole * denominator) + glyph.Numerator;
            return true;
        }

        int slash = s.IndexOf('/');
        if (slash < 0)
            return BigInteger.TryParse(s, NumberStyles.None, provider, out numerator);

        ReadOnlySpan<char> leftText = s[..slash].Trim();
        ReadOnlySpan<char> rightText = s.Slice(slash + 1).Trim();
        if (leftText.IsEmpty || rightText.IsEmpty)
            return false;

        if (!BigInteger.TryParse(rightText, NumberStyles.None, provider, out denominator))
            return false;

        int space = leftText.IndexOfAny(' ', '\t');
        if (space < 0)
            return BigInteger.TryParse(leftText, NumberStyles.None, provider, out numerator);

        ReadOnlySpan<char> wholePart = leftText[..space].Trim();
        ReadOnlySpan<char> fractionPart = leftText.Slice(space + 1).Trim();
        if (!BigInteger.TryParse(wholePart, NumberStyles.None, provider, out BigInteger mixedWhole)
            || !BigInteger.TryParse(fractionPart, NumberStyles.None, provider, out BigInteger mixedNumerator))
        {
            return false;
        }

        numerator = (mixedWhole * denominator) + mixedNumerator;
        return true;
    }
}
