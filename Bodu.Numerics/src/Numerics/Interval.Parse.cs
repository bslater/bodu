// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Interval.Parse.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Numerics;

namespace Bodu.Numerics;

public readonly partial struct Interval<T> :
    IParsable<Interval<T>>,
    ISpanParsable<Interval<T>>
{
    /// <summary>
    /// Parses an interval from its ISO 31-11 bracket-notation string representation.
    /// </summary>
    /// <param name="s">The text to parse — for example <c>"[1, 5)"</c>, <c>"(0, 1)"</c>, or <c>"∅"</c>.</param>
    /// <param name="provider">The culture used to parse each endpoint.</param>
    /// <returns>The parsed interval.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="s" /> is <see langword="null" />.</exception>
    /// <exception cref="FormatException">Thrown when <paramref name="s" /> is not a valid interval representation.</exception>
    public static Interval<T> Parse(string s, IFormatProvider? provider)
    {
        ArgumentNullException.ThrowIfNull(s);
        return Parse(s.AsSpan(), provider);
    }

    /// <summary>
    /// Parses an interval from its ISO 31-11 bracket-notation span representation.
    /// </summary>
    /// <param name="s">The text to parse — for example <c>"[1, 5)"</c>, <c>"(0, 1)"</c>, or <c>"∅"</c>.</param>
    /// <param name="provider">The culture used to parse each endpoint.</param>
    /// <returns>The parsed interval.</returns>
    /// <exception cref="FormatException">Thrown when <paramref name="s" /> is not a valid interval representation.</exception>
    public static Interval<T> Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
    {
        if (!TryParse(s, provider, out Interval<T> value))
            throw new FormatException($"The value '{s.ToString()}' is not a valid {nameof(Interval<T>)}<{typeof(T).Name}> representation.");

        return value;
    }

    /// <summary>
    /// Attempts to parse an interval from its ISO 31-11 bracket-notation string representation.
    /// </summary>
    /// <param name="s">The text to parse.</param>
    /// <param name="provider">The culture used to parse each endpoint.</param>
    /// <param name="result">
    /// When this method returns <see langword="true" />, the parsed interval; otherwise the default value.
    /// </param>
    /// <returns><see langword="true" /> when parsing succeeded; otherwise <see langword="false" />.</returns>
    public static bool TryParse(string? s, IFormatProvider? provider, out Interval<T> result)
    {
        if (s is null)
        {
            result = default;
            return false;
        }

        return TryParse(s.AsSpan(), provider, out result);
    }

    /// <summary>
    /// Attempts to parse an interval from its ISO 31-11 bracket-notation span representation.
    /// </summary>
    /// <param name="s">The text to parse.</param>
    /// <param name="provider">The culture used to parse each endpoint.</param>
    /// <param name="result">
    /// When this method returns <see langword="true" />, the parsed interval; otherwise the default value.
    /// </param>
    /// <returns><see langword="true" /> when parsing succeeded; otherwise <see langword="false" />.</returns>
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Interval<T> result)
    {
        ReadOnlySpan<char> trimmed = s.Trim();

        if (trimmed.IsEmpty)
        {
            result = default;
            return false;
        }

        // The empty-interval glyph (U+2205) parses to Empty regardless of culture.
        if (trimmed.Length == 1 && trimmed[0] == '∅')
        {
            result = Empty;
            return true;
        }

        if (trimmed.Length < 5)
        {
            // Minimum non-empty interval is "[a,b]" — five characters.
            result = default;
            return false;
        }

        char openBracket = trimmed[0];
        char closeBracket = trimmed[^1];

        bool lowerInclusive;
        switch (openBracket)
        {
            case '[': lowerInclusive = true; break;
            case '(': lowerInclusive = false; break;
            default:
                result = default;
                return false;
        }

        bool upperInclusive;
        switch (closeBracket)
        {
            case ']': upperInclusive = true; break;
            case ')': upperInclusive = false; break;
            default:
                result = default;
                return false;
        }

        ReadOnlySpan<char> body = trimmed[1..^1];
        int commaIndex = body.IndexOf(',');
        if (commaIndex < 0)
        {
            result = default;
            return false;
        }

        ReadOnlySpan<char> lowerText = body[..commaIndex].Trim();
        ReadOnlySpan<char> upperText = body[(commaIndex + 1)..].Trim();

        if (lowerText.IsEmpty || upperText.IsEmpty)
        {
            result = default;
            return false;
        }

        IFormatProvider effectiveProvider = provider ?? CultureInfo.CurrentCulture;
        if (!T.TryParse(lowerText, NumberStyles.Any, effectiveProvider, out T? lower)
            || !T.TryParse(upperText, NumberStyles.Any, effectiveProvider, out T? upper))
        {
            result = default;
            return false;
        }

        result = new Interval<T>(lower, upper, lowerInclusive, upperInclusive);
        return true;
    }
}
