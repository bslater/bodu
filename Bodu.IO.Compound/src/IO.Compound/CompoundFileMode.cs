// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundFileMode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound;

/// <summary>
/// Specifies how a <see cref="CompoundFile" /> is opened, controlling whether the directory and streams may be
/// modified.
/// </summary>
/// <remarks>
/// <para>
/// Only <see cref="Read" /> is supported by the current release. The remaining members are reserved for a future
/// read-write implementation and are rejected by
/// <see cref="CompoundFile.Open(System.IO.Stream, CompoundFileMode, bool)" /> until that capability is delivered. They
/// are declared now so the access model is stable across releases.
/// </para>
/// </remarks>
public enum CompoundFileMode
{
    /// <summary>
    /// Opens the compound file for read-only access. The directory and stream contents cannot be modified.
    /// </summary>
    Read = 0,

    /// <summary>
    /// Reserved for opening an existing compound file for read and write access. Not yet supported.
    /// </summary>
    ReadWrite = 1,

    /// <summary>
    /// Reserved for creating a new compound file, overwriting any existing content. Not yet supported.
    /// </summary>
    Create = 2,

    /// <summary>
    /// Reserved for creating a new compound file and failing when one already exists. Not yet supported.
    /// </summary>
    CreateNew = 3,
}
