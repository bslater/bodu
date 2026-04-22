// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SkipjackAlgorithmTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

﻿namespace Bodu.Security.Cryptography
{
    [TestClass]
    public sealed partial class SkipjackAlgorithmTests : SymmetricAlgorithmTests<Skipjack>
    {
        protected override Skipjack CreateAlgorithm() => new Skipjack();
    }
}
