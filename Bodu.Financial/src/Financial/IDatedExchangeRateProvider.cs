// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IDatedExchangeRateProvider.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

/// <summary>
/// Defines the contract for an exchange-rate provider that resolves dated lookups and returns metadata describing how
/// each rate was selected.
/// </summary>
/// <remarks>
/// <para>
/// Implementations are expected to validate their string and option arguments and throw rather than return failure when
/// inputs are invalid. The distinction between
/// <see cref="GetRate(string, string, DateOnly, ExchangeRateLookupOptions?)" /> and
/// <see cref="TryGetRate(string, string, DateOnly, ExchangeRateLookupOptions?, out ExchangeRateLookupResult)" /> is
/// reserved for the case where no rate is available for an otherwise valid request: the former throws
/// <see cref="KeyNotFoundException" />, the latter returns <see langword="false" /> without allocating.
/// </para>
/// <para>
/// Implementations must accept a <see langword="null" /> <paramref name="options" /> argument and substitute
/// <see cref="ExchangeRateLookupOptions.Exact" /> so callers can opt into the documented safe default by omission.
/// </para>
/// </remarks>
public interface IDatedExchangeRateProvider
{
    /// <summary>
    /// Resolves the exchange rate from <paramref name="fromIsoCode" /> to <paramref name="toIsoCode" /> on
    /// <paramref name="date" /> under <paramref name="options" />, throwing if no rate is available.
    /// </summary>
    /// <param name="fromIsoCode">The source-currency ISO-style code.</param>
    /// <param name="toIsoCode">The destination-currency ISO-style code.</param>
    /// <param name="date">The calendar date for which a rate is required.</param>
    /// <param name="options">
    /// The lookup rules to apply, including date-resolution policy and tolerance. <see langword="null" /> is treated as
    /// <see cref="ExchangeRateLookupOptions.Exact" />.
    /// </param>
    /// <returns>The resolved <see cref="ExchangeRateLookupResult" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="fromIsoCode" /> or <paramref name="toIsoCode" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown if either ISO code is not a three-character uppercase ASCII code, or if <paramref name="options" /> is
    /// invalid (for example, <see cref="ExchangeRateDateResolution.Exact" /> with non-zero tolerance).
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="options" /> contains a negative tolerance or an undefined enum value.
    /// </exception>
    /// <exception cref="KeyNotFoundException">
    /// Thrown if no rate is available for the request under the supplied options.
    /// </exception>
    ExchangeRateLookupResult GetRate(
        string fromIsoCode,
        string toIsoCode,
        DateOnly date,
        ExchangeRateLookupOptions? options = null);

    /// <summary>
    /// Attempts to resolve the exchange rate from <paramref name="fromIsoCode" /> to <paramref name="toIsoCode" /> on
    /// <paramref name="date" /> under <paramref name="options" />, returning a flag indicating whether a rate was
    /// found.
    /// </summary>
    /// <param name="fromIsoCode">The source-currency ISO-style code.</param>
    /// <param name="toIsoCode">The destination-currency ISO-style code.</param>
    /// <param name="date">The calendar date for which a rate is required.</param>
    /// <param name="options">
    /// The lookup rules to apply, including date-resolution policy and tolerance. <see langword="null" /> is treated as
    /// <see cref="ExchangeRateLookupOptions.Exact" />.
    /// </param>
    /// <param name="result">
    /// When this method returns <see langword="true" />, contains the resolved <see cref="ExchangeRateLookupResult" />;
    /// otherwise, contains <see langword="default" />.
    /// </param>
    /// <returns><see langword="true" /> if a rate was resolved; otherwise <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="fromIsoCode" /> or <paramref name="toIsoCode" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown if either ISO code is not a three-character uppercase ASCII code, or if <paramref name="options" /> is
    /// invalid.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="options" /> contains a negative tolerance or an undefined enum value.
    /// </exception>
    bool TryGetRate(
        string fromIsoCode,
        string toIsoCode,
        DateOnly date,
        ExchangeRateLookupOptions? options,
        out ExchangeRateLookupResult result);

    /// <summary>
    /// Returns every available rate from <paramref name="fromIsoCode" /> to <paramref name="toIsoCode" /> whose
    /// observation date falls within the inclusive range <paramref name="startDate" /> to <paramref name="endDate" />.
    /// </summary>
    /// <param name="fromIsoCode">The source-currency ISO-style code.</param>
    /// <param name="toIsoCode">The destination-currency ISO-style code.</param>
    /// <param name="startDate">The inclusive start of the range.</param>
    /// <param name="endDate">The inclusive end of the range.</param>
    /// <param name="cancellationToken">A token to observe while awaiting the operation.</param>
    /// <returns>The rates in the range ordered by date, or an empty list when none are available.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="fromIsoCode" /> or <paramref name="toIsoCode" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown if either ISO code is not a three-character uppercase ASCII code, or if <paramref name="endDate" />
    /// precedes <paramref name="startDate" />.
    /// </exception>
    /// <remarks>
    /// The lookup is range-based and does not apply a date-resolution policy: it returns the observations that exist
    /// within the window rather than resolving a single date. Implementations backed by a remote feed may fetch on
    /// demand, which is why the method is asynchronous.
    /// </remarks>
    ValueTask<IReadOnlyList<ExchangeRate>> GetRatesAsync(
        string fromIsoCode,
        string toIsoCode,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default);
}
