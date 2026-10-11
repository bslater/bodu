// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundContainerBytes.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Text;
using Bodu.IO.Compound.Internal;

namespace Bodu.IO.Compound;

/// <summary>
/// Provides helpers that locate the allocation table and the directory entries in the bytes of a serialized compound
/// file, so that a test can inspect or patch what the writer produced.
/// </summary>
/// <remarks>
/// The FAT is read through the header's DIFAT array only, which serves containers of up to 109 FAT sectors. A FAT
/// sector that lies past the end of the bytes reads as free entries, so the directory of a container whose write
/// stopped after the directory can still be found.
/// </remarks>
internal static class CompoundContainerBytes
{
    /// <summary>The size of a directory entry, in bytes.</summary>
    internal const int DirectoryEntrySize = 128;

    /// <summary>The offset, within a directory entry, of the 16-bit name length.</summary>
    internal const int NameLengthOffset = 64;

    /// <summary>The offset, within a directory entry, of the object type byte.</summary>
    internal const int TypeOffset = 66;

    /// <summary>The offset, within a directory entry, of the color flag byte.</summary>
    internal const int ColorOffset = 67;

    /// <summary>The offset, within a directory entry, of the left sibling stream identifier.</summary>
    internal const int LeftSiblingOffset = 68;

    /// <summary>The offset, within a directory entry, of the right sibling stream identifier.</summary>
    internal const int RightSiblingOffset = 72;

    /// <summary>The offset, within a directory entry, of the child stream identifier.</summary>
    internal const int ChildOffset = 76;

    /// <summary>The offset, within a directory entry, of the class identifier.</summary>
    internal const int ClassIdOffset = 80;

    /// <summary>The offset, within a directory entry, of the creation time.</summary>
    internal const int CreationTimeOffset = 100;

    /// <summary>The offset, within a directory entry, of the modified time.</summary>
    internal const int ModifiedTimeOffset = 108;

    /// <summary>The offset, within a directory entry, of the starting sector.</summary>
    internal const int StartSectorOffset = 116;

    /// <summary>The offset, within a directory entry, of the 64-bit stream size.</summary>
    internal const int SizeOffset = 120;

    /// <summary>
    /// Returns the catalogue pattern, whose byte at offset <c>i</c> is <c>(i * 31 + 7) mod 256</c>.
    /// </summary>
    /// <param name="length">The number of bytes to return.</param>
    /// <param name="start">The offset of the first byte within the pattern.</param>
    /// <returns>The bytes of the pattern from <paramref name="start" />.</returns>
    internal static byte[] Pattern(int length, long start = 0)
    {
        byte[] bytes = new byte[length];
        for (int i = 0; i < length; i++)
            bytes[i] = PatternByte(start + i);

        return bytes;
    }

    /// <summary>
    /// Returns the byte of the catalogue pattern at the specified offset.
    /// </summary>
    /// <param name="offset">The offset within the pattern.</param>
    /// <returns><c>(offset * 31 + 7) mod 256</c>.</returns>
    internal static byte PatternByte(long offset) =>
        unchecked((byte)((offset * 31) + 7));

    /// <summary>
    /// Returns the byte offset at which the specified sector begins.
    /// </summary>
    /// <param name="header">The container's header.</param>
    /// <param name="sector">The sector number.</param>
    /// <returns>The offset of the sector, one sector past the start of the header.</returns>
    internal static long SectorOffset(CfbHeader header, uint sector) =>
        ((long)sector + 1) * header.SectorSize;

    /// <summary>
    /// Reads the FAT sectors that the header's DIFAT array lists.
    /// </summary>
    /// <param name="container">The bytes of the container.</param>
    /// <returns>The FAT entries, with the entries of a sector past the end of the bytes read as free.</returns>
    internal static uint[] ReadFat(byte[] container)
    {
        var header = CfbHeader.Parse(container);
        int perSector = header.EntriesPerSector;
        int listed = (int)Math.Min(header.FatSectorCount, CfbHeader.HeaderDifatCount);
        uint[] fat = new uint[listed * perSector];
        fat.AsSpan().Fill(CfbHeader.FreeSector);

        for (int i = 0; i < listed; i++)
        {
            long offset = SectorOffset(header, header.Difat[i]);
            if (offset + header.SectorSize > container.Length)
                continue;

            for (int j = 0; j < perSector; j++)
                fat[(i * perSector) + j] = BinaryPrimitives.ReadUInt32LittleEndian(container.AsSpan((int)offset + (j * 4)));
        }

        return fat;
    }

    /// <summary>
    /// Returns the byte offset of every directory entry slot, in stream identifier order.
    /// </summary>
    /// <param name="container">The bytes of the container.</param>
    /// <returns>The offsets of the slots of each directory sector the directory chain reaches.</returns>
    internal static List<int> DirectoryEntryOffsets(byte[] container)
    {
        var header = CfbHeader.Parse(container);
        uint[] fat = ReadFat(container);
        int perSector = header.SectorSize / DirectoryEntrySize;
        List<int> offsets = [];

        uint sector = header.FirstDirectorySector;
        while (sector < CfbHeader.DifatSector && SectorOffset(header, sector) + header.SectorSize <= container.Length)
        {
            int start = (int)SectorOffset(header, sector);
            for (int i = 0; i < perSector; i++)
                offsets.Add(start + (i * DirectoryEntrySize));

            sector = sector < fat.Length ? fat[sector] : CfbHeader.EndOfChain;
        }

        return offsets;
    }

    /// <summary>
    /// Returns the byte offset of the directory entry with the specified name.
    /// </summary>
    /// <param name="container">The bytes of the container.</param>
    /// <param name="name">The name of the entry; the root is <c>Root Entry</c>.</param>
    /// <returns>The offset of the first entry whose name is <paramref name="name" />.</returns>
    /// <exception cref="InvalidOperationException">No entry has the name.</exception>
    internal static int FindEntryOffset(byte[] container, string name)
    {
        foreach (int offset in DirectoryEntryOffsets(container))
        {
            int nameLength = BinaryPrimitives.ReadUInt16LittleEndian(container.AsSpan(offset + NameLengthOffset));
            if (nameLength is >= 2 and <= 64 && Encoding.Unicode.GetString(container, offset, nameLength - 2) == name)
                return offset;
        }

        throw new InvalidOperationException($"The container has no directory entry named '{name}'.");
    }

    /// <summary>
    /// Returns the stream identifier of the directory entry with the specified name.
    /// </summary>
    /// <param name="container">The bytes of the container.</param>
    /// <param name="name">The name of the entry.</param>
    /// <returns>The index of the entry's slot in the directory.</returns>
    internal static uint FindStreamId(byte[] container, string name) =>
        (uint)DirectoryEntryOffsets(container).IndexOf(FindEntryOffset(container, name));

    /// <summary>
    /// Returns the length a container needs to hold every sector its FAT allocates.
    /// </summary>
    /// <param name="container">The bytes of the container.</param>
    /// <returns>The header sector plus every sector up to the last whose FAT entry is not free, in bytes.</returns>
    internal static long AllocatedLength(byte[] container)
    {
        var header = CfbHeader.Parse(container);
        uint[] fat = ReadFat(container);
        int last = Array.FindLastIndex(fat, static entry => entry != CfbHeader.FreeSector);

        return SectorOffset(header, (uint)(last + 1));
    }

    /// <summary>
    /// Writes a 32-bit little-endian value into the container.
    /// </summary>
    /// <param name="container">The bytes of the container.</param>
    /// <param name="offset">The offset of the value.</param>
    /// <param name="value">The value to write.</param>
    internal static void WriteUInt32(byte[] container, int offset, uint value) =>
        BinaryPrimitives.WriteUInt32LittleEndian(container.AsSpan(offset), value);

    /// <summary>
    /// Writes a 64-bit little-endian value into the container.
    /// </summary>
    /// <param name="container">The bytes of the container.</param>
    /// <param name="offset">The offset of the value.</param>
    /// <param name="value">The value to write.</param>
    internal static void WriteInt64(byte[] container, int offset, long value) =>
        BinaryPrimitives.WriteInt64LittleEndian(container.AsSpan(offset), value);
}
