// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SimpleReversingSymmetricAlgorithmTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

[TestClass]
public partial class SimpleReversingSymmetricAlgorithmTests
    : SymmetricAlgorithmTests<SimpleReversingSymmetricAlgorithmTests, SimpleReversingSymmetricAlgorithm>
{
    /// <inheritdoc />
    protected override SimpleReversingSymmetricAlgorithm CreateAlgorithm() => new SimpleReversingSymmetricAlgorithm();

    /// <inheritdoc />
    protected override void SetBlockMode(SimpleReversingSymmetricAlgorithm algorithm, CipherBlockMode mode) =>
        algorithm.BlockMode = mode;

    /// <inheritdoc />
    protected override IEnumerable<BlockCipherKnownAnswer> GetKnownAnswers() =>
        SimpleReversingKnownAnswers.For(SingleTestVariant.Default);


    /// <inheritdoc />
    protected override SimpleReversingSymmetricAlgorithm CreateAlgorithmForKnownAnswer(BlockCipherKnownAnswer answer)
    {
        var algorithm = SimpleReversingSymmetricAlgorithm.Create();
        algorithm.BlockMode = CipherBlockMode.ECB;
        algorithm.Padding = PaddingMode.None;
        algorithm.Key = answer.Key!;
        algorithm.IV = new byte[algorithm.BlockSize / 8];
        return algorithm;
    }

    /// <inheritdoc />
    protected override SymmetricAlgorithmSpecification GetSpecification() =>
        new SymmetricAlgorithmSpecification
        {
            BlockSizeBits = 128,
            DefaultKeySizeBits = 128,
            LegalKeySizesBits = Enumerable.Range(1, 256)
                .Select(i => i * 8)
                .ToArray(),
        };
}
