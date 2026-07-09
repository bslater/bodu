// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateCacheFileLayoutTests.ResolveFileName.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

public sealed partial class RateCacheFileLayoutTests
{
    /// <summary>
    /// Verifies that the single-file layout names a pair's file by its currency-pair token, ignoring the partition key.
    /// </summary>
    [TestMethod]
    public void ResolveFileName_ForSingleFile_ShouldIgnorePartitionKey() =>
        Assert.AreEqual("AUDUSD.json", RateCacheFileLayout.SingleFile.ResolveFileName("RBA", Pair, "2023-01", ".json"));

    /// <summary>
    /// Verifies that a partitioned layout names a file by its partition key and the format extension.
    /// </summary>
    [TestMethod]
    public void ResolveFileName_ForPartitioned_ShouldUsePartitionKey() =>
        Assert.AreEqual("2023-01.toml", RateCacheFileLayout.Monthly.ResolveFileName("RBA", Pair, "2023-01", ".toml"));
}
