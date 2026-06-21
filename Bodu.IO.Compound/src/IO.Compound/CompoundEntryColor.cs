// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundEntryColor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound;

/// <summary>
/// Identifies the red-black tree node color recorded for a directory entry.
/// </summary>
/// <remarks>
/// Within a compound file, the children of each storage are arranged as a red-black tree keyed by name. The color flag
/// is a structural artifact of that tree; it is surfaced for diagnostic completeness and is not required to navigate
/// the directory.
/// </remarks>
public enum CompoundEntryColor : byte
{
    /// <summary>
    /// A red node in the directory's red-black tree.
    /// </summary>
    Red = 0,

    /// <summary>
    /// A black node in the directory's red-black tree.
    /// </summary>
    Black = 1,
}
