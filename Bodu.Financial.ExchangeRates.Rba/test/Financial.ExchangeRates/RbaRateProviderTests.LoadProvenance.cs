// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaRateProviderTests.LoadProvenance.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Net;
using Bodu.Financial.ExchangeRates.Testing;

namespace Bodu.Financial.ExchangeRates;

public partial class RbaRateProviderTests
{
    /// <summary>
    /// Verifies that an era served from the on-disk workbook cache is stamped with the time the cache entry was written,
    /// the time its bytes were fetched from the RBA, rather than the time it was read back.
    /// </summary>
    [TestMethod]
    public async Task LoadEraAsync_WhenServedFromDiskCache_ShouldStampTheTimeTheEntryWasWritten()
    {
        string directory = Path.Combine(Path.GetTempPath(), "bodu-rba-test", Guid.NewGuid().ToString("N"));
        try
        {
            RbaEraWorkbook era = RbaEraWorkbook.Default[^1];
            RbaRateProviderOptions options = new()
            {
                EnableDiskCache = true,
                CacheDirectory = directory,
                CurrentEraRefreshInterval = TimeSpan.FromDays(7),
            };
            FileSystemRbaWorkbookCache cache = new(directory);
            cache.Store(era, RbaFixtures.ReadBytes(era.FileName));
            string path = Path.Combine(directory, era.FileName);
            File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddDays(-1));
            DateTimeOffset written = new(File.GetLastWriteTimeUtc(path), TimeSpan.Zero);

            using StubHttpMessageHandler handler = new([], HttpStatusCode.ServiceUnavailable);
            using HttpClient client = new(handler);
            using RbaRateProvider provider = new(
                new RbaXlsRateTableSource(client, options, cache),
                options,
                timeProvider: new MutableTimeProvider(DateTimeOffset.UtcNow));

            await provider.LoadEraAsync(era);

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
