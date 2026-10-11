// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ImfRateProviderTests.LoadMonthAsync.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class ImfRateProviderTests
{
    /// <summary>
    /// Verifies that a failed fetch does not mark its month loaded: the next load fetches it again, and a load after
    /// that one succeeds does not fetch it a third time.
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

    /// <summary>
    /// A table source whose first fetch fails with an <see cref="IOException" /> and whose later fetches return an
    /// empty table.
    /// </summary>
    private sealed class FailOnceSource : IImfRateTableSource
    {
        /// <summary>
        /// Gets the number of fetches made so far.
        /// </summary>
        internal int Calls { get; private set; }

        /// <inheritdoc />
        public ValueTask<ImfRateTable> GetTableAsync(ImfReportMonth month, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Calls == 1
                ? ValueTask.FromException<ImfRateTable>(new IOException("first attempt"))
                : ValueTask.FromResult(new ImfRateTable(Array.Empty<ImfRateObservation>()));
        }
    }
}
