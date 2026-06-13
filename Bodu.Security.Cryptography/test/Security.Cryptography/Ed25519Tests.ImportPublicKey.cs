// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Ed25519Tests.ImportPublicKey.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains unit tests for <see cref="Ed25519.ImportPublicKey" />.
/// </summary>
public partial class Ed25519Tests
{
    /// <summary>
    /// Verifies that <see cref="Ed25519.ImportPublicKey" /> produces a verify-only instance whose key round-trips
    /// through <see cref="Ed25519.ExportPublicKey" />.
    /// </summary>
    [TestMethod]
    public void ImportPublicKey_WhenGivenValidKey_ShouldCreateVerifyOnlyInstance()
    {
        var publicKey = Convert.FromHexString("d75a980182b10ab7d54bfed3c964073a0ee172f3daa62325af021a68f707511a");

        using var algorithm = new Ed25519();
        algorithm.ImportPublicKey(publicKey);

        Assert.IsFalse(algorithm.HasPrivateKey);
        Assert.IsTrue(algorithm.HasPublicKey);
        CollectionAssert.AreEqual(publicKey, algorithm.ExportPublicKey());
    }

    /// <summary>
    /// Verifies that <see cref="Ed25519.ImportPublicKey" /> discards any private key previously held by the
    /// instance.
    /// </summary>
    [TestMethod]
    public void ImportPublicKey_WhenInstanceHeldPrivateKey_ShouldDiscardPrivateKey()
    {
        using var algorithm = new Ed25519();
        algorithm.GenerateKey();

        algorithm.ImportPublicKey(Convert.FromHexString("d75a980182b10ab7d54bfed3c964073a0ee172f3daa62325af021a68f707511a"));

        Assert.IsFalse(algorithm.HasPrivateKey);
    }

    /// <summary>
    /// Verifies that <see cref="Ed25519.ImportPublicKey" /> throws <see cref="ArgumentException" /> for material
    /// that is the wrong length, not on the curve, or a non-canonical point encoding.
    /// </summary>
    [TestMethod]
    [DataRow("y=2 not on curve", "0200000000000000000000000000000000000000000000000000000000000000")]
    [DataRow("non-canonical y=p", "edffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f")]
    [DataRow("x=0 with sign bit set", "0100000000000000000000000000000000000000000000000000000000000080")]
    public void ImportPublicKey_WhenEncodingIsInvalid_ShouldThrowArgumentException(string testName, string encodedHex)
    {
        _ = testName;

        using var algorithm = new Ed25519();

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            algorithm.ImportPublicKey(Convert.FromHexString(encodedHex));
        });

        Assert.AreEqual("publicKey", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="Ed25519.ImportPublicKey" /> throws <see cref="ArgumentException" /> for key material
    /// that is not exactly 32 bytes.
    /// </summary>
    [TestMethod]
    public void ImportPublicKey_WhenLengthIsInvalid_ShouldThrowArgumentException()
    {
        using var algorithm = new Ed25519();

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            algorithm.ImportPublicKey(new byte[31]);
        });

        Assert.AreEqual("publicKey", ex.ParamName);
    }
}
