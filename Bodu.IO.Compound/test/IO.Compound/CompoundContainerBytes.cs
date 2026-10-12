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
/// The FAT is located through the header's DIFAT array and the DIFAT chain. A FAT sector that lies past the end of the
/// bytes reads as free entries, so the directory of a container whose write stopped after the directory can still be
/// found.
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
    /// Returns the sectors of the DIFAT chain.
    /// </summary>
    /// <param name="container">The bytes of the container.</param>
    /// <returns>The DIFAT sectors in chain order; empty when the header counts none.</returns>
    internal static List<uint> DifatSectorIds(byte[] container)
    {
        var header = CfbHeader.Parse(container);
        List<uint> sectors = [];
        uint sector = header.FirstDifatSector;
        while (sectors.Count < header.DifatSectorCount && sector < CfbHeader.DifatSector)
        {
            sectors.Add(sector);
            long next = SectorOffset(header, sector) + header.SectorSize - 4;
            sector = next + 4 <= container.Length ? BinaryPrimitives.ReadUInt32LittleEndian(container.AsSpan((int)next)) : CfbHeader.EndOfChain;
        }

        return sectors;
    }

    /// <summary>
    /// Returns the sectors that hold the FAT, from the header's DIFAT array and then the DIFAT chain.
    /// </summary>
    /// <param name="container">The bytes of the container.</param>
    /// <returns>The FAT sectors in FAT order.</returns>
    internal static List<uint> FatSectorIds(byte[] container)
    {
        var header = CfbHeader.Parse(container);
        List<uint> sectors = [];
        foreach (uint sector in header.Difat)
        {
            if (sectors.Count < header.FatSectorCount && sector < CfbHeader.DifatSector)
                sectors.Add(sector);
        }

        foreach (uint difat in DifatSectorIds(container))
        {
            long offset = SectorOffset(header, difat);
            if (offset + header.SectorSize > container.Length)
                break;

            for (int i = 0; i < header.EntriesPerSector - 1 && sectors.Count < header.FatSectorCount; i++)
            {
                uint sector = BinaryPrimitives.ReadUInt32LittleEndian(container.AsSpan((int)offset + (i * 4)));
                if (sector < CfbHeader.DifatSector)
                    sectors.Add(sector);
            }
        }

        return sectors;
    }

    /// <summary>
    /// Reads the FAT.
    /// </summary>
    /// <param name="container">The bytes of the container.</param>
    /// <returns>The FAT entries, with the entries of a FAT sector past the end of the bytes read as free.</returns>
    internal static uint[] ReadFat(byte[] container)
    {
        var header = CfbHeader.Parse(container);
        int perSector = header.EntriesPerSector;
        List<uint> fatSectors = FatSectorIds(container);
        uint[] fat = new uint[fatSectors.Count * perSector];
        fat.AsSpan().Fill(CfbHeader.FreeSector);

        for (int i = 0; i < fatSectors.Count; i++)
        {
            long offset = SectorOffset(header, fatSectors[i]);
            if (offset + header.SectorSize > container.Length)
                continue;

            for (int j = 0; j < perSector; j++)
                fat[(i * perSector) + j] = BinaryPrimitives.ReadUInt32LittleEndian(container.AsSpan((int)offset + (j * 4)));
        }

        return fat;
    }

    /// <summary>
    /// Writes the FAT entry of a sector.
    /// </summary>
    /// <param name="container">The bytes of the container.</param>
    /// <param name="sector">The sector whose entry is written.</param>
    /// <param name="value">The value to write.</param>
    internal static void SetFatEntry(byte[] container, uint sector, uint value)
    {
        var header = CfbHeader.Parse(container);
        uint fatSector = FatSectorIds(container)[(int)(sector / (uint)header.EntriesPerSector)];
        long offset = SectorOffset(header, fatSector) + ((sector % (uint)header.EntriesPerSector) * 4);
        WriteUInt32(container, (int)offset, value);
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

    /// <summary>
    /// Returns the sectors of a chain.
    /// </summary>
    /// <param name="container">The bytes of the container.</param>
    /// <param name="start">The first sector of the chain.</param>
    /// <returns>The sectors in chain order.</returns>
    internal static List<uint> Chain(byte[] container, uint start)
    {
        uint[] fat = ReadFat(container);
        List<uint> chain = [];
        for (uint sector = start; sector < fat.Length && chain.Count <= fat.Length; sector = fat[sector])
            chain.Add(sector);

        return chain;
    }

    /// <summary>
    /// Moves the last sector of a chain to a new sector after the end of the container, as a writer that appended it
    /// last would have placed it, and relinks the chain.
    /// </summary>
    /// <param name="container">The bytes of the container, as the writer wrote them.</param>
    /// <param name="startFieldOffset">The offset of the field that holds the chain's first sector.</param>
    /// <returns>The container, one sector longer.</returns>
    /// <remarks>
    /// The new sector is the one that begins at the old end of the container, so it must fall within the FAT as written.
    /// </remarks>
    internal static byte[] MoveLastSector(byte[] container, int startFieldOffset)
    {
        var header = CfbHeader.Parse(container);
        List<uint> chain = Chain(container, BinaryPrimitives.ReadUInt32LittleEndian(container.AsSpan(startFieldOffset)));
        uint last = chain[^1];
        uint moved = (uint)((container.Length / header.SectorSize) - 1);
        byte[] result = new byte[container.Length + header.SectorSize];
        container.CopyTo(result, 0);
        container.AsSpan((int)SectorOffset(header, last), header.SectorSize).CopyTo(result.AsSpan((int)SectorOffset(header, moved)));

        if (chain.Count >= 2)
            SetFatEntry(result, chain[^2], moved);
        else
            WriteUInt32(result, startFieldOffset, moved);

        SetFatEntry(result, moved, CfbHeader.EndOfChain);
        SetFatEntry(result, last, CfbHeader.FreeSector);
        return result;
    }
}
