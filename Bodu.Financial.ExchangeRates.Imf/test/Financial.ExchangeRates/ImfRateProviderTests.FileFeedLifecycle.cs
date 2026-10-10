// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ImfRateProviderTests.FileFeedLifecycle.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class ImfRateProviderTests
{
    /// <summary>
    /// A failed shared fetch must not poison the month key; retry succeeds and subsequent loads are idempotent.
    /// </summary>
    [TestMethod]
    public async Task LoadMonthAsync_WhenFirstFetchFails_ShouldRetryAndThenSkipLoadedMonth()
    {
        var source = new FailOnceSource();
        var options = new ImfRateProviderOptions { EnableDiskCache = false };
        using var provider = new ImfRateProvider(source, options);
        var month = new ImfReportMonth(2026, 4);

        await Assert.ThrowsExactlyAsync<IOException>(async () => await provider.LoadMonthAsync(month));
        await provider.LoadMonthAsync(month);
        await provider.LoadMonthAsync(month);

        Assert.AreEqual(2, source.Calls);
    }

    private sealed class FailOnceSource : IImfRateTableSource
    {
        internal int Calls { get; private set; }

        public ValueTask<ImfRateTable> GetTableAsync(ImfReportMonth month, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Calls == 1
                ? ValueTask.FromException<ImfRateTable>(new IOException("first attempt"))
                : ValueTask.FromResult(new ImfRateTable(Array.Empty<ImfRateObservation>()));
        }
    }
}
