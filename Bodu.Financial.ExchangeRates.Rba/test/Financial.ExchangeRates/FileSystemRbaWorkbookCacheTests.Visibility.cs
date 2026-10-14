// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FileSystemRbaWorkbookCacheTests.Visibility.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class FileSystemRbaWorkbookCacheTests
{
    /// <summary>
    /// Verifies that the on-disk RBA workbook cache is not public: only the provider constructs it, and a caller
    /// reaches it through the options' <c>EnableDiskCache</c> and <c>CacheDirectory</c>.
    /// </summary>
    [TestMethod]
    public void Type_ShouldNotBePublic()
    {
        Assert.IsFalse(typeof(FileSystemRbaWorkbookCache).IsPublic);
        Assert.IsTrue(typeof(FileSystemRbaWorkbookCache).IsNotPublic);
    }
}
