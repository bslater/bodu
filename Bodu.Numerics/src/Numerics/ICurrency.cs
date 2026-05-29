// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ICurrency.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

/// <summary>
/// Identifies a currency at the type-system level, supplying the ISO 4217 code and minor-unit precision used to
/// normalize and format <see cref="Money{TCurrency}" /> instances.
/// </summary>
/// <remarks>
/// <para>
/// Implementations of <see cref="ICurrency" /> are tag types — they exist solely to parameterize
/// <see cref="Money{TCurrency}" /> and carry the currency's static metadata. They are never instantiated; the
/// shipped implementations declare a private constructor and expose only static members.
/// </para>
/// <para>
/// The interface relies on the C# 11 static-abstract-member feature, so members are accessed through the type
/// itself (<c>USD.IsoCode</c>, <c>USD.MinorUnits</c>) and via <c>TCurrency.IsoCode</c> from within generic code
/// constrained by <c>where TCurrency : ICurrency</c>.
/// </para>
/// </remarks>
public interface ICurrency
{
    /// <summary>
    /// Gets the ISO 4217 three-letter alphabetic code that identifies the currency.
    /// </summary>
    /// <returns>An uppercase three-letter currency code, such as <c>"USD"</c>, <c>"EUR"</c>, or <c>"JPY"</c>.</returns>
    static abstract string IsoCode { get; }

    /// <summary>
    /// Gets the number of fractional digits in the currency's minor unit — the precision <see cref="Money{TCurrency}" />
    /// rounds to on construction and formats by default.
    /// </summary>
    /// <returns>
    /// The non-negative number of decimal places the currency uses; typically two (for <c>USD</c>, <c>EUR</c>, etc.),
    /// zero (for <c>JPY</c>, <c>KRW</c>, <c>CLP</c>), or three (for <c>BHD</c>, <c>KWD</c>, <c>OMR</c>).
    /// </returns>
    static abstract int MinorUnits { get; }
}
