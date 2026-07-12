// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FileSystemImfReportCacheTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies the freshness and resilience behaviour of <see cref="FileSystemImfReportCache" />, in particular the
/// permanent freshness of a closed month's report.
/// </summary>
[TestClass]
public sealed class FileSystemImfReportCacheTests
{
    /// <summary>
    /// Verifies that a report stored for the current month is returned while still within the refresh interval.
    /// </summary>
    [TestMethod]
    public void TryGet_WhenCurrentMonthAndFresh_ShouldReturnBytes()
    {
        string directory = CreateTempDirectory();
        try
        {
            FileSystemImfReportCache cache = new(directory);
            DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
            ImfReportMonth month = new(today.Year, today.Month);
            byte[] payload = new byte[] { 1, 2, 3, 4 };
            cache.Store(month, payload);

            bool hit = cache.TryGet(month, TimeSpan.FromHours(1), out byte[]? bytes);

            Assert.IsTrue(hit);
            CollectionAssert.AreEqual(payload, bytes);
        }
        finally
        {
            Cleanup(directory);
        }
    }

    /// <summary>
    /// Verifies that a current-month report older than the refresh interval is reported as a miss.
    /// </summary>
    [TestMethod]
    public void TryGet_WhenCurrentMonthStale_ShouldReturnMiss()
    {
        string directory = CreateTempDirectory();
        try
        {
            FileSystemImfReportCache cache = new(directory);
            DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
            ImfReportMonth month = new(today.Year, today.Month);
            cache.Store(month, new byte[] { 1 });

            string path = Path.Combine(directory, month.FileName);
            File.SetLastWriteTimeUtc(path, DateTime.UtcNow - TimeSpan.FromHours(2));

            bool hit = cache.TryGet(month, TimeSpan.FromMinutes(30), out _);

            Assert.IsFalse(hit);
        }
        finally
        {
            Cleanup(directory);
        }
    }

    /// <summary>
    /// Verifies that a report for a closed (past) month is served regardless of age, because a closed month's report is
    /// immutable.
    /// </summary>
    [TestMethod]
    public void TryGet_WhenClosedMonthAndOld_ShouldReturnBytes()
    {
        string directory = CreateTempDirectory();
        try
        {
            FileSystemImfReportCache cache = new(directory);
            ImfReportMonth month = new(2020, 1);
            byte[] payload = new byte[] { 9, 8, 7 };
            cache.Store(month, payload);

            string path = Path.Combine(directory, month.FileName);
            File.SetLastWriteTimeUtc(path, DateTime.UtcNow - TimeSpan.FromDays(365));

            bool hit = cache.TryGet(month, TimeSpan.FromMinutes(1), out byte[]? bytes);

            Assert.IsTrue(hit);
            CollectionAssert.AreEqual(payload, bytes);
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
        FileSystemImfReportCache cache = new(null);

        Assert.AreEqual(Path.Combine(Path.GetTempPath(), "bodu-imf"), cache.Directory);
    }

    /// <summary>
    /// Creates a unique temporary directory for a test.
    /// </summary>
    /// <returns>The directory path.</returns>
    private static string CreateTempDirectory() =>
        Path.Combine(Path.GetTempPath(), "bodu-imf-test", Guid.NewGuid().ToString("N"));

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
