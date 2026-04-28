// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThreefishTransformTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Concrete test class that exercises the <see cref="BlockCipherTransformTests{TCryptoTransform}" /> base tests
/// against the <see cref="ThreefishTransform" /> implementation.
/// </summary>
[TestClass]
internal sealed class ThreefishTransformTests
    : BlockCipherTransformTests<ThreefishTransform>
{
    /// <inheritdoc />
    protected override ThreefishTransform CreateAlgorithm()
    {
        var algorithm = new Threefish256();
        algorithm.GenerateKey();
        algorithm.GenerateIV();
        algorithm.GenerateTweak();
        return (ThreefishTransform)algorithm.CreateEncryptor(algorithm.Key, algorithm.IV, algorithm.Tweak);
    }
}
