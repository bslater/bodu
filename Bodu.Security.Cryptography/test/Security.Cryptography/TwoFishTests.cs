// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TwofishTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

[TestClass]
public sealed partial class TwofishTests
    : SymmetricAlgorithmTests<TwofishTests, Twofish>
{
    /// <inheritdoc />
    protected override Twofish CreateAlgorithm() => Twofish.Create();

    /// <inheritdoc />
    protected override SymmetricAlgorithmSpecification GetSpecification() =>
        new SymmetricAlgorithmSpecification
        {
            BlockSizeBits = 128,
            DefaultKeySizeBits = 256,
            LegalKeySizesBits = [128, 192, 256],
        };
}
