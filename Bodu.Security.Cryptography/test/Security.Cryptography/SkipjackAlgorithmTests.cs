// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SkipjackAlgorithmTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

[TestClass]
public sealed partial class SkipjackAlgorithmTests
    : SymmetricAlgorithmTests<SkipjackAlgorithmTests, Skipjack>
{
    /// <inheritdoc />
    protected override Skipjack CreateAlgorithm() => new Skipjack();

    /// <inheritdoc />
    protected override void SetEcbMode(Skipjack algorithm) =>
        algorithm.BlockMode = CipherBlockMode.ECB;

    /// <inheritdoc />
    protected override SymmetricAlgorithmSpecification GetSpecification() =>
        new SymmetricAlgorithmSpecification
        {
            BlockSizeBits = 64,
            DefaultKeySizeBits = 80,
            LegalKeySizesBits = [80],
        };

    /// <inheritdoc />
    protected override IEnumerable<BlockCipherKnownAnswer> GetKnownAnswers() =>
        SkipjackKnownAnswers.For(SingleTestVariant.Default);

    /// <inheritdoc />
    protected override Skipjack CreateAlgorithmForKnownAnswer(BlockCipherKnownAnswer answer)
    {
        var algorithm = new Skipjack
        {
            Mode = CipherMode.ECB,
            Padding = PaddingMode.None,
        };
        algorithm.Key = answer.Key!;
        algorithm.IV = new byte[algorithm.BlockSize / 8];
        return algorithm;
    }
}
