// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ReloadableNotableDateServiceTests.Concurrency.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class ReloadableNotableDateServiceTests
{
    /// <summary>
    /// Verifies that concurrent queries racing repeated reloads always observe a complete, consistent result — the
    /// special day on either the pre-reload or post-reload date, never a torn or empty state — exercising the lock-free
    /// snapshot fast path and the gated rebuild.
    /// </summary>
    [TestMethod]
    public async Task Resolve_WhenQueriedConcurrentlyDuringReloads_ShouldAlwaysReturnAConsistentResource()
    {
        NotableDateResource january = NotableDateResourceLoader.Load(JanuaryXml);
        NotableDateResource february = NotableDateResourceLoader.Load(FebruaryXml);
        MutableNotableDateResourceProvider provider = new(january);
        ReloadableNotableDateService service = new(provider);

        DateOnly januaryDate = new(2025, 1, 1);
        DateOnly februaryDate = new(2025, 2, 1);

        using CancellationTokenSource stop = new();

        Task reloader = Task.Run(() =>
        {
            var swap = false;

            while (!stop.IsCancellationRequested)
            {
                provider.Reload(swap ? january : february);
                swap = !swap;
            }
        });

        Task[] readers = Enumerable.Range(0, 4)
            .Select(_ => Task.Run(() =>
            {
                for (var i = 0; i < 500; i++)
                {
                    IReadOnlyList<NotableDate> result = service.Resolve(Year2025, "XX");

                    Assert.HasCount(1, result, "A query observed a torn or empty resolution state.");
                    Assert.IsTrue(
                        result[0].Date == januaryDate || result[0].Date == februaryDate,
                        $"A query observed an inconsistent date {result[0].Date:yyyy-MM-dd}.");
                }
            }))
            .ToArray();

        await Task.WhenAll(readers);
        await stop.CancelAsync();
        await reloader;
    }
}
