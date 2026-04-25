// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TwofishTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

[TestClass]
public sealed partial class TwofishTests
    : SymmetricAlgorithmTests<Twofish>
{
    protected override Twofish CreateAlgorithm() => TwoFish.Create();
}