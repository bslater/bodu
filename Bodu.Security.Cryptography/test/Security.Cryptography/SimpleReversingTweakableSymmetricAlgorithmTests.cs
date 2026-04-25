// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SimpleReversingTweakableSymmetricAlgorithmTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

[TestClass]
public partial class SimpleReversingTweakableSymmetricAlgorithmTests
    : TweakableSymmetricAlgorithmTests<SimpleReversingTweakableSymmetricAlgorithmTests, SimpleReversingTweakableSymmetricAlgorithm>
{
    /// <inheritdoc />
    protected override SimpleReversingTweakableSymmetricAlgorithm CreateAlgorithm() => new SimpleReversingTweakableSymmetricAlgorithm();

    /// <inheritdoc />
    protected override SymmetricAlgorithmSpecification GetSpecification() =>
        new SymmetricAlgorithmSpecification
        {
            BlockSizeBits = 128,
            DefaultKeySizeBits = 128,
            LegalKeySizesBits = [8, 128, 256, 2048],
        };
}
