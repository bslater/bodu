// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GatedEcbExchangeRateTableSource.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// An <see cref="IEcbExchangeRateTableSource" /> whose fetch blocks until released, so a test can hold several
/// concurrent callers inside the source at once and prove the provider coalesces them into a single fetch.
/// </summary>
internal sealed class GatedEcbExchangeRateTableSource
    : IEcbExchangeRateTableSource
{
    /// <summary>The options used when parsing the feed fixture once the gate opens.</summary>
    private readonly EcbExchangeRateOptions _options;

    /// <summary>The embedded fixture file name to parse.</summary>
    private readonly string _fileName;

    /// <summary>The gate that callers await; the fetch completes only after it is released.</summary>
    private readonly TaskCompletionSource _gate = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Signals when the first caller has entered the fetch, so the test can release the gate after a race has formed.</summary>
    private readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>The number of times <see cref="GetTableAsync" /> has been entered.</summary>
    private int _callCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="GatedEcbExchangeRateTableSource" /> class.
    /// </summary>
    /// <param name="options">The options used when parsing the feed fixture.</param>
    /// <param name="fileName">The embedded fixture file name to parse.</param>
    public GatedEcbExchangeRateTableSource(EcbExchangeRateOptions options, string fileName = EcbFixtures.Sample)
    {
        _options = options;
        _fileName = fileName;
    }

    /// <summary>
    /// Gets the number of times <see cref="GetTableAsync" /> has been entered.
    /// </summary>
    /// <returns>The fetch count, read atomically.</returns>
    public int CallCount => Volatile.Read(ref _callCount);

    /// <summary>
    /// Gets a task that completes once the first caller has entered the fetch.
    /// </summary>
    /// <returns>The arrival task.</returns>
    public Task Entered => _entered.Task;

    /// <summary>
    /// Releases the gate so every waiting fetch can complete.
    /// </summary>
    public void Release() => _gate.TrySetResult();

    /// <inheritdoc />
    public async ValueTask<EcbExchangeRateTable> GetTableAsync(EcbExchangeRateFeed feed, CancellationToken cancellationToken = default)
    {
        if (Interlocked.Increment(ref _callCount) == 1)
            _entered.TrySetResult();

        await _gate.Task.ConfigureAwait(false);

        using MemoryStream stream = EcbFixtures.OpenStream(_fileName);
        return EcbExchangeRateXmlParser.Parse(stream, _options);
    }
}
