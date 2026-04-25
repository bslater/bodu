// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TwofishTransformTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Concrete test class that exercises the <see cref="BlockCipherTransformTests{TCryptoTransform}" /> base tests
/// against the <see cref="TwofishTransform" /> implementation.
/// </summary>
[TestClass]
internal sealed class TwofishTransformTests
    : BlockCipherTransformTests<TwofishTransform>
{
    /// <inheritdoc />
    protected override TwofishTransform CreateAlgorithm()
    {
        var algorithm = new Twofish();
        algorithm.GenerateKey();
        algorithm.GenerateIV();
        return (TwofishTransform)algorithm.CreateEncryptor(algorithm.Key, algorithm.IV);
    }
}