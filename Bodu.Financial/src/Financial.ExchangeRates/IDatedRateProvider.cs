// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IDatedRateProvider.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Defines the contract for an exchange-rate provider that resolves dated and latest lookups and returns metadata
/// describing how each rate was selected, over symmetric synchronous and asynchronous surfaces.
/// </summary>
/// <remarks>
/// <para>
/// Implementations are expected to validate their string and option arguments and throw rather than return failure when
/// inputs are invalid. The distinction between <see cref="GetRate(string, string, DateOnly, RateLookupOptions?)" /> and
/// <see cref="TryGetRate(string, string, DateOnly, RateLookupOptions?, out RateLookupResult)" /> is reserved for the
/// case where no rate is available for an otherwise valid request: the former throws
/// <see cref="KeyNotFoundException" />, the latter returns <see langword="false" /> without allocating.
/// </para>
/// <para>
/// Implementations must accept a <see langword="null" /> <c>options</c> argument and substitute
/// <see cref="RateLookupOptions.Exact" /> (or, for the undated latest surface, an implementation-defined most-recent
/// policy) so callers can opt into the documented safe default by omission.
/// </para>
/// <para>
/// Every getter returns the same element type — a single <see cref="RateLookupResult" /> for the point lookups, and an
/// <see cref="IEnumerable{T}" /> of them for the range lookups. The synchronous getters and the asynchronous getters
/// resolve identical results; the asynchronous surface exists because an implementation backed by a remote feed may
/// fetch on demand, and the synchronous surface may block to do so (or serve only already-loaded data, at the
/// implementation's discretion).
/// </para>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using Bodu.Financial;
///
/// IDatedRateProvider provider = new FixedDatedRateProvider(new[]
/// {
///     new ExchangeRate(CurrencyCode.USD, CurrencyCode.EUR, new DateOnly(2024, 3, 1), 0.92m, "ECB"),
/// });
///
/// // TryGetRate signals a missing rate without throwing; GetRate throws KeyNotFoundException.
/// var date = new DateOnly(2024, 3, 1);
/// if (provider.TryGetRate("USD", "EUR", date, RateLookupOptions.Exact, out var result))
/// {
///     decimal rate = result.Rate.Rate;   // 0.92
/// }
///]]>
/// </code>
/// </example>
/// </remarks>
public interface IDatedRateProvider
{
    /// <summary>
    /// Resolves the most recent available exchange rate from <paramref name="fromIsoCode" /> to
    /// <paramref name="toIsoCode" /> under <paramref name="options" />, throwing if no rate is available.
    /// </summary>
    /// <param name="fromIsoCode">The source-currency ISO-style code.</param>
    /// <param name="toIsoCode">The destination-currency ISO-style code.</param>
    /// <param name="options">
    /// The lookup rules to apply. <see langword="null" /> selects the implementation's default most-recent policy.
    /// </param>
    /// <returns>The resolved <see cref="RateLookupResult" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="fromIsoCode" /> or <paramref name="toIsoCode" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown if either ISO code is not a three-character uppercase ASCII code, or if <paramref name="options" /> is
    /// invalid.
    /// </exception>
    /// <exception cref="KeyNotFoundException">Thrown if no rate is available for the request.</exception>
    RateLookupResult GetRate(
        string fromIsoCode,
        string toIsoCode,
        RateLookupOptions? options = null);

    /// <summary>
    /// Resolves the exchange rate from <paramref name="fromIsoCode" /> to <paramref name="toIsoCode" /> on
    /// <paramref name="date" /> under <paramref name="options" />, throwing if no rate is available.
    /// </summary>
    /// <param name="fromIsoCode">The source-currency ISO-style code.</param>
    /// <param name="toIsoCode">The destination-currency ISO-style code.</param>
    /// <param name="date">The calendar date for which a rate is required.</param>
    /// <param name="options">
    /// The lookup rules to apply, including date-resolution policy and tolerance. <see langword="null" /> is treated as
    /// <see cref="RateLookupOptions.Exact" />.
    /// </param>
    /// <returns>The resolved <see cref="RateLookupResult" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="fromIsoCode" /> or <paramref name="toIsoCode" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown if either ISO code is not a three-character uppercase ASCII code, or if <paramref name="options" /> is
    /// invalid (for example, <see cref="RateDateResolution.Exact" /> with non-zero tolerance).
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="options" /> contains a negative tolerance or an undefined enum value.
    /// </exception>
    /// <exception cref="KeyNotFoundException">
    /// Thrown if no rate is available for the request under the supplied options.
    /// </exception>
    RateLookupResult GetRate(
        string fromIsoCode,
        string toIsoCode,
        DateOnly date,
        RateLookupOptions? options = null);

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
    /// <see cref="RateLookupOptions.Exact" />.
    /// </param>
    /// <param name="result">
    /// When this method returns <see langword="true" />, contains the resolved <see cref="RateLookupResult" />;
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
        RateLookupOptions? options,
        out RateLookupResult result);

    /// <summary>
    /// Returns every available rate from <paramref name="fromIsoCode" /> to <paramref name="toIsoCode" /> whose
    /// observation date falls within the inclusive range <paramref name="startDate" /> to <paramref name="endDate" />.
    /// </summary>
    /// <param name="fromIsoCode">The source-currency ISO-style code.</param>
    /// <param name="toIsoCode">The destination-currency ISO-style code.</param>
    /// <param name="startDate">The inclusive start of the range.</param>
    /// <param name="endDate">The inclusive end of the range.</param>
    /// <returns>
    /// An <see cref="RateRangeResult" /> carrying the rates in the range ordered by date, the requested window, and the
    /// observed span; the result is empty when no rates are available.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="fromIsoCode" /> or <paramref name="toIsoCode" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown if either ISO code is not a three-character uppercase ASCII code, or if <paramref name="endDate" />
    /// precedes <paramref name="startDate" />.
    /// </exception>
    /// <remarks>
    /// The lookup is range-based and does not apply a date-resolution policy: it returns the observations that exist
    /// within the window rather than resolving a single date. An implementation backed by a remote feed may block to
    /// fetch on demand, or serve only already-loaded data; use <see cref="GetRatesAsync" /> to fetch without blocking.
    /// </remarks>
    RateRangeResult GetRates(
        string fromIsoCode,
        string toIsoCode,
        DateOnly startDate,
        DateOnly endDate);

    /// <summary>
    /// Asynchronously resolves the most recent available exchange rate from <paramref name="fromIsoCode" /> to
    /// <paramref name="toIsoCode" /> under <paramref name="options" />, throwing if no rate is available.
    /// </summary>
    /// <param name="fromIsoCode">The source-currency ISO-style code.</param>
    /// <param name="toIsoCode">The destination-currency ISO-style code.</param>
    /// <param name="options">
    /// The lookup rules to apply. <see langword="null" /> selects the implementation's default most-recent policy.
    /// </param>
    /// <param name="cancellationToken">A token to observe while awaiting the operation.</param>
    /// <returns>A task that yields the resolved <see cref="RateLookupResult" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="fromIsoCode" /> or <paramref name="toIsoCode" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown if either ISO code is not a three-character uppercase ASCII code, or if <paramref name="options" /> is
    /// invalid.
    /// </exception>
    /// <exception cref="KeyNotFoundException">Thrown if no rate is available for the request.</exception>
    ValueTask<RateLookupResult> GetRateAsync(
        string fromIsoCode,
        string toIsoCode,
        RateLookupOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously resolves the exchange rate from <paramref name="fromIsoCode" /> to <paramref name="toIsoCode" />
    /// on <paramref name="date" /> under <paramref name="options" />, throwing if no rate is available.
    /// </summary>
    /// <param name="fromIsoCode">The source-currency ISO-style code.</param>
    /// <param name="toIsoCode">The destination-currency ISO-style code.</param>
    /// <param name="date">The calendar date for which a rate is required.</param>
    /// <param name="options">
    /// The lookup rules to apply, including date-resolution policy and tolerance. <see langword="null" /> is treated as
    /// <see cref="RateLookupOptions.Exact" />.
    /// </param>
    /// <param name="cancellationToken">A token to observe while awaiting the operation.</param>
    /// <returns>A task that yields the resolved <see cref="RateLookupResult" />.</returns>
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
    /// <exception cref="KeyNotFoundException">
    /// Thrown if no rate is available for the request under the supplied options.
    /// </exception>
    ValueTask<RateLookupResult> GetRateAsync(
        string fromIsoCode,
        string toIsoCode,
        DateOnly date,
        RateLookupOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously returns every available rate from <paramref name="fromIsoCode" /> to
    /// <paramref name="toIsoCode" /> whose observation date falls within the inclusive range
    /// <paramref name="startDate" /> to <paramref name="endDate" />.
    /// </summary>
    /// <param name="fromIsoCode">The source-currency ISO-style code.</param>
    /// <param name="toIsoCode">The destination-currency ISO-style code.</param>
    /// <param name="startDate">The inclusive start of the range.</param>
    /// <param name="endDate">The inclusive end of the range.</param>
    /// <param name="cancellationToken">A token to observe while awaiting the operation.</param>
    /// <returns>
    /// A task that yields an <see cref="RateRangeResult" /> carrying the rates in the range ordered by date, the
    /// requested window, and the observed span; the result is empty when no rates are available.
    /// </returns>
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
    ValueTask<RateRangeResult> GetRatesAsync(
        string fromIsoCode,
        string toIsoCode,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default);
}
