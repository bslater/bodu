// ---------------------------------------------------------------------------------------------------------------
// <copyright file="X25519Tests.GenerateKey.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains unit tests for <see cref="X25519.GenerateKey" />.
/// </summary>
public partial class X25519Tests
{
    /// <summary>
    /// Verifies that <see cref="X25519.GenerateKey" /> populates both the private and public key.
    /// </summary>
    [TestMethod]
    public void GenerateKey_WhenCalled_ShouldPopulatePrivateAndPublicKey()
    {
        using var algorithm = new X25519();

        algorithm.GenerateKey();

        Assert.IsTrue(algorithm.HasPrivateKey);
        Assert.IsTrue(algorithm.HasPublicKey);
        Assert.AreEqual(X25519.KeySizeInBytes, algorithm.ExportPrivateKey().Length);
        Assert.AreEqual(X25519.KeySizeInBytes, algorithm.ExportPublicKey().Length);
    }

    /// <summary>
    /// Verifies that two calls to <see cref="X25519.GenerateKey" /> produce different key pairs.
    /// </summary>
    [TestMethod]
    public void GenerateKey_WhenCalledTwice_ShouldProduceDifferentKeyPairs()
    {
        using var algorithm = new X25519();

        algorithm.GenerateKey();
        var firstPrivate = algorithm.ExportPrivateKey();

        algorithm.GenerateKey();
        var secondPrivate = algorithm.ExportPrivateKey();

        CollectionAssert.AreNotEqual(firstPrivate, secondPrivate);
    }

    /// <summary>
    /// Verifies that the public key cached by <see cref="X25519.GenerateKey" /> matches an independent base-point
    /// derivation from the exported private key.
    /// </summary>
    [TestMethod]
    public void GenerateKey_WhenCalled_ShouldCachePublicKeyConsistentWithPrivateKey()
    {
        using var algorithm = new X25519();
        algorithm.GenerateKey();

        var expectedPublic = new byte[X25519.KeySizeInBytes];
        Curve25519.ScalarMultBase(algorithm.ExportPrivateKey(), expectedPublic);

        CollectionAssert.AreEqual(expectedPublic, algorithm.ExportPublicKey());
    }
}
