// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SkipjackAlgorithmTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
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
