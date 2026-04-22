// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThreeFishCipherTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

﻿namespace Bodu.Security.Cryptography
{
    public enum ThreeFishCipherTestVariant
    {
        ZeroedKeyAndTweak,
        DefaultKeyAndTweak
    }

    internal abstract partial class ThreeFishCipherTests<TTest, TCipher>
        : BlockCipherTests<TTest, TCipher, ThreeFishCipherTestVariant>
        where TTest : ThreeFishCipherTests<TTest, TCipher>, new()
        where TCipher : ThreefishBlockCipher
    {
        public override IEnumerable<ThreeFishCipherTestVariant> GetBlockCipherVariants() =>
            Enum.GetValues<ThreeFishCipherTestVariant>().ToArray();

        /// <summary>
        /// Creates an initialised instance of the Threefish variant under test with a freshly
        /// generated key, IV, and tweak.
        /// </summary>
        protected abstract Threefish CreateInitialisedAlgorithm();
    }
}