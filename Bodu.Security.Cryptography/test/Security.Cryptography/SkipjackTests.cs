// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SkipjackTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

[TestClass]
public sealed partial class SkipjackTests
    : SymmetricAlgorithmTests<SkipjackTests, Skipjack>
{
    /// <inheritdoc />
    protected override Skipjack CreateAlgorithm() => new Skipjack();

    /// <inheritdoc />
    protected override void SetBlockMode(Skipjack algorithm, CipherModeKind mode) =>
        algorithm.BlockMode = mode;

    /// <inheritdoc />
    protected override CipherModeKind GetBlockMode(Skipjack algorithm) =>
        algorithm.BlockMode;

    /// <inheritdoc />
    protected override SymmetricAlgorithmSpecification GetSpecification() =>
        new SymmetricAlgorithmSpecification
        {
            BlockSizeBits = 64,
            DefaultKeySizeBits = 80,
            LegalKeySizesBits = [80],
        };
}
