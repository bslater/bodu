// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TwoFishTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

[TestClass]
public sealed partial class TwoFishTests
    : SymmetricAlgorithmTests<TwoFish>
{
    protected override TwoFish CreateAlgorithm() => TwoFish.Create();
}