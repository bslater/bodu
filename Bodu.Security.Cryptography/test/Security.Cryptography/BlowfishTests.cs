// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlowfishTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

[TestClass]
public sealed partial class BlowfishTests
    : SymmetricAlgorithmTests<BlowfishTests, Blowfish>
{
    /// <inheritdoc />
    protected override Blowfish CreateAlgorithm() => Blowfish.Create();

    /// <inheritdoc />
    protected override SymmetricAlgorithmSpecification GetSpecification() =>
        new SymmetricAlgorithmSpecification
        {
            BlockSizeBits = 64,
            DefaultKeySizeBits = 128,
            LegalKeySizesBits = [32, 128, 256, 448],
        };
}
