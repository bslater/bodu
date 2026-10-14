// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FileSystemBoeResponseCacheTests.Visibility.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class FileSystemBoeResponseCacheTests
{
    /// <summary>
    /// Verifies that the on-disk Bank of England response cache is not public: only the provider constructs it, and a caller
    /// reaches it through the options' <c>EnableDiskCache</c> and <c>CacheDirectory</c>.
    /// </summary>
    [TestMethod]
    public void Type_ShouldNotBePublic()
    {
        Assert.IsFalse(typeof(FileSystemBoeResponseCache).IsPublic);
        Assert.IsTrue(typeof(FileSystemBoeResponseCache).IsNotPublic);
    }
}
