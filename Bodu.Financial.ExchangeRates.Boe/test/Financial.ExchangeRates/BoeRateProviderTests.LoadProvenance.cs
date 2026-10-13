// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoeRateProviderTests.LoadProvenance.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Net;
using Bodu.Financial.ExchangeRates.Testing;

namespace Bodu.Financial.ExchangeRates;

public partial class BoeRateProviderTests
{
    /// <summary>
    /// Verifies that a range served from the on-disk response cache is stamped with the time the cache entry was
    /// written, the time its bytes were fetched from the Bank of England, rather than the time it was read back.
    /// </summary>
    [TestMethod]
    public async Task LoadRangeAsync_WhenServedFromDiskCache_ShouldStampTheTimeTheEntryWasWritten()
    {
        string directory = Path.Combine(Path.GetTempPath(), "bodu-boe-test", Guid.NewGuid().ToString("N"));
        try
        {
            DateOnly start = new(2023, 1, 1);
            DateOnly end = new(2023, 1, 31);
            BoeRateProviderOptions options = new() { CacheDirectory = directory, RefreshInterval = TimeSpan.FromDays(7) };
            FileSystemBoeResponseCache cache = new(directory);
            cache.Store((start, end), BoeFixtures.ReadBytes(BoeFixtures.Sample));
            string path = Path.Combine(directory, "boe_20230101_20230131.csv");
            File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddDays(-1));
            DateTimeOffset written = new(File.GetLastWriteTimeUtc(path), TimeSpan.Zero);

            using StubHttpMessageHandler handler = new([], HttpStatusCode.ServiceUnavailable);
            using HttpClient client = new(handler);
            using BoeRateProvider provider = new(
                new BoeCsvRateTableSource(client, options, cache),
                options,
                timeProvider: new MutableTimeProvider(DateTimeOffset.UtcNow));

            await provider.LoadRangeAsync(start, end);

            Assert.AreEqual(0, handler.RequestCount);
            Assert.AreEqual(written, provider.GetLoadedBook().EnumerateSeries().First().FetchedAtUtc);
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }
}
