// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DisposableProbeProvider.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// A disposable inner provider used only to observe whether a wrapping decorator disposes it. Its lookup members are
/// unsupported because the disposal tests never resolve a rate through it.
/// </summary>
internal sealed class DisposableProbeProvider
    : IDatedRateProvider, IDisposable
{
    /// <summary>
    /// Gets the number of times <see cref="Dispose" /> has been called.
    /// </summary>
    /// <value>The dispose count.</value>
    public int DisposeCount { get; private set; }

    /// <summary>
    /// Records a disposal.
    /// </summary>
    public void Dispose() => DisposeCount++;

    /// <summary>
    /// Not supported; this probe exists only to observe disposal.
    /// </summary>
    /// <param name="fromIsoCode">Unused.</param>
    /// <param name="toIsoCode">Unused.</param>
    /// <param name="options">Unused.</param>
    /// <returns>Never returns.</returns>
    public RateLookupResult GetRate(string fromIsoCode, string toIsoCode, RateLookupOptions? options = null) =>
        throw new NotSupportedException();

    /// <summary>
    /// Not supported; this probe exists only to observe disposal.
    /// </summary>
    /// <param name="fromIsoCode">Unused.</param>
    /// <param name="toIsoCode">Unused.</param>
    /// <param name="date">Unused.</param>
    /// <param name="options">Unused.</param>
    /// <returns>Never returns.</returns>
    public RateLookupResult GetRate(string fromIsoCode, string toIsoCode, DateOnly date, RateLookupOptions? options = null) =>
        throw new NotSupportedException();

    /// <summary>
    /// Not supported; this probe exists only to observe disposal.
    /// </summary>
    /// <param name="fromIsoCode">Unused.</param>
    /// <param name="toIsoCode">Unused.</param>
    /// <param name="date">Unused.</param>
    /// <param name="options">Unused.</param>
    /// <param name="result">Unused.</param>
    /// <returns>Never returns.</returns>
    public bool TryGetRate(string fromIsoCode, string toIsoCode, DateOnly date, RateLookupOptions? options, out RateLookupResult result) =>
        throw new NotSupportedException();

    /// <summary>
    /// Not supported; this probe exists only to observe disposal.
    /// </summary>
    /// <param name="fromIsoCode">Unused.</param>
    /// <param name="toIsoCode">Unused.</param>
    /// <param name="startDate">Unused.</param>
    /// <param name="endDate">Unused.</param>
    /// <returns>Never returns.</returns>
    public RateRangeResult GetRates(string fromIsoCode, string toIsoCode, DateOnly startDate, DateOnly endDate) =>
        throw new NotSupportedException();

    /// <summary>
    /// Not supported; this probe exists only to observe disposal.
    /// </summary>
    /// <param name="fromIsoCode">Unused.</param>
    /// <param name="toIsoCode">Unused.</param>
    /// <param name="options">Unused.</param>
    /// <param name="cancellationToken">Unused.</param>
    /// <returns>Never returns.</returns>
    public ValueTask<RateLookupResult> GetRateAsync(string fromIsoCode, string toIsoCode, RateLookupOptions? options = null, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    /// <summary>
    /// Not supported; this probe exists only to observe disposal.
    /// </summary>
    /// <param name="fromIsoCode">Unused.</param>
    /// <param name="toIsoCode">Unused.</param>
    /// <param name="date">Unused.</param>
    /// <param name="options">Unused.</param>
    /// <param name="cancellationToken">Unused.</param>
    /// <returns>Never returns.</returns>
    public ValueTask<RateLookupResult> GetRateAsync(string fromIsoCode, string toIsoCode, DateOnly date, RateLookupOptions? options = null, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    /// <summary>
    /// Not supported; this probe exists only to observe disposal.
    /// </summary>
    /// <param name="fromIsoCode">Unused.</param>
    /// <param name="toIsoCode">Unused.</param>
    /// <param name="startDate">Unused.</param>
    /// <param name="endDate">Unused.</param>
    /// <param name="cancellationToken">Unused.</param>
    /// <returns>Never returns.</returns>
    public ValueTask<RateRangeResult> GetRatesAsync(string fromIsoCode, string toIsoCode, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}
