// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstNodeInfo.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Pst;

/// <summary>
/// Provides an immutable snapshot of a node's directory facts: its identifier, its parent, the size of its data
/// payload, and whether it carries a subnode tree.
/// </summary>
/// <param name="NodeId">The node identifier.</param>
/// <param name="ParentNodeId">
/// The parent node identifier the node database records, or the zero identifier when none is recorded. For a subnode,
/// this is the owning node's identifier.
/// </param>
/// <param name="DataLength">The total data payload length in bytes; <c>0</c> when the node carries no data.</param>
/// <param name="HasSubnodes">Whether the node carries a subnode tree.</param>
public sealed record PstNodeInfo(
    PstNodeId NodeId,
    PstNodeId ParentNodeId,
    long DataLength,
    bool HasSubnodes);
