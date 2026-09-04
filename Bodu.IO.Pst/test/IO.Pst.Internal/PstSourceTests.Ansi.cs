// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstSourceTests.Ansi.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;

namespace Bodu.IO.Pst.Internal;

public partial class PstSourceTests
{
    /// <summary>
    /// Builds a single-node ANSI store.
    /// </summary>
    /// <param name="builder">When this method returns, the builder, for block offsets.</param>
    /// <param name="payload">The node payload.</param>
    /// <param name="dataBlockId">When this method returns, the data block identifier.</param>
    /// <returns>The file bytes.</returns>
    private static byte[] BuildSingleAnsiNode(out PstFixtureBuilder builder, byte[] payload, out ulong dataBlockId)
    {
        builder = new PstFixtureBuilder { Format = PstFileFormat.Ansi };
        dataBlockId = builder.AddDataBlock(payload);
        builder.AddNode(NodeId, dataBlockId);

        return builder.Build();
    }

    /// <summary>
    /// Verifies that an ANSI block is read through its 12-byte trailer and that, under strict validation, a corrupted
    /// block identifier — which the ANSI trailer places before the checksum — is reported as an invalid block.
    /// </summary>
    [TestMethod]
    public void ReadBlock_WhenAnsiTrailerBlockIdIsCorrupt_ShouldThrowInvalidBlock()
    {
        byte[] payload = [.. Enumerable.Range(0, 100).Select(static i => (byte)i)];
        byte[] file = BuildSingleAnsiNode(out PstFixtureBuilder builder, payload, out ulong blockId);
        CollectionAssert.AreEqual(payload, ReadNodePayload(file, PstValidationLevel.Compatible));

        long trailer = builder.BlockOffsets[blockId] + PstFixtureBuilder.BlockDiskLength(payload.Length, PstLayout.Ansi) - PstLayout.Ansi.BlockTrailerSize;
        file[trailer + PstLayout.Ansi.BlockTrailerBlockIdOffset] ^= 0x01;

        var ex = Assert.ThrowsExactly<PstFileFormatException>(() => _ = ReadNodePayload(file, PstValidationLevel.Strict));

        Assert.AreEqual(PstFileError.InvalidBlock, ex.Error);
    }

    /// <summary>
    /// Verifies that, under strict validation, a corrupted ANSI block checksum — the trailer's last four bytes — is
    /// reported as an invalid block.
    /// </summary>
    [TestMethod]
    public void ReadBlock_WhenAnsiTrailerCrcIsCorrupt_ShouldThrowInvalidBlock()
    {
        byte[] payload = [.. Enumerable.Range(0, 100).Select(static i => (byte)i)];
        byte[] file = BuildSingleAnsiNode(out PstFixtureBuilder builder, payload, out ulong blockId);

        long trailer = builder.BlockOffsets[blockId] + PstFixtureBuilder.BlockDiskLength(payload.Length, PstLayout.Ansi) - PstLayout.Ansi.BlockTrailerSize;
        file[trailer + PstLayout.Ansi.BlockTrailerCrcOffset] ^= 0x01;

        var ex = Assert.ThrowsExactly<PstFileFormatException>(() => _ = ReadNodePayload(file, PstValidationLevel.Strict));

        Assert.AreEqual(PstFileError.InvalidBlock, ex.Error);
    }

    /// <summary>
    /// Verifies that an ANSI page is validated through its trailer at offset 500: a wrong page type is an invalid page.
    /// </summary>
    [TestMethod]
    public void ReadPage_WhenAnsiPageTypeIsCorrupt_ShouldThrowInvalidPage()
    {
        byte[] file = BuildSingleAnsiNode(out _, [1, 2, 3], out _);
        long root = PstFixtureBuilder.ReadNodeTreeRootOffset(file, PstLayout.Ansi);
        file[root + PstLayout.Ansi.PageTrailerOffset] = 0x80;

        var ex = Assert.ThrowsExactly<PstFileFormatException>(() => _ = ReadNodePayload(file, PstValidationLevel.Compatible));

        Assert.AreEqual(PstFileError.InvalidPage, ex.Error);
    }

    /// <summary>
    /// Verifies that a payload filling the larger ANSI block (8,180 bytes) reads back intact.
    /// </summary>
    [TestMethod]
    public void ReadBlock_WhenAnsiPayloadFillsBlock_ShouldReadWholePayload()
    {
        byte[] payload = [.. Enumerable.Range(0, PstLayout.Ansi.MaxBlockPayload).Select(static i => (byte)(i * 13))];
        byte[] file = BuildSingleAnsiNode(out _, payload, out _);

        CollectionAssert.AreEqual(payload, ReadNodePayload(file, PstValidationLevel.Strict));
    }
}
