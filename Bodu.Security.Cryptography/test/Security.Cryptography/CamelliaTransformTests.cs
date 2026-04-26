// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CamelliaTransformTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Exercises the <see cref="CamelliaTransform" /> implementation against the shared
/// <see cref="BlockCipherTransformTests{TCryptoTransform}" /> suite.
/// </summary>
[TestClass]
internal sealed class CamelliaTransformTests
    : BlockCipherTransformTests<CamelliaTransform>
{
    /// <inheritdoc />
    protected override CamelliaTransform CreateAlgorithm()
    {
        var algorithm = new Camellia();
        algorithm.GenerateKey();
        algorithm.GenerateIV();
        return (CamelliaTransform)algorithm.CreateEncryptor(algorithm.Key, algorithm.IV);
    }
}
