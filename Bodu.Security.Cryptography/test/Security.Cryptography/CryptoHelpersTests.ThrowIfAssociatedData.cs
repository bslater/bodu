// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CryptoHelpersTests.ThrowIfAssociatedData.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public partial class CryptoHelpersTests
{
    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowIfAssociatedDataAlreadyProcessed(bool)"/> throws an
    /// <see cref="InvalidOperationException"/> when associated data has already been processed.
    /// </summary>
    [TestMethod]
    public void ThrowIfAssociatedDataAlreadyProcessed_WhenAlreadyProcessed_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            CryptoHelpers.ThrowIfAssociatedDataAlreadyProcessed(true);
        });
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowIfAssociatedDataAlreadyProcessed(bool)"/> does not throw
    /// when associated data has not yet been processed.
    /// </summary>
    [TestMethod]
    public void ThrowIfAssociatedDataAlreadyProcessed_WhenNotProcessed_ShouldNotThrow() => CryptoHelpers.ThrowIfAssociatedDataAlreadyProcessed(false);

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowIfAssociatedDataNotProcessed(bool)"/> throws a
    /// <see cref="CryptographicException"/> when associated data has not yet been processed.
    /// </summary>
    [TestMethod]
    public void ThrowIfAssociatedDataNotProcessed_WhenNotProcessed_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            CryptoHelpers.ThrowIfAssociatedDataNotProcessed(false);
        });
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowIfAssociatedDataNotProcessed(bool)"/> does not throw
    /// when associated data has been processed.
    /// </summary>
    [TestMethod]
    public void ThrowIfAssociatedDataNotProcessed_WhenProcessed_ShouldNotThrow() => CryptoHelpers.ThrowIfAssociatedDataNotProcessed(true);
}
