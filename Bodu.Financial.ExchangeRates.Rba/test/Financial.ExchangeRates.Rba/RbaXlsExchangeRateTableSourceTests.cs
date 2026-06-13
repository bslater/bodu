// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaXlsExchangeRateTableSourceTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Linq;

namespace Bodu.Financial.ExchangeRates.Rba;

/// <summary>
/// Verifies that <see cref="RbaXlsExchangeRateTableSource" /> downloads, parses, and caches era files.
/// </summary>
[TestClass]
public class RbaXlsExchangeRateTableSourceTests
{
    private static readonly RbaEra s_immutableEra = new("2018-2022", new DateOnly(2018, 1, 1), new DateOnly(2022, 12, 31));

    /// <summary>
    /// Verifies that the source downloads an era file and parses it into a table.
    /// </summary>
    [TestMethod]
    public async Task GetTableAsync_ShouldDownloadAndParse()
    {
        RbaExchangeRateOptions options = new();
        StubHttpMessageHandler handler = new(RbaFixtures.ReadBytes(RbaFixtures.Sample));
        using HttpClient client = new(handler);
        RbaXlsExchangeRateTableSource source = new(client, options, NullRbaWorkbookCache.Instance);

        RbaExchangeRateTable table = await source.GetTableAsync(s_immutableEra);

        Assert.IsTrue(table.Series.Any(s => s.CurrencyCode == "USD"));
        Assert.AreEqual(1, handler.RequestCount);
    }

    /// <summary>
    /// Verifies that a cached immutable era is served from disk on the second request without re-downloading.
    /// </summary>
    [TestMethod]
    public async Task GetTableAsync_Whens_immutableEraCached_ShouldNotRefetch()
    {
        var directory = Path.Combine(Path.GetTempPath(), "bodu-rba-src-" + Guid.NewGuid().ToString("N"));
        try
        {
            RbaExchangeRateOptions options = new();
            StubHttpMessageHandler handler = new(RbaFixtures.ReadBytes(RbaFixtures.Sample));
            using HttpClient client = new(handler);
            RbaXlsExchangeRateTableSource source = new(client, options, new FileSystemRbaWorkbookCache(directory));

            _ = await source.GetTableAsync(s_immutableEra);
            _ = await source.GetTableAsync(s_immutableEra);

            Assert.AreEqual(1, handler.RequestCount);
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }
}
