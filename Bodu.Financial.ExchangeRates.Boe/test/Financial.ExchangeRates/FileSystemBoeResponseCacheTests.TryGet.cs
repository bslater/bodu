// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FileSystemBoeResponseCacheTests.TryGet.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class FileSystemBoeResponseCacheTests
{
    /// <summary>
    /// Verifies that bytes stored for a range are returned while still within the refresh interval.
    /// </summary>
    [TestMethod]
    public void TryGet_WhenStoredAndFresh_ShouldReturnBytes()
    {
        string directory = CreateTempDirectory();
        try
        {
            FileSystemBoeResponseCache cache = new(directory);
            byte[] payload = new byte[] { 1, 2, 3, 4 };
            cache.Store((s_from, s_to), payload);

            bool hit = cache.TryGet((s_from, s_to), TimeSpan.FromHours(1), out byte[]? bytes);

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
        string directory = CreateTempDirectory();
        try
        {
            FileSystemBoeResponseCache cache = new(directory);
            cache.Store((s_from, s_to), new byte[] { 1 });

            bool hit = cache.TryGet((s_from, new DateOnly(2023, 2, 28)), TimeSpan.FromHours(1), out _);

            Assert.IsFalse(hit);
        }
        finally
        {
            Cleanup(directory);
        }
    }

    /// <summary>
    /// Verifies that a cached range that was still open when it was cached, and is older than the refresh interval, is
    /// reported as a miss.
    /// </summary>
    [TestMethod]
    public void TryGet_WhenStale_ShouldReturnMiss()
    {
        string directory = CreateTempDirectory();
        try
        {
            DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
            (DateOnly StartDate, DateOnly EndDate) range = (today.AddDays(-30), today);
            FileSystemBoeResponseCache cache = new(directory);
            cache.Store(range, new byte[] { 1 });

            string path = Directory.GetFiles(directory).Single();
            File.SetLastWriteTimeUtc(path, DateTime.UtcNow - TimeSpan.FromHours(2));

            bool hit = cache.TryGet(range, TimeSpan.FromMinutes(30), out _);

            Assert.IsFalse(hit);
        }
        finally
        {
            Cleanup(directory);
        }
    }

    /// <summary>
    /// Verifies that a response for a range that had ended before it was cached is served however old it is: the Bank
    /// of England does not revise a day once it has passed, so downloading the range again could only return the same
    /// rates.
    /// </summary>
    [TestMethod]
    public void TryGet_WhenRangeEndedBeforeItWasCached_ShouldReturnBytesHoweverOld()
    {
        string directory = CreateTempDirectory();
        try
        {
            FileSystemBoeResponseCache cache = new(directory);
            cache.Store((s_from, s_to), new byte[] { 1 });
            File.SetLastWriteTimeUtc(Path.Combine(directory, "boe_20230101_20230131.csv"), DateTime.UtcNow.AddDays(-30));

            bool hit = cache.TryGet((s_from, s_to), TimeSpan.FromMinutes(30), out byte[]? bytes);

            Assert.IsTrue(hit);
            CollectionAssert.AreEqual(new byte[] { 1 }, bytes);
        }
        finally
        {
            Cleanup(directory);
        }
    }

    /// <summary>
    /// Verifies that a response for a range that ended on the day it was cached still goes stale, since that day's
    /// rate could still have been published after it was cached.
    /// </summary>
    [TestMethod]
    public void TryGet_WhenRangeEndedTheDayItWasCached_ShouldExpireAfterTheRefreshInterval()
    {
        string directory = CreateTempDirectory();
        try
        {
            FileSystemBoeResponseCache cache = new(directory);
            cache.Store((s_from, s_to), new byte[] { 1 });
            File.SetLastWriteTimeUtc(Path.Combine(directory, "boe_20230101_20230131.csv"), new DateTime(2023, 1, 31, 23, 0, 0, DateTimeKind.Utc));

            bool hit = cache.TryGet((s_from, s_to), TimeSpan.FromMinutes(30), out _);

            Assert.IsFalse(hit);
        }
        finally
        {
            Cleanup(directory);
        }
    }
}
