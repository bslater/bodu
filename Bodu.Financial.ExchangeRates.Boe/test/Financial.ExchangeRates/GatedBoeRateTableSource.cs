// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GatedBoeRateTableSource.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// An <see cref="IBoeRateTableSource" /> whose fetch blocks until released, so a test can hold several
/// concurrent callers inside the source at once and prove the provider coalesces them into a single fetch.
/// </summary>
internal sealed class GatedBoeRateTableSource
    : IBoeRateTableSource
{
    /// <summary>The options used when parsing the response fixture once the gate opens.</summary>
    private readonly BoeRateProviderOptions _options;

    /// <summary>The embedded fixture file name to parse.</summary>
    private readonly string _fileName;

    /// <summary>The gate that callers await; the fetch completes only after it is released.</summary>
    private readonly TaskCompletionSource _gate = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Signals when the first caller has entered the fetch, so the test can release the gate after a race has formed.</summary>
    private readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>The number of times <see cref="GetTableAsync" /> has been entered.</summary>
    private int _callCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="GatedBoeRateTableSource" /> class.
    /// </summary>
    /// <param name="options">The options used when parsing the response fixture.</param>
    /// <param name="fileName">The embedded fixture file name to parse.</param>
    public GatedBoeRateTableSource(BoeRateProviderOptions options, string fileName = BoeFixtures.Sample)
    {
        _options = options;
        _fileName = fileName;
    }

    /// <summary>
    /// Gets the number of times <see cref="GetTableAsync" /> has been entered.
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
    public async ValueTask<BoeRateTable> GetTableAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
    {
        if (Interlocked.Increment(ref _callCount) == 1)
            _entered.TrySetResult();

        await _gate.Task.ConfigureAwait(false);

        using MemoryStream stream = BoeFixtures.OpenStream(_fileName);
        return BoeExchangeRateCsvParser.Parse(stream, _options);
    }
}
