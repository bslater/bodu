// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SimpleReversingTweakableSymmetricAlgorithmTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

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
    protected override IEnumerable<BlockCipherKnownAnswer> GetKnownAnswers() =>
        SimpleReversingTweakableKnownAnswers.For(SingleTestVariant.Default);

    /// <inheritdoc />
    protected override SimpleReversingTweakableSymmetricAlgorithm CreateAlgorithmForKnownAnswer(BlockCipherKnownAnswer answer)
    {
        var algorithm = SimpleReversingTweakableSymmetricAlgorithm.Create();
        algorithm.BlockMode = CipherBlockMode.ECB;
        algorithm.Padding = PaddingMode.None;
        algorithm.Key = answer.Key!;
        algorithm.IV = new byte[algorithm.BlockSize / 8];
        algorithm.Tweak = answer.Tweak!;
        return algorithm;
    }

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
