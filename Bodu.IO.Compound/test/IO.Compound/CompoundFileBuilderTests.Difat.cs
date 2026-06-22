// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundFileBuilderTests.Difat.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using Bodu.Test;

namespace Bodu.IO.Compound;

public partial class CompoundFileBuilderTests
{
    /// <summary>
    /// Verifies that a container large enough to require more than the 109 inline FAT pointers — and therefore extended
    /// DIFAT sectors — round-trips correctly.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    public void ToArray_WhenContainerRequiresExtendedDifat_ShouldRoundTrip()
    {
        // A v3 FAT sector maps 128 sectors, so 109 inline FAT sectors cover ~7 MB. An 8 MB stream forces extended
        // DIFAT sectors to be allocated.
        byte[] payload = new byte[8 * 1024 * 1024];
        new Random(1234).NextBytes(payload);
        string expected = Convert.ToHexString(SHA256.HashData(payload)).ToLowerInvariant();

        var builder = new CompoundFileBuilder(new CompoundFileBuilderOptions { Version = CompoundFileVersion.V3 });
        _ = builder.Root.AddStream("Big", payload);

        byte[] bytes = builder.ToArray();

        using CompoundFile file = CompoundFile.Open(new MemoryStream(bytes));
        Assert.IsTrue(file.RootStorage.TryOpenStream("Big", out CompoundStreamEntry? entry));
        Assert.AreEqual(payload.Length, entry.Length);
        Assert.AreEqual(expected, Convert.ToHexString(SHA256.HashData(entry.ReadAllBytes().Span)).ToLowerInvariant());
    }
}
