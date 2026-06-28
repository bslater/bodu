// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GatedXeExchangeRateSource.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// An <see cref="IExchangeRatePairSource{TSeries}" /> whose fetch blocks until released, so a test can hold several
/// concurrent callers inside the source at once and prove the provider coalesces them into a single fetch.
/// </summary>
internal sealed class GatedXeExchangeRateSource
    : IExchangeRatePairSource<XeSeriesInfo>
{
    /// <summary>The provider options used while parsing the fixture once the gate opens.</summary>
    private readonly XeExchangeRateOptions _options;

    /// <summary>The gate that callers await; the fetch completes only after it is released.</summary>
    private readonly TaskCompletionSource _gate = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Signals when the first caller has entered the fetch, so the test can release the gate after a race has formed.</summary>
    private readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>The number of times <see cref="GetPairAsync" /> has been entered.</summary>
    private int _callCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="GatedXeExchangeRateSource" /> class.
    /// </summary>
    /// <param name="options">The provider options used while parsing the fixture.</param>
    public GatedXeExchangeRateSource(XeExchangeRateOptions options)
    {
        _options = options;
    }

    /// <summary>
    /// Gets the number of times <see cref="GetPairAsync" /> has been entered.
    /// </summary>
    /// <value>The fetch count, read atomically.</value>
    public int CallCount => Volatile.Read(ref _callCount);

    /// <summary>
    /// Gets a task that completes once the first caller has entered the fetch.
    /// </summary>
    /// <value>The arrival task.</value>
    public Task Entered => _entered.Task;

    /// <summary>
    /// Releases the gate so every waiting fetch can complete.
    /// </summary>
    public void Release() => _gate.TrySetResult();

    /// <inheritdoc />
    public async ValueTask<PairRateData<XeSeriesInfo>> GetPairAsync(ExchangeRatePairRequest request, CancellationToken cancellationToken = default)
    {
        if (Interlocked.Increment(ref _callCount) == 1)
            _entered.TrySetResult();

        await _gate.Task.ConfigureAwait(false);

        byte[] json = XeFixtures.ReadBytes(XeFixtures.AudUsd);
        return XeChartingRatesResponseParser.Parse(json, request, _options);
    }
}
