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
            cache.Store(s_from, s_to, payload);

            bool hit = cache.TryGet(s_from, s_to, TimeSpan.FromHours(1), out byte[]? bytes);

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
            cache.Store(s_from, s_to, new byte[] { 1 });

            bool hit = cache.TryGet(s_from, new DateOnly(2023, 2, 28), TimeSpan.FromHours(1), out _);

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
        string directory = CreateTempDirectory();
        try
        {
            FileSystemBoeResponseCache cache = new(directory);
            cache.Store(s_from, s_to, new byte[] { 1 });

            string path = Path.Combine(directory, "boe_20230101_20230131.csv");
            File.SetLastWriteTimeUtc(path, DateTime.UtcNow - TimeSpan.FromHours(2));

            bool hit = cache.TryGet(s_from, s_to, TimeSpan.FromMinutes(30), out _);

            Assert.IsFalse(hit);
        }
        finally
        {
            Cleanup(directory);
        }
    }
}
