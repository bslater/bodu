// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstSourceTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using Bodu.IO.Pst;

namespace Bodu.IO.Pst.Internal;

/// <summary>
/// Verifies the validated read layer: range checks, page and block trailers, and the content decoding applied to
/// external block data.
/// </summary>
/// <remarks>
/// These are the guards that stand between a corrupt or hostile file and the rest of the reader, and none of them had
/// been exercised - the reference corpus is four well-formed files, so every rejection arm was dead. Each test starts
/// from a container the reader accepts and introduces one defect, so a failure names the guard that stopped working
/// rather than merely reporting that a damaged file was refused.
/// </remarks>
[TestClass]
public partial class PstSourceTests
{
    /// <summary>The node identifier the fixtures use.</summary>
    private const uint NodeId = 0x21;

    /// <summary>
    /// Builds a container with one node over one data block and returns its bytes.
    /// </summary>
    /// <param name="builder">Receives the builder, for the block offsets it recorded.</param>
    /// <param name="payload">The node payload.</param>
    /// <param name="dataBlockId">Receives the data block's identifier.</param>
    /// <returns>The container bytes.</returns>
    private static byte[] BuildSingleNode(out PstFixtureBuilder builder, byte[] payload, out ulong dataBlockId)
    {
        builder = new PstFixtureBuilder();
        dataBlockId = builder.AddDataBlock(payload);
        builder.AddNode(NodeId, dataBlockId);

        return builder.Build();
    }

    /// <summary>
    /// Opens a container and reads the node's payload, which forces every read the guards protect.
    /// </summary>
    /// <param name="file">The container bytes.</param>
    /// <param name="validationLevel">The validation level to open at.</param>
    /// <returns>The node payload.</returns>
    private static byte[] ReadNodePayload(byte[] file, PstValidationLevel validationLevel)
    {
        using PstFile pst = PstFile.Open(new MemoryStream(file, writable: false), new PstFileOptions { ValidationLevel = validationLevel });

        return pst.GetNode(new PstNodeId(NodeId)).ReadAllBytes();
    }

    /// <summary>
    /// Finds a block's leaf entry in the block B-tree root page, which is also its only page in these fixtures.
    /// </summary>
    /// <param name="file">The container bytes.</param>
    /// <param name="blockId">The block identifier to locate.</param>
    /// <returns>The absolute offset of the entry.</returns>
    private static long FindBlockTreeEntry(byte[] file, ulong blockId)
    {
        long page = PstFixtureBuilder.ReadBlockTreeRootOffset(file);
        int count = file[page + 488];
        int stride = file[page + 490];

        for (int i = 0; i < count; i++)
        {
            long entry = page + (i * stride);
            if (BinaryPrimitives.ReadUInt64LittleEndian(file.AsSpan((int)entry)) == blockId)
                return entry;
        }

        Assert.Fail($"Block {blockId} is not in the block B-tree.");

        return 0;
    }

    /// <summary>
    /// Verifies that a page reference pointing beyond the end of the file is rejected, rather than read from
    /// whatever the last bytes happen to be.
    /// </summary>
    [TestMethod]
    public void ReadAt_WhenRangeEscapesTheStream_ShouldThrowPstFileFormatException()
    {
        byte[] file = BuildSingleNode(out _, [1, 2, 3], out _);
        BinaryPrimitives.WriteUInt64LittleEndian(file.AsSpan(240), 0x0100_0000UL);
        PstFixtureBuilder.RepairHeaderChecksum(file);

        var ex = Assert.ThrowsExactly<PstFileFormatException>(() => _ = ReadNodePayload(file, PstValidationLevel.Compatible));

        // Naming the offset distinguishes the range guard from the header checksum, which also covers this field.
        Assert.Contains("16777216", ex.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that a page whose type byte names the wrong tree is rejected, which is what stops a reference that
    /// happens to land on a valid page of the other kind from being walked as if it belonged.
    /// </summary>
    [TestMethod]
    public void ReadPage_WhenPageTypeIsWrong_ShouldThrowPstFileFormatException()
    {
        byte[] file = BuildSingleNode(out _, [1, 2, 3], out _);
        long page = PstFixtureBuilder.ReadBlockTreeRootOffset(file);
        file[page + 496] = PstBTree.NodePageType;
        file[page + 497] = PstBTree.NodePageType;

        _ = Assert.ThrowsExactly<PstFileFormatException>(() => _ = ReadNodePayload(file, PstValidationLevel.Compatible));
    }

    /// <summary>
    /// Verifies that the two type bytes must agree, so a single flipped byte does not pass as a page of that type.
    /// </summary>
    [TestMethod]
    public void ReadPage_WhenRepeatedTypeByteDisagrees_ShouldThrowPstFileFormatException()
    {
        byte[] file = BuildSingleNode(out _, [1, 2, 3], out _);
        long page = PstFixtureBuilder.ReadBlockTreeRootOffset(file);
        file[page + 497] = 0x00;

        _ = Assert.ThrowsExactly<PstFileFormatException>(() => _ = ReadNodePayload(file, PstValidationLevel.Compatible));
    }

    /// <summary>
    /// Verifies that a page trailer inconsistent with the reference that reached it is rejected under strict
    /// validation, covering the identifier, the checksum, and the signature independently.
    /// </summary>
    /// <param name="testName">The scenario name.</param>
    /// <param name="fieldOffset">The offset within the page of the field to corrupt.</param>
    [TestMethod]
    [DataRow("signature", 498)]
    [DataRow("checksum", 500)]
    [DataRow("identifier", 504)]
    public void ReadPage_WhenTrailerIsInconsistent_ForStrict_ShouldThrowPstFileFormatException(string testName, int fieldOffset)
    {
        Assert.IsNotNull(testName);

        byte[] file = BuildSingleNode(out _, [1, 2, 3], out _);
        long page = PstFixtureBuilder.ReadBlockTreeRootOffset(file);
        file[page + fieldOffset] ^= 0xFF;

        _ = Assert.ThrowsExactly<PstFileFormatException>(() => _ = ReadNodePayload(file, PstValidationLevel.Strict));
    }

    /// <summary>
    /// Verifies that the same inconsistent page trailer is tolerated below strict validation, so a file whose
    /// checksums have drifted is still readable when the caller has asked for that.
    /// </summary>
    [TestMethod]
    public void ReadPage_WhenTrailerIsInconsistent_ForCompatible_ShouldStillRead()
    {
        byte[] expected = [1, 2, 3];
        byte[] file = BuildSingleNode(out _, expected, out _);
        long page = PstFixtureBuilder.ReadBlockTreeRootOffset(file);
        file[page + 500] ^= 0xFF;

        CollectionAssert.AreEqual(expected, ReadNodePayload(file, PstValidationLevel.Compatible));
    }

    /// <summary>
    /// Verifies that a block whose recorded length is zero is rejected. Zero is not a legal payload length, and
    /// accepting it would make the trailer overlap the payload.
    /// </summary>
    [TestMethod]
    public void ReadBlock_WhenRecordedLengthIsZero_ShouldThrowPstFileFormatException()
    {
        byte[] file = BuildSingleNode(out _, [1, 2, 3], out ulong blockId);
        long entry = FindBlockTreeEntry(file, blockId);
        BinaryPrimitives.WriteUInt16LittleEndian(file.AsSpan((int)entry + 16), 0);

        _ = Assert.ThrowsExactly<PstFileFormatException>(() => _ = ReadNodePayload(file, PstValidationLevel.Compatible));
    }

    /// <summary>
    /// Verifies that a block whose recorded length exceeds the maximum block size is rejected before the read is
    /// attempted, rather than allocating and reading whatever follows.
    /// </summary>
    [TestMethod]
    public void ReadBlock_WhenRecordedLengthExceedsTheMaximum_ShouldThrowPstFileFormatException()
    {
        byte[] file = BuildSingleNode(out _, [1, 2, 3], out ulong blockId);
        long entry = FindBlockTreeEntry(file, blockId);
        BinaryPrimitives.WriteUInt16LittleEndian(file.AsSpan((int)entry + 16), ushort.MaxValue);

        _ = Assert.ThrowsExactly<PstFileFormatException>(() => _ = ReadNodePayload(file, PstValidationLevel.Compatible));
    }

    /// <summary>
    /// Verifies that a block trailer whose length disagrees with the block B-tree entry is rejected at every
    /// validation level, since the two are the only record of where the payload ends.
    /// </summary>
    /// <param name="validationLevel">The validation level to open at.</param>
    [TestMethod]
    [DataRow(PstValidationLevel.Minimal)]
    [DataRow(PstValidationLevel.Compatible)]
    [DataRow(PstValidationLevel.Strict)]
    public void ReadBlock_WhenTrailerLengthDisagrees_ShouldThrowPstFileFormatException(PstValidationLevel validationLevel)
    {
        byte[] payload = [1, 2, 3];
        byte[] file = BuildSingleNode(out PstFixtureBuilder builder, payload, out ulong blockId);
        long trailer = builder.BlockOffsets[blockId] + PstFixtureBuilder.BlockDiskLength(payload.Length) - 16;
        BinaryPrimitives.WriteUInt16LittleEndian(file.AsSpan((int)trailer), 2);

        _ = Assert.ThrowsExactly<PstFileFormatException>(() => _ = ReadNodePayload(file, validationLevel));
    }

    /// <summary>
    /// Verifies that a block trailer inconsistent with its entry is rejected under strict validation, covering the
    /// signature, the checksum, and the identifier independently.
    /// </summary>
    /// <param name="testName">The scenario name.</param>
    /// <param name="fieldOffset">The offset within the trailer of the field to corrupt.</param>
    [TestMethod]
    [DataRow("signature", 2)]
    [DataRow("checksum", 4)]
    [DataRow("identifier", 8)]
    public void ReadBlock_WhenTrailerIsInconsistent_ForStrict_ShouldThrowPstFileFormatException(string testName, int fieldOffset)
    {
        Assert.IsNotNull(testName);

        byte[] payload = [1, 2, 3];
        byte[] file = BuildSingleNode(out PstFixtureBuilder builder, payload, out ulong blockId);
        long trailer = builder.BlockOffsets[blockId] + PstFixtureBuilder.BlockDiskLength(payload.Length) - 16;
        file[trailer + fieldOffset] ^= 0xFF;

        _ = Assert.ThrowsExactly<PstFileFormatException>(() => _ = ReadNodePayload(file, PstValidationLevel.Strict));
    }

    /// <summary>
    /// Verifies that the same inconsistent block trailer is tolerated below strict validation.
    /// </summary>
    [TestMethod]
    public void ReadBlock_WhenTrailerIsInconsistent_ForCompatible_ShouldStillRead()
    {
        byte[] payload = [1, 2, 3];
        byte[] file = BuildSingleNode(out PstFixtureBuilder builder, payload, out ulong blockId);
        long trailer = builder.BlockOffsets[blockId] + PstFixtureBuilder.BlockDiskLength(payload.Length) - 16;
        file[trailer + 4] ^= 0xFF;

        CollectionAssert.AreEqual(payload, ReadNodePayload(file, PstValidationLevel.Compatible));
    }

    /// <summary>
    /// Verifies that external block data is decoded per the file's declared content encoding, for each of the three
    /// methods. The bytes on disk differ between them, so a reader that ignored the declaration would return the
    /// encoded form.
    /// </summary>
    /// <param name="cryptMethod">The content encoding the container declares.</param>
    [TestMethod]
    [DataRow(PstCryptMethod.None)]
    [DataRow(PstCryptMethod.Permute)]
    [DataRow(PstCryptMethod.Cyclic)]
    public void ReadBlock_ShouldDecodeExternalDataPerTheDeclaredEncoding(PstCryptMethod cryptMethod)
    {
        byte[] expected = [.. Enumerable.Range(0, 300).Select(static i => (byte)i)];
        var builder = new PstFixtureBuilder { CryptMethod = cryptMethod };
        ulong blockId = builder.AddDataBlock(expected);
        builder.AddNode(NodeId, blockId);
        byte[] file = builder.Build();

        CollectionAssert.AreEqual(expected, ReadNodePayload(file, PstValidationLevel.Strict));
    }

    /// <summary>
    /// Verifies that tree metadata is never decoded, whatever the file declares: an <c>XBLOCK</c> is stored verbatim
    /// while the data blocks beneath it are encoded, so a reader that decoded both would fail to follow the tree.
    /// </summary>
    [TestMethod]
    public void ReadBlock_WhenEncodingIsDeclared_ShouldLeaveInternalBlocksVerbatim()
    {
        byte[] first = [.. Enumerable.Range(0, 400).Select(static i => (byte)i)];
        byte[] second = [.. Enumerable.Range(0, 400).Select(static i => (byte)(255 - i))];
        var builder = new PstFixtureBuilder { CryptMethod = PstCryptMethod.Permute };
        builder.AddNode(NodeId, builder.AddXBlock(800, builder.AddDataBlock(first), builder.AddDataBlock(second)));

        byte[] expected = [.. first, .. second];

        CollectionAssert.AreEqual(expected, ReadNodePayload(builder.Build(), PstValidationLevel.Strict));
    }
}
