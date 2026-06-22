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
    V3 = 3,

    /// <summary>
    /// Version 4, using 4096-byte sectors, suited to larger containers.
    /// </summary>
    V4 = 4,
}
