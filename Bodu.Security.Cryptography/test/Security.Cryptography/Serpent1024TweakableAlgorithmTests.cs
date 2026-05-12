// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Serpent1024TweakableAlgorithmTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Exercises the <see cref="TweakableSymmetricAlgorithmTests{TTest, TAlgorithm}" /> base test suite against
/// <see cref="Serpent1024" /> — validating tweak property behaviour, defensive copies, invalid-size handling, and disposal
/// semantics for the 1024-bit wide-block Serpent variant.
/// </summary>
[TestClass]
public partial class Serpent1024TweakableAlgorithmTests
    : TweakableSymmetricAlgorithmTests<Serpent1024TweakableAlgorithmTests, Serpent1024>
{
    /// <inheritdoc />
    protected override Serpent1024 CreateAlgorithm() => new Serpent1024();

    /// <inheritdoc />
    protected override void SetBlockMode(Serpent1024 algorithm, CipherBlockMode mode) =>
        algorithm.BlockMode = mode;

    /// <inheritdoc />
    protected override TweakableSymmetricAlgorithmSpecification GetSpecification() =>
        new TweakableSymmetricAlgorithmSpecification
        {
            BlockSizeBits = 1024,
            DefaultKeySizeBits = 1024,
            LegalKeySizesBits = [1024],
            DefaultTweakSizeBits = 128,
            LegalTweakSizesBits = [128],
        };
}
