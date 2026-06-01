// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateLookupOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

/// <summary>
/// Encapsulates the rules an exchange-rate lookup must apply when an exact-date match is unavailable.
/// </summary>
/// <param name="DateResolution">The fallback policy when no rate exists on the requested date.</param>
/// <param name="ToleranceDays">
/// The maximum permitted absolute distance, in days, between the requested date and the resolved date. Must be zero
/// when <paramref name="DateResolution" /> is <see cref="ExchangeRateDateResolution.Exact" />.
/// </param>
/// <param name="AllowInverse">
/// When <see langword="true" />, the lookup may fall back to the reverse-direction pair (returning the reciprocal rate)
/// when the direct pair has no rate. When <see langword="false" />, only the direct pair is consulted.
/// </param>
/// <param name="AllowSameCurrencyIdentityRate">
/// When <see langword="true" />, a lookup whose source and destination ISO codes are equal returns a synthetic identity
/// rate of <c>1</c>. When <see langword="false" />, the lookup falls through to the underlying table.
/// </param>
/// <remarks>
/// <para>
/// Use the static factory members (<see cref="Exact" />, <see cref="PreviousWithin(int)" />,
/// <see cref="NextWithin(int)" />, <see cref="NearestWithin(int)" />) for the common configurations; construct an
/// options value directly only when a less common combination is required.
/// </para>
/// </remarks>
public readonly record struct ExchangeRateLookupOptions(
    ExchangeRateDateResolution DateResolution,
    int ToleranceDays = 0,
    bool AllowInverse = true,
    bool AllowSameCurrencyIdentityRate = true)
{
    /// <summary>
    /// Gets a configuration that requires an exact-date match, allowing inverse and same-currency identity fallbacks.
    /// </summary>
    /// <returns>
    /// An <see cref="ExchangeRateLookupOptions" /> with <see cref="ExchangeRateDateResolution.Exact" />.
    /// </returns>
    public static ExchangeRateLookupOptions Exact { get; } =
        new(ExchangeRateDateResolution.Exact);

    /// <summary>
    /// Returns a configuration that resolves to the most recent rate on or before the requested date, within
    /// <paramref name="toleranceDays" />.
    /// </summary>
    /// <param name="toleranceDays">
    /// The maximum permitted distance, in days, between requested and resolved dates.
    /// </param>
    /// <returns>
    /// An <see cref="ExchangeRateLookupOptions" /> with <see cref="ExchangeRateDateResolution.PreviousOnOrBefore" />.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="toleranceDays" /> is negative.
    /// </exception>
    public static ExchangeRateLookupOptions PreviousWithin(int toleranceDays)
    {
        ThrowHelper.ThrowIfNegative(toleranceDays);
        return new(ExchangeRateDateResolution.PreviousOnOrBefore, toleranceDays);
    }

    /// <summary>
    /// Returns a configuration that resolves to the earliest rate on or after the requested date, within
    /// <paramref name="toleranceDays" />.
    /// </summary>
    /// <param name="toleranceDays">
    /// The maximum permitted distance, in days, between requested and resolved dates.
    /// </param>
    /// <returns>
    /// An <see cref="ExchangeRateLookupOptions" /> with <see cref="ExchangeRateDateResolution.NextOnOrAfter" />.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="toleranceDays" /> is negative.
    /// </exception>
    public static ExchangeRateLookupOptions NextWithin(int toleranceDays)
    {
        ThrowHelper.ThrowIfNegative(toleranceDays);
        return new(ExchangeRateDateResolution.NextOnOrAfter, toleranceDays);
    }

    /// <summary>
    /// Returns a configuration that resolves to the closest available date within <paramref name="toleranceDays" />,
    /// preferring the previous date on ties.
    /// </summary>
    /// <param name="toleranceDays">
    /// The maximum permitted distance, in days, between requested and resolved dates.
    /// </param>
    /// <returns>
    /// An <see cref="ExchangeRateLookupOptions" /> with <see cref="ExchangeRateDateResolution.NearestPreferPrevious" />
    /// .
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="toleranceDays" /> is negative.
    /// </exception>
    public static ExchangeRateLookupOptions NearestWithin(int toleranceDays)
    {
        ThrowHelper.ThrowIfNegative(toleranceDays);
        return new(ExchangeRateDateResolution.NearestPreferPrevious, toleranceDays);
    }

    /// <summary>
    /// Validates the option values, throwing if any rule is violated.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <see cref="DateResolution" /> is not a defined <see cref="ExchangeRateDateResolution" /> member, or if
    /// <see cref="ToleranceDays" /> is negative.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown if <see cref="DateResolution" /> is <see cref="ExchangeRateDateResolution.Exact" /> and
    /// <see cref="ToleranceDays" /> is non-zero.
    /// </exception>
    public void Validate()
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(DateResolution, nameof(DateResolution));
        ThrowHelper.ThrowIfNegative(ToleranceDays, nameof(ToleranceDays));

        if (DateResolution == ExchangeRateDateResolution.Exact && ToleranceDays != 0)
        {
            throw new ArgumentException(
                FinancialResourceStrings.Arg_Invalid_LookupToleranceWithExactResolution,
                nameof(ToleranceDays));
        }
    }
}
