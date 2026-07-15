// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GatedDatedRateProvider.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// An <see cref="IDatedRateProvider" /> test double whose lookups block on a gate and count invocations, so a test can
/// hold a background refresh in flight, pile up further triggers, and assert how many inner fetches actually ran.
/// </summary>
internal sealed class GatedDatedRateProvider : IDatedRateProvider
{
    /// <summary>The gate every lookup waits on before resolving.</summary>
    private readonly ManualResetEventSlim _gate = new(initialState: false);

    /// <summary>The fixed provider backing the gated lookups.</summary>
    private readonly FixedDatedRateProvider _inner;

    /// <summary>The number of single-date lookups started.</summary>
    private int _singleDateCalls;

    /// <summary>The number of range lookups started.</summary>
    private int _rangeCalls;

    /// <summary>
    /// Initializes a new instance of the <see cref="GatedDatedRateProvider" /> class.
    /// </summary>
    /// <param name="rates">The observations the inner provider resolves from.</param>
    public GatedDatedRateProvider(IEnumerable<ExchangeRate> rates) =>
        _inner = new FixedDatedRateProvider(rates);

    /// <summary>Gets the number of single-date lookups started.</summary>
    /// <value>The invocation count.</value>
    public int SingleDateCalls => Volatile.Read(ref _singleDateCalls);

    /// <summary>Gets the number of range lookups started.</summary>
    /// <value>The invocation count.</value>
    public int RangeCalls => Volatile.Read(ref _rangeCalls);

    /// <summary>Releases every caller waiting on the gate.</summary>
    public void Open() => _gate.Set();

    /// <inheritdoc />
    public RateLookupResult GetRate(string fromIsoCode, string toIsoCode, RateLookupOptions? options = null)
    {
        EnterSingleDate();
        return _inner.GetRate(fromIsoCode, toIsoCode, options);
    }

    /// <inheritdoc />
    public RateLookupResult GetRate(string fromIsoCode, string toIsoCode, DateOnly date, RateLookupOptions? options = null)
    {
        EnterSingleDate();
        return _inner.GetRate(fromIsoCode, toIsoCode, date, options);
    }

    /// <inheritdoc />
    public bool TryGetRate(string fromIsoCode, string toIsoCode, DateOnly date, RateLookupOptions? options, out RateLookupResult result)
    {
        EnterSingleDate();
        return _inner.TryGetRate(fromIsoCode, toIsoCode, date, options, out result);
    }

    /// <inheritdoc />
    public RateRangeResult GetRates(string fromIsoCode, string toIsoCode, DateOnly startDate, DateOnly endDate)
    {
        EnterRange();
        return _inner.GetRates(fromIsoCode, toIsoCode, startDate, endDate);
    }

    /// <inheritdoc />
    public ValueTask<RateLookupResult> GetRateAsync(string fromIsoCode, string toIsoCode, RateLookupOptions? options = null, CancellationToken cancellationToken = default)
    {
        EnterSingleDate();
        return _inner.GetRateAsync(fromIsoCode, toIsoCode, options, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<RateLookupResult> GetRateAsync(string fromIsoCode, string toIsoCode, DateOnly date, RateLookupOptions? options = null, CancellationToken cancellationToken = default)
    {
        EnterSingleDate();
        return _inner.GetRateAsync(fromIsoCode, toIsoCode, date, options, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<RateRangeResult> GetRatesAsync(string fromIsoCode, string toIsoCode, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
    {
        EnterRange();
        return _inner.GetRatesAsync(fromIsoCode, toIsoCode, startDate, endDate, cancellationToken);
    }

    /// <summary>Counts a single-date lookup and blocks on the gate.</summary>
    private void EnterSingleDate()
    {
        Interlocked.Increment(ref _singleDateCalls);
        _gate.Wait(TimeSpan.FromSeconds(30));
    }

    /// <summary>Counts a range lookup and blocks on the gate.</summary>
    private void EnterRange()
    {
        Interlocked.Increment(ref _rangeCalls);
        _gate.Wait(TimeSpan.FromSeconds(30));
    }
}
