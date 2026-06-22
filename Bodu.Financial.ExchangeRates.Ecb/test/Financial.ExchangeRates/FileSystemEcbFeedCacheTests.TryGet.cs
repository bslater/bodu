// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FileSystemEcbFeedCacheTests.TryGet.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class FileSystemEcbFeedCacheTests
{
    /// <summary>
    /// Verifies that bytes stored for a feed are returned while still within the refresh interval.
    /// </summary>
    [TestMethod]
    public void TryGet_WhenStoredAndFresh_ShouldReturnBytes()
    {
        string directory = CreateTempDirectory();
        try
        {
            FileSystemEcbFeedCache cache = new(directory);
            byte[] payload = new byte[] { 1, 2, 3, 4 };
            cache.Store(EcbExchangeRateFeed.Full, payload);

            bool hit = cache.TryGet(EcbExchangeRateFeed.Full, TimeSpan.FromHours(1), out byte[]? bytes);

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
        string directory = CreateTempDirectory();
        try
        {
            FileSystemEcbFeedCache cache = new(directory);
            cache.Store(EcbExchangeRateFeed.Full, new byte[] { 1 });

            string path = Path.Combine(directory, EcbExchangeRateFeed.Full.FileName);
            File.SetLastWriteTimeUtc(path, DateTime.UtcNow - TimeSpan.FromHours(2));

            bool hit = cache.TryGet(EcbExchangeRateFeed.Full, TimeSpan.FromMinutes(30), out _);

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
        string directory = CreateTempDirectory();
        try
        {
            FileSystemEcbFeedCache cache = new(directory);

            bool hit = cache.TryGet(EcbExchangeRateFeed.Full, TimeSpan.FromHours(1), out _);

            Assert.IsFalse(hit);
        }
        finally
        {
            Cleanup(directory);
        }
    }
}
