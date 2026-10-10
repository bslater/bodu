// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaRateProviderTests.FileFeedLifecycle.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace Bodu.Financial.ExchangeRates;

public partial class RbaRateProviderTests
{
    /// <summary>
    /// Confirms that stream failures are classified as expected download failures by the shared file-feed loader.
    /// The exception is rethrown unchanged and the provider-specific diagnostic event is retained.
    /// </summary>
    [TestMethod]
    public async Task LoadRangeAsync_WhenSourceThrowsIOException_ShouldLogExpectedFailureAndRethrow()
    {
        (RbaRateProvider provider, CapturingLogger logger) = CreateThrowing(new IOException("simulated stream failure"));

        await Assert.ThrowsExactlyAsync<IOException>(async () =>
            await provider.LoadRangeAsync(new DateOnly(2023, 1, 1), new DateOnly(2023, 1, 31)));

        Assert.AreEqual(1, logger.Entries.Count(entry => entry.EventId.Id == EraLoadFailedEventId && entry.Level == LogLevel.Warning));
    }
}
