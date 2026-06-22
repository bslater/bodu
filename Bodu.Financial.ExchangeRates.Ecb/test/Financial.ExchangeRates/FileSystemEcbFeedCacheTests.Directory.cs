// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FileSystemEcbFeedCacheTests.Directory.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class FileSystemEcbFeedCacheTests
{
    /// <summary>
    /// Verifies that a <see langword="null" /> directory falls back to a folder under the system temporary path.
    /// </summary>
    [TestMethod]
    public void Directory_WhenNull_ShouldUseTempFallback()
    {
        FileSystemEcbFeedCache cache = new(null);

        Assert.AreEqual(Path.Combine(Path.GetTempPath(), "bodu-ecb"), cache.Directory);
    }
}
