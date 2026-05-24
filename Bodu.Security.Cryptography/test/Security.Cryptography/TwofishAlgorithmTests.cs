// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TwofishAlgorithmTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Exercises the <see cref="Twofish" /> algorithm class against the shared <see cref="SymmetricAlgorithmTests{TTest, TAlgorithm}" />
/// suite, verifying constructor defaults, key and IV generation, encryptor and decryptor creation, and disposal behaviour.
/// </summary>
[TestClass]
public sealed partial class TwofishAlgorithmTests
    : SymmetricAlgorithmTests<TwofishAlgorithmTests, Twofish>
{
    /// <inheritdoc />
    protected override Twofish CreateAlgorithm() => Twofish.Create();

    /// <inheritdoc />
    protected override void SetBlockMode(Twofish algorithm, CipherModeKind mode) =>
        algorithm.BlockMode = mode;

    /// <inheritdoc />
    protected override SymmetricAlgorithmSpecification GetSpecification() =>
        new SymmetricAlgorithmSpecification
        {
            BlockSizeBits = 128,
            DefaultKeySizeBits = 256,
            LegalKeySizesBits = [128, 192, 256],
        };

    /// <inheritdoc />
    /// <remarks>
    /// Flattens the per-key-size Twofish AES-submission ECB intermediate-values vectors across
    /// <see cref="BlockCipherKeyVariant.Key128" />, <see cref="BlockCipherKeyVariant.Key192" />, and
    /// <see cref="BlockCipherKeyVariant.Key256" /> so the full 18-vector corpus runs through the
    /// Algorithm-layer harness in turn.
    /// </remarks>
    protected override IEnumerable<BlockCipherKnownAnswer> GetKnownAnswers() =>
        Enum.GetValues<BlockCipherKeyVariant>().SelectMany(TwofishKnownAnswers.For);

    /// <inheritdoc />
    protected override Twofish CreateAlgorithmForKnownAnswer(BlockCipherKnownAnswer answer)
    {
        var algorithm = Twofish.Create();
        algorithm.Mode = CipherMode.ECB;
        algorithm.Padding = PaddingMode.None;
        algorithm.Key = answer.Key!;
        algorithm.IV = new byte[algorithm.BlockSize / 8];
        return algorithm;
    }
}
