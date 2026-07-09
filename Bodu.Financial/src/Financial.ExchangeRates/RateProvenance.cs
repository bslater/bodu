// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateProvenance.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Captures the lineage of a single served exchange rate: the provider name it is attributed to, whether it was
/// resolved directly by a provider or served from a cache, the cache backend that served it, and — for a cache serve —
/// the instant the served data was cached together with the derived age at the time of the lookup.
/// </summary>
/// <param name="Provider">The provider name the served rate is attributed to.</param>
/// <param name="Origin">Whether the rate was resolved directly by a provider or served from a cache.</param>
/// <param name="Backend">
/// The runtime identity of the cache backend that served the request, or <see langword="null" /> for a rate resolved
/// directly by a provider.
/// </param>
/// <param name="CachedAtUtc">
/// The UTC instant the served data was cached, or <see langword="null" /> for a rate resolved directly by a provider.
/// </param>
/// <param name="Age">
/// The elapsed time between <paramref name="CachedAtUtc" /> and the lookup instant, or <see langword="null" /> for a
/// rate resolved directly by a provider or when no cached row backs the serve.
/// </param>
/// <remarks>
/// <para>
/// Every <see cref="RateLookupResult" /> carries a populated <see cref="RateProvenance" />. A rate
/// resolved directly by a provider reports <see cref="RateOrigin.Live" /> with a <see langword="null" />
/// <see cref="Backend" />, <see cref="CachedAtUtc" />, and <see cref="Age" />; a rate served from a cache reports
/// <see cref="RateOrigin.Cache" /> with the serving backend, the instant the data was cached, and the age it
/// had carried at the lookup instant.
/// </para>
/// <para>
/// Construct instances through <see cref="Live(string)" />, <see cref="Live(string, string)" />, and
/// <see cref="FromCache" /> rather than the positional constructor so the origin-specific invariants — a live serve
/// carries neither a cache instant nor an age — are applied consistently.
/// </para>
/// </remarks>
public readonly record struct RateProvenance(
    string Provider,
    RateOrigin Origin,
    string? Backend,
    DateTimeOffset? CachedAtUtc,
    TimeSpan? Age)
{
    /// <summary>
    /// Creates provenance for a rate resolved directly by a provider, leaving the cache backend, cache instant, and age
    /// unset.
    /// </summary>
    /// <param name="provider">The provider name the rate is attributed to.</param>
    /// <returns>
    /// An <see cref="RateProvenance" /> with <see cref="Origin" /> set to
    /// <see cref="RateOrigin.Live" /> and no backend, cache instant, or age.
    /// </returns>
    public static RateProvenance Live(string provider) =>
        new(provider, RateOrigin.Live, Backend: null, CachedAtUtc: null, Age: null);

    /// <summary>
    /// Creates provenance for a rate resolved live through a cache-fronted provider's wrapped inner provider, recording
    /// the cache backend that handled the miss while leaving the cache instant and age unset.
    /// </summary>
    /// <param name="provider">The provider name the rate is attributed to.</param>
    /// <param name="backend">The runtime identity of the cache backend that handled the miss.</param>
    /// <returns>
    /// An <see cref="RateProvenance" /> with <see cref="Origin" /> set to
    /// <see cref="RateOrigin.Live" />, carrying <paramref name="backend" /> but no cache instant or age.
    /// </returns>
    public static RateProvenance Live(string provider, string backend) =>
        new(provider, RateOrigin.Live, backend, CachedAtUtc: null, Age: null);

    /// <summary>
    /// Creates provenance for a rate served from a cache, deriving its age from the served instant and the lookup
    /// instant.
    /// </summary>
    /// <param name="provider">The provider name the rate is attributed to.</param>
    /// <param name="backend">The runtime identity of the cache backend that served the request.</param>
    /// <param name="cachedAtUtc">
    /// The UTC instant the served data was cached, or <see langword="null" /> when no cached row backs the serve.
    /// </param>
    /// <param name="asOf">The lookup instant against which the age is derived.</param>
    /// <returns>
    /// An <see cref="RateProvenance" /> with <see cref="Origin" /> set to
    /// <see cref="RateOrigin.Cache" />, carrying <paramref name="cachedAtUtc" /> and the derived age, or no age
    /// when <paramref name="cachedAtUtc" /> is <see langword="null" />.
    /// </returns>
    /// <remarks>
    /// The age is clamped to <see cref="TimeSpan.Zero" /> so a never-negative value is reported: cache freshness
    /// tolerates a row stamped slightly ahead of <paramref name="asOf" /> (a row may be written up to about a minute in
    /// the future of the lookup clock), which would otherwise make <c>asOf - cachedAtUtc</c> marginally negative.
    /// </remarks>
    public static RateProvenance FromCache(string provider, string backend, DateTimeOffset? cachedAtUtc, DateTimeOffset asOf) =>
        new(
            provider,
            RateOrigin.Cache,
            backend,
            cachedAtUtc,
            cachedAtUtc is { } c ? Max(asOf - c, TimeSpan.Zero) : null);

    /// <summary>
    /// Returns the greater of two <see cref="TimeSpan" /> values.
    /// </summary>
    /// <param name="left">The first value to compare.</param>
    /// <param name="right">The second value to compare.</param>
    /// <returns>
    /// <paramref name="left" /> when it is greater than or equal to <paramref name="right" />; otherwise
    /// <paramref name="right" />.
    /// </returns>
    private static TimeSpan Max(TimeSpan left, TimeSpan right) =>
        left >= right ? left : right;
}
