// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Complex{T}.Parse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Numerics;

public readonly partial struct Complex<T> :
    IParsable<Complex<T>>,
    ISpanParsable<Complex<T>>
{
    /// <summary>
    /// Parses a string into a <see cref="Complex{T}" />.
    /// </summary>
    /// <param name="s">The string to parse.</param>
    /// <returns>The complex value the string represents.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="s" /> is <see langword="null" />.</exception>
    /// <exception cref="FormatException">Thrown if <paramref name="s" /> is not a valid complex value.</exception>
    public static Complex<T> Parse(string s) =>
        Parse(s, null);

    /// <summary>
    /// Parses a string into a <see cref="Complex{T}" /> using the specified culture.
    /// </summary>
    /// <param name="s">The string to parse.</param>
    /// <param name="provider">The culture used to interpret the numeric components.</param>
    /// <returns>The complex value the string represents.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="s" /> is <see langword="null" />.</exception>
    /// <exception cref="FormatException">Thrown if <paramref name="s" /> is not a valid complex value.</exception>
    public static Complex<T> Parse(string s, IFormatProvider? provider)
    {
        ThrowHelper.ThrowIfNull(s);

        return Parse(s.AsSpan(), provider);
    }

    /// <summary>
    /// Parses a character span into a <see cref="Complex{T}" /> using the specified culture.
    /// </summary>
    /// <param name="s">The span to parse.</param>
    /// <param name="provider">The culture used to interpret the numeric components.</param>
    /// <returns>The complex value the span represents.</returns>
    /// <exception cref="FormatException">Thrown if <paramref name="s" /> is not a valid complex value.</exception>
    public static Complex<T> Parse(ReadOnlySpan<char> s, IFormatProvider? provider) =>
        TryParseCore(s, provider, out Complex<T> result)
            ? result
            : throw new FormatException(
                string.Format(CultureInfo.CurrentCulture, NumericsResourceStrings.Format_Invalid_ComplexText, s.ToString()));

    /// <summary>
    /// Attempts to parse a string into a <see cref="Complex{T}" />.
    /// </summary>
    /// <param name="s">The string to parse.</param>
    /// <param name="result">When this method returns, contains the parsed value, or zero on failure.</param>
    /// <returns><see langword="true" /> if parsing succeeded; otherwise, <see langword="false" />.</returns>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// // Parse the canonical bracketed form and a bare real value.
    /// var z = Complex<double>.Parse("<3; 4>", null);         // 3 + 4i
    /// var real = Complex<double>.Parse("5", null);           // 5 + 0i
    ///
    /// // Round-trip: the parsed value formats back to the same canonical text.
    /// string text = z.ToString();                            // "<3; 4>"
    ///]]>
    /// </code>
    /// </example>
    public static bool TryParse(string? s, out Complex<T> result) =>
        TryParse(s, null, out result);

    /// <summary>
    /// Attempts to parse a string into a <see cref="Complex{T}" /> using the specified culture.
    /// </summary>
    /// <param name="s">The string to parse.</param>
    /// <param name="provider">The culture used to interpret the numeric components.</param>
    /// <param name="result">When this method returns, contains the parsed value, or zero on failure.</param>
    /// <returns><see langword="true" /> if parsing succeeded; otherwise, <see langword="false" />.</returns>
    public static bool TryParse(string? s, IFormatProvider? provider, out Complex<T> result)
    {
        if (s is null)
        {
            result = default;
            return false;
        }

        return TryParseCore(s.AsSpan(), provider, out result);
    }

    /// <summary>
    /// Attempts to parse a character span into a <see cref="Complex{T}" /> using the specified culture.
    /// </summary>
    /// <param name="s">The span to parse.</param>
    /// <param name="provider">The culture used to interpret the numeric components.</param>
    /// <param name="result">When this method returns, contains the parsed value, or zero on failure.</param>
    /// <returns><see langword="true" /> if parsing succeeded; otherwise, <see langword="false" />.</returns>
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Complex<T> result) =>
        TryParseCore(s, provider, out result);

    /// <summary>
    /// Performs the shared parsing logic for every string and span entry point.
    /// </summary>
    /// <param name="s">The text to parse.</param>
    /// <param name="provider">The culture used to interpret the numeric components.</param>
    /// <param name="result">When this method returns, contains the parsed value, or zero on failure.</param>
    /// <returns><see langword="true" /> if parsing succeeded; otherwise, <see langword="false" />.</returns>
    /// <remarks>
    /// Accepts the canonical bracketed form <c>&lt;real; imaginary&gt;</c> and, as a convenience, a bare real value
    /// that is placed on the real axis.
    /// </remarks>
    private static bool TryParseCore(ReadOnlySpan<char> s, IFormatProvider? provider, out Complex<T> result)
    {
        const NumberStyles style = NumberStyles.Float;

        result = default;

        s = s.Trim();
        if (s.IsEmpty)
            return false;

        if (s[0] == '<')
        {
            if (s[^1] != '>')
                return false;

            ReadOnlySpan<char> inner = s[1..^1];
            int separator = inner.IndexOf(';');
            if (separator < 0)
                return false;

            ReadOnlySpan<char> realText = inner[..separator].Trim();
            ReadOnlySpan<char> imaginaryText = inner.Slice(separator + 1).Trim();

            if (T.TryParse(realText, style, provider, out T? real)
                && T.TryParse(imaginaryText, style, provider, out T? imaginary))
            {
                result = new Complex<T>(real, imaginary);
                return true;
            }

            return false;
        }

        if (T.TryParse(s, style, provider, out T? realOnly))
        {
            result = new Complex<T>(realOnly, T.Zero);
            return true;
        }

        return false;
    }
}
