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
/// This enumeration is superseded by the BCL <see cref="FileMode" /> and <see cref="FileAccess" /> types accepted by
/// <see cref="CompoundFile.Open(System.IO.Stream, FileMode, FileAccess, bool, bool)" />, in line with
/// <c>System.IO.Packaging.Package.Open</c>. It is retained only for source compatibility.
/// </remarks>
[Obsolete("Use System.IO.FileMode and System.IO.FileAccess with CompoundFile.Open(Stream, FileMode, FileAccess, ...).")]
public enum CompoundFileMode
{
    /// <summary>
    /// Opens the compound file for read-only access. The directory and stream contents cannot be modified.
    /// </summary>
    Read = 0,
}
