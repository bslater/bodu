// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SimpleReversingSymmetricAlgorithmTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

[TestClass]
public partial class SimpleReversingSymmetricAlgorithmTests
    : SymmetricAlgorithmTests<SimpleReversingSymmetricAlgorithm>
{
    protected override SimpleReversingSymmetricAlgorithm CreateAlgorithm() => new SimpleReversingSymmetricAlgorithm();
}
