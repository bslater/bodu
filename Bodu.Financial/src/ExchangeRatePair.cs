// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRatePair.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Financial;

/// <summary>
/// Represents an ordered pair of ISO-style currency codes that identifies the direction of an exchange-rate quotation.
/// </summary>
/// <remarks>
/// <para>
/// A pair is a strongly typed key intended for use in dictionaries and lookup tables. Compared to a raw tuple of
/// strings, it centralises validation and makes the directional meaning of each code obvious at the call site.
/// </para>
/// </remarks>
[DebuggerDisplay("{FromIsoCode,nq}/{ToIsoCode,nq}")]
public readonly record struct ExchangeRatePair
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExchangeRatePair" /> class.
    /// </summary>
    /// <param name="fromIsoCode">The source-currency ISO-style code (three uppercase ASCII letters).</param>
    /// <param name="toIsoCode">The destination-currency ISO-style code (three uppercase ASCII letters).</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="fromIsoCode" /> or <paramref name="toIsoCode" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="fromIsoCode" /> or <paramref name="toIsoCode" /> is not a three-character uppercase
    /// ASCII code.
    /// </exception>
    public ExchangeRatePair(string fromIsoCode, string toIsoCode)
    {
        ExchangeRateValidation.RequireIsoCode(fromIsoCode);
        ExchangeRateValidation.RequireIsoCode(toIsoCode);

        FromIsoCode = fromIsoCode;
        ToIsoCode = toIsoCode;
    }

    /// <summary>
    /// Gets the source-currency ISO-style code.
    /// </summary>
    /// <returns>A three-character uppercase ASCII code identifying the source currency.</returns>
    public string FromIsoCode { get; }

    /// <summary>
    /// Gets the destination-currency ISO-style code.
    /// </summary>
    /// <returns>A three-character uppercase ASCII code identifying the destination currency.</returns>
    public string ToIsoCode { get; }

    /// <summary>
    /// Returns the inverse pair with <see cref="FromIsoCode" /> and <see cref="ToIsoCode" /> swapped.
    /// </summary>
    /// <returns>A new <see cref="ExchangeRatePair" /> describing the reverse-direction quotation.</returns>
    public ExchangeRatePair Inverse() => new(ToIsoCode, FromIsoCode);
}
