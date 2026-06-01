// ---------------------------------------------------------------------------------------------------------------
// <copyright file="KnownAnswerTest.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

/// <summary>
/// Represents a named known-answer test case for a non-cryptographic hash algorithm — an input payload paired
/// with its expected digest.
/// </summary>
/// <remarks>
/// Instances are yielded from
/// <see cref="NonCryptographicHashAlgorithmTests{TTest, TAlgorithm, TVariant}.GetTestVectors" /> and consumed by
/// data-driven verification methods that assert the algorithm reproduces the expected output for each named
/// shared input.
/// </remarks>
public sealed class KnownAnswerTest
{

    /// <summary>Gets the expected digest for this known-answer test case.</summary>
    public byte[] ExpectedOutput { get; init; } = [];

    /// <summary>Gets the input message to be hashed.</summary>
    public byte[] Input { get; init; } = [];

    /// <summary>Gets the name of the test case (for example, <c>Empty</c>, <c>ABC</c>, <c>Zeros_16</c>).</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Gets the optional auxiliary parameters associated with this test case.</summary>
    public IDictionary<string, object> Parameters { get; init; } = new Dictionary<string, object>();

}
