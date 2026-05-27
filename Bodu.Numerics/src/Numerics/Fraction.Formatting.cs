// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Fraction.Formatting.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;
using System.Text;

namespace Bodu.Numerics;

public readonly partial struct Fraction<T> :
    IFormattable,
    ISpanFormattable,
    IUtf8SpanFormattable
{
    /// <summary>
    /// Maps a proper-fraction numerator and denominator to its Unicode vulgar-fraction glyph.
    /// </summary>
    private static readonly Dictionary<(int Numerator, int Denominator), char> s_vulgarFractions = new()
    {
        [(1, 2)] = '½',
        [(1, 3)] = '⅓',
        [(2, 3)] = '⅔',
        [(1, 4)] = '¼',
        [(3, 4)] = '¾',
        [(1, 5)] = '⅕',
        [(2, 5)] = '⅖',
        [(3, 5)] = '⅗',
        [(4, 5)] = '⅘',
        [(1, 6)] = '⅙',
        [(5, 6)] = '⅚',
        [(1, 7)] = '⅐',
        [(1, 8)] = '⅛',
        [(3, 8)] = '⅜',
        [(5, 8)] = '⅝',
        [(7, 8)] = '⅞',
        [(1, 9)] = '⅑',
        [(1, 10)] = '⅒',
    };

    /// <summary>
    /// Returns the default string representation of this rational value.
    /// </summary>
    /// <returns>The value formatted as <c>numerator/denominator</c>, or as a bare integer when whole.</returns>
    public override string ToString() =>
        Format(default, null);

    /// <summary>
    /// Returns a string representation of this rational value using the specified format.
    /// </summary>
    /// <param name="format">The format specifier: <c>G</c>, <c>M</c>, <c>U</c>, or <c>P</c>.</param>
    /// <returns>The value formatted according to <paramref name="format" />.</returns>
    /// <exception cref="FormatException">Thrown if <paramref name="format" /> is not a supported specifier.</exception>
    public string ToString(string? format) =>
        Format(format, null);

    /// <summary>
    /// Returns a string representation of this rational value using the specified format and culture.
    /// </summary>
    /// <param name="format">The format specifier: <c>G</c>, <c>M</c>, <c>U</c>, or <c>P</c>.</param>
    /// <param name="formatProvider">The culture used to render the numeric components.</param>
    /// <returns>The value formatted according to <paramref name="format" />.</returns>
    /// <exception cref="FormatException">Thrown if <paramref name="format" /> is not a supported specifier.</exception>
    public string ToString(string? format, IFormatProvider? formatProvider) =>
        Format(format, formatProvider);

    /// <summary>
    /// Attempts to format this rational value into the provided character span.
    /// </summary>
    /// <param name="destination">The span that receives the formatted characters.</param>
    /// <param name="charsWritten">When this method returns, contains the number of characters written.</param>
    /// <param name="format">The format specifier.</param>
    /// <param name="provider">The culture used to render the numeric components.</param>
    /// <returns><see langword="true" /> if formatting succeeded; otherwise, <see langword="false" />.</returns>
    /// <exception cref="FormatException">Thrown if <paramref name="format" /> is not a supported specifier.</exception>
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        var text = Format(format, provider);
        if (text.Length <= destination.Length)
        {
            text.CopyTo(destination);
            charsWritten = text.Length;
            return true;
        }

        charsWritten = 0;
        return false;
    }

    /// <summary>
    /// Attempts to format this rational value into the provided UTF-8 byte span.
    /// </summary>
    /// <param name="utf8Destination">The span that receives the formatted UTF-8 bytes.</param>
    /// <param name="bytesWritten">When this method returns, contains the number of bytes written.</param>
    /// <param name="format">The format specifier.</param>
    /// <param name="provider">The culture used to render the numeric components.</param>
    /// <returns><see langword="true" /> if formatting succeeded; otherwise, <see langword="false" />.</returns>
    /// <exception cref="FormatException">Thrown if <paramref name="format" /> is not a supported specifier.</exception>
    public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        var text = Format(format, provider);
        return Encoding.UTF8.TryGetBytes(text, utf8Destination, out bytesWritten);
    }

    /// <summary>
    /// Returns a string representation of this value using Unicode vulgar-fraction glyphs where available.
    /// </summary>
    /// <param name="provider">The culture used to render the numeric components.</param>
    /// <returns>The Unicode string representation of this value.</returns>
    public string ToUnicodeString(IFormatProvider? provider = null) =>
        FormatUnicode(provider);

    /// <summary>
    /// Returns a string representation of this value scaled to a percentage.
    /// </summary>
    /// <param name="provider">The culture used to render the numeric components.</param>
    /// <returns>The percentage string representation of this value.</returns>
    public string ToPercentString(IFormatProvider? provider = null) =>
        FormatPercent(provider);

    /// <summary>
    /// Returns a string representation of this value as a mixed number.
    /// </summary>
    /// <param name="provider">The culture used to render the numeric components.</param>
    /// <returns>The mixed-number string representation of this value.</returns>
    public string ToMixedString(IFormatProvider? provider = null) =>
        FormatMixed(provider);

    /// <summary>
    /// Returns a string representation of this value as a mixed number.
    /// </summary>
    /// <param name="provider">The culture used to render the numeric components.</param>
    /// <returns>The mixed-number string representation of this value.</returns>
    /// <remarks>
    /// This method is an alias for <see cref="ToMixedString" />.
    /// </remarks>
    public string ToMixedNumberString(IFormatProvider? provider = null) =>
        ToMixedString(provider);

    /// <summary>
    /// Formats this rational value according to the specified format specifier.
    /// </summary>
    /// <param name="format">The format specifier; an empty span selects the general format.</param>
    /// <param name="provider">The culture used to render the numeric components.</param>
    /// <returns>The formatted string representation.</returns>
    /// <exception cref="FormatException">Thrown if <paramref name="format" /> is not a supported specifier.</exception>
    private string Format(ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        var specifier = format.IsEmpty ? 'G' : char.ToUpperInvariant(format[0]);

        return specifier switch
        {
            'G' => FormatGeneral(provider),
            'M' => FormatMixed(provider),
            'U' => FormatUnicode(provider),
            'P' => FormatPercent(provider),
            _ => throw new FormatException($"The format string '{format}' is not supported."),
        };
    }

    /// <summary>
    /// Formats this value as <c>numerator/denominator</c>, or as a bare integer when whole.
    /// </summary>
    /// <param name="provider">The culture used to render the numeric components.</param>
    /// <returns>The general string representation.</returns>
    private string FormatGeneral(IFormatProvider? provider) =>
        BigDenominator.IsOne
            ? BigNumerator.ToString(provider)
            : $"{BigNumerator.ToString(provider)}/{BigDenominator.ToString(provider)}";

    /// <summary>
    /// Formats this value as a mixed number, separating the whole and fractional parts.
    /// </summary>
    /// <param name="provider">The culture used to render the numeric components.</param>
    /// <returns>The mixed-number string representation.</returns>
    private string FormatMixed(IFormatProvider? provider)
    {
        BigInteger numerator = BigNumerator;
        BigInteger denominator = BigDenominator;

        if (denominator.IsOne)
            return numerator.ToString(provider);

        var magnitude = BigInteger.Abs(numerator);
        if (magnitude < denominator)
            return $"{numerator.ToString(provider)}/{denominator.ToString(provider)}";

        BigInteger whole = numerator / denominator;
        BigInteger remainder = magnitude - (BigInteger.Abs(whole) * denominator);

        return $"{whole.ToString(provider)} {remainder.ToString(provider)}/{denominator.ToString(provider)}";
    }

    /// <summary>
    /// Formats this value using a Unicode vulgar-fraction glyph when one exists, otherwise as a mixed number.
    /// </summary>
    /// <param name="provider">The culture used to render the numeric components.</param>
    /// <returns>The Unicode string representation.</returns>
    private string FormatUnicode(IFormatProvider? provider)
    {
        BigInteger numerator = BigNumerator;
        BigInteger denominator = BigDenominator;

        if (denominator.IsOne)
            return numerator.ToString(provider);

        var magnitude = BigInteger.Abs(numerator);
        BigInteger whole = magnitude / denominator;
        BigInteger remainder = magnitude % denominator;

        if (denominator <= 16
            && remainder >= BigInteger.One
            && s_vulgarFractions.TryGetValue(((int)remainder, (int)denominator), out var glyph))
        {
            var sign = numerator.Sign < 0 ? "-" : string.Empty;
            var wholePart = whole.IsZero ? string.Empty : whole.ToString(provider);
            return $"{sign}{wholePart}{glyph}";
        }

        return FormatMixed(provider);
    }

    /// <summary>
    /// Formats this value as a percentage by scaling it by one hundred and appending a percent sign.
    /// </summary>
    /// <param name="provider">The culture used to render the numeric components.</param>
    /// <returns>The percentage string representation.</returns>
    private string FormatPercent(IFormatProvider? provider)
    {
        BigInteger scaledNumerator = BigNumerator * 100;
        BigInteger denominator = BigDenominator;

        var divisor = BigInteger.GreatestCommonDivisor(BigInteger.Abs(scaledNumerator), denominator);
        if (divisor > BigInteger.One)
        {
            scaledNumerator /= divisor;
            denominator /= divisor;
        }

        return denominator.IsOne
            ? $"{scaledNumerator.ToString(provider)}%"
            : $"{scaledNumerator.ToString(provider)}/{denominator.ToString(provider)}%";
    }
}
