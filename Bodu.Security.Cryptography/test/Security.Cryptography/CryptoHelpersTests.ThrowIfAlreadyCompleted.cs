// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CryptoHelpersTests.ThrowIfAlreadyCompleted.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class CryptoHelpersTests
{
    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowIfAlreadyCompleted(bool)"/> throws an
    /// <see cref="InvalidOperationException"/> when the transform is reported as completed.
    /// </summary>
    [TestMethod]
    public void ThrowIfAlreadyCompleted_WhenCompleted_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            CryptoHelpers.ThrowIfAlreadyCompleted(true);
        });
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowIfAlreadyCompleted(bool)"/> does not throw when the
    /// transform is not yet completed.
    /// </summary>
    [TestMethod]
    public void ThrowIfAlreadyCompleted_WhenNotCompleted_ShouldNotThrow()
    {
        CryptoHelpers.ThrowIfAlreadyCompleted(false);
    }
}
