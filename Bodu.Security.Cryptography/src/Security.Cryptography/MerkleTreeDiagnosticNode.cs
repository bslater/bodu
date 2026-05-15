// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MerkleTreeDiagnosticNode.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Linq;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Represents a single node captured during a Merkle tree hash computation, recording the child
/// hashes used as input and the hash value produced as output.
/// </summary>
/// <param name="Level">
///   The zero-based level of this node within the tree. Level 0 contains leaf nodes derived
///   directly from input blocks; higher levels contain internal nodes derived by combining
///   groups of child hashes.
/// </param>
/// <param name="Index">The zero-based position of this node within its level.</param>
/// <param name="IsLeaf">
///   <see langword="true"/> if this node is a leaf; <see langword="false"/> if it is an
///   internal node. Leaf nodes have no child hashes.
/// </param>
/// <param name="Hash">The hash value computed for this node.</param>
/// <param name="ChildHashes">
///   The child hash values whose concatenation was hashed to produce <paramref name="Hash"/>.
///   Empty for leaf nodes.
/// </param>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "StyleCop.CSharp.NamingRules",
    "SA1313:Parameter names should begin with lower-case letter",
    Justification = "The positional record parameters intentionally use PascalCase because they define the generated public property names; using lower-case parameter names would produce lower-case public properties and violate .NET member naming conventions.")]
public sealed record MerkleTreeDiagnosticNode(
    int Level,
    int Index,
    bool IsLeaf,
    byte[] Hash,
    IReadOnlyList<byte[]> ChildHashes);
