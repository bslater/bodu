// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyCompactFormattingExtensions.ToCompactString.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial.Extensions;

public static partial class MoneyCompactFormattingExtensions
{
    /// <summary>
    /// Returns a compact-notation representation of <paramref name="money" /> with a magnitude suffix (K, M, B, T)
    /// applied when the amount is at least one thousand in magnitude.
    /// </summary>
    /// <typeparam name="TCurrency">The currency tag type.</typeparam>
    /// <param name="money">The monetary amount to format.</param>
    /// <param name="format">
    /// The format specifier; see <see cref="Money{TCurrency}.ToString(string?, IFormatProvider?)" /> for the supported
    /// vocabulary. The <c>R</c> specifier is rejected because compact notation cannot round-trip. Defaults to
    /// <c>"C"</c>.
    /// </param>
    /// <param name="provider">
    /// The culture used to render the numeric portion. When omitted, the current culture is used.
    /// </param>
    /// <param name="precision">
    /// The fractional-digit count to render in the scaled portion. Must be non-negative; defaults to <c>1</c>.
    /// </param>
    /// <returns>A compact-notation string such as <c>"$1.2K"</c>, <c>"€1.5M"</c>, or <c>"USD 2.3B"</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="precision" /> is negative.</exception>
    /// <exception cref="FormatException">
    /// Thrown when <paramref name="format" /> is not a supported specifier, or when it is <c>R</c> (which cannot be
    /// combined with compact notation).
    /// </exception>
    public static string ToCompactString<TCurrency>(
        this Money<TCurrency> money,
        string? format = "C",
        IFormatProvider? provider = null,
        int precision = DefaultCompactPrecision)
        where TCurrency : ICurrency
    {
        ThrowHelper.ThrowIfNegative(precision);
        RejectRoundTripSpecifier(format);

        (decimal scaled, string? suffix) = Scale(money.Amount);
        ReadOnlySpan<char> formatWithPrecision = BuildFormatWithPrecision(format, precision, suffix.Length > 0);

        return Money<TCurrency>.FormatScaled(scaled, suffix, formatWithPrecision, provider);
    }

    /// <summary>
    /// Returns a compact-notation representation of <paramref name="money" /> with a magnitude suffix (K, M, B, T)
    /// applied when the amount is at least one thousand in magnitude.
    /// </summary>
    /// <param name="money">The runtime-tagged monetary amount to format.</param>
    /// <param name="format">
    /// The format specifier; see <see cref="Money.ToString(string?, IFormatProvider?)" /> for the supported vocabulary.
    /// The <c>R</c> specifier is rejected because compact notation cannot round-trip. Defaults to <c>"C"</c>.
    /// </param>
    /// <param name="provider">
    /// The culture used to render the numeric portion. When omitted, the current culture is used.
    /// </param>
    /// <param name="precision">
    /// The fractional-digit count to render in the scaled portion. Must be non-negative; defaults to <c>1</c>.
    /// </param>
    /// <returns>A compact-notation string such as <c>"$1.2K"</c>, <c>"€1.5M"</c>, or <c>"USD 2.3B"</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="precision" /> is negative.</exception>
    /// <exception cref="FormatException">
    /// Thrown when <paramref name="format" /> is not a supported specifier, or when it is <c>R</c> (which cannot be
    /// combined with compact notation).
    /// </exception>
    public static string ToCompactString(
        this Money money,
        string? format = "C",
        IFormatProvider? provider = null,
        int precision = DefaultCompactPrecision)
    {
        ThrowHelper.ThrowIfNegative(precision);
        RejectRoundTripSpecifier(format);

        (decimal scaled, string? suffix) = Scale(money.Amount);
        string formatWithPrecision = BuildFormatWithPrecision(format, precision, suffix.Length > 0).ToString();

        // Use Money's instance Format directly with the scaled amount via a temporary normalised wrapper.
        // The wrapped value bypasses normalisation so the scaled fractional portion is preserved.
        var scaledMoney = Money.FromNormalized(scaled, money.Code);
        return scaledMoney.Format(formatWithPrecision, provider, suffix);
    }
}
