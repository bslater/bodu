// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundEntryInfo.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound;

/// <summary>
/// Provides an immutable snapshot of the metadata recorded for a single compound-file directory entry — a storage, a
/// stream, or the root storage.
/// </summary>
/// <remarks>
/// <para>
/// This record is the managed counterpart of the COM <c>STATSTG</c> structure: it carries the name, kind, payload size,
/// class identifier, state bits, and time stamps that the directory stores for each entry, without exposing the
/// underlying sector layout.
/// </para>
/// <para>
/// The <see cref="CreationTime" /> and <see cref="LastModifiedTime" /> values are meaningful for storage and
/// root-storage entries; stream entries conventionally record zero, which is surfaced as <see langword="null" />.
/// </para>
/// </remarks>
public sealed record CompoundEntryInfo
{
    /// <summary>
    /// Gets the entry name as stored in the directory.
    /// </summary>
    /// <returns>
    /// The directory name. Control prefixes used by some Office streams (for example, the <c>\x05</c> prefix on
    /// summary-information streams) are preserved verbatim.
    /// </returns>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the kind of object this entry describes.
    /// </summary>
    /// <returns>The <see cref="CompoundEntryType" /> of the entry.</returns>
    public CompoundEntryType EntryType { get; init; }

    /// <summary>
    /// Gets the payload size, in bytes.
    /// </summary>
    /// <returns>
    /// The number of payload bytes for a stream entry; <c>0</c> for storage and root-storage entries.
    /// </returns>
    public long Length { get; init; }

    /// <summary>
    /// Gets the class identifier (CLSID) associated with the entry.
    /// </summary>
    /// <returns>The entry's class identifier, or <see cref="Guid.Empty" /> when none is recorded.</returns>
    /// <value>
    /// For storage and root-storage entries this is the object class that created the storage; stream entries typically
    /// record <see cref="Guid.Empty" />.
    /// </value>
    public Guid ClassId { get; init; }

    /// <summary>
    /// Gets the user-defined state bits recorded for the entry.
    /// </summary>
    /// <returns>The raw state-bit flags, or <c>0</c> when none are recorded.</returns>
    public int StateBits { get; init; }

    /// <summary>
    /// Gets the creation time recorded for the entry.
    /// </summary>
    /// <returns>The creation time, or <see langword="null" /> when the directory records no time stamp.</returns>
    public DateTimeOffset? CreationTime { get; init; }

    /// <summary>
    /// Gets the last-modified time recorded for the entry.
    /// </summary>
    /// <returns>The last-modified time, or <see langword="null" /> when the directory records no time stamp.</returns>
    public DateTimeOffset? LastModifiedTime { get; init; }

    /// <summary>
    /// Gets the red-black tree node color recorded for the entry.
    /// </summary>
    /// <returns>The <see cref="CompoundEntryColor" /> of the entry's directory node.</returns>
    public CompoundEntryColor Color { get; init; }
}
