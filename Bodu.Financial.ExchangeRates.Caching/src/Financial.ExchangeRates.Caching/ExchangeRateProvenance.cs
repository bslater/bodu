// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateProvenance.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Captures the lineage of a single served exchange-rate result: the source it was cached under, whether it was a live
/// fetch or a cache serve, the cache backend that served it, and — for a cache serve — the instant the served data was
/// cached and the derived age at the time of the lookup.
/// </summary>
/// <param name="Provider">The provider name the served rows are cached under.</param>
/// <param name="Origin">Whether the rate was resolved live from the inner provider or served from the cache.</param>
/// <param name="Backend">The runtime identity of the cache backend that served the request.</param>
/// <param name="CachedAtUtc">
/// The UTC instant the served rows were cached, or <see langword="null" /> for a live serve.
/// </param>
/// <param name="Age">
/// The elapsed time between <paramref name="CachedAtUtc" /> and the lookup instant, or <see langword="null" /> for a
/// live serve or when no row backs the serve.
/// </param>
/// <remarks>
/// This type is an internal diagnostic carrier produced at each serve point of
/// <see cref="CachingExchangeRateProviderBase" />; it does not form part of any public surface. Construct instances
/// through <see cref="Live" /> and <see cref="FromCache" /> rather than the positional constructor so the
/// origin-specific invariants — a live serve carries neither a cache instant nor an age — are applied consistently.
/// </remarks>
internal readonly record struct ExchangeRateProvenance(
    string Provider,
    ExchangeRateOrigin Origin,
    string Backend,
    DateTimeOffset? CachedAtUtc,
    TimeSpan? Age)
{
    /// <summary>
    /// Creates provenance for a rate resolved live from the wrapped inner provider, leaving the cache instant and age
    /// unset.
    /// </summary>
    /// <param name="provider">The provider name the rate is cached under.</param>
    /// <param name="backend">The runtime identity of the cache backend.</param>
    /// <returns>
    /// An <see cref="ExchangeRateProvenance" /> with <see cref="Origin" /> set to
    /// <see cref="ExchangeRateOrigin.Live" /> and no cache instant or age.
    /// </returns>
    public static ExchangeRateProvenance Live(string provider, string backend) =>
        new(provider, ExchangeRateOrigin.Live, backend, CachedAtUtc: null, Age: null);

    /// <summary>
    /// Creates provenance for a rate served from the cache, deriving its age from the served instant and the lookup
    /// instant.
    /// </summary>
    /// <param name="provider">The provider name the rate is cached under.</param>
    /// <param name="backend">The runtime identity of the cache backend that served the request.</param>
    /// <param name="cachedAtUtc">
    /// The UTC instant the served rows were cached, or <see langword="null" /> when no row backs the serve.
    /// </param>
    /// <param name="asOf">The lookup instant against which the age is derived.</param>
    /// <returns>
    /// An <see cref="ExchangeRateProvenance" /> with <see cref="Origin" /> set to
    /// <see cref="ExchangeRateOrigin.Cache" />, carrying <paramref name="cachedAtUtc" /> and the derived age, or no age
    /// when <paramref name="cachedAtUtc" /> is <see langword="null" />.
    /// </returns>
    public static ExchangeRateProvenance FromCache(string provider, string backend, DateTimeOffset? cachedAtUtc, DateTimeOffset asOf) =>
        new(provider, ExchangeRateOrigin.Cache, backend, cachedAtUtc, cachedAtUtc is { } c ? asOf - c : null);
}
