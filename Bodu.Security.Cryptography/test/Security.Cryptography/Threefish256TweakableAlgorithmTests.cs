// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Threefish256TweakableAlgorithmTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Exercises the <see cref="TweakableSymmetricAlgorithmTests{TTest, TAlgorithm}" /> base test suite against
/// <see cref="Threefish256" /> — validating tweak property behaviour, defensive copies, invalid-size handling,
/// disposal semantics, and the curated <see cref="Threefish256KnownAnswers" /> data set at the algorithm tier.
/// </summary>
[TestClass]
public sealed partial class Threefish256TweakableAlgorithmTests
    : TweakableSymmetricAlgorithmTests<Threefish256TweakableAlgorithmTests, Threefish256>
{
    /// <inheritdoc />
    protected override Threefish256 CreateAlgorithm() => Threefish256.Create();

    /// <inheritdoc />
    protected override void SetBlockMode(Threefish256 algorithm, CipherBlockMode mode) =>
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

    /// <inheritdoc />
    /// <remarks>
    /// Flattens both <see cref="TweakableBlockCipherVariant" /> values for the curated Threefish-256 KAT data set
    /// so each variant runs through the algorithm-tier <c>CreateEncryptor</c> / <c>CreateDecryptor</c> pipeline.
    /// </remarks>
    protected override IEnumerable<BlockCipherKnownAnswer> GetKnownAnswers() =>
        Enum.GetValues<TweakableBlockCipherVariant>().SelectMany(Threefish256KnownAnswers.For);

    /// <inheritdoc />
    protected override Threefish256 CreateAlgorithmForKnownAnswer(BlockCipherKnownAnswer answer)
    {
        var algorithm = (Threefish256)Threefish256.Create();
        algorithm.Mode = CipherMode.ECB;
        algorithm.Padding = PaddingMode.None;
        algorithm.Key = answer.Key!;
        algorithm.IV = new byte[algorithm.BlockSize / 8];
        algorithm.Tweak = answer.Tweak!;
        return algorithm;
    }
}
