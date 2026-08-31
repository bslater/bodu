// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstFileTests.BlockCache.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.IO;

namespace Bodu.IO.Pst;

public partial class PstFileTests
{
    /// <summary>
    /// Verifies that the decoded-block cache eliminates source reads for repeated structural access: with the default
    /// options, a second <see cref="PstNode.ReadPropertyContext" /> over the same node performs no additional reads
    /// against the underlying stream — every page and block it needs was cached by the first read.
    /// </summary>
    [TestMethod]
    public void Open_WhenBlockCacheEnabled_ShouldNotRereadSourceForRepeatedPropertyContextReads()
    {
        using MemoryStream fixture = PstReferenceFixtures.OpenStream(Sample1);
        using var counting = new CountingStream(fixture);
        using PstFile file = PstFile.Open(counting, new PstFileOptions());

        PstNode node = file.GetNode(PstNodeId.MessageStore);
        PstPropertyContext first = node.ReadPropertyContext();
        foreach (PstPropertyValue value in first)
            _ = value.GetBytes();
        int readsAfterFirst = counting.ReadCount;

        PstPropertyContext second = node.ReadPropertyContext();
        foreach (PstPropertyValue value in second)
            _ = value.GetBytes();

        Assert.AreEqual(first.Count, second.Count, "Both reads must decode the same property context.");
        Assert.AreEqual(readsAfterFirst, counting.ReadCount,
            "A repeated property-context read over the same node must be served entirely from the decoded-block " +
            "cache — no additional reads against the source stream.");
    }
}
