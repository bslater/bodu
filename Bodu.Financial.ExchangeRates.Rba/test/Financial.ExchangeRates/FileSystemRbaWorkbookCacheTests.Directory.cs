// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FileSystemRbaWorkbookCacheTests.Directory.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class FileSystemRbaWorkbookCacheTests
{
    /// <summary>
    /// Verifies that a <see langword="null" /> directory argument falls back to a <c>bodu-rba</c> folder under the
    /// system temporary path.
    /// </summary>
    [TestMethod]
    public void Directory_WhenNull_ShouldUseTempFallback()
    {
        FileSystemRbaWorkbookCache cache = new(null);

        Assert.AreEqual(Path.Combine(Path.GetTempPath(), "bodu-rba"), cache.Directory);
    }
}
