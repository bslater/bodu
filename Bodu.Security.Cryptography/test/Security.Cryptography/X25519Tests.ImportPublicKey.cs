// ---------------------------------------------------------------------------------------------------------------
// <copyright file="X25519Tests.ImportPublicKey.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains unit tests for <see cref="X25519.ImportPublicKey" />.
/// </summary>
public partial class X25519Tests
{
    /// <summary>
    /// Verifies that <see cref="X25519.ImportPublicKey" /> produces a public-only instance whose key round-trips
    /// through <see cref="X25519.ExportPublicKey" />.
    /// </summary>
    [TestMethod]
    public void ImportPublicKey_WhenGivenValidKey_ShouldCreatePublicOnlyInstance()
    {
        var publicKey = Convert.FromHexString("de9edb7d7b7dc1b4d35b61c2ece435373f8343c85b78674dadfc7e146f882b4f");

        using var algorithm = new X25519();
        algorithm.ImportPublicKey(publicKey);

        Assert.IsFalse(algorithm.HasPrivateKey);
        Assert.IsTrue(algorithm.HasPublicKey);
        CollectionAssert.AreEqual(publicKey, algorithm.ExportPublicKey());
    }

    /// <summary>
    /// Verifies that <see cref="X25519.ImportPublicKey" /> discards any private key previously held by the instance.
    /// </summary>
    [TestMethod]
    public void ImportPublicKey_WhenInstanceHeldPrivateKey_ShouldDiscardPrivateKey()
    {
        using var algorithm = new X25519();
        algorithm.GenerateKey();

        algorithm.ImportPublicKey(new byte[X25519.KeySizeInBytes]);

        Assert.IsFalse(algorithm.HasPrivateKey);
    }

    /// <summary>
    /// Verifies that <see cref="X25519.ImportPublicKey" /> throws <see cref="ArgumentException" /> for key material
    /// that is not exactly 32 bytes.
    /// </summary>
    [TestMethod]
    [DataRow("31 bytes", 31)]
    [DataRow("33 bytes", 33)]
    public void ImportPublicKey_WhenLengthIsInvalid_ShouldThrowArgumentException(string testName, int length)
    {
        _ = testName;

        using var algorithm = new X25519();

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            algorithm.ImportPublicKey(new byte[length]);
        });

        Assert.AreEqual("publicKey", ex.ParamName);
    }
}
