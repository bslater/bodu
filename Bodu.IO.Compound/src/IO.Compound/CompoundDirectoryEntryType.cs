// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundDirectoryEntryType.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound;

/// <summary>
/// Identifies the kind of object a <see cref="CompoundDirectoryEntry" /> describes within a compound file's directory.
/// </summary>
/// <remarks>
/// The numeric values match the object-type byte stored in each directory entry by the OLE2 / Compound File Binary
/// specification, so the enum can be assigned directly from the parsed byte.
/// </remarks>
public enum CompoundDirectoryEntryType : byte
{
    /// <summary>
    /// An unknown or unallocated directory entry. Such entries are skipped during enumeration.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// A storage object, equivalent to a directory in a file system; it may contain child storages and streams.
    /// </summary>
    Storage = 1,

    /// <summary>
    /// A stream object, equivalent to a file in a file system; it holds an opaque byte payload.
    /// </summary>
    Stream = 2,

    /// <summary>
    /// The single root-storage entry that anchors the directory tree and owns the mini-stream.
    /// </summary>
    RootStorage = 5,
}
