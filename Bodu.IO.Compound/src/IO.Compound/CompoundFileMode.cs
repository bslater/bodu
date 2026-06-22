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
/// The current release supports read-only access only, so <see cref="Read" /> is the single defined member. Read-write
/// modes will be introduced alongside the write implementation rather than declared ahead of support, so that every
/// member of this enumeration corresponds to a capability the library actually provides.
/// </remarks>
public enum CompoundFileMode
{
    /// <summary>
    /// Opens the compound file for read-only access. The directory and stream contents cannot be modified.
    /// </summary>
    Read = 0,
}
