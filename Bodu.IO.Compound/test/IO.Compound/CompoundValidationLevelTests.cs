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

        using CompoundFile file = CompoundFile.Open(new MemoryStream(bytes));

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

        using CompoundFile file = CompoundFile.Open(new MemoryStream(bytes));
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

        using CompoundFile file = CompoundFile.Open(new MemoryStream(bytes), options);
        byte[] payload = file.RootStorage.OpenStream("Big").ReadAllBytes().ToArray();

        Assert.AreEqual(200_000, payload.Length);
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

        using CompoundFile file = CompoundFile.Open(new MemoryStream(bytes), options);
        byte[] payload = file.RootStorage.OpenStream("Doc").ReadAllBytes().ToArray();

        Assert.AreEqual(4000, payload.Length);
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
    /// Builds a container whose third (unused) directory slot is turned into a named entry with an invalid type, so it
    /// is an orphan not referenced by any storage tree.
    /// </summary>
    /// <returns>The serialized compound-file bytes.</returns>
    private static byte[] BuildWithOrphanEntry()
    {
        byte[] bytes = BuildSingleStream("Doc", 100);
        CfbHeader header = CfbHeader.Parse(bytes);

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
        CfbHeader header = CfbHeader.Parse(bytes);

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
