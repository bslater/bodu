// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SkipjackAlgorithmTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

[TestClass]
public sealed partial class SkipjackAlgorithmTests
    : SymmetricAlgorithmTests<SkipjackAlgorithmTests, Skipjack>
{
    /// <inheritdoc />
    protected override Skipjack CreateAlgorithm() => new Skipjack();

    /// <inheritdoc />
    protected override void SetBlockMode(Skipjack algorithm, CipherModeKind mode) =>
        algorithm.BlockMode = mode;

    /// <inheritdoc />
    protected override SymmetricAlgorithmSpecification GetSpecification() =>
        new SymmetricAlgorithmSpecification
        {
            BlockSizeBits = 64,
            DefaultKeySizeBits = 80,
            LegalKeySizesBits = [80],
        };
}
