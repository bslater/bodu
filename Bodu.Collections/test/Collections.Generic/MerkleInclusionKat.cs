// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MerkleInclusionKat.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Collections.Generic;

/// <summary>
/// A published authentication path: the tree size and leaf index it belongs to, and the expected sibling hashes
/// leaf-upward.
/// </summary>
/// <param name="Name">The row's display name, surfaced on failure.</param>
/// <param name="TreeSize">The number of entries in the tree.</param>
/// <param name="LeafIndex">The zero-based index of the leaf the path proves.</param>
/// <param name="Path">The expected path steps, lowercase hex, leaf-upward.</param>
/// <remarks>
/// A bespoke record rather than <see cref="ValidKat{TInput,TExpected}" /> because the input is a pair — tree size and
/// leaf index — that the generic's single <c>Input</c> cannot carry without a tuple that reads worse than this.
/// </remarks>
public sealed record MerkleInclusionKat(
    string Name,
    int TreeSize,
    int LeafIndex,
    string[] Path) : IKat;
