// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Interval{T}.Parse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Numerics;

public readonly partial struct Interval<T> :
    IParsable<Interval<T>>,
    ISpanParsable<Interval<T>>
{
    /// <summary>
    /// Parses an interval from its ISO 31-11 bracket-notation string representation.
    /// </summary>
    /// <param name="s">The text to parse - for example <c>"[1, 5)"</c>, <c>"(0, 1)"</c>, or <c>"∅"</c>.</param>
    /// <param name="provider">The culture used to parse each endpoint.</param>
    /// <returns>The parsed interval.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="s" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="FormatException">
    /// Thrown when <paramref name="s" /> is not a valid interval representation.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The endpoints are separated by a comma, or by a semicolon when the decimal separator of
    /// <paramref name="provider" /> is a comma, as <see cref="ToString(string?, IFormatProvider?)" /> writes them.
    /// </para>
    /// <para>
    /// An infinity token beside an open bracket denotes an unbounded side: <c>-∞</c>, <c>-Infinity</c> or <c>-inf</c>
    /// for the lower side, and <c>+∞</c>, <c>∞</c>, <c>+Infinity</c>, <c>Infinity</c>, <c>+inf</c> or <c>inf</c> for
    /// the upper side, the words in any case. Beside a closed bracket the same text is parsed as an endpoint value of
    /// <typeparamref name="T" /> under <paramref name="provider" />, because an unbounded side is never closed. A
    /// bounded interval with an infinite endpoint on a closed side, such as <c>[0, Infinity]</c> over
    /// <see cref="double" />, therefore reads back as itself, while an infinite endpoint on an open side reads back as
    /// an unbounded side, which holds the same finite values but is not equal to the bounded interval.
    /// </para>
    /// <para>
    /// A half-bounded text whose bounded endpoint is the infinity on its unbounded side, such as <c>[Infinity, +∞)</c>,
    /// holds no value and reads as <see cref="Empty" />, as the half-bounded factories return it.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// // ISO 31-11 bracket notation; the bracket style selects endpoint inclusivity.
    /// var closedOpen = Interval<int>.Parse("[1, 5)", null);   // closed lower, open upper
    /// var open = Interval<int>.Parse("(0, 1)", null);         // both endpoints excluded
    /// var empty = Interval<int>.Parse("∅", null);             // the empty interval
    ///
    /// // Round-trip: a parsed interval formats back to its bracket notation.
    /// string text = closedOpen.ToString();                    // "[1, 5)"
    ///
    /// if (!Interval<int>.TryParse("1..5", null, out var parsed))
    /// {
    ///     // reached - "1..5" is not ISO 31-11 notation
    /// }
    ///]]>
    /// </code>
    /// </example>
    public static Interval<T> Parse(string s, IFormatProvider? provider)
    {
        ThrowHelper.ThrowIfNull(s);
        return Parse(s.AsSpan(), provider);
    }

    /// <summary>
    /// Parses an interval from its ISO 31-11 bracket-notation string representation using the current culture.
    /// </summary>
    /// <param name="s">The text to parse - for example <c>"[1, 5)"</c>, <c>"(0, 1)"</c>, or <c>"∅"</c>.</param>
    /// <returns>The parsed interval.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="s" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="FormatException">
    /// Thrown when <paramref name="s" /> is not a valid interval representation.
    /// </exception>
    public static Interval<T> Parse(string s) =>
        Parse(s, null);

    /// <summary>
    /// Parses an interval from its ISO 31-11 bracket-notation span representation.
    /// </summary>
    /// <param name="s">The text to parse - for example <c>"[1, 5)"</c>, <c>"(0, 1)"</c>, or <c>"∅"</c>.</param>
    /// <param name="provider">The culture used to parse each endpoint.</param>
    /// <returns>The parsed interval.</returns>
    /// <exception cref="FormatException">
    /// Thrown when <paramref name="s" /> is not a valid interval representation.
    /// </exception>
    public static Interval<T> Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
    {
        return !TryParse(s, provider, out Interval<T> value)
            ? throw new FormatException(
                string.Format(CultureInfo.CurrentCulture, NumericsResourceStrings.Format_Invalid_IntervalText, s.ToString(), typeof(T).Name))
            : value;
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
    /// Attempts to parse an interval from its ISO 31-11 bracket-notation string representation using the current
    /// culture.
    /// </summary>
    /// <param name="s">The text to parse.</param>
    /// <param name="result">
    /// When this method returns <see langword="true" />, the parsed interval; otherwise the default value.
    /// </param>
    /// <returns><see langword="true" /> when parsing succeeded; otherwise <see langword="false" />.</returns>
    public static bool TryParse(string? s, out Interval<T> result) =>
        TryParse(s, null, out result);

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
            // Minimum non-empty interval is "[a,b]" - five characters.
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

        // The endpoint separator mirrors the formatter: a semicolon under comma-decimal cultures, otherwise a comma.
        char separator = NumberFormatInfo.GetInstance(provider).NumberDecimalSeparator == "," ? ';' : ',';

        ReadOnlySpan<char> body = trimmed[1..^1];
        int separatorIndex = body.IndexOf(separator);
        if (separatorIndex < 0)
        {
            result = default;
            return false;
        }

        ReadOnlySpan<char> lowerText = body[..separatorIndex].Trim();
        ReadOnlySpan<char> upperText = body[(separatorIndex + 1)..].Trim();

        if (lowerText.IsEmpty || upperText.IsEmpty)
        {
            result = default;
            return false;
        }

        IFormatProvider effectiveProvider = provider ?? CultureInfo.CurrentCulture;
        byte flags = 0;
        T lower = T.Zero;
        T upper = T.Zero;

        // An infinity token beside an open bracket is an unbounded side. An unbounded side is never closed, so beside a
        // closed bracket the token can only be an infinite endpoint value, which T parses if it has one: that is how a
        // bounded interval such as [0, Infinity] over double reads back as itself.
        if (!lowerInclusive && IsNegativeInfinityToken(lowerText))
        {
            flags |= LowerUnboundedFlag;
        }
        else
        {
            // NaN is rejected to mirror the public constructor's endpoint guard: parsing must not construct an
            // interval the constructor would refuse.
            if (!T.TryParse(lowerText, NumberStyles.Any, effectiveProvider, out T? parsedLower) || T.IsNaN(parsedLower))
            {
                result = default;
                return false;
            }

            lower = parsedLower;
            if (lowerInclusive)
                flags |= LowerInclusiveFlag;
        }

        if (!upperInclusive && IsPositiveInfinityToken(upperText))
        {
            flags |= UpperUnboundedFlag;
        }
        else
        {
            if (!T.TryParse(upperText, NumberStyles.Any, effectiveProvider, out T? parsedUpper) || T.IsNaN(parsedUpper))
            {
                result = default;
                return false;
            }

            upper = parsedUpper;
            if (upperInclusive)
                flags |= UpperInclusiveFlag;
        }

        // A half-bounded result goes through its factory, which returns Empty when the bounded endpoint is the infinity
        // on the unbounded side (as in "[Infinity, +∞)"), since that side holds no infinity.
        result = (flags & (LowerUnboundedFlag | UpperUnboundedFlag)) switch
        {
            LowerUnboundedFlag | UpperUnboundedFlag => All,
            UpperUnboundedFlag => lowerInclusive ? AtLeast(lower) : GreaterThan(lower),
            LowerUnboundedFlag => upperInclusive ? AtMost(upper) : LessThan(upper),
            _ => new Interval<T>(lower, upper, flags),
        };
        return true;
    }

    /// <summary>
    /// Determines whether <paramref name="s" /> is a negative-infinity token (<c>-&#x221E;</c>, <c>-Infinity</c>, or
    /// <c>-inf</c>, case-insensitive).
    /// </summary>
    /// <param name="s">The endpoint text.</param>
    /// <returns><see langword="true" /> when the text denotes negative infinity.</returns>
    private static bool IsNegativeInfinityToken(ReadOnlySpan<char> s) =>
        s.SequenceEqual(NegativeInfinityText)
        || s.Equals("-Infinity", StringComparison.OrdinalIgnoreCase)
        || s.Equals("-inf", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Determines whether <paramref name="s" /> is a positive-infinity token (<c>+&#x221E;</c>, <c>&#x221E;</c>,
    /// <c>Infinity</c>, or <c>inf</c>, case-insensitive).
    /// </summary>
    /// <param name="s">The endpoint text.</param>
    /// <returns><see langword="true" /> when the text denotes positive infinity.</returns>
    private static bool IsPositiveInfinityToken(ReadOnlySpan<char> s) =>
        s.SequenceEqual(PositiveInfinityText)
        || s.SequenceEqual("∞")
        || s.Equals("+Infinity", StringComparison.OrdinalIgnoreCase)
        || s.Equals("Infinity", StringComparison.OrdinalIgnoreCase)
        || s.Equals("+inf", StringComparison.OrdinalIgnoreCase)
        || s.Equals("inf", StringComparison.OrdinalIgnoreCase);
}
