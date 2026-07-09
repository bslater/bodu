// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlFileRateCacheTests.Strict.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Verifies the opt-in strict-failure behaviour of <see cref="TomlFileRateCache" /> (through the shared file
/// base): when <see cref="RateCacheOptions.ThrowOnStorageFailure" /> or
/// <see cref="RateCacheOptions.ValidateStorageOnStart" /> is set, a file-system failure is surfaced rather than
/// degraded to a skipped write or empty read.
/// </summary>
public sealed partial class TomlFileRateCacheTests
{
    /// <summary>
    /// Verifies that an atomic range write whose underlying file write fails rethrows the failure when strict failure is
    /// enabled, rather than reporting <see cref="RateCacheWriteStatus.Failed" />.
    /// </summary>
    [TestMethod]
    public void StoreFetchedRange_WhenFileWriteFailsAndStrict_ShouldThrow()
    {
        // Occupy the configured cache directory path with a regular file so creating the per-pair directory beneath it
        // fails the write with an IOException.
        Directory.CreateDirectory(Path.GetDirectoryName(_directory)!);
        File.WriteAllText(_directory, "not a directory");

        var cache = new TomlFileRateCache(new FileRateCacheOptions
        {
            Provider = Provider,
            CacheDirectory = _directory,
            ThrowOnStorageFailure = true,
        });
        DateTimeOffset now = DateTimeOffset.UtcNow;

        _ = Assert.Throws<IOException>(() =>
        {
            _ = cache.StoreFetchedRange(
                Pair,
                new[] { new CachedRate(new DateOnly(2023, 1, 3), 0.5000m, now) },
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

        _ = Assert.Throws<IOException>(() =>
        {
            _ = new TomlFileRateCache(new FileRateCacheOptions
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
        var cache = new TomlFileRateCache(new FileRateCacheOptions
        {
            Provider = Provider,
            CacheDirectory = _directory,
            ValidateStorageOnStart = true,
        });

        Assert.AreEqual(Provider, cache.Provider);
    }
}
