// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FileSystemBoeResponseCacheTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Boe;

/// <summary>
/// Verifies the freshness and resilience behavior of <see cref="FileSystemBoeResponseCache" />.
/// </summary>
[TestClass]
public class FileSystemBoeResponseCacheTests
{
    /// <summary>
    /// The inclusive start of the range used by the cache tests.
    /// </summary>
    private static readonly DateOnly s_from = new(2023, 1, 1);

    /// <summary>
    /// The inclusive end of the range used by the cache tests.
    /// </summary>
    private static readonly DateOnly s_to = new(2023, 1, 31);

    /// <summary>
    /// Verifies that bytes stored for a range are returned while still within the refresh interval.
    /// </summary>
    [TestMethod]
    public void TryGet_WhenStoredAndFresh_ShouldReturnBytes()
    {
        var directory = CreateTempDirectory();
        try
        {
            FileSystemBoeResponseCache cache = new(directory);
            var payload = new byte[] { 1, 2, 3, 4 };
            cache.Store(s_from, s_to, payload);

            var hit = cache.TryGet(s_from, s_to, TimeSpan.FromHours(1), out var bytes);

            Assert.IsTrue(hit);
            CollectionAssert.AreEqual(payload, bytes);
        }
        finally
        {
            Cleanup(directory);
        }
    }

    /// <summary>
    /// Verifies that two distinct ranges are cached independently.
    /// </summary>
    [TestMethod]
    public void TryGet_WhenDifferentRange_ShouldReturnMiss()
    {
        var directory = CreateTempDirectory();
        try
        {
            FileSystemBoeResponseCache cache = new(directory);
            cache.Store(s_from, s_to, new byte[] { 1 });

            var hit = cache.TryGet(s_from, new DateOnly(2023, 2, 28), TimeSpan.FromHours(1), out _);

            Assert.IsFalse(hit);
        }
        finally
        {
            Cleanup(directory);
        }
    }

    /// <summary>
    /// Verifies that a cached range older than the refresh interval is reported as a miss.
    /// </summary>
    [TestMethod]
    public void TryGet_WhenStale_ShouldReturnMiss()
    {
        var directory = CreateTempDirectory();
        try
        {
            FileSystemBoeResponseCache cache = new(directory);
            cache.Store(s_from, s_to, new byte[] { 1 });

            var path = Path.Combine(directory, "boe_20230101_20230131.csv");
            File.SetLastWriteTimeUtc(path, DateTime.UtcNow - TimeSpan.FromHours(2));

            var hit = cache.TryGet(s_from, s_to, TimeSpan.FromMinutes(30), out _);

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
        FileSystemBoeResponseCache cache = new(null);

        Assert.AreEqual(Path.Combine(Path.GetTempPath(), "bodu-boe"), cache.Directory);
    }

    /// <summary>
    /// Creates a unique temporary directory for a test.
    /// </summary>
    /// <returns>The directory path.</returns>
    private static string CreateTempDirectory() =>
        Path.Combine(Path.GetTempPath(), "bodu-boe-test", Guid.NewGuid().ToString("N"));

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
