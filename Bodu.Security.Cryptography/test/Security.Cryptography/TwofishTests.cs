// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TwofishTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

[TestClass]
public sealed partial class TwofishTests
    : SymmetricAlgorithmTests<TwofishTests, Twofish>
{
    /// <inheritdoc />
    protected override Twofish CreateAlgorithm() => Twofish.Create();

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
        var algorithm = (Twofish)Twofish.Create();
        algorithm.Mode = CipherMode.ECB;
        algorithm.Padding = PaddingMode.None;
        algorithm.Key = answer.Key!;
        algorithm.IV = new byte[algorithm.BlockSize / 8];
        return algorithm;
    }
}
