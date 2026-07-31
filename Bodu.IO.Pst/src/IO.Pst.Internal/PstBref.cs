// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstBref.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Pst.Internal;

/// <summary>
/// Represents a block reference (<c>BREF</c>): a block identifier paired with its absolute file offset.
/// </summary>
/// <param name="BlockId">The block identifier; bit 1 marks an internal (metadata) block.</param>
/// <param name="Offset">The absolute byte offset of the block or page.</param>
internal readonly record struct PstBref(
    ulong BlockId,
    ulong Offset)
{
    /// <summary>
    /// Gets a value indicating whether the referenced block is internal (tree metadata, never encoded).
    /// </summary>
    /// <value><see langword="true" /> when bit 1 of the identifier is set.</value>
    internal bool IsInternal =>
        (BlockId & 0x2) != 0;
}
