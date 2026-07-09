// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GatedYahooExchangeRateSource.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// An <see cref="IPairRateSource{TSeries}" /> whose fetch blocks until released, so a test can hold several
/// concurrent callers inside the source at once and prove the provider coalesces them into a single fetch.
/// </summary>
internal sealed class GatedYahooExchangeRateSource
    : IPairRateSource<YahooSeriesInfo>
{
    /// <summary>The provider options used while parsing the fixture once the gate opens.</summary>
    private readonly YahooRateProviderOptions _options;

    /// <summary>The gate that callers await; the fetch completes only after it is released.</summary>
    private readonly TaskCompletionSource _gate = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Signals when the first caller has entered the fetch, so the test can release the gate after a race has formed.</summary>
    private readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>The number of times <see cref="GetPairAsync" /> has been entered.</summary>
    private int _callCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="GatedYahooExchangeRateSource" /> class.
    /// </summary>
    /// <param name="options">The provider options used while parsing the fixture.</param>
    public GatedYahooExchangeRateSource(YahooRateProviderOptions options)
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
    public async ValueTask<PairRateData<YahooSeriesInfo>> GetPairAsync(CurrencyPairRequest request, CancellationToken cancellationToken = default)
    {
        if (Interlocked.Increment(ref _callCount) == 1)
            _entered.TrySetResult();

        await _gate.Task.ConfigureAwait(false);

        string symbol = _options.BuildSymbol(request.Pair.From.ToString(), request.Pair.To.ToString());
        byte[] json = YahooFixtures.ReadBytes(YahooFixtures.AudUsd);
        return YahooChartResponseParser.Parse(json, request, symbol, _options);
    }
}
