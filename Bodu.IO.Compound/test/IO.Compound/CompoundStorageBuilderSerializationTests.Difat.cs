// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundStorageBuilderSerializationTests.Difat.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using Bodu.IO.Compound.Builders;
using Bodu.IO.Compound.Internal;
using Bodu.Test;

namespace Bodu.IO.Compound;

public partial class CompoundStorageBuilderSerializationTests
{
    /// <summary>
    /// The payload size that forces extended DIFAT sectors at version 3.
    /// </summary>
    /// <remarks>
    /// A v3 sector is 512 bytes and a FAT sector maps 128 of them, so the 109 FAT pointers the header carries inline
    /// cover 109 x 128 x 512 = ~7.1 MB. Anything past that needs FAT pointers the header cannot hold, which is what
    /// the extended DIFAT chain exists for. 8 MB clears the threshold with room to spare.
    /// </remarks>
    private const int ExtendedDifatPayloadBytes = 8 * 1024 * 1024;

    /// <summary>
    /// Verifies that a container large enough to require more than the 109 inline FAT pointers — and therefore extended
    /// DIFAT sectors — round-trips correctly.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    public void ToArray_WhenContainerRequiresExtendedDifat_ShouldRoundTrip()
    {
        byte[] payload = new byte[ExtendedDifatPayloadBytes];
        new Random(1234).NextBytes(payload);
        string expected = Convert.ToHexString(SHA256.HashData(payload)).ToLowerInvariant();

        var builder = CompoundStorageBuilder.CreateRoot();
        _ = builder.AddStream("Big", payload);

        byte[] bytes = builder.ToArray(new CompoundBuildOptions { Version = CompoundFileVersion.V3 });

        AssertExtendedDifatWasEmitted(bytes);

        using var file = CompoundFile.Open(new MemoryStream(bytes));
        Assert.IsTrue(file.RootStorage.TryOpenStream("Big", out CompoundStream? entry));
        Assert.AreEqual(payload.Length, entry.Length);
        Assert.AreEqual(expected, Convert.ToHexString(SHA256.HashData(entry.AsMemory().Span)).ToLowerInvariant());
    }

    /// <summary>
    /// Verifies that the asynchronous emitter allocates and writes the extended DIFAT chain exactly as the
    /// synchronous one does, producing byte-identical output for the same tree.
    /// </summary>
    /// <returns>A task that completes when the assertion has run.</returns>
    /// <remarks>
    /// The extended-DIFAT emitter is the last piece of the async path with no synchronous-equivalent coverage, and it
    /// is reached only above the inline-pointer threshold - so a smaller payload would exercise the ordinary FAT path
    /// and pass while proving nothing. Byte-for-byte equality against the synchronous emitter is the assertion,
    /// because the two write the same container through different code.
    /// </remarks>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    public async Task WriteToAsync_WhenContainerRequiresExtendedDifat_ShouldMatchSyncPathByteForByte()
    {
        byte[] payload = new byte[ExtendedDifatPayloadBytes];
        new Random(1234).NextBytes(payload);

        // A deferred source is re-opened per emit, so the 8 MB payload is not buffered a second time by the builder.
        var builder = CompoundStorageBuilder.CreateRoot();
        _ = builder.AddStream("Big", () => new MemoryStream(payload, writable: false), payload.Length);

        var options = new CompoundBuildOptions { Version = CompoundFileVersion.V3 };

        using var actual = new MemoryStream();
        await CompoundContainerLayout.WriteToAsync(actual, builder, options, CancellationToken.None);

        byte[] asyncBytes = actual.ToArray();
        AssertExtendedDifatWasEmitted(asyncBytes);
        CollectionAssert.AreEqual(builder.ToArray(options), asyncBytes);
    }

    /// <summary>
    /// Asserts that the container actually carries an extended DIFAT chain, rather than having stayed under the
    /// inline-pointer threshold.
    /// </summary>
    /// <param name="bytes">The serialized container.</param>
    private static void AssertExtendedDifatWasEmitted(byte[] bytes)
    {
        CfbHeader header = CfbHeader.Parse(bytes);

        Assert.IsGreaterThan(0u, header.DifatSectorCount, "The container carries no extended DIFAT sectors.");
        Assert.AreNotEqual(CfbHeader.EndOfChain, header.FirstDifatSector);
    }
}
