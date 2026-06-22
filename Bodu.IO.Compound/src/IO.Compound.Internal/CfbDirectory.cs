// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CfbDirectory.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Text;

namespace Bodu.IO.Compound.Internal;

/// <summary>
/// Parses the directory chain of a compound file and reconstructs the storage hierarchy from the red-black tree links
/// stored in each directory entry.
/// </summary>
/// <remarks>
/// The directory is a sequence of fixed 128-byte entries indexed by stream identifier (SID). Each storage entry points
/// at the root of a red-black tree whose in-order traversal yields that storage's children in canonical order. This
/// type performs the flat parse and then walks the trees once, recording each entry's direct children, guarding against
/// cyclic or out-of-range links.
/// </remarks>
internal sealed class CfbDirectory
{
    /// <summary>The fixed size, in bytes, of a single directory entry.</summary>
    internal const int DirectoryEntrySize = 128;

    /// <summary>The stream identifier of the root storage entry.</summary>
    private const int RootSid = 0;

    /// <summary>The parsed entries indexed by stream identifier; unallocated slots are <see langword="null" />.</summary>
    private readonly CfbDirectoryEntry?[] _entries;

    /// <summary>The validation level governing how malformed entries and cyclic links are handled.</summary>
    private readonly CompoundValidationLevel _level;

    /// <summary>
    /// Initializes a new instance of the <see cref="CfbDirectory" /> class by parsing the directory bytes and building
    /// the storage hierarchy.
    /// </summary>
    /// <param name="directory">The full byte content of the directory chain.</param>
    /// <param name="header">The parsed compound-file header.</param>
    /// <param name="level">The validation level governing malformed entries and cyclic links.</param>
    /// <exception cref="CompoundFileFormatException">
    /// Thrown when the directory is malformed, lacks a root storage, or contains cyclic or out-of-range tree links.
    /// </exception>
    internal CfbDirectory(byte[] directory, CfbHeader header, CompoundValidationLevel level)
    {
        _level = level;
        _entries = ParseEntries(directory, header, level);

        CompoundThrowHelper.ThrowFormatIf(
            _entries.Length == 0 || _entries[RootSid] is not { Type: CompoundEntryType.RootStorage },
            CompoundResourceStrings.Format_Invalid_CompoundDirectory,
            CompoundFileError.InvalidRootStorage);

        Root = _entries[RootSid]!;
        BuildChildren();
    }

    /// <summary>
    /// Gets the root storage entry that anchors the directory hierarchy.
    /// </summary>
    /// <returns>The root storage entry.</returns>
    internal CfbDirectoryEntry Root { get; }

    /// <summary>
    /// Gets the entry with the specified stream identifier.
    /// </summary>
    /// <param name="sid">The stream identifier to resolve.</param>
    /// <returns>
    /// The entry at <paramref name="sid" />, or <see langword="null" /> when the slot is unallocated.
    /// </returns>
    internal CfbDirectoryEntry? GetEntry(int sid) =>
        (uint)sid < (uint)_entries.Length ? _entries[sid] : null;

    /// <summary>
    /// Parses every directory record into a SID-indexed array, leaving unallocated slots as <see langword="null" />.
    /// </summary>
    /// <param name="directory">The directory byte content.</param>
    /// <param name="header">The parsed compound-file header.</param>
    /// <param name="level">
    /// The validation level; <see cref="CompoundValidationLevel.Strict" /> rejects malformed entries that other levels
    /// skip.
    /// </param>
    /// <returns>The parsed entries indexed by stream identifier.</returns>
    /// <exception cref="CompoundFileFormatException">
    /// Thrown at <see cref="CompoundValidationLevel.Strict" /> when an entry has an invalid name length or entry type.
    /// </exception>
    private static CfbDirectoryEntry?[] ParseEntries(byte[] directory, CfbHeader header, CompoundValidationLevel level)
    {
        int count = directory.Length / DirectoryEntrySize;
        CfbDirectoryEntry?[] entries = new CfbDirectoryEntry?[count];
        bool strict = level == CompoundValidationLevel.Strict;

        for (int sid = 0; sid < count; sid++)
        {
            ReadOnlySpan<byte> record = directory.AsSpan(sid * DirectoryEntrySize, DirectoryEntrySize);

            int nameLength = BinaryPrimitives.ReadUInt16LittleEndian(record.Slice(64));

            // A zero name length marks an unused (free) directory slot, which is normal padding at every level.
            if (nameLength == 0)
                continue;

            if (nameLength > 64)
            {
                CompoundThrowHelper.ThrowFormatIf(strict, CompoundResourceStrings.Format_Invalid_CompoundDirectory, CompoundFileError.InvalidDirectory);
                continue;
            }

            var type = (CompoundEntryType)record[66];
            if (type is not CompoundEntryType.Storage
                and not CompoundEntryType.Stream
                and not CompoundEntryType.RootStorage)
            {
                CompoundThrowHelper.ThrowFormatIf(strict, CompoundResourceStrings.Format_Invalid_CompoundDirectory, CompoundFileError.InvalidDirectory);
                continue;
            }

            int characters = (nameLength / 2) - 1;
            string name = characters > 0 ? Encoding.Unicode.GetString(record.Slice(0, characters * 2)) : string.Empty;

            var color = (CompoundEntryColor)record[67];
            uint left = BinaryPrimitives.ReadUInt32LittleEndian(record.Slice(68));
            uint right = BinaryPrimitives.ReadUInt32LittleEndian(record.Slice(72));
            uint child = BinaryPrimitives.ReadUInt32LittleEndian(record.Slice(76));
            var classId = new Guid(record.Slice(80, 16));
            int stateBits = BinaryPrimitives.ReadInt32LittleEndian(record.Slice(96));
            long creationFileTime = BinaryPrimitives.ReadInt64LittleEndian(record.Slice(100));
            long modifiedFileTime = BinaryPrimitives.ReadInt64LittleEndian(record.Slice(108));
            uint startSector = BinaryPrimitives.ReadUInt32LittleEndian(record.Slice(116));
            ulong rawSize = BinaryPrimitives.ReadUInt64LittleEndian(record.Slice(120));

            // Version-3 files (512-byte sectors) store the size in the low 32 bits; the high DWORD must be ignored.
            long size = header.SectorSize == 512 ? (long)(rawSize & 0xFFFFFFFF) : (long)rawSize;

            entries[sid] = new CfbDirectoryEntry(
                sid, name, type, color, left, right, child, classId, stateBits, creationFileTime, modifiedFileTime, startSector, size);
        }

        return entries;
    }

    /// <summary>
    /// Walks the red-black tree under each storage, populating every entry's resolved child list in canonical order.
    /// </summary>
    /// <exception cref="CompoundFileFormatException">
    /// Thrown when the tree contains a cyclic, duplicated, or out-of-range link.
    /// </exception>
    private void BuildChildren()
    {
        bool[] visited = new bool[_entries.Length];
        Queue<CfbDirectoryEntry> storages = new();
        storages.Enqueue(Root);

        while (storages.Count > 0)
        {
            CfbDirectoryEntry storage = storages.Dequeue();
            CollectChildren(storage.ChildId, storage.Children, visited);

            foreach (int childSid in storage.Children)
            {
                CfbDirectoryEntry child = _entries[childSid]!;
                if (child.Type is CompoundEntryType.Storage or CompoundEntryType.RootStorage)
                    storages.Enqueue(child);
            }
        }
    }

    /// <summary>
    /// Performs an in-order traversal of a child red-black tree, appending each node's stream identifier to
    /// <paramref name="children" /> in canonical order.
    /// </summary>
    /// <param name="sid">The stream identifier of the subtree root, or <see cref="CfbHeader.NoStream" />.</param>
    /// <param name="children">The list receiving the ordered child stream identifiers.</param>
    /// <param name="visited">Tracks visited stream identifiers to detect cycles and shared nodes.</param>
    /// <exception cref="CompoundFileFormatException">
    /// Thrown when a link is out of range, references an unallocated entry, or revisits an entry.
    /// </exception>
    private void CollectChildren(uint sid, List<int> children, bool[] visited)
    {
        if (sid == CfbHeader.NoStream)
            return;

        if (sid >= (uint)_entries.Length || _entries[sid] is null || visited[sid])
        {
            // A tolerant level prunes the offending subtree instead of rejecting the whole directory.
            if (_level == CompoundValidationLevel.Minimal)
                return;

            CompoundThrowHelper.ThrowFormat(CompoundResourceStrings.Format_Invalid_CompoundDirectoryTree, CompoundFileError.DirectoryCycle);
        }

        visited[sid] = true;
        CfbDirectoryEntry entry = _entries[sid]!;

        CollectChildren(entry.LeftSiblingId, children, visited);
        children.Add((int)sid);
        CollectChildren(entry.RightSiblingId, children, visited);
    }
}
