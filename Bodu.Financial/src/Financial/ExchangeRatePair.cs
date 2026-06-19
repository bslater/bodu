// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRatePair.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Bodu.Financial.Currencies;
using Bodu.Financial.Serialization;

namespace Bodu.Financial;

/// <summary>
/// Represents an ordered pair of currencies that identifies the direction of an exchange-rate quotation.
/// </summary>
/// <remarks>
/// <para>
/// A pair is a strongly typed key intended for use in dictionaries and lookup tables. Compared to a raw tuple of
/// codes, it centralises validation and makes the directional meaning of each currency obvious at the call site.
/// </para>
/// </remarks>
[DebuggerDisplay("{From,nq}/{To,nq}")]
[JsonConverter(typeof(ExchangeRatePairJsonConverter))]
public readonly record struct ExchangeRatePair
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExchangeRatePair" /> struct.
    /// </summary>
    /// <param name="from">The source currency.</param>
    /// <param name="to">The destination currency.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="from" /> or <paramref name="to" /> is <see cref="CurrencyCode.None" /> or is not a
    /// defined <see cref="CurrencyCode" /> member.
    /// </exception>
    public ExchangeRatePair(CurrencyCode from, CurrencyCode to)
    {
        FinancialThrowHelper.ThrowIfNotDefinedCurrencyCode(from);
        FinancialThrowHelper.ThrowIfNotDefinedCurrencyCode(to);

        From = from;
        To = to;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExchangeRatePair" /> struct from two ISO 4217 alphabetic codes.
    /// </summary>
    /// <param name="fromIsoCode">The source-currency ISO 4217 alphabetic code.</param>
    /// <param name="toIsoCode">The destination-currency ISO 4217 alphabetic code.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="fromIsoCode" /> or <paramref name="toIsoCode" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="fromIsoCode" /> or <paramref name="toIsoCode" /> is not a known currency.
    /// </exception>
    /// <remarks>
    /// Transitional string entry point retained while the exchange-rate surface migrates to <see cref="CurrencyCode" />;
    /// prefer the <see cref="ExchangeRatePair(CurrencyCode, CurrencyCode)" /> constructor.
    /// </remarks>
    public ExchangeRatePair(string fromIsoCode, string toIsoCode)
        : this(Parse(fromIsoCode), Parse(toIsoCode))
    {
    }

    /// <summary>
    /// Gets the source currency.
    /// </summary>
    /// <returns>The currency an amount is converted from.</returns>
    public CurrencyCode From { get; }

    /// <summary>
    /// Gets the destination currency.
    /// </summary>
    /// <returns>The currency an amount is converted to.</returns>
    public CurrencyCode To { get; }

    /// <summary>
    /// Gets the source-currency ISO 4217 alphabetic code.
    /// </summary>
    /// <returns>The source currency's code, or an empty string when <see cref="From" /> is <see cref="CurrencyCode.None" />.</returns>
    public string FromIsoCode => From == CurrencyCode.None ? string.Empty : From.ToString();

    /// <summary>
    /// Gets the destination-currency ISO 4217 alphabetic code.
    /// </summary>
    /// <returns>The destination currency's code, or an empty string when <see cref="To" /> is <see cref="CurrencyCode.None" />.</returns>
    public string ToIsoCode => To == CurrencyCode.None ? string.Empty : To.ToString();

    /// <summary>
    /// Gets a value indicating whether this instance carries two real currencies and can therefore be used safely as a
    /// directional key.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when both <see cref="From" /> and <see cref="To" /> are not <see cref="CurrencyCode.None" />;
    /// otherwise <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// Because <see cref="ExchangeRatePair" /> is a value type, <see langword="default" /><c>(ExchangeRatePair)</c>
    /// bypasses the validating constructor and leaves both currencies <see cref="CurrencyCode.None" />. Public
    /// boundaries that accept an <see cref="ExchangeRatePair" /> should reject any instance whose <see cref="IsValid" />
    /// property is <see langword="false" />.
    /// </remarks>
    public bool IsValid => From != CurrencyCode.None && To != CurrencyCode.None;

    /// <summary>
    /// Returns the inverse pair with <see cref="From" /> and <see cref="To" /> swapped.
    /// </summary>
    /// <returns>A new <see cref="ExchangeRatePair" /> describing the reverse-direction quotation.</returns>
    public ExchangeRatePair Inverse() => new(To, From);

    /// <summary>
    /// Resolves an ISO 4217 alphabetic code to a <see cref="CurrencyCode" />, reporting the originating constructor
    /// parameter on failure.
    /// </summary>
    /// <param name="isoCode">The ISO 4217 alphabetic code to resolve.</param>
    /// <param name="paramName">The originating parameter name; inferred from the call site.</param>
    /// <returns>The resolved <see cref="CurrencyCode" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="isoCode" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="isoCode" /> is not a known currency.</exception>
    private static CurrencyCode Parse(string isoCode, [CallerArgumentExpression(nameof(isoCode))] string? paramName = null)
    {
        ThrowHelper.ThrowIfNull(isoCode, paramName);

        if (!CurrencyInfo.TryGetCurrencyCode(isoCode, out CurrencyCode code))
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, FinancialResourceStrings.Arg_Invalid_UnknownCurrencyRejected, isoCode),
                paramName);

        return code;
    }
}
