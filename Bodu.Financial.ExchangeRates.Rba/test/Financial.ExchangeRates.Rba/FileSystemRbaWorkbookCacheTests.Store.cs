// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FileSystemRbaWorkbookCacheTests.Store.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Rba;

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
}
