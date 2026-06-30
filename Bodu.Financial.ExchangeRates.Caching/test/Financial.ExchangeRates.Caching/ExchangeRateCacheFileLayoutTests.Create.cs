// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateCacheFileLayoutTests.Create.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

public sealed partial class ExchangeRateCacheFileLayoutTests
{
    /// <summary>
    /// Verifies that a custom layout uses the supplied directory and file-name delegates.
    /// </summary>
    [TestMethod]
    public void Create_WhenGivenDelegates_ShouldUseThem()
    {
        var layout = ExchangeRateCacheFileLayout.Create(
            ExchangeRateCachePartitionStrategy.Single,
            directory: ctx => Path.Combine(ctx.Root, "fx", ctx.Provider),
            fileName: ctx => $"{ctx.Pair.From}-{ctx.Pair.To}{ctx.FileExtension}");

        Assert.AreEqual(Path.Combine("root", "fx", "RBA"), layout.ResolveDirectory("root", "RBA", Pair));
        Assert.AreEqual("AUD-USD.toml", layout.ResolveFileName("RBA", Pair, string.Empty, ".toml"));
    }

    /// <summary>
    /// Verifies that a custom layout falls back to the default directory and file-name rules when no delegates are given.
    /// </summary>
    [TestMethod]
    public void Create_WhenNoDelegates_ShouldUsePartitionedDefaults()
    {
        var layout = ExchangeRateCacheFileLayout.Create(ExchangeRateCachePartitionStrategy.Yearly);

        Assert.IsTrue(layout.IsPartitioned);
        Assert.AreEqual(Path.Combine("root", "RBA", "AUDUSD"), layout.ResolveDirectory("root", "RBA", Pair));
        Assert.AreEqual("2023.toml", layout.ResolveFileName("RBA", Pair, "2023", ".toml"));
    }

    /// <summary>
    /// Verifies that creating a layout with a <see langword="null" /> partition strategy throws.
    /// </summary>
    [TestMethod]
    public void Create_WhenStrategyIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = ExchangeRateCacheFileLayout.Create(null!);
        });

        Assert.AreEqual("partitionStrategy", ex.ParamName);
    }
}
