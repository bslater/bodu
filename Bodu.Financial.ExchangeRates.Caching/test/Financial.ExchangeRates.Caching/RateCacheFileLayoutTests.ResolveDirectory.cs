// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateCacheFileLayoutTests.ResolveDirectory.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

public sealed partial class RateCacheFileLayoutTests
{
    /// <summary>
    /// Verifies that the single-file layout places a pair's file directly under a per-provider folder.
    /// </summary>
    [TestMethod]
    public void ResolveDirectory_ForSingleFile_ShouldBeProviderFolder() =>
        Assert.AreEqual(
            Path.Combine("root", "RBA"),
            RateCacheFileLayout.SingleFile.ResolveDirectory("root", "RBA", Pair));

    /// <summary>
    /// Verifies that a partitioned layout isolates a pair in its own folder beneath the provider folder.
    /// </summary>
    [TestMethod]
    public void ResolveDirectory_ForPartitioned_ShouldBePerPairFolder() =>
        Assert.AreEqual(
            Path.Combine("root", "RBA", "AUDUSD"),
            RateCacheFileLayout.Monthly.ResolveDirectory("root", "RBA", Pair));

    /// <summary>
    /// Verifies that a provider name containing a path-illegal character is sanitized into the resolved folder.
    /// </summary>
    [TestMethod]
    public void ResolveDirectory_WhenProviderHasIllegalCharacter_ShouldSanitizeSegment() =>
        Assert.AreEqual(
            Path.Combine("root", "a_b"),
            RateCacheFileLayout.SingleFile.ResolveDirectory("root", "a/b", Pair));
}
