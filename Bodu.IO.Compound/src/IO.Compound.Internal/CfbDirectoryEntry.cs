// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CfbDirectoryEntry.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound.Internal;

/// <summary>
/// Represents a single parsed entry of a compound file's directory, retaining the red-black tree links and full
/// metadata needed to reconstruct the storage hierarchy.
/// </summary>
/// <remarks>
/// Instances are produced by <see cref="CfbDirectory" /> while reading the directory chain. The type is internal and
/// mutable so the directory builder can populate the resolved child list after parsing; the public surface exposes only
/// the immutable <see cref="CompoundEntryInfo" /> projection returned by <see cref="ToEntryInfo" />.
/// </remarks>
internal sealed class CfbDirectoryEntry
{
    /// <summary>The largest Windows FILETIME that maps to a representable <see cref="DateTime" />.</summary>
    private static readonly long s_maxFileTime = DateTime.MaxValue.ToFileTimeUtc();

    /// <summary>
    /// Initializes a new instance of the <see cref="CfbDirectoryEntry" /> class.
    /// </summary>
    /// <param name="sid">The zero-based stream identifier (directory index) of the entry.</param>
    /// <param name="name">The entry name as stored in the directory.</param>
    /// <param name="type">The kind of object the entry describes.</param>
    /// <param name="color">The red-black tree node color of the entry.</param>
    /// <param name="leftSiblingId">
    /// The stream identifier of the left sibling, or <see cref="CfbHeader.NoStream" />.
    /// </param>
    /// <param name="rightSiblingId">
    /// The stream identifier of the right sibling, or <see cref="CfbHeader.NoStream" />.
    /// </param>
    /// <param name="childId">
    /// The stream identifier of the child-tree root, or <see cref="CfbHeader.NoStream" />.
    /// </param>
    /// <param name="classId">The class identifier (CLSID) recorded for the entry.</param>
    /// <param name="stateBits">The user-defined state bits recorded for the entry.</param>
    /// <param name="creationFileTime">The raw creation Windows FILETIME.</param>
    /// <param name="modifiedFileTime">The raw last-modified Windows FILETIME.</param>
    /// <param name="startSector">The first sector (or mini sector) of the entry's payload.</param>
    /// <param name="size">The payload size, in bytes.</param>
    internal CfbDirectoryEntry(
        int sid,
        string name,
        CompoundEntryType type,
        CompoundEntryColor color,
        uint leftSiblingId,
        uint rightSiblingId,
        uint childId,
        Guid classId,
        int stateBits,
        long creationFileTime,
        long modifiedFileTime,
        uint startSector,
        long size)
    {
        Sid = sid;
        Name = name;
        Type = type;
        Color = color;
        LeftSiblingId = leftSiblingId;
        RightSiblingId = rightSiblingId;
        ChildId = childId;
        ClassId = classId;
        StateBits = stateBits;
        CreationFileTime = creationFileTime;
        ModifiedFileTime = modifiedFileTime;
        StartSector = startSector;
        Size = size;
        Children = new List<int>();
    }

    /// <summary>
    /// Gets the zero-based stream identifier (directory index) of the entry.
    /// </summary>
    /// <returns>The entry's stream identifier.</returns>
    internal int Sid { get; }

    /// <summary>
    /// Gets the entry name as stored in the directory.
    /// </summary>
    /// <returns>The directory name, with any control prefix preserved.</returns>
    internal string Name { get; }

    /// <summary>
    /// Gets the kind of object the entry describes.
    /// </summary>
    /// <returns>The entry type.</returns>
    internal CompoundEntryType Type { get; }

    /// <summary>
    /// Gets the red-black tree node color of the entry.
    /// </summary>
    /// <returns>The entry color.</returns>
    internal CompoundEntryColor Color { get; }

    /// <summary>
    /// Gets the stream identifier of the left sibling in the parent's child tree.
    /// </summary>
    /// <returns>The left-sibling identifier, or <see cref="CfbHeader.NoStream" /> when absent.</returns>
    internal uint LeftSiblingId { get; }

    /// <summary>
    /// Gets the stream identifier of the right sibling in the parent's child tree.
    /// </summary>
    /// <returns>The right-sibling identifier, or <see cref="CfbHeader.NoStream" /> when absent.</returns>
    internal uint RightSiblingId { get; }

    /// <summary>
    /// Gets the stream identifier of the root of this entry's child tree.
    /// </summary>
    /// <returns>
    /// The child-tree root identifier, or <see cref="CfbHeader.NoStream" /> when the entry has no children.
    /// </returns>
    internal uint ChildId { get; }

    /// <summary>
    /// Gets the class identifier (CLSID) recorded for the entry.
    /// </summary>
    /// <returns>The entry's class identifier.</returns>
    internal Guid ClassId { get; }

    /// <summary>
    /// Gets the user-defined state bits recorded for the entry.
    /// </summary>
    /// <returns>The raw state-bit flags.</returns>
    internal int StateBits { get; }

    /// <summary>
    /// Gets the raw creation Windows FILETIME recorded for the entry.
    /// </summary>
    /// <returns>The 100-nanosecond ticks since 1601-01-01 UTC, or <c>0</c> when none is recorded.</returns>
    internal long CreationFileTime { get; }

    /// <summary>
    /// Gets the raw last-modified Windows FILETIME recorded for the entry.
    /// </summary>
    /// <returns>The 100-nanosecond ticks since 1601-01-01 UTC, or <c>0</c> when none is recorded.</returns>
    internal long ModifiedFileTime { get; }

    /// <summary>
    /// Gets the first sector identifier of the entry's payload chain. For stream entries smaller than the mini-stream
    /// cutoff this indexes the mini-FAT; otherwise it indexes the regular FAT. For the root storage it is the first
    /// sector of the mini stream.
    /// </summary>
    /// <returns>The starting sector or mini-sector identifier.</returns>
    internal uint StartSector { get; }

    /// <summary>
    /// Gets the payload size, in bytes.
    /// </summary>
    /// <returns>The payload byte count for a stream entry; <c>0</c> for storage entries.</returns>
    internal long Size { get; }

    /// <summary>
    /// Gets the resolved stream identifiers of this entry's direct children, in directory order.
    /// </summary>
    /// <returns>A mutable list populated by the directory builder; empty for stream entries.</returns>
    internal List<int> Children { get; }

    /// <summary>
    /// Projects the entry into an immutable <see cref="CompoundEntryInfo" /> snapshot for the public surface.
    /// </summary>
    /// <returns>A populated <see cref="CompoundEntryInfo" /> describing the entry.</returns>
    internal CompoundEntryInfo ToEntryInfo() =>
        new()
        {
            Name = Name,
            EntryType = Type,
            Length = Size,
            ClassId = ClassId,
            StateBits = StateBits,
            CreationTime = ToDateTimeOffset(CreationFileTime),
            LastModifiedTime = ToDateTimeOffset(ModifiedFileTime),
            Color = Color,
        };

    /// <summary>
    /// Converts a raw Windows FILETIME into a UTC <see cref="DateTimeOffset" />.
    /// </summary>
    /// <param name="fileTime">The 100-nanosecond ticks since 1601-01-01 UTC.</param>
    /// <returns>
    /// The corresponding UTC <see cref="DateTimeOffset" />, or <see langword="null" /> when the value is zero or
    /// outside the representable range.
    /// </returns>
    private static DateTimeOffset? ToDateTimeOffset(long fileTime)
    {
        if (fileTime <= 0 || fileTime > s_maxFileTime)
            return null;

        return new DateTimeOffset(DateTime.FromFileTimeUtc(fileTime), TimeSpan.Zero);
    }
}
