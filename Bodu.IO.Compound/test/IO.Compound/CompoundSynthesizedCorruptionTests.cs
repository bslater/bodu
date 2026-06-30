// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundSynthesizedCorruptionTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Text;
using Bodu.IO.Compound.Builders;
using Bodu.IO.Compound.Internal;
using Bodu.Test;

namespace Bodu.IO.Compound;

/// <summary>
/// Verifies that the reader rejects specific structural corruptions that are awkward to express as static fixtures by
/// synthesizing a valid container and surgically corrupting it. The embedded <c>invalid/*.dat</c> corpus already covers
/// FAT/directory cycles, invalid sibling/child ids, red-black ordering (<c>invalid_dir_indexes1.dat</c>), truncation,
/// and general MiniFAT/size defects; this class pins the explicit mini-FAT cycle and over-long stream cases.
/// </summary>
[TestClass]
public class CompoundSynthesizedCorruptionTests
{
    /// <summary>The byte offset, within a directory entry, of the 16-bit UTF-16 name length.</summary>
    private const int NameLengthOffset = 64;

    /// <summary>The byte offset, within a directory entry, of the 32-bit starting sector.</summary>
    private const int StartSectorOffset = 116;

    /// <summary>The byte offset, within a directory entry, of the 64-bit payload size.</summary>
    private const int SizeOffset = 120;

    /// <summary>The fixed size, in bytes, of a directory entry.</summary>
    private const int DirectoryEntrySize = 128;

    /// <summary>
    /// Verifies that a stream whose declared size exceeds its sector chain is rejected with
    /// <see cref="CompoundFileError.StreamChainTooShort" /> when read.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    public void OpenStream_WhenDeclaredSizeExceedsChain_ShouldThrowStreamChainTooShort()
    {
        // A 5000-byte stream is a regular (non-mini) stream; inflating its size keeps it on the regular-chain path.
        byte[] bytes = BuildSingleStream("Big", 5000);
        var header = CfbHeader.Parse(bytes);
        int entry = FindEntryOffset(bytes, header, "Big");

        BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(entry + SizeOffset), 200_000);

        using var file = CompoundFile.Open(new MemoryStream(bytes));
        CompoundFileFormatException ex = Assert.ThrowsExactly<CompoundFileFormatException>(
            () => _ = file.RootStorage.OpenStream("Big").ReadAllBytes());

        Assert.AreEqual(CompoundFileError.StreamChainTooShort, ex.Category);
    }

    /// <summary>
    /// Verifies that a mini-FAT entry that points at itself is rejected with
    /// <see cref="CompoundFileError.InvalidMiniFat" /> rather than looping forever.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    public void OpenStream_WhenMiniFatSelfCycle_ShouldThrowInvalidMiniFat()
    {
        // A 100-byte stream is stored in the mini stream, so it is resolved through the mini-FAT.
        byte[] bytes = BuildSingleStream("Small", 100);
        var header = CfbHeader.Parse(bytes);
        int entry = FindEntryOffset(bytes, header, "Small");

        uint startMiniSector = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(entry + StartSectorOffset));
        long miniFatOffset = ((long)header.FirstMiniFatSector + 1) * header.SectorSize;

        // Point the stream's first mini-sector at itself, forming a cycle.
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan((int)(miniFatOffset + (startMiniSector * sizeof(uint)))), startMiniSector);

        using var file = CompoundFile.Open(new MemoryStream(bytes));
        CompoundFileFormatException ex = Assert.ThrowsExactly<CompoundFileFormatException>(
            () => _ = file.RootStorage.OpenStream("Small").ReadAllBytes());

        Assert.AreEqual(CompoundFileError.InvalidMiniFat, ex.Category);
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

            string entryName = Encoding.Unicode.GetString(bytes, entryOffset, nameLength - 2);
            if (entryName == name)
                return entryOffset;
        }

        throw new InvalidOperationException($"Directory entry '{name}' was not found.");
    }
}
