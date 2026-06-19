// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FileSystemBoeResponseCacheTests.Directory.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Boe;

public partial class FileSystemBoeResponseCacheTests
{
    /// <summary>
    /// Verifies that a <see langword="null" /> directory falls back to a folder under the system temporary path.
    /// </summary>
    [TestMethod]
    public void Directory_WhenNull_ShouldUseTempFallback()
    {
        FileSystemBoeResponseCache cache = new(null);

        Assert.AreEqual(Path.Combine(Path.GetTempPath(), "bodu-boe"), cache.Directory);
    }
}
