// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstBbtEntry.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Pst.Internal;

/// <summary>
/// Represents a leaf entry of the block B-tree (<c>BBTENTRY</c>): a block's location, payload size, and reference
/// count.
/// </summary>
/// <param name="Bref">The block reference.</param>
/// <param name="Length">The payload size in bytes, excluding padding and the trailer.</param>
/// <param name="ReferenceCount">The number of references to the block.</param>
internal readonly record struct PstBbtEntry(
    PstBref Bref,
    ushort Length,
    ushort ReferenceCount);
