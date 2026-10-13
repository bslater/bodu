// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HeldImfRateTableSource.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// An <see cref="IImfRateTableSource" /> that holds every download until released and then completes them latest
/// month first, so a test can make a later month's report arrive before an earlier one's.
/// </summary>
/// <remarks>
/// Each report quotes the euro and the yen on every weekday of its month. Released downloads continue on the releasing
/// thread, one after another, so each runs as far as it can before the next is completed. A download that starts after
/// the release completes at once.
/// </remarks>
internal sealed class HeldImfRateTableSource
    : IImfRateTableSource
{
    /// <summary>The downloads held so far, each with the gate it awaits; guarded by its own lock.</summary>
    private readonly List<(ImfReportMonth Month, TaskCompletionSource Gate)> _held = [];

    /// <summary>Whether <see cref="ReleaseLatestFirst" /> has been called; guarded by the lock on <see cref="_held" />.</summary>
    private bool _released;

    /// <summary>
    /// Completes every held download, the latest month first, and lets every later download complete at once.
    /// </summary>
    public void ReleaseLatestFirst()
    {
        List<(ImfReportMonth Month, TaskCompletionSource Gate)> held;
        lock (_held)
        {
            _released = true;
            held = _held.OrderByDescending(static entry => (entry.Month.Year * 12) + entry.Month.Month).ToList();
            _held.Clear();
        }

        foreach ((ImfReportMonth _, TaskCompletionSource gate) in held)
            gate.SetResult();
    }

    /// <inheritdoc />
    public async ValueTask<ImfRateTable> GetTableAsync(ImfReportMonth month, CancellationToken cancellationToken = default)
    {
        TaskCompletionSource? gate = null;
        lock (_held)
        {
            if (!_released)
            {
                // Continuations run on the releasing thread, so a released download proceeds before the next is released.
                gate = new TaskCompletionSource();
                _held.Add((month, gate));
            }
        }

        if (gate is not null)
            await gate.Task.ConfigureAwait(false);

        List<ImfRateObservation> observations = [];
        for (DateOnly day = new(month.Year, month.Month, 1); day.Month == month.Month; day = day.AddDays(1))
        {
            if (day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                continue;

            observations.Add(new ImfRateObservation(day, "EUR", 1.1m));
            observations.Add(new ImfRateObservation(day, "JPY", 0.0067m));
        }

        return new ImfRateTable(observations);
    }
}
