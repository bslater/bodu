// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SimpleReversingSymmetricAlgorithmTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

[TestClass]
public partial class SimpleReversingSymmetricAlgorithmTests
    : SymmetricAlgorithmTests<SimpleReversingSymmetricAlgorithmTests, SimpleReversingSymmetricAlgorithm>
{
    /// <inheritdoc />
    protected override SimpleReversingSymmetricAlgorithm CreateAlgorithm() => new SimpleReversingSymmetricAlgorithm();

    /// <inheritdoc />
    protected override void SetBlockMode(SimpleReversingSymmetricAlgorithm algorithm, CipherModeKind mode) =>
        algorithm.BlockMode = mode;

    /// <inheritdoc />
    protected override CipherModeKind GetBlockMode(SimpleReversingSymmetricAlgorithm algorithm) =>
        algorithm.BlockMode;

    /// <inheritdoc />
    protected override SymmetricAlgorithmSpecification GetSpecification() =>
        new SymmetricAlgorithmSpecification
        {
            BlockSizeBits = 128,
            DefaultKeySizeBits = 128,
            LegalKeySizesBits = Enumerable.Range(1, 256)
                .Select(i => i * 8)
                .ToArray(),
        };
}
