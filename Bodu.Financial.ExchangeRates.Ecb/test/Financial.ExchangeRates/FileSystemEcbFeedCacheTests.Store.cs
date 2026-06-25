// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FileSystemEcbFeedCacheTests.Store.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class FileSystemEcbFeedCacheTests
{
    /// <summary>
    /// Verifies that a write failure is swallowed so a cache problem never breaks rate retrieval: when the cache
    /// directory path is occupied by a file, <see cref="FileSystemEcbFeedCache.Store" /> reports the I/O failure as a
    /// no-op and the cache remains usable, returning a miss on the subsequent read.
    /// </summary>
    [TestMethod]
    public void Store_WhenDirectoryPathIsAFile_ShouldSwallowIOExceptionAndNotThrow()
    {
        string directory = CreateTempDirectory();
        try
        {
            Directory.CreateDirectory(directory);
            string filePath = Path.Combine(directory, "occupied");
            File.WriteAllText(filePath, "not a directory");

            FileSystemEcbFeedCache cache = new(filePath);

            cache.Store(EcbExchangeRateFeed.Full, new byte[] { 1, 2, 3 });

            bool hit = cache.TryGet(EcbExchangeRateFeed.Full, TimeSpan.FromHours(1), out _);
            Assert.IsFalse(hit);
        }
        finally
        {
            Cleanup(directory);
        }
    }
}
