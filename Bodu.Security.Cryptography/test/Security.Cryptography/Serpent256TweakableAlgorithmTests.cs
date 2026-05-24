// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Serpent256TweakableAlgorithmTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Exercises the <see cref="TweakableSymmetricAlgorithmTests{TTest, TAlgorithm}" /> base test suite against
/// <see cref="Serpent256" /> — validating tweak property behaviour, defensive copies, invalid-size handling, and disposal
/// semantics for the 256-bit wide-block Serpent variant.
/// </summary>
[TestClass]
public partial class Serpent256TweakableAlgorithmTests
    : TweakableSymmetricAlgorithmTests<Serpent256TweakableAlgorithmTests, Serpent256>
{
    /// <inheritdoc />
    protected override Serpent256 CreateAlgorithm() => new Serpent256();

    /// <inheritdoc />
    protected override void SetBlockMode(Serpent256 algorithm, CipherModeKind mode) =>
        algorithm.BlockMode = mode;

    /// <inheritdoc />
    protected override TweakableSymmetricAlgorithmSpecification GetSpecification() =>
        new TweakableSymmetricAlgorithmSpecification
        {
            BlockSizeBits = 256,
            DefaultKeySizeBits = 256,
            LegalKeySizesBits = [256],
            DefaultTweakSizeBits = 128,
            LegalTweakSizesBits = [128],
        };
}
