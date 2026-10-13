// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HeldRbaRateTableSource.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Formats.Excel;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// An <see cref="IRbaRateTableSource" /> that holds every download until released and then completes them latest era
/// first, so a test can make a later era's download finish before an earlier one's.
/// </summary>
/// <remarks>
/// Released downloads continue on the releasing thread, one after another, so each runs as far as it can before the
/// next is completed. A download that starts after the release completes at once.
/// </remarks>
internal sealed class HeldRbaRateTableSource
    : IRbaRateTableSource
{
    /// <summary>The options used when parsing an era's workbook.</summary>
    private readonly RbaRateProviderOptions _options;

    /// <summary>The downloads held so far, each with the gate it awaits; guarded by its own lock.</summary>
    private readonly List<(RbaEraWorkbook Era, TaskCompletionSource Gate)> _held = [];

    /// <summary>Whether <see cref="ReleaseLatestFirst" /> has been called; guarded by the lock on <see cref="_held" />.</summary>
    private bool _released;

    /// <summary>
    /// Initializes a new instance of the <see cref="HeldRbaRateTableSource" /> class.
    /// </summary>
    /// <param name="options">The options used when parsing an era's workbook.</param>
    public HeldRbaRateTableSource(RbaRateProviderOptions options)
    {
        _options = options;
    }

    /// <summary>
    /// Completes every held download, the latest era first, and lets every later download complete at once.
    /// </summary>
    public void ReleaseLatestFirst()
    {
        List<(RbaEraWorkbook Era, TaskCompletionSource Gate)> held;
        lock (_held)
        {
            _released = true;
            held = _held.OrderByDescending(static entry => entry.Era.Start).ToList();
            _held.Clear();
        }

        foreach ((RbaEraWorkbook _, TaskCompletionSource gate) in held)
            gate.SetResult();
    }

    /// <inheritdoc />
    public async ValueTask<RbaRateTable> GetTableAsync(RbaEraWorkbook era, CancellationToken cancellationToken = default)
    {
        TaskCompletionSource? gate = null;
        lock (_held)
        {
            if (!_released)
            {
                // Continuations run on the releasing thread, so a released download proceeds before the next is released.
                gate = new TaskCompletionSource();
                _held.Add((era, gate));
            }
        }

        if (gate is not null)
            await gate.Task.ConfigureAwait(false);

        using MemoryStream stream = RbaFixtures.OpenStream(era.FileName);
        using var workbook = ExcelBinaryWorkbook.OpenRead(stream, leaveOpen: true);
        return RbaRateWorkbookParser.Parse(workbook, _options);
    }
}
