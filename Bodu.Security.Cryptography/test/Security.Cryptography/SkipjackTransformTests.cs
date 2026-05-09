// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SkipjackTransformTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Concrete test class that exercises the <see cref="BlockCipherTransformTests{TTest, TCryptoTransform}" /> base tests
/// against the <see cref="SkipjackTransform" /> implementation.
/// </summary>
[TestClass]
internal sealed class SkipjackTransformTests
    : BlockCipherTransformTests<SkipjackTransformTests, SkipjackTransform>
{
    /// <inheritdoc />
    protected override SkipjackTransform CreateAlgorithm()
    {
        var algorithm = new Skipjack();
        algorithm.GenerateKey();
        algorithm.GenerateIV();
        return (SkipjackTransform)algorithm.CreateEncryptor(algorithm.Key, algorithm.IV);
    }

    /// <inheritdoc />
    protected override IEnumerable<BlockCipherKnownAnswer> GetKnownAnswers() =>
        SkipjackKnownAnswers.For(SingleTestVariant.Default);

    /// <inheritdoc />
    protected override SkipjackTransform CreateTransformForKnownAnswer(BlockCipherKnownAnswer answer, bool forEncryption)
    {
        var algorithm = new Skipjack
        {
            Mode = CipherMode.ECB,
            Padding = PaddingMode.None,
        };
        algorithm.Key = answer.Key!;
        algorithm.IV = new byte[algorithm.BlockSize / 8];

        ICryptoTransform transform = forEncryption
            ? algorithm.CreateEncryptor()
            : algorithm.CreateDecryptor();
        return (SkipjackTransform)transform;
    }
}
