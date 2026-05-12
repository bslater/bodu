// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Threefish1024TweakableAlgorithmTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Exercises the <see cref="TweakableSymmetricAlgorithmTests{TTest, TAlgorithm}" /> base test suite against
/// <see cref="Threefish1024" /> — validating tweak property behaviour, defensive copies, invalid-size handling,
/// disposal semantics, and the curated <see cref="Threefish1024KnownAnswers" /> data set at the algorithm tier.
/// </summary>
[TestClass]
public sealed partial class Threefish1024TweakableAlgorithmTests
    : TweakableSymmetricAlgorithmTests<Threefish1024TweakableAlgorithmTests, Threefish1024>
{
    /// <inheritdoc />
    protected override Threefish1024 CreateAlgorithm() => Threefish1024.Create();

    /// <inheritdoc />
    protected override void SetBlockMode(Threefish1024 algorithm, CipherBlockMode mode) =>
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

    /// <inheritdoc />
    /// <remarks>
    /// Flattens both <see cref="TweakableBlockCipherVariant" /> values for the curated Threefish-1024 KAT data set
    /// so each variant runs through the algorithm-tier <c>CreateEncryptor</c> / <c>CreateDecryptor</c> pipeline.
    /// </remarks>
    protected override IEnumerable<BlockCipherKnownAnswer> GetKnownAnswers() =>
        Enum.GetValues<TweakableBlockCipherVariant>().SelectMany(Threefish1024KnownAnswers.For);

    /// <inheritdoc />
    protected override Threefish1024 CreateAlgorithmForKnownAnswer(BlockCipherKnownAnswer answer)
    {
        var algorithm = (Threefish1024)Threefish1024.Create();
        algorithm.Mode = CipherMode.ECB;
        algorithm.Padding = PaddingMode.None;
        algorithm.Key = answer.Key!;
        algorithm.IV = new byte[algorithm.BlockSize / 8];
        algorithm.Tweak = answer.Tweak!;
        return algorithm;
    }
}
