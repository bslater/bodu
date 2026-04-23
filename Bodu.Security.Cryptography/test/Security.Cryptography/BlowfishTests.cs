// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlowfishTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

[TestClass]
public sealed partial class BlowfishTests
    : SymmetricAlgorithmTests<Blowfish>
{
    protected override Blowfish CreateAlgorithm() => Blowfish.Create();
}
