// ---------------------------------------------------------------------------------------------------------------
// <copyright file="X25519Tests.ExportKeys.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains unit tests for <see cref="X25519.ExportPrivateKey" /> and <see cref="X25519.ExportPublicKey" />.
/// </summary>
public partial class X25519Tests
{
    /// <summary>
    /// Verifies that <see cref="X25519.ExportPrivateKey" /> throws <see cref="CryptographicException" /> when the
    /// instance holds no private key.
    /// </summary>
    [TestMethod]
    public void ExportPrivateKey_WhenNoPrivateKeyPresent_ShouldThrowCryptographicException()
    {
        using var algorithm = new X25519();

        var ex = Assert.ThrowsExactly<CryptographicException>(() =>
        {
            _ = algorithm.ExportPrivateKey();
        });

        Assert.IsNotNull(ex);
    }

    /// <summary>
    /// Verifies that <see cref="X25519.ExportPublicKey" /> throws <see cref="CryptographicException" /> when the
    /// instance holds no public key.
    /// </summary>
    [TestMethod]
    public void ExportPublicKey_WhenNoPublicKeyPresent_ShouldThrowCryptographicException()
    {
        using var algorithm = new X25519();

        var ex = Assert.ThrowsExactly<CryptographicException>(() =>
        {
            _ = algorithm.ExportPublicKey();
        });

        Assert.IsNotNull(ex);
    }

    /// <summary>
    /// Verifies that the array returned by <see cref="X25519.ExportPrivateKey" /> is a defensive copy whose mutation
    /// does not affect later exports.
    /// </summary>
    [TestMethod]
    public void ExportPrivateKey_WhenReturnedArrayIsMutated_ShouldNotAffectStoredKey()
    {
        using var algorithm = new X25519();
        algorithm.GenerateKey();

        var first = algorithm.ExportPrivateKey();
        var original = (byte[])first.Clone();
        first[0] ^= 0xFF;

        CollectionAssert.AreEqual(original, algorithm.ExportPrivateKey());
    }

    /// <summary>
    /// Verifies that the array returned by <see cref="X25519.ExportPublicKey" /> is a defensive copy whose mutation
    /// does not affect later exports.
    /// </summary>
    [TestMethod]
    public void ExportPublicKey_WhenReturnedArrayIsMutated_ShouldNotAffectStoredKey()
    {
        using var algorithm = new X25519();
        algorithm.GenerateKey();

        var first = algorithm.ExportPublicKey();
        var original = (byte[])first.Clone();
        first[0] ^= 0xFF;

        CollectionAssert.AreEqual(original, algorithm.ExportPublicKey());
    }
}
