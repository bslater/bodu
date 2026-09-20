// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MerkleConsistencyKat.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Collections.Merkle;

/// <summary>
/// A published consistency proof: the two tree sizes it relates, and the expected proof steps.
/// </summary>
/// <param name="Name">The row's display name, surfaced on failure.</param>
/// <param name="FirstSize">The earlier tree's entry count.</param>
/// <param name="SecondSize">The later tree's entry count.</param>
/// <param name="Proof">The expected proof steps, lowercase hex.</param>
/// <remarks>
/// A bespoke record rather than <see cref="ValidKat{TInput,TExpected}" /> because the input is the pair of sizes,
/// which the generic's single <c>Input</c> cannot carry legibly.
/// </remarks>
public sealed record MerkleConsistencyKat(
    string Name,
    int FirstSize,
    int SecondSize,
    string[] Proof) : IKat;
