// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SerpentTransformTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Concrete test class that exercises the <see cref="BlockCipherTransformTests{TTest, TCryptoTransform}" /> base tests against the
/// wide-block tweakable <see cref="SerpentTransform" /> implementation, distinct from the standardised 128-bit
/// <see cref="Serpent128Transform" /> covered by <see cref="Serpent128TransformTests" />. The wide-block engine has no published
/// reference vectors, so this test class only exercises the transform-tier behavioural surface (construction, raw-block
/// transform, disposal); KAT coverage is omitted by design and tracked separately — see issue #142.
/// </summary>
[TestClass]
internal sealed class SerpentTransformTests
    : BlockCipherTransformTests<SerpentTransformTests, SerpentTransform>
{
    /// <inheritdoc />
    protected override SerpentTransform CreateAlgorithm()
    {
        var algorithm = new Serpent256();
        algorithm.GenerateKey();
        algorithm.GenerateIV();
        algorithm.GenerateTweak();
        return (SerpentTransform)algorithm.CreateEncryptor(algorithm.Key, algorithm.IV, algorithm.Tweak);
    }

    /// <inheritdoc />
    protected override SymmetricAlgorithm CreateSymmetricAlgorithm(PaddingMode padding)
    {
        var algorithm = new Serpent256
        {
            Padding = padding,
            Mode = CipherMode.CBC,
        };
        algorithm.GenerateKey();
        algorithm.GenerateIV();
        algorithm.GenerateTweak();
        return algorithm;
    }
}
