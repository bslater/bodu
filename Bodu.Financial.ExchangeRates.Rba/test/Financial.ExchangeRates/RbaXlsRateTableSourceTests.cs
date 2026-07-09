// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaXlsRateTableSourceTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies that <see cref="RbaXlsRateTableSource" /> downloads, parses, and caches era files.
/// </summary>
[TestClass]
public class RbaXlsRateTableSourceTests
{
    private static readonly RbaEraWorkbook s_immutableEra = new("2018-2022", new DateOnly(2018, 1, 1), new DateOnly(2022, 12, 31));

    /// <summary>
    /// Verifies that the source downloads an era file and parses it into a table.
    /// </summary>
    [TestMethod]
    public async Task GetTableAsync_ShouldDownloadAndParse()
    {
        RbaRateProviderOptions options = new();
        StubHttpMessageHandler handler = new(RbaFixtures.ReadBytes(RbaFixtures.Sample));
        using HttpClient client = new(handler);
        RbaXlsRateTableSource source = new(client, options, NullRbaWorkbookCache.Instance);

        RbaRateTable table = await source.GetTableAsync(s_immutableEra);

        Assert.Contains(s => s.CurrencyCode == "USD", table.Series);
        Assert.AreEqual(1, handler.RequestCount);
    }

    /// <summary>
    /// Verifies that a cached immutable era is served from disk on the second request without re-downloading.
    /// </summary>
    [TestMethod]
    public async Task GetTableAsync_Whens_immutableEraCached_ShouldNotRefetch()
    {
        string directory = Path.Combine(Path.GetTempPath(), "bodu-rba-src-" + Guid.NewGuid().ToString("N"));
        try
        {
            RbaRateProviderOptions options = new();
            StubHttpMessageHandler handler = new(RbaFixtures.ReadBytes(RbaFixtures.Sample));
            using HttpClient client = new(handler);
            RbaXlsRateTableSource source = new(client, options, new FileSystemRbaWorkbookCache(directory));

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
