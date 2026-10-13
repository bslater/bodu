// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EcbRateProviderTests.LoadProvenance.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Net;
using Bodu.Financial.ExchangeRates.Testing;

namespace Bodu.Financial.ExchangeRates;

public partial class EcbRateProviderTests
{
    /// <summary>
    /// Verifies that a feed served from the on-disk feed cache is stamped with the time the cache entry was written, the
    /// time its bytes were fetched from the ECB, rather than the time it was read back.
    /// </summary>
    [TestMethod]
    public async Task LoadFeedAsync_WhenServedFromDiskCache_ShouldStampTheTimeTheEntryWasWritten()
    {
        string directory = Path.Combine(Path.GetTempPath(), "bodu-ecb-test", Guid.NewGuid().ToString("N"));
        try
        {
            EcbRateProviderOptions options = new() { CacheDirectory = directory, RefreshInterval = TimeSpan.FromDays(7) };
            FileSystemEcbFeedCache cache = new(directory);
            cache.Store(EcbRateFeed.Full, EcbFixtures.ReadBytes(EcbFixtures.Sample));
            string path = Path.Combine(directory, EcbRateFeed.Full.FileName);
            File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddDays(-1));
            DateTimeOffset written = new(File.GetLastWriteTimeUtc(path), TimeSpan.Zero);

            using StubHttpMessageHandler handler = new([], HttpStatusCode.ServiceUnavailable);
            using HttpClient client = new(handler);
            using EcbRateProvider provider = new(
                new EcbXmlRateTableSource(client, options, cache),
                options,
                timeProvider: new MutableTimeProvider(DateTimeOffset.UtcNow));

            await provider.LoadFeedAsync(EcbRateFeed.Full);

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
