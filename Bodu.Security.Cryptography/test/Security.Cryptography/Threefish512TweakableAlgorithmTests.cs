// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Threefish512TweakableAlgorithmTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Exercises the <see cref="TweakableSymmetricAlgorithmTests{TTest, TAlgorithm}" /> base test suite against
/// <see cref="Threefish512" /> — validating tweak property behaviour, defensive copies, invalid-size handling,
/// disposal semantics, and the curated <see cref="Threefish512KnownAnswers" /> data set at the algorithm tier.
/// </summary>
[TestClass]
public sealed partial class Threefish512TweakableAlgorithmTests
    : TweakableSymmetricAlgorithmTests<Threefish512TweakableAlgorithmTests, Threefish512>
{
    /// <inheritdoc />
    protected override Threefish512 CreateAlgorithm() => Threefish512.Create();

    /// <inheritdoc />
    protected override void SetEcbMode(Threefish512 algorithm) =>
        algorithm.BlockMode = CipherBlockMode.ECB;

    /// <inheritdoc />
    protected override SymmetricAlgorithmSpecification GetSpecification() =>
        new SymmetricAlgorithmSpecification
        {
            BlockSizeBits = 512,
            DefaultKeySizeBits = 512,
            LegalKeySizesBits = [512],
        };

    /// <inheritdoc />
    /// <remarks>
    /// Flattens both <see cref="TweakableBlockCipherVariant" /> values for the curated Threefish-512 KAT data set
    /// so each variant runs through the algorithm-tier <c>CreateEncryptor</c> / <c>CreateDecryptor</c> pipeline.
    /// </remarks>
    protected override IEnumerable<BlockCipherKnownAnswer> GetKnownAnswers() =>
        Enum.GetValues<TweakableBlockCipherVariant>().SelectMany(Threefish512KnownAnswers.For);

    /// <inheritdoc />
    protected override Threefish512 CreateAlgorithmForKnownAnswer(BlockCipherKnownAnswer answer)
    {
        var algorithm = (Threefish512)Threefish512.Create();
        algorithm.Mode = CipherMode.ECB;
        algorithm.Padding = PaddingMode.None;
        algorithm.Key = answer.Key!;
        algorithm.IV = new byte[algorithm.BlockSize / 8];
        algorithm.Tweak = answer.Tweak!;
        return algorithm;
    }
}
