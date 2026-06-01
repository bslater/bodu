// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CryptoHelpersTests.GetRandomNonZeroBytes.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class CryptoHelpersTests
{
    /// <summary>
    /// Verifies that <see cref="CryptographyHelper.GetRandomNonZeroBytes" /> throws ArgumentOutOfRangeException when length is 0.
    /// </summary>
    [TestMethod]
    public void GetRandomNonZeroBytes_WhenLengthIsLessThanOrEqualToZero_ShouldThrowExactly() => Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => CryptographyHelper.GetRandomNonZeroBytes(0));

    /// <summary>
    /// Verifies that <see cref="CryptographyHelper.GetRandomNonZeroBytes" /> returns a buffer with only non-zero bytes.
    /// </summary>
    [TestMethod]
    public void GetRandomNonZeroBytes_WhenValidLength_ShouldReturnArrayWithOnlyNonZeroBytes()
    {
        var result = CryptographyHelper.GetRandomNonZeroBytes(32);
        Assert.AreEqual(32, result.Length);
        CollectionAssert.DoesNotContain(result, (byte)0);
    }
}
