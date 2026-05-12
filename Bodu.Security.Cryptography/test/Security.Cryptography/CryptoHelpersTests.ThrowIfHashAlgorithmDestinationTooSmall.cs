// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CryptoHelpersTests.ThrowIfHashAlgorithmDestinationTooSmall.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public partial class CryptoHelpersTests
{
    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowIfHashAlgorithmDestinationTooSmall(bool)"/> does not throw
    /// when the supplied success flag is <see langword="true"/>.
    /// </summary>
    [TestMethod]
    public void ThrowIfHashAlgorithmDestinationTooSmall_WhenSuccess_ShouldNotThrow()
    {
        CryptoHelpers.ThrowIfHashAlgorithmDestinationTooSmall(true);
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowIfHashAlgorithmDestinationTooSmall(bool)"/> throws a
    /// <see cref="CryptographicException"/> when the supplied success flag is <see langword="false"/>.
    /// </summary>
    [TestMethod]
    public void ThrowIfHashAlgorithmDestinationTooSmall_WhenFailure_ShouldThrowCryptographicException()
    {
        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            CryptoHelpers.ThrowIfHashAlgorithmDestinationTooSmall(false);
        });
    }
}
