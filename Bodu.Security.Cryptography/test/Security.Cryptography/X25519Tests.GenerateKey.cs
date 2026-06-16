// ---------------------------------------------------------------------------------------------------------------
// <copyright file="X25519Tests.GenerateKey.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains X25519-specific tests for <see cref="X25519.GenerateKey" />; the population and uniqueness contract is
/// inherited from the asymmetric base.
/// </summary>
public sealed partial class X25519Tests
{
    /// <summary>
    /// Verifies that the public key cached by <see cref="X25519.GenerateKey" /> matches an independent base-point
    /// derivation from the exported private key.
    /// </summary>
    [TestMethod]
    public void GenerateKey_WhenCalled_ShouldCachePublicKeyConsistentWithPrivateKey()
    {
        using var algorithm = new X25519();
        algorithm.GenerateKey();

        byte[] expectedPublic = new byte[X25519.KeySizeInBytes];
        Curve25519.ScalarMultBase(algorithm.ExportPrivateKey(), expectedPublic);

        CollectionAssert.AreEqual(expectedPublic, algorithm.ExportPublicKey());
    }
}
