// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundFileVersion.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound;

/// <summary>
/// Specifies the compound-file format version, which determines the sector size used when writing.
/// </summary>
public enum CompoundFileVersion
{
    /// <summary>
    /// Version 3, using 512-byte sectors. This is the most widely compatible layout and the default.
    /// </summary>
    /// <remarks>
    /// [MS-CFB] 2.9 limits a version-3 file to 2 GB, so writing a tree whose container would be larger throws
    /// <see cref="CompoundFileSerializationException" /> before any byte is written.
    /// </remarks>
    V3 = 3,

    /// <summary>
    /// Version 4, using 4096-byte sectors, suited to larger containers.
    /// </summary>
    /// <remarks>
    /// A version-4 container that passes 2 GB keeps the range lock sector [MS-CFB] 2.8 reserves (sector 0x7FFFE, which
    /// covers file offsets 0x7FFFFF00-0x7FFFFFFF) free of data: the writer marks it ENDOFCHAIN in the FAT, writes it as
    /// zeros, and places the sector that would have taken it, and every sector after, one sector later, so no chain
    /// passes through it.
    /// </remarks>
    V4 = 4,
}
