// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlowfishTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

[TestClass]
public sealed partial class BlowfishTests
    : SymmetricAlgorithmTests<BlowfishTests, Blowfish>
{
    /// <inheritdoc />
    protected override Blowfish CreateAlgorithm() => Blowfish.Create();

    /// <inheritdoc />
    protected override SymmetricAlgorithmSpecification GetSpecification() =>
        new SymmetricAlgorithmSpecification
        {
            BlockSizeBits = 64,
            DefaultKeySizeBits = 128,
            LegalKeySizesBits = [32, 128, 256, 448],
        };

    /// <inheritdoc />
    protected override IEnumerable<BlockCipherKnownAnswer> GetKnownAnswers() =>
        BlowfishKnownAnswers.For(SingleTestVariant.Default);

    /// <inheritdoc />
    protected override Blowfish CreateAlgorithmForKnownAnswer(BlockCipherKnownAnswer answer)
    {
        var algorithm = (Blowfish)Blowfish.Create();
        algorithm.Mode = CipherMode.ECB;
        algorithm.Padding = PaddingMode.None;
        algorithm.Key = answer.Key!;
        algorithm.IV = new byte[algorithm.BlockSize / 8];
        return algorithm;
    }
}
