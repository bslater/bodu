// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlowfishTransformTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Concrete test class that exercises the <see cref="BlockCipherTransformTests{TCryptoTransform}" /> base tests
/// against the <see cref="TwoFishTransform" /> implementation.
/// </summary>
[TestClass]
internal sealed class TwoFishTransformTests
    : BlockCipherTransformTests<TwoFishTransform>
{
    /// <inheritdoc />
    protected override TwoFishTransform CreateAlgorithm()
    {
        var algorithm = new TwoFish();
        algorithm.GenerateKey();
        algorithm.GenerateIV();
        return (TwoFishTransform)algorithm.CreateEncryptor(algorithm.Key, algorithm.IV);
    }
}