// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CamelliaAlgorithmTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Exercises the <see cref="Camellia" /> algorithm class against the shared <see cref="SymmetricAlgorithmTests{TTest,TAlgorithm}" />
/// suite, verifying constructor defaults, key and IV generation, encryptor and decryptor creation, and disposal behaviour.
/// </summary>
[TestClass]
public sealed partial class CamelliaAlgorithmTests
    : SymmetricAlgorithmTests<CamelliaAlgorithmTests, Camellia>
{
    /// <inheritdoc />
    protected override Camellia CreateAlgorithm() => new Camellia();

    /// <inheritdoc />
    protected override void SetBlockMode(Camellia algorithm, CipherModeKind mode) =>
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
    /// Flattens the per-key-size RFC 3713 Appendix A vectors across <see cref="BlockCipherKeyVariant.Key128" />,
    /// <see cref="BlockCipherKeyVariant.Key192" />, and <see cref="BlockCipherKeyVariant.Key256" /> so each runs
    /// through the Algorithm-layer harness in turn.
    /// </remarks>
    protected override IEnumerable<BlockCipherKnownAnswer> GetKnownAnswers() =>
        Enum.GetValues<BlockCipherKeyVariant>().SelectMany(CamelliaKnownAnswers.For);

    /// <inheritdoc />
    protected override Camellia CreateAlgorithmForKnownAnswer(BlockCipherKnownAnswer answer)
    {
        var algorithm = new Camellia
        {
            Mode = CipherMode.ECB,
            Padding = PaddingMode.None,
        };
        algorithm.Key = answer.Key!;
        algorithm.IV = new byte[algorithm.BlockSize / 8];
        return algorithm;
    }
}
