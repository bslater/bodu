// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CryptoHelpersTests.ThrowIfHashAlgorithmDestinationTooSmall.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public partial class CryptoHelpersTests
{
    /// <summary>
    /// Verifies that <see cref="CryptographyThrowHelper.ThrowIfHashAlgorithmDestinationTooSmall(bool)"/> does not throw
    /// when the supplied success flag is <see langword="true"/>.
    /// </summary>
    [TestMethod]
    public void ThrowIfHashAlgorithmDestinationTooSmall_WhenSuccess_ShouldNotThrow() => CryptographyThrowHelper.ThrowIfHashAlgorithmDestinationTooSmall(true);

    /// <summary>
    /// Verifies that <see cref="CryptographyThrowHelper.ThrowIfHashAlgorithmDestinationTooSmall(bool)"/> throws a
    /// <see cref="CryptographicException"/> when the supplied success flag is <see langword="false"/>.
    /// </summary>
    [TestMethod]
    public void ThrowIfHashAlgorithmDestinationTooSmall_WhenFailure_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            CryptographyThrowHelper.ThrowIfHashAlgorithmDestinationTooSmall(false);
        });
    }
}
