// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GatedImfRateTableSource.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// A test table source whose fetches all wait on one gate, recording how many have started, so a test can observe how
/// many downloads a provider runs at once.
/// </summary>
internal sealed class GatedImfRateTableSource
    : IImfRateTableSource
{
    /// <summary>The gate every fetch awaits before it completes.</summary>
    private readonly TaskCompletionSource _gate = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>The number of fetches started.</summary>
    private int _callCount;

    /// <summary>
    /// Gets the number of fetches started.
    /// </summary>
    /// <value>The number of times <see cref="GetTableAsync" /> has been invoked.</value>
    public int CallCount => Volatile.Read(ref _callCount);

    /// <summary>
    /// Releases every fetch waiting on the gate, and every later one.
    /// </summary>
    public void Release() => _gate.TrySetResult();

    /// <inheritdoc />
    public async ValueTask<ImfRateTable> GetTableAsync(ImfReportMonth month, CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _callCount);

        await _gate.Task.ConfigureAwait(false);

        return new ImfRateTable(Array.Empty<ImfRateObservation>());
    }
}
