// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CryptoHelpersTests.GetRandomBytesExcluding.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

﻿namespace Bodu.Security.Cryptography
{
    public partial class CryptoHelpersTests
    {
        /// <summary>
        /// Verifies that GetRandomBytesExcluding throws ArgumentOutOfRangeException when length is invalid.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void GetRandomBytesExcluding_WhenLengthIsLessThanOrEqualToZero_ShouldThrowExactly()
        {
            _ = CryptoHelpers.GetRandomBytesExcluding(0x01, 0);
        }

        /// <summary>
        /// Verifies that GetRandomBytesExcluding returns an array that does not contain the forbidden byte.
        /// </summary>
        [TestMethod]
        public void GetRandomBytesExcluding_WhenValidInput_ShouldReturnArrayWithoutForbiddenByte()
        {
            byte[] result = CryptoHelpers.GetRandomBytesExcluding(0xAA, 64);
            Assert.AreEqual(64, result.Length);
            CollectionAssert.DoesNotContain(result, (byte)0xAA);
        }
    }
}