// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ICurrency.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

/// <summary>
/// Identifies a currency at the type-system level, supplying the ISO 4217 code, minor-unit precision, and optional
/// cash-rounding and historicity metadata used by <see cref="Money{TCurrency}" />.
/// </summary>
/// <remarks>
/// <para>
/// Implementations of <see cref="ICurrency" /> are tag types — they exist solely to parameterize
/// <see cref="Money{TCurrency}" /> and carry the currency's static metadata. They are never instantiated; the shipped
/// implementations declare a private constructor and expose only static members.
/// </para>
/// <para>
/// The interface relies on the C# 11 static-abstract-member feature, so members are accessed through the type itself (
/// <c>USD.IsoCode</c>, <c>USD.MinorUnits</c>) and via <c>TCurrency.IsoCode</c> from within generic code constrained by
/// <c>where TCurrency : ICurrency</c>.
/// </para>
/// <para>
/// <see cref="IsoCode" /> and <see cref="MinorUnits" /> are required. The remaining members (
/// <see cref="CashRoundingIncrement" />, <see cref="IsHistoric" />, <see cref="DemonetizedOn" />,
/// <see cref="SuccessorIsoCode" />) are <c>static virtual</c> with sensible defaults so existing custom implementations
/// remain source-compatible — only the shipped <c>Bodu.Financial.Currencies</c> tags override the defaults to surface
/// country-specific cash rounding or historic-currency metadata.
/// </para>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using Bodu.Financial;
/// using Bodu.Financial.Currencies;
///
/// // Metadata is read statically from the tag type - no instance is ever created.
/// string code = USD.IsoCode;        // "USD"
/// int minorUnits = USD.MinorUnits;  // 2
/// int numeric = USD.NumericCode;    // 840
///
/// // The same members are reachable through a generic constraint.
/// static string DescribeCurrency<TCurrency>() where TCurrency : ICurrency =>
///     $"{TCurrency.IsoCode} ({TCurrency.MinorUnits} minor units)";
///]]>
/// </code>
/// </example>
/// </remarks>
public interface ICurrency
{
    /// <summary>
    /// Gets the ISO 4217 three-letter alphabetic code that identifies the currency.
    /// </summary>
    /// <returns>An uppercase three-letter currency code, such as <c>"USD"</c>, <c>"EUR"</c>, or <c>"JPY"</c>.</returns>
    static abstract string IsoCode { get; }

    /// <summary>
    /// Gets the number of fractional digits in the currency's minor unit — the precision
    /// <see cref="Money{TCurrency}" /> rounds to on construction and formats by default.
    /// </summary>
    /// <returns>
    /// The non-negative number of decimal places the currency uses; typically two (for <c>USD</c>, <c>EUR</c>, etc.),
    /// zero (for <c>JPY</c>, <c>KRW</c>, <c>CLP</c>), or three (for <c>BHD</c>, <c>KWD</c>, <c>OMR</c>).
    /// </returns>
    static abstract int MinorUnits { get; }

    /// <summary>
    /// Gets the ISO 4217 three-digit numeric code that identifies the currency.
    /// </summary>
    /// <returns>
    /// The three-digit numeric code (for example, <c>840</c> for <c>USD</c>, <c>36</c> for <c>AUD</c>, <c>392</c> for
    /// <c>JPY</c>), or <c>0</c> when the currency is custom or the numeric code is unknown.
    /// </returns>
    /// <remarks>
    /// The numeric code is the second public identifier defined by ISO 4217 and is widely used by payment formats
    /// (SWIFT MT messages, ISO 20022). The default of <c>0</c> keeps existing custom <see cref="ICurrency" />
    /// implementations source-compatible; the shipped <c>Bodu.Financial.Currencies</c> tags override it with the
    /// canonical ISO numeric value.
    /// </remarks>
    static virtual int NumericCode => 0;

    /// <summary>
    /// Gets the smallest cash denomination of the currency, expressed in the major unit.
    /// </summary>
    /// <returns>
    /// The cash-rounding increment in the major unit (for example, <c>0.05m</c> for CHF's five-rappen smallest
    /// circulating coin, <c>0.05m</c> for AUD / CAD cash totals since smallest coins were withdrawn, or <c>1m</c> for
    /// SEK / NOK / ISK), or <c>0m</c> when the currency does not require special cash rounding beyond its
    /// <see cref="MinorUnits" /> precision.
    /// </returns>
    /// <remarks>
    /// The default of <c>0m</c> is interpreted by <see cref="Money{TCurrency}.RoundToCash(MidpointRounding)" /> as "no
    /// special cash rounding required", so the rounding becomes a no-op. Cash-rounding rules apply only to physical
    /// cash totals; electronic transactions retain the full <see cref="MinorUnits" /> precision.
    /// </remarks>
    static virtual decimal CashRoundingIncrement => 0m;

    /// <summary>
    /// Gets a value indicating whether the currency has been demonetized and is no longer in active circulation.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when the currency is a historic predecessor (for example, the Euro-zone national
    /// currencies replaced in 2002, or <c>ZWL</c> since its 2024 demonetization); otherwise <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// Historic currencies still participate fully in arithmetic and formatting so consumers can process legacy data;
    /// <see cref="IsHistoric" /> exists for filtering and reporting only.
    /// </remarks>
    static virtual bool IsHistoric => false;

    /// <summary>
    /// Gets the date the currency was withdrawn from circulation, if known.
    /// </summary>
    /// <returns>
    /// The demonetization date, or <see langword="null" /> when the currency is active or the date is unknown.
    /// </returns>
    static virtual DateOnly? DemonetizedOn => null;

    /// <summary>
    /// Gets the ISO 4217 alphabetic code of the currency that replaced this one, when applicable.
    /// </summary>
    /// <returns>
    /// The three-letter ISO code of the successor currency (for example, <c>"EUR"</c> for the Euro-zone predecessor
    /// currencies), or <see langword="null" /> when there is no defined successor.
    /// </returns>
    static virtual string? SuccessorIsoCode => null;

    /// <summary>
    /// Gets the English-language name of the currency.
    /// </summary>
    /// <returns>
    /// The currency's name in English, in singular form and Title Case (for example, <c>"United States Dollar"</c>,
    /// <c>"Euro"</c>, or <c>"Australian Dollar"</c>), or an empty string when no name is supplied.
    /// </returns>
    /// <remarks>
    /// The English name is used by <see cref="Money{TCurrency}" /> formatting in the <c>L</c> specifier path (
    /// <c>"1,234.56 Australian Dollar"</c>) and is available through <see cref="CurrencyInfo.EnglishName" /> on the
    /// runtime-tagged registry entry. The default is the empty string so existing <see cref="ICurrency" />
    /// implementations remain source-compatible; when empty, the <c>L</c> specifier falls back to the ISO-substitution
    /// form rather than emitting a trailing space with no name.
    /// </remarks>
    static virtual string EnglishName => string.Empty;
}
