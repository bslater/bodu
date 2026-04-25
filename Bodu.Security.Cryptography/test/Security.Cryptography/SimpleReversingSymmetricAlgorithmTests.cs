// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SimpleReversingSymmetricAlgorithmTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

[TestClass]
public partial class SimpleReversingSymmetricAlgorithmTests
    : SymmetricAlgorithmTests<SimpleReversingSymmetricAlgorithmTests, SimpleReversingSymmetricAlgorithm>
{
    /// <inheritdoc />
    protected override SimpleReversingSymmetricAlgorithm CreateAlgorithm() => new SimpleReversingSymmetricAlgorithm();

    /// <inheritdoc />
    protected override SymmetricAlgorithmSpecification GetSpecification() =>
        new SymmetricAlgorithmSpecification
        {
            BlockSizeBits = 128,
            DefaultKeySizeBits = 128,
            LegalKeySizesBits = [8, 128, 256, 2048],
        };
}
