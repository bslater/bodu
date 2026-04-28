// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SkipjackTransformTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Concrete test class that exercises the <see cref="BlockCipherTransformTests{TCryptoTransform}" /> base tests
/// against the <see cref="SkipjackTransform" /> implementation.
/// </summary>
[TestClass]
internal sealed class SkipjackTransformTests
    : BlockCipherTransformTests<SkipjackTransform>
{
    /// <inheritdoc />
    protected override SkipjackTransform CreateAlgorithm()
    {
        var algorithm = new Skipjack();
        algorithm.GenerateKey();
        algorithm.GenerateIV();
        return (SkipjackTransform)algorithm.CreateEncryptor(algorithm.Key, algorithm.IV);
    }
}
