// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SkipjackAlgorithmTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

[TestClass]
public sealed partial class SkipjackAlgorithmTests
    : SymmetricAlgorithmTests<SkipjackAlgorithmTests, Skipjack>
{
    /// <inheritdoc />
    protected override Skipjack CreateAlgorithm() => new Skipjack();

    /// <inheritdoc />
    protected override SymmetricAlgorithmSpecification GetSpecification() =>
        new SymmetricAlgorithmSpecification
        {
            BlockSizeBits = 64,
            DefaultKeySizeBits = 80,
            LegalKeySizesBits = [80],
        };
}
