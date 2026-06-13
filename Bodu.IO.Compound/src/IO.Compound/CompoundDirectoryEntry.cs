// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundDirectoryEntry.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound;

/// <summary>
/// Describes a single entry in a compound file's directory — a storage, a stream, or the root storage — exposing the
/// name, kind, and payload size without revealing the underlying sector layout.
/// </summary>
/// <remarks>
/// Instances are produced by <see cref="CompoundBinaryFile" /> while reading the directory and are immutable snapshots.
/// </remarks>
public sealed class CompoundDirectoryEntry
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompoundDirectoryEntry" /> class.
    /// </summary>
    /// <param name="name">The entry name as stored in the directory.</param>
    /// <param name="type">The kind of object the entry describes.</param>
    /// <param name="startSector">The first sector (or mini sector) of the entry's payload.</param>
    /// <param name="size">The payload size, in bytes.</param>
    internal CompoundDirectoryEntry(string name, CompoundDirectoryEntryType type, uint startSector, long size)
    {
        Name = name;
        Type = type;
        StartSector = startSector;
        Size = size;
    }

    /// <summary>
    /// Gets the entry name as stored in the directory.
    /// </summary>
    /// <returns>
    /// The directory name. Control prefixes used by some Office streams (for example, the <c>\x05</c> prefix on
    /// summary-information streams) are preserved verbatim.
    /// </returns>
    public string Name { get; }

    /// <summary>
    /// Gets the kind of object this entry describes.
    /// </summary>
    /// <returns>The <see cref="CompoundDirectoryEntryType" /> of the entry.</returns>
    public CompoundDirectoryEntryType Type { get; }

    /// <summary>
    /// Gets the payload size, in bytes.
    /// </summary>
    /// <returns>The number of payload bytes for a stream entry; <c>0</c> for storage entries.</returns>
    public long Size { get; }

    /// <summary>
    /// Gets the first sector identifier of the entry's payload chain. For stream entries smaller than the mini-stream
    /// cutoff this indexes the mini-FAT; otherwise it indexes the regular FAT. For the root storage it is the first
    /// sector of the mini stream.
    /// </summary>
    /// <returns>The starting sector or mini-sector identifier.</returns>
    internal uint StartSector { get; }
}
