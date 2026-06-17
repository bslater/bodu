// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlFileExchangeRateCacheTests.Strict.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Verifies the opt-in strict-failure behaviour of <see cref="TomlFileExchangeRateCache" /> (through the shared file
/// base): when <see cref="ExchangeRateCacheOptions.ThrowOnStorageFailure" /> or
/// <see cref="ExchangeRateCacheOptions.ValidateStorageOnStart" /> is set, a file-system failure is surfaced rather than
/// degraded to a skipped write or empty read.
/// </summary>
public sealed partial class TomlFileExchangeRateCacheTests
{
    /// <summary>
    /// Verifies that an atomic range write whose underlying file write fails rethrows the failure when strict failure is
    /// enabled, rather than reporting <see cref="ExchangeRateCacheWriteStatus.Failed" />.
    /// </summary>
    [TestMethod]
    public void StoreFetchedRange_WhenFileWriteFailsAndStrict_ShouldThrow()
    {
        // Occupy the configured cache directory path with a regular file so creating the per-pair directory beneath it
        // fails the write with an IOException.
        Directory.CreateDirectory(Path.GetDirectoryName(_directory)!);
        File.WriteAllText(_directory, "not a directory");

        var cache = new TomlFileExchangeRateCache(new FileExchangeRateCacheOptions
        {
            Provider = Provider,
            CacheDirectory = _directory,
            ThrowOnStorageFailure = true,
        });
        var now = DateTimeOffset.UtcNow;

        _ = Assert.ThrowsExactly<DirectoryNotFoundException>(() =>
        {
            _ = cache.StoreFetchedRange(
                Pair,
                new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5000m, now) },
                new DateOnly(2023, 1, 3),
                new DateOnly(2023, 1, 3),
                Duration,
                now);
        });
    }

    /// <summary>
    /// Verifies that constructing the cache with startup validation over an uncreatable directory throws, surfacing the
    /// misconfigured path at construction.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenDirectoryUncreatableAndValidateStorageOnStart_ShouldThrow()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_directory)!);
        File.WriteAllText(_directory, "not a directory");

        _ = Assert.ThrowsExactly<DirectoryNotFoundException>(() =>
        {
            _ = new TomlFileExchangeRateCache(new FileExchangeRateCacheOptions
            {
                Provider = Provider,
                CacheDirectory = _directory,
                ValidateStorageOnStart = true,
            });
        });
    }

    /// <summary>
    /// Verifies that constructing the cache with startup validation over a creatable directory succeeds, so the probe is
    /// a no-op when the directory can be created.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenDirectoryCreatableAndValidateStorageOnStart_ShouldNotThrow()
    {
        var cache = new TomlFileExchangeRateCache(new FileExchangeRateCacheOptions
        {
            Provider = Provider,
            CacheDirectory = _directory,
            ValidateStorageOnStart = true,
        });

        Assert.AreEqual(Provider, cache.Provider);
    }
}
