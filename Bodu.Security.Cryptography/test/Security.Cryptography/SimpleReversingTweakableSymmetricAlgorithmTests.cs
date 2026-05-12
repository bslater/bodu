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
    protected override void SetBlockMode(SimpleReversingTweakableSymmetricAlgorithm algorithm, CipherBlockMode mode) =>
        algorithm.BlockMode = mode;

    /// <inheritdoc />
    protected override TweakableSymmetricAlgorithmSpecification GetSpecification() =>
        new TweakableSymmetricAlgorithmSpecification
        {
            BlockSizeBits = 128,
            DefaultKeySizeBits = 128,
            LegalKeySizesBits = Enumerable.Range(1, 256)
                .Select(i => i * 8)
                .ToArray(),
            DefaultTweakSizeBits = 128,
            LegalTweakSizesBits = [128, 192, 256, 448, 576, 1024, 1536, 2048],
        };
}
