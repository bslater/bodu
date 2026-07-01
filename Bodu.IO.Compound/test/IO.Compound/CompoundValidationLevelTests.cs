// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundValidationLevelTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using Bodu.IO.Compound.Builders;
using Bodu.IO.Compound.Internal;

namespace Bodu.IO.Compound;

/// <summary>
/// Verifies that <see cref="CompoundValidationLevel" /> gates how the reader responds to recoverable inconsistencies:
/// <see cref="CompoundValidationLevel.Strict" /> rejects malformed directory entries the default tolerates, and
/// <see cref="CompoundValidationLevel.Minimal" /> recovers from chain anomalies the default rejects.
/// </summary>
[TestClass]
public class CompoundValidationLevelTests
{
    /// <summary>The byte offset, within a directory entry, of the 16-bit UTF-16 name length.</summary>
    private const int NameLengthOffset = 64;

    /// <summary>The byte offset, within a directory entry, of the 64-bit payload size.</summary>
    private const int SizeOffset = 120;

    /// <summary>The fixed size, in bytes, of a directory entry.</summary>
    private const int DirectoryEntrySize = 128;

    /// <summary>
    /// Verifies that a named orphan directory entry is tolerated at the default level.
    /// </summary>
    [TestMethod]
    public void Open_WhenNamedOrphanEntry_ForCompatible_ShouldTolerate()
    {
        byte[] bytes = BuildWithOrphanEntry();

        using var file = CompoundFile.Open(new MemoryStream(bytes));

        Assert.IsTrue(file.RootStorage.TryOpenStream("Doc", out _));
    }

    /// <summary>
    /// Verifies that a named orphan directory entry is rejected at <see cref="CompoundValidationLevel.Strict" />.
    /// </summary>
    [TestMethod]
    public void Open_WhenNamedOrphanEntry_ForStrict_ShouldThrowInvalidDirectory()
    {
        byte[] bytes = BuildWithOrphanEntry();
        var options = new CompoundFileOptions { ValidationLevel = CompoundValidationLevel.Strict };

        CompoundFileFormatException ex = Assert.ThrowsExactly<CompoundFileFormatException>(() =>
        {
            using var _ = CompoundFile.Open(new MemoryStream(bytes), options);
        });

        Assert.AreEqual(CompoundFileError.InvalidDirectory, ex.Category);
    }

    /// <summary>
    /// Verifies that a stream whose declared size exceeds its chain is rejected at the default level.
    /// </summary>
    [TestMethod]
    public void OpenStream_WhenSizeExceedsChain_ForCompatible_ShouldThrowStreamChainTooShort()
    {
        byte[] bytes = BuildOverlongStream();

        using var file = CompoundFile.Open(new MemoryStream(bytes));
        CompoundFileFormatException ex = Assert.ThrowsExactly<CompoundFileFormatException>(
            () => _ = file.RootStorage.OpenStream("Big").ReadAllBytes());

        Assert.AreEqual(CompoundFileError.StreamChainTooShort, ex.Category);
    }

    /// <summary>
    /// Verifies that a stream whose declared size exceeds its chain is recovered (zero-padded) at
    /// <see cref="CompoundValidationLevel.Minimal" /> rather than throwing.
    /// </summary>
    [TestMethod]
    public void OpenStream_WhenSizeExceedsChain_ForMinimal_ShouldReturnPaddedPayload()
    {
        byte[] bytes = BuildOverlongStream();
        var options = new CompoundFileOptions { ValidationLevel = CompoundValidationLevel.Minimal };

        using var file = CompoundFile.Open(new MemoryStream(bytes), options);
        byte[] payload = file.RootStorage.OpenStream("Big").ReadAllBytes();

        Assert.HasCount(200_000, payload);
    }

    /// <summary>
    /// Verifies that <see cref="CompoundReadStrategy.Auto" /> reads correctly through the on-demand path when the source
    /// exceeds <see cref="CompoundFileOptions.MaxBufferedBytes" />.
    /// </summary>
    [TestMethod]
    public void Open_WhenAutoStrategyOverThreshold_ShouldReadOnDemand()
    {
        byte[] bytes = BuildSingleStream("Doc", 4000);
        var options = new CompoundFileOptions { ReadStrategy = CompoundReadStrategy.Auto, MaxBufferedBytes = 16 };

        using var file = CompoundFile.Open(new MemoryStream(bytes), options);
        byte[] payload = file.RootStorage.OpenStream("Doc").ReadAllBytes();

        Assert.HasCount(4000, payload);
    }

    /// <summary>
    /// Verifies that an invalid red-black color flag is rejected at <see cref="CompoundValidationLevel.Strict" /> but
    /// tolerated at the default level.
    /// </summary>
    [TestMethod]
    public void Open_WhenColorFlagInvalid_ForStrict_ShouldThrowInvalidDirectory()
    {
        byte[] bytes = BuildSingleStream("Doc", 100);
        var header = CfbHeader.Parse(bytes);
        int entry = FindEntryOffset(bytes, header, "Doc");
        bytes[entry + 67] = 5; // color flag must be 0 (red) or 1 (black)

        using (var tolerated = CompoundFile.Open(new MemoryStream(bytes)))
            Assert.IsTrue(tolerated.RootStorage.TryOpenStream("Doc", out _));

        var options = new CompoundFileOptions { ValidationLevel = CompoundValidationLevel.Strict };
        CompoundFileFormatException ex = Assert.ThrowsExactly<CompoundFileFormatException>(() =>
        {
            using var _ = CompoundFile.Open(new MemoryStream(bytes), options);
        });

        Assert.AreEqual(CompoundFileError.InvalidDirectory, ex.Category);
    }

    /// <summary>
    /// Verifies that unsorted sibling entries are rejected at <see cref="CompoundValidationLevel.Strict" /> but
    /// tolerated at the default level.
    /// </summary>
    [TestMethod]
    public void Open_WhenSiblingsUnsorted_ForStrict_ShouldThrowInvalidDirectory()
    {
        byte[] bytes = BuildWithUnsortedSiblings();

        using (var tolerated = CompoundFile.Open(new MemoryStream(bytes)))
            Assert.IsNotEmpty(tolerated.RootStorage.EnumerateStreams());

        var options = new CompoundFileOptions { ValidationLevel = CompoundValidationLevel.Strict };
        CompoundFileFormatException ex = Assert.ThrowsExactly<CompoundFileFormatException>(() =>
        {
            using var _ = CompoundFile.Open(new MemoryStream(bytes), options);
        });

        Assert.AreEqual(CompoundFileError.InvalidDirectory, ex.Category);
    }

    /// <summary>
    /// Builds a valid version-3 container with a single named stream of the requested size.
    /// </summary>
    /// <param name="name">The stream name.</param>
    /// <param name="size">The stream payload length, in bytes.</param>
    /// <returns>The serialized compound-file bytes.</returns>
    private static byte[] BuildSingleStream(string name, int size)
    {
        byte[] payload = new byte[size];
        for (int i = 0; i < size; i++)
            payload[i] = (byte)((i * 31) + 7);

        var builder = CompoundStorageBuilder.CreateRoot();
        _ = builder.AddStream(name, payload);
        return builder.ToArray();
    }

    /// <summary>
    /// Builds a two-stream container and swaps the names of its two stream entries so the directory's in-order child
    /// sequence is no longer sorted.
    /// </summary>
    /// <returns>The serialized compound-file bytes.</returns>
    private static byte[] BuildWithUnsortedSiblings()
    {
        var root = CompoundStorageBuilder.CreateRoot();
        _ = root.AddStream("A", new byte[] { 1 });
        _ = root.AddStream("B", new byte[] { 2 });
        byte[] bytes = root.ToArray();

        var header = CfbHeader.Parse(bytes);
        int a = FindEntryOffset(bytes, header, "A");
        int b = FindEntryOffset(bytes, header, "B");

        // Swap the single-character names in place: the tree structure stays, but the traversal order is now descending.
        (bytes[a], bytes[b]) = (bytes[b], bytes[a]);
        return bytes;
    }

    /// <summary>
    /// Locates the byte offset of the directory entry with the specified name within the first directory sector.
    /// </summary>
    /// <param name="bytes">The compound-file bytes.</param>
    /// <param name="header">The parsed header.</param>
    /// <param name="name">The entry name to find.</param>
    /// <returns>The zero-based byte offset of the matching directory entry.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the entry is not found.</exception>
    private static int FindEntryOffset(byte[] bytes, CfbHeader header, string name)
    {
        int dirOffset = (int)(((long)header.FirstDirectorySector + 1) * header.SectorSize);
        int perSector = header.SectorSize / DirectoryEntrySize;

        for (int i = 0; i < perSector; i++)
        {
            int entryOffset = dirOffset + (i * DirectoryEntrySize);
            ushort nameLength = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(entryOffset + NameLengthOffset));
            if (nameLength is < 4 or > 64)
                continue;

            string entryName = System.Text.Encoding.Unicode.GetString(bytes, entryOffset, nameLength - 2);
            if (entryName == name)
                return entryOffset;
        }

        throw new InvalidOperationException($"Directory entry '{name}' was not found.");
    }

    /// <summary>
    /// Builds a container whose third (unused) directory slot is turned into a named entry with an invalid type, so it
    /// is an orphan not referenced by any storage tree.
    /// </summary>
    /// <returns>The serialized compound-file bytes.</returns>
    private static byte[] BuildWithOrphanEntry()
    {
        byte[] bytes = BuildSingleStream("Doc", 100);
        var header = CfbHeader.Parse(bytes);

        // Slots 0 (root) and 1 (Doc) are in use; slot 2 is free padding (name length 0, type 0).
        int dirOffset = (int)(((long)header.FirstDirectorySector + 1) * header.SectorSize);
        int orphan = dirOffset + (2 * DirectoryEntrySize);

        // A non-zero name length on a type-0 slot makes it a named entry with an invalid type.
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(orphan + NameLengthOffset), 10);
        return bytes;
    }

    /// <summary>
    /// Builds a container whose single regular stream declares a size far larger than its sector chain.
    /// </summary>
    /// <returns>The serialized compound-file bytes.</returns>
    private static byte[] BuildOverlongStream()
    {
        byte[] bytes = BuildSingleStream("Big", 5000);
        var header = CfbHeader.Parse(bytes);

        int dirOffset = (int)(((long)header.FirstDirectorySector + 1) * header.SectorSize);
        int perSector = header.SectorSize / DirectoryEntrySize;
        for (int i = 0; i < perSector; i++)
        {
            int entry = dirOffset + (i * DirectoryEntrySize);
            ushort nameLength = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(entry + NameLengthOffset));
            if (nameLength == 8)
            {
                BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(entry + SizeOffset), 200_000);
                return bytes;
            }
        }

        throw new InvalidOperationException("Stream entry 'Big' was not found.");
    }
}
