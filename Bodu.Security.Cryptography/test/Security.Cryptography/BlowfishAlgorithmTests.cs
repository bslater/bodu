// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlowfishAlgorithmTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Exercises the <see cref="Blowfish" /> algorithm class against the shared <see cref="SymmetricAlgorithmTests{TTest, TAlgorithm}" />
/// suite, verifying constructor defaults, key and IV generation, encryptor and decryptor creation, and disposal behaviour.
/// </summary>
[TestClass]
public sealed partial class BlowfishAlgorithmTests
    : SymmetricAlgorithmTests<BlowfishAlgorithmTests, Blowfish>
{
    /// <inheritdoc />
    protected override Blowfish CreateAlgorithm() => Blowfish.Create();

    /// <inheritdoc />
    protected override void SetBlockMode(Blowfish algorithm, CipherModeKind mode) =>
        algorithm.BlockMode = mode;

    /// <inheritdoc />
    protected override SymmetricAlgorithmSpecification GetSpecification() =>
        new SymmetricAlgorithmSpecification
        {
            BlockSizeBits = 64,
            DefaultKeySizeBits = 128,
            LegalKeySizesBits = Enumerable.Range(4, 53)
                .Select(i => i * 8)
                .ToArray(),
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
