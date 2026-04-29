// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TwofishTransformTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Concrete test class that exercises the <see cref="BlockCipherTransformTests{TTest, TCryptoTransform}" /> base tests
/// against the <see cref="TwofishTransform" /> implementation.
/// </summary>
[TestClass]
internal sealed class TwofishTransformTests
    : BlockCipherTransformTests<TwofishTransformTests, TwofishTransform>
{
    /// <inheritdoc />
    protected override TwofishTransform CreateAlgorithm()
    {
        var algorithm = new Twofish();
        algorithm.GenerateKey();
        algorithm.GenerateIV();
        return (TwofishTransform)algorithm.CreateEncryptor(algorithm.Key, algorithm.IV);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Flattens the per-key-size Twofish AES-submission ECB intermediate-values vectors across
    /// <see cref="BlockCipherKeyVariant.Key128" />, <see cref="BlockCipherKeyVariant.Key192" />, and
    /// <see cref="BlockCipherKeyVariant.Key256" /> so the full 18-vector corpus runs through the
    /// Transform-layer harness in turn.
    /// </remarks>
    protected override IEnumerable<BlockCipherKnownAnswer> GetKnownAnswers() =>
        Enum.GetValues<BlockCipherKeyVariant>().SelectMany(TwofishKnownAnswers.For);

    /// <inheritdoc />
    protected override TwofishTransform CreateTransformForKnownAnswer(BlockCipherKnownAnswer answer, bool forEncryption)
    {
        var algorithm = new Twofish
        {
            Mode = CipherMode.ECB,
            Padding = PaddingMode.None,
        };
        algorithm.Key = answer.Key!;
        algorithm.IV = new byte[algorithm.BlockSize / 8];

        ICryptoTransform transform = forEncryption
            ? algorithm.CreateEncryptor()
            : algorithm.CreateDecryptor();
        return (TwofishTransform)transform;
    }
}