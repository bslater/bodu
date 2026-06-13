// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FileSystemEcbFeedCacheTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Ecb;

/// <summary>
/// Verifies the freshness and resilience behavior of <see cref="FileSystemEcbFeedCache" />.
/// </summary>
[TestClass]
public class FileSystemEcbFeedCacheTests
{
    /// <summary>
    /// Verifies that bytes stored for a feed are returned while still within the refresh interval.
    /// </summary>
    [TestMethod]
    public void TryGet_WhenStoredAndFresh_ShouldReturnBytes()
    {
        var directory = CreateTempDirectory();
        try
        {
            FileSystemEcbFeedCache cache = new(directory);
            var payload = new byte[] { 1, 2, 3, 4 };
            cache.Store(EcbExchangeRateFeed.Full, payload);

            var hit = cache.TryGet(EcbExchangeRateFeed.Full, TimeSpan.FromHours(1), out var bytes);

            Assert.IsTrue(hit);
            CollectionAssert.AreEqual(payload, bytes);
        }
        finally
        {
            Cleanup(directory);
        }
    }

    /// <summary>
    /// Verifies that a cached feed older than the refresh interval is reported as a miss.
    /// </summary>
    [TestMethod]
    public void TryGet_WhenStale_ShouldReturnMiss()
    {
        var directory = CreateTempDirectory();
        try
        {
            FileSystemEcbFeedCache cache = new(directory);
            cache.Store(EcbExchangeRateFeed.Full, new byte[] { 1 });

            var path = Path.Combine(directory, EcbExchangeRateFeed.Full.FileName);
            File.SetLastWriteTimeUtc(path, DateTime.UtcNow - TimeSpan.FromHours(2));

            var hit = cache.TryGet(EcbExchangeRateFeed.Full, TimeSpan.FromMinutes(30), out _);

            Assert.IsFalse(hit);
        }
        finally
        {
            Cleanup(directory);
        }
    }

    /// <summary>
    /// Verifies that requesting a feed never stored is reported as a miss.
    /// </summary>
    [TestMethod]
    public void TryGet_WhenAbsent_ShouldReturnMiss()
    {
        var directory = CreateTempDirectory();
        try
        {
            FileSystemEcbFeedCache cache = new(directory);

            var hit = cache.TryGet(EcbExchangeRateFeed.Full, TimeSpan.FromHours(1), out _);

            Assert.IsFalse(hit);
        }
        finally
        {
            Cleanup(directory);
        }
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> directory falls back to a folder under the system temporary path.
    /// </summary>
    [TestMethod]
    public void Directory_WhenNull_ShouldUseTempFallback()
    {
        FileSystemEcbFeedCache cache = new(null);

        Assert.AreEqual(Path.Combine(Path.GetTempPath(), "bodu-ecb"), cache.Directory);
    }

    /// <summary>
    /// Creates a unique temporary directory for a test.
    /// </summary>
    /// <returns>The directory path.</returns>
    private static string CreateTempDirectory() =>
        Path.Combine(Path.GetTempPath(), "bodu-ecb-test", Guid.NewGuid().ToString("N"));

    /// <summary>
    /// Removes a temporary directory, ignoring failures.
    /// </summary>
    /// <param name="directory">The directory to remove.</param>
    private static void Cleanup(string directory)
    {
        try
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
        catch (IOException)
        {
            // Best-effort cleanup.
        }
    }
}
