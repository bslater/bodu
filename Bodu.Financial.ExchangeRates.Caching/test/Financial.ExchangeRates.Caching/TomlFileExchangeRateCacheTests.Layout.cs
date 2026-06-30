// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlFileExchangeRateCacheTests.Layout.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Verifies how <see cref="TomlFileExchangeRateCache" /> resolves a pair's directory and file paths under the configured
/// layout, including custom folder and file-name rules and the partitioned-layout path members.
/// </summary>
public sealed partial class TomlFileExchangeRateCacheTests
{
    /// <summary>
    /// Verifies that a custom layout's directory and file-name delegates determine the resolved single-file path, and
    /// that the cache writes the pair to that path.
    /// </summary>
    [TestMethod]
    public void ResolveFilePath_WhenCustomLayout_ShouldUseDelegatePath()
    {
        ExchangeRateCacheFileLayout layout = ExchangeRateCacheFileLayout.Create(
            ExchangeRateCachePartitionStrategy.Single,
            directory: ctx => Path.Combine(ctx.Root, "fx", ctx.Provider),
            fileName: ctx => $"{ctx.Pair.From}-{ctx.Pair.To}{ctx.FileExtension}");
        TomlFileExchangeRateCache cache = CreateCache(layout);

        string expected = Path.Combine(_directory, "fx", Provider, "AUD-USD.toml");
        Assert.AreEqual(expected, cache.ResolveFilePath(Pair));

        DateTimeOffset now = DateTimeOffset.UtcNow;
        cache.Store(Pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5m, now) }, Duration, now);

        Assert.IsTrue(File.Exists(expected), "the pair is written to the custom layout's path");
    }

    /// <summary>
    /// Verifies that resolving a single backing file throws for a partitioned layout, where a pair spans multiple files.
    /// </summary>
    [TestMethod]
    public void ResolveFilePath_WhenLayoutIsPartitioned_ShouldThrowInvalidOperationException()
    {
        TomlFileExchangeRateCache cache = CreateCache(ExchangeRateCacheFileLayout.Monthly);

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = cache.ResolveFilePath(Pair);
        });
    }

    /// <summary>
    /// Verifies that the pair directory resolves to the per-pair folder under the provider folder for a partitioned
    /// layout.
    /// </summary>
    [TestMethod]
    public void ResolveDirectory_WhenLayoutIsPartitioned_ShouldReturnPerPairFolder()
    {
        TomlFileExchangeRateCache cache = CreateCache(ExchangeRateCacheFileLayout.Monthly);

        Assert.AreEqual(Path.Combine(_directory, Provider, "AUDUSD"), cache.ResolveDirectory(Pair));
    }

    /// <summary>
    /// Verifies that a partition path resolves to the per-month file named by partition key under the pair's folder.
    /// </summary>
    [TestMethod]
    public void ResolvePartitionPath_WhenLayoutIsMonthly_ShouldReturnMonthKeyedFile()
    {
        TomlFileExchangeRateCache cache = CreateCache(ExchangeRateCacheFileLayout.Monthly);

        string expected = Path.Combine(_directory, Provider, "AUDUSD", "2023-01.toml");
        Assert.AreEqual(expected, cache.ResolvePartitionPath(Pair, new DateOnly(2023, 1, 15)));
    }

    /// <summary>
    /// Verifies that for a single-file layout a partition path resolves to the pair's one file regardless of the date.
    /// </summary>
    [TestMethod]
    public void ResolvePartitionPath_WhenLayoutIsSingleFile_ShouldReturnTheOneFile()
    {
        TomlFileExchangeRateCache cache = CreateCache();

        Assert.AreEqual(cache.ResolveFilePath(Pair), cache.ResolvePartitionPath(Pair, new DateOnly(2023, 1, 15)));
    }

    /// <summary>
    /// Verifies that the constructor rejects options whose layout is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenLayoutIsNull_ShouldThrowArgumentException()
    {
        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new TomlFileExchangeRateCache(new FileExchangeRateCacheOptions { Provider = Provider, CacheDirectory = _directory, Layout = null! });
        });

        Assert.AreEqual("Layout", ex.ParamName);
    }
}
