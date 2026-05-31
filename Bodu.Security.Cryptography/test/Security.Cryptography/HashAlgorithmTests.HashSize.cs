// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HashAlgorithmTests.HashSize.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public abstract partial class HashAlgorithmTests<TTest, TAlgorithm, TVariant>
{
    /// <summary>
    /// Verifies that the declared <see cref="HashAlgorithm.HashSize" /> matches the bit length of the computed hash.
    /// </summary>
    [TestMethod]
    public void HashSize_Get_DeclaredHashSize_ShouldMatchComputedHashLength()
    {
        using TAlgorithm algorithm = CreateAlgorithm();
        var result = algorithm.ComputeHash(Array.Empty<byte>());
        var computedBitLength = result.ToBitLength();
        Assert.AreEqual(computedBitLength, algorithm.HashSize);
    }
}
