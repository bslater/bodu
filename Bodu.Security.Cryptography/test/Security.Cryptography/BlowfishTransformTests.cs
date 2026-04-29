// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlowfishTransformTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Concrete test class that exercises the <see cref="BlockCipherTransformTests{TTest, TCryptoTransform}" /> base tests
/// against the <see cref="BlowfishTransform" /> implementation.
/// </summary>
[TestClass]
internal sealed class BlowfishTransformTests
    : BlockCipherTransformTests<BlowfishTransformTests, BlowfishTransform>
{
    /// <inheritdoc />
    protected override BlowfishTransform CreateAlgorithm()
    {
        var algorithm = new Blowfish();
        algorithm.GenerateKey();
        algorithm.GenerateIV();
        return (BlowfishTransform)algorithm.CreateEncryptor(algorithm.Key, algorithm.IV);
    }

    /// <inheritdoc />
    protected override IEnumerable<BlockCipherKnownAnswer> GetKnownAnswers() =>
        BlowfishKnownAnswers.For(SingleTestVariant.Default);

    /// <inheritdoc />
    protected override BlowfishTransform CreateTransformForKnownAnswer(BlockCipherKnownAnswer answer, bool forEncryption)
    {
        var algorithm = new Blowfish
        {
            Mode = CipherMode.ECB,
            Padding = PaddingMode.None,
        };
        algorithm.Key = answer.Key!;
        algorithm.IV = new byte[algorithm.BlockSize / 8];

        ICryptoTransform transform = forEncryption
            ? algorithm.CreateEncryptor()
            : algorithm.CreateDecryptor();
        return (BlowfishTransform)transform;
    }
}
