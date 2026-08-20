// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstBthHeader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Pst.Internal;

/// <summary>
/// Represents a parsed <c>BTHHEADER</c>: the geometry of a BTree-on-heap.
/// </summary>
/// <param name="KeySize">The key width in bytes (<c>cbKey</c>): 2, 4, 8, or 16.</param>
/// <param name="DataSize">The leaf-record data width in bytes (<c>cbEnt</c>).</param>
/// <param name="IndexLevels">The number of index levels above the leaves (<c>bIdxLevels</c>); zero for a leaf-only tree.</param>
/// <param name="RootHid">The <c>HID</c> of the root record item (<c>hidRoot</c>); zero when the tree is empty.</param>
internal readonly record struct PstBthHeader(
    byte KeySize,
    byte DataSize,
    byte IndexLevels,
    uint RootHid);
