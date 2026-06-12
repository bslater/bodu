// ---------------------------------------------------------------------------------------------------------------
// <copyright file="X25519Tests.ImportPrivateKey.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains unit tests for <see cref="X25519.ImportPrivateKey" />.
/// </summary>
public partial class X25519Tests
{
    /// <summary>
    /// Verifies that <see cref="X25519.ImportPrivateKey" /> stores the private key and derives the matching public
    /// key from the RFC 7748 §6.1 example.
    /// </summary>
    [TestMethod]
    public void ImportPrivateKey_WhenGivenRfc7748PrivateKey_ShouldDerivePublishedPublicKey()
    {
        var privateKey = Convert.FromHexString("77076d0a7318a57d3c16c17251b26645df4c2f87ebc0992ab177fba51db92c2a");
        var expectedPublic = Convert.FromHexString("8520f0098930a754748b7ddcb43ef75a0dbf3a0d26381af4eba4a98eaa9b4e6a");

        using var algorithm = new X25519();
        algorithm.ImportPrivateKey(privateKey);

        Assert.IsTrue(algorithm.HasPrivateKey);
        Assert.IsTrue(algorithm.HasPublicKey);
        CollectionAssert.AreEqual(expectedPublic, algorithm.ExportPublicKey());
    }

    /// <summary>
    /// Verifies that <see cref="X25519.ImportPrivateKey" /> preserves the imported scalar byte-for-byte rather than
    /// storing a clamped form.
    /// </summary>
    [TestMethod]
    public void ImportPrivateKey_WhenScalarIsUnclamped_ShouldRoundTripExactBytes()
    {
        // Bits 0-2 set and bit 254 clear: clamping would alter this value, so an exact round-trip proves the
        // stored form is the caller's original scalar.
        var privateKey = new byte[X25519.KeySizeInBytes];
        privateKey[0] = 0x07;
        privateKey[31] = 0x00;
        privateKey[15] = 0xAB;

        using var algorithm = new X25519();
        algorithm.ImportPrivateKey(privateKey);

        CollectionAssert.AreEqual(privateKey, algorithm.ExportPrivateKey());
    }

    /// <summary>
    /// Verifies that <see cref="X25519.ImportPrivateKey" /> throws <see cref="ArgumentException" /> for key material
    /// that is not exactly 32 bytes.
    /// </summary>
    [TestMethod]
    [DataRow("31 bytes", 31)]
    [DataRow("33 bytes", 33)]
    [DataRow("empty", 0)]
    public void ImportPrivateKey_WhenLengthIsInvalid_ShouldThrowArgumentException(string testName, int length)
    {
        _ = testName;

        using var algorithm = new X25519();

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            algorithm.ImportPrivateKey(new byte[length]);
        });

        Assert.AreEqual("privateKey", ex.ParamName);
    }
}
