// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ReloadableNotableDateServiceTests.Concurrency.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class ReloadableNotableDateServiceTests
{
    /// <summary>
    /// Verifies that concurrent queries racing repeated reloads always observe a complete, consistent result - the
    /// special day on either the pre-reload or post-reload date, never a torn or empty state - exercising the lock-free
    /// snapshot fast path and the gated rebuild.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The test's running time is bounded whatever the schedule. The reloader gives way after every swap, so that it
    /// cannot hold a processor the readers need, and it stops when the readers finish or after five seconds, whichever
    /// comes first. Once it stops, the snapshot is rebuilt at most once more and every later query takes the fast path.
    /// A run normally takes milliseconds. An earlier form, whose reloader neither gave way nor stopped before the
    /// readers finished, once took more than eleven minutes on a hosted runner.
    /// </para>
    /// <para>
    /// The timeout fails a pathological run instead of letting it consume the job's time: it stops the reloader, and
    /// each reader throws at its next query, so the run is reported as timed out rather than passing on fewer queries.
    /// </para>
    /// </remarks>
    /// <returns>A task that completes when the readers and the reloader have finished.</returns>
    [TestMethod]
    [Timeout(60_000, CooperativeCancellation = true)]
    public async Task Resolve_WhenQueriedConcurrentlyDuringReloads_ShouldAlwaysReturnAConsistentResource()
    {
        NotableDateResource january = NotableDateResourceLoader.Load(JanuaryXml);
        NotableDateResource february = NotableDateResourceLoader.Load(FebruaryXml);
        MutableNotableDateResourceProvider provider = new(january);
        ReloadableNotableDateService service = new(provider);

        DateOnly januaryDate = new(2025, 1, 1);
        DateOnly februaryDate = new(2025, 2, 1);

        CancellationToken timeout = TestContext.CancellationToken;
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(timeout);
        stop.CancelAfter(TimeSpan.FromSeconds(5));

        Task reloader = Task.Run(() =>
        {
            var swap = false;

            while (!stop.IsCancellationRequested)
            {
                provider.Reload(swap ? january : february);
                swap = !swap;

                // A loop that never gives way holds a processor the readers need, and on a small runner may leave them
                // too little to finish.
                Thread.Yield();
            }
        });

        Task[] readers = Enumerable.Range(0, 4)
            .Select(_ => Task.Run(() =>
            {
                for (var i = 0; i < 500; i++)
                {
                    timeout.ThrowIfCancellationRequested();

                    IReadOnlyList<NotableDate> result = service.Resolve(Year2025, "XX");

                    Assert.HasCount(1, result, "A query observed a torn or empty resolution state.");
                    Assert.IsTrue(
                        result[0].Date == januaryDate || result[0].Date == februaryDate,
                        $"A query observed an inconsistent date {result[0].Date:yyyy-MM-dd}.");
                }
            }))
            .ToArray();

        try
        {
            await Task.WhenAll(readers);
        }
        finally
        {
            // Stop the reloader however the readers ended, so that a failed assertion or a timeout does not leave it
            // spinning in the test host.
            await stop.CancelAsync();
            await reloader;
        }
    }
}
