// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IExchangeRatePairLoader.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

/// <summary>
/// Defines the common warm-up and discovery surface shared by every web-backed exchange-rate provider: a caller can
/// prime a pair's window and enumerate the pairs currently held without knowing the concrete provider or how its feed
/// is organized (per pair, per era, per feed, or per range).
/// </summary>
/// <remarks>
/// <para>
/// Every <see cref="WebExchangeRateProvider" /> implements this contract, so a consumer can treat a pair feed (such as
/// Yahoo) and a bulk feed (such as RBA, ECB, or Bank of England) uniformly for cache warming. Providers additionally
/// expose their own strongly-typed discovery surface (for example a <c>GetAvailablePairs</c> that returns feed-specific
/// series metadata); this interface deliberately projects only the common <see cref="ExchangeRatePair" />.
/// </para>
/// </remarks>
public interface IExchangeRatePairLoader
{
    /// <summary>
    /// Fetches and accumulates the inclusive window for a currency pair, unless that window is already covered.
    /// </summary>
    /// <param name="fromIsoCode">The source-currency ISO code.</param>
    /// <param name="toIsoCode">The destination-currency ISO code.</param>
    /// <param name="startDate">The inclusive start of the range.</param>
    /// <param name="endDate">The inclusive end of the range.</param>
    /// <param name="cancellationToken">A token to observe while awaiting the fetch.</param>
    /// <returns>A task that completes when the pair's window has been loaded.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an ISO code is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when an ISO code is malformed, the range is inverted, or the provider does not serve the requested pair.
    /// </exception>
    Task LoadPairAsync(string fromIsoCode, string toIsoCode, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the distinct currency pairs for which observations have been accumulated so far.
    /// </summary>
    /// <returns>A snapshot of the loaded pairs; empty when nothing has been loaded.</returns>
    IReadOnlyCollection<ExchangeRatePair> GetLoadedPairs();
}
