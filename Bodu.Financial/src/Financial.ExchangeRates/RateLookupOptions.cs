// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateLookupOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Encapsulates the rules an exchange-rate lookup must apply when an exact-date match is unavailable.
/// </summary>
/// <remarks>
/// <para>
/// The type is a reference type so that <see langword="default" /><c>(RateLookupOptions)</c> evaluates to
/// <see langword="null" /> rather than a partially populated value. All public surfaces that accept lookup options
/// allow <see langword="null" /> and substitute <see cref="Exact" /> when no options are supplied, ensuring the
/// out-of-the-box behaviour matches the documented safe default.
/// </para>
/// <para>
/// Use the static factory members (<see cref="Exact" />, <see cref="PreviousWithin(int)" />,
/// <see cref="NextWithin(int)" />, <see cref="NearestWithin(int)" />) for the common configurations; construct an
/// options instance directly only when a less common combination is required.
/// </para>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using Bodu.Financial;
///
/// // Require an exact-date hit (the default safe behaviour).
/// var exact = RateLookupOptions.Exact;
///
/// // Fall back to the most recent rate on or before the requested date, up to five days back.
/// var previous = RateLookupOptions.PreviousWithin(5);
///
/// // Pick the closest available date either side, within a week, preferring the earlier on ties.
/// var nearest = RateLookupOptions.NearestWithin(7);
///]]>
/// </code>
/// </example>
/// </remarks>
public sealed class RateLookupOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RateLookupOptions" /> class.
    /// </summary>
    /// <param name="dateResolution">The fallback policy when no rate exists on the requested date.</param>
    /// <param name="toleranceDays">
    /// The maximum permitted absolute distance, in days, between the requested date and the resolved date. Must be zero
    /// when <paramref name="dateResolution" /> is <see cref="RateDateResolution.Exact" />.
    /// </param>
    /// <param name="allowInverse">
    /// When <see langword="true" />, the lookup may fall back to the reverse-direction pair (returning the reciprocal
    /// rate) when the direct pair has no rate. When <see langword="false" />, only the direct pair is consulted.
    /// </param>
    /// <param name="allowSameCurrencyIdentityRate">
    /// When <see langword="true" />, a lookup whose source and destination ISO codes are equal returns a synthetic
    /// identity rate of <c>1</c>. When <see langword="false" />, the lookup falls through to the underlying table.
    /// </param>
    public RateLookupOptions(
        RateDateResolution dateResolution,
        int toleranceDays = 0,
        bool allowInverse = true,
        bool allowSameCurrencyIdentityRate = true)
    {
        DateResolution = dateResolution;
        ToleranceDays = toleranceDays;
        AllowInverse = allowInverse;
        AllowSameCurrencyIdentityRate = allowSameCurrencyIdentityRate;
    }

    /// <summary>
    /// Gets a configuration that requires an exact-date match, allowing inverse and same-currency identity fallbacks.
    /// </summary>
    /// <value>An <see cref="RateLookupOptions" /> with <see cref="RateDateResolution.Exact" />.</value>
    public static RateLookupOptions Exact { get; } = new(RateDateResolution.Exact);

    /// <summary>
    /// Gets the fallback policy applied when no rate exists on the requested date.
    /// </summary>
    /// <value>The configured <see cref="RateDateResolution" />.</value>
    public RateDateResolution DateResolution { get; }

    /// <summary>
    /// Gets the maximum permitted absolute distance, in days, between the requested date and the resolved date.
    /// </summary>
    /// <value>A non-negative tolerance in days.</value>
    public int ToleranceDays { get; }

    /// <summary>
    /// Gets a value indicating whether the lookup may fall back to the reverse-direction pair.
    /// </summary>
    /// <value>
    /// <see langword="true" /> when the reverse-direction pair may be consulted and its rate inverted; otherwise
    /// <see langword="false" />.
    /// </value>
    public bool AllowInverse { get; }

    /// <summary>
    /// Gets a value indicating whether a same-currency lookup returns a synthetic identity rate of <c>1</c>.
    /// </summary>
    /// <value>
    /// <see langword="true" /> when same-currency lookups return the identity rate; otherwise <see langword="false" />.
    /// </value>
    public bool AllowSameCurrencyIdentityRate { get; }

    /// <summary>
    /// Returns a configuration that resolves to the most recent rate on or before the requested date, within
    /// <paramref name="toleranceDays" />.
    /// </summary>
    /// <param name="toleranceDays">
    /// The maximum permitted distance, in days, between requested and resolved dates.
    /// </param>
    /// <returns>
    /// An <see cref="RateLookupOptions" /> with <see cref="RateDateResolution.PreviousOnOrBefore" />.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="toleranceDays" /> is negative.
    /// </exception>
    public static RateLookupOptions PreviousWithin(int toleranceDays)
    {
        ThrowHelper.ThrowIfNegative(toleranceDays);
        return new(RateDateResolution.PreviousOnOrBefore, toleranceDays);
    }

    /// <summary>
    /// Returns a configuration that resolves to the earliest rate on or after the requested date, within
    /// <paramref name="toleranceDays" />.
    /// </summary>
    /// <param name="toleranceDays">
    /// The maximum permitted distance, in days, between requested and resolved dates.
    /// </param>
    /// <returns>An <see cref="RateLookupOptions" /> with <see cref="RateDateResolution.NextOnOrAfter" />.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="toleranceDays" /> is negative.
    /// </exception>
    public static RateLookupOptions NextWithin(int toleranceDays)
    {
        ThrowHelper.ThrowIfNegative(toleranceDays);
        return new(RateDateResolution.NextOnOrAfter, toleranceDays);
    }

    /// <summary>
    /// Returns a configuration that resolves to the closest available date within <paramref name="toleranceDays" />,
    /// preferring the previous date on ties.
    /// </summary>
    /// <param name="toleranceDays">
    /// The maximum permitted distance, in days, between requested and resolved dates.
    /// </param>
    /// <returns>
    /// An <see cref="RateLookupOptions" /> with <see cref="RateDateResolution.NearestPreferPrevious" /> .
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="toleranceDays" /> is negative.
    /// </exception>
    public static RateLookupOptions NearestWithin(int toleranceDays)
    {
        ThrowHelper.ThrowIfNegative(toleranceDays);
        return new(RateDateResolution.NearestPreferPrevious, toleranceDays);
    }

    /// <summary>
    /// Validates the option values, throwing if any rule is violated.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <see cref="DateResolution" /> is not a defined <see cref="RateDateResolution" /> member, or if
    /// <see cref="ToleranceDays" /> is negative.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown if <see cref="DateResolution" /> is <see cref="RateDateResolution.Exact" /> and
    /// <see cref="ToleranceDays" /> is non-zero.
    /// </exception>
    public void Validate()
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(DateResolution, nameof(DateResolution));
        ThrowHelper.ThrowIfNegative(ToleranceDays, nameof(ToleranceDays));

        if (DateResolution == RateDateResolution.Exact && ToleranceDays != 0)
        {
            throw new ArgumentException(
                FinancialResourceStrings.Arg_Invalid_LookupToleranceWithExactResolution,
                nameof(ToleranceDays));
        }
    }
}
