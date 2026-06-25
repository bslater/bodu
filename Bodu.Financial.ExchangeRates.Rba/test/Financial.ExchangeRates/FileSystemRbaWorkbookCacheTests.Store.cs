// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FileSystemRbaWorkbookCacheTests.Store.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class FileSystemRbaWorkbookCacheTests
{
    /// <summary>
    /// Verifies that bytes stored for an immutable era are returned on a subsequent read.
    /// </summary>
    [TestMethod]
    public void Store_ThenTryGet_ShouldReturnStoredBytes()
    {
        FileSystemRbaWorkbookCache cache = new(_directory);
        byte[] payload = new byte[] { 1, 2, 3, 4 };

        cache.Store(s_immutableEra, payload);
        bool found = cache.TryGet(s_immutableEra, TimeSpan.FromHours(12), out byte[]? bytes);

        Assert.IsTrue(found);
        CollectionAssert.AreEqual(payload, bytes);
    }

    /// <summary>
    /// Verifies that a write failure is swallowed so a cache problem never breaks rate retrieval: when the cache
    /// directory path is occupied by a file, <see cref="FileSystemRbaWorkbookCache.Store" /> reports the I/O failure as
    /// a no-op and the cache remains usable, returning a miss on the subsequent read.
    /// </summary>
    [TestMethod]
    public void Store_WhenDirectoryPathIsAFile_ShouldSwallowIOExceptionAndNotThrow()
    {
        Directory.CreateDirectory(_directory);
        string filePath = Path.Combine(_directory, "occupied");
        File.WriteAllText(filePath, "not a directory");

        FileSystemRbaWorkbookCache cache = new(filePath);

        cache.Store(s_immutableEra, new byte[] { 1, 2, 3 });

        bool found = cache.TryGet(s_immutableEra, TimeSpan.FromHours(12), out _);
        Assert.IsFalse(found);
    }
}
