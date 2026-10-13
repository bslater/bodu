// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ImfRateProviderTests.LoadProvenance.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Net;
using Bodu.Financial.ExchangeRates.Testing;
using Bodu.Test.Time;

namespace Bodu.Financial.ExchangeRates;

public partial class ImfRateProviderTests
{
    /// <summary>
    /// Verifies that a month served from the on-disk report cache is stamped with the time the cache entry was written,
    /// the time its bytes were fetched from the IMF, rather than the time it was read back.
    /// </summary>
    [TestMethod]
    public async Task LoadMonthAsync_WhenServedFromDiskCache_ShouldStampTheTimeTheEntryWasWritten()
    {
        string directory = Path.Combine(Path.GetTempPath(), "bodu-imf-test", Guid.NewGuid().ToString("N"));
        try
        {
            ImfReportMonth month = new(2026, 4);
            ImfRateProviderOptions options = new() { CacheDirectory = directory };
            FileSystemImfReportCache cache = new(directory);
            cache.Store(month, ImfFixtures.ReadBytes(ImfFixtures.Rep202604));
            string path = Path.Combine(directory, month.FileName);
            File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddDays(-1));
            DateTimeOffset written = new(File.GetLastWriteTimeUtc(path), TimeSpan.Zero);

            using StubHttpMessageHandler handler = new([], HttpStatusCode.ServiceUnavailable);
            using HttpClient client = new(handler);
            using ImfRateProvider provider = new(
                new ImfReportTableSource(client, options, cache),
                options,
                timeProvider: new MutableTimeProvider(DateTimeOffset.UtcNow));

            await provider.LoadMonthAsync(month);

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
