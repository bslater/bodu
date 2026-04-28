// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SerpentTransformTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Concrete test class that exercises the <see cref="BlockCipherTransformTests{TTest, TCryptoTransform}" /> base tests against the
/// wide-block tweakable <see cref="SerpentTransform" /> implementation (using <see cref="Serpent256" /> as the backing
/// algorithm).
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
}
