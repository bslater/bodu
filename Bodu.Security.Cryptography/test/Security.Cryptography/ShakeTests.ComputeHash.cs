// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ShakeTests.ComputeHash.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class ShakeTests
{
    /// <summary>
    /// Verifies that SHAKE128 and SHAKE256 produce different digests for the same input and output length.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenSecurityLevelsDiffer_ShouldProduceDifferentHashes()
    {
        byte[] input = System.Text.Encoding.ASCII.GetBytes("test");

        using Shake shake128 = new(256, 128);
        using Shake shake256 = new(256, 256);

        byte[] hash128 = shake128.ComputeHash(input);
        byte[] hash256 = shake256.ComputeHash(input);

        CollectionAssert.AreNotEqual(hash128, hash256,
            "SHAKE128 and SHAKE256 must produce different digests for the same input.");
    }
}
