// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstNbtEntry.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Pst.Internal;

/// <summary>
/// Represents a leaf entry of the node B-tree (<c>NBTENTRY</c>): a node with its data and subnode block identifiers and
/// its recorded parent.
/// </summary>
/// <param name="NodeId">The node identifier.</param>
/// <param name="DataBlockId">
/// The identifier of the node's data block or data tree; <c>0</c> when the node has no data.
/// </param>
/// <param name="SubnodeBlockId">The identifier of the node's subnode tree block; <c>0</c> when none.</param>
/// <param name="ParentNodeId">The recorded parent node identifier; <c>0</c> when none is recorded.</param>
internal readonly record struct PstNbtEntry(
    uint NodeId,
    ulong DataBlockId,
    ulong SubnodeBlockId,
    uint ParentNodeId);
