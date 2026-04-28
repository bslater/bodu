// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Serpent128TransformTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Concrete test class that exercises the <see cref="BlockCipherTransformTests{TTest, TCryptoTransform}" /> base tests against the
/// canonical <see cref="Serpent128Transform" /> implementation.
/// </summary>
[TestClass]
internal sealed class Serpent128TransformTests
    : BlockCipherTransformTests<Serpent128TransformTests, Serpent128Transform>
{
    /// <inheritdoc />
    protected override Serpent128Transform CreateAlgorithm()
    {
        var algorithm = new Serpent128();
        algorithm.GenerateKey();
        algorithm.GenerateIV();
        return (Serpent128Transform)algorithm.CreateEncryptor(algorithm.Key, algorithm.IV);
    }
}
