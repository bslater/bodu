// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Ed25519Tests.VerifyData.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains unit tests for <see cref="Ed25519.VerifyData(ReadOnlySpan{byte}, ReadOnlySpan{byte})" />, including
/// tamper detection and malformed-signature handling.
/// </summary>
public partial class Ed25519Tests
{
    /// <summary>
    /// Verifies that a signature produced by <see cref="Ed25519.SignData(ReadOnlySpan{byte})" /> verifies under the
    /// matching public key, including through a verify-only instance.
    /// </summary>
    [TestMethod]
    public void VerifyData_WhenSignatureIsGenuine_ShouldReturnTrue()
    {
        using var signer = new Ed25519();
        signer.GenerateKey();
        var message = new byte[] { 10, 20, 30 };
        var signature = signer.SignData(message);

        Assert.IsTrue(signer.VerifyData(message, signature));

        using var verifier = new Ed25519();
        verifier.ImportPublicKey(signer.ExportPublicKey());
        Assert.IsTrue(verifier.VerifyData(message, signature));
    }

    /// <summary>
    /// Verifies that flipping any single region of the signature or the message causes verification to return
    /// <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void VerifyData_WhenSignatureOrMessageIsTampered_ShouldReturnFalse()
    {
        using var algorithm = new Ed25519();
        algorithm.GenerateKey();
        var message = new byte[] { 1, 2, 3, 4 };
        var signature = algorithm.SignData(message);

        var tamperedR = (byte[])signature.Clone();
        tamperedR[0] ^= 0x01;
        Assert.IsFalse(algorithm.VerifyData(message, tamperedR));

        var tamperedS = (byte[])signature.Clone();
        tamperedS[40] ^= 0x01;
        Assert.IsFalse(algorithm.VerifyData(message, tamperedS));

        var tamperedMessage = (byte[])message.Clone();
        tamperedMessage[2] ^= 0x01;
        Assert.IsFalse(algorithm.VerifyData(tamperedMessage, signature));
    }

    /// <summary>
    /// Verifies that wrong-length signatures return <see langword="false" /> without throwing.
    /// </summary>
    [TestMethod]
    [DataRow("empty", 0)]
    [DataRow("63 bytes", 63)]
    [DataRow("65 bytes", 65)]
    public void VerifyData_WhenSignatureLengthIsInvalid_ShouldReturnFalse(string testName, int length)
    {
        _ = testName;

        using var algorithm = new Ed25519();
        algorithm.GenerateKey();

        Assert.IsFalse(algorithm.VerifyData(new byte[] { 1 }, new byte[length]));
    }

    /// <summary>
    /// Verifies that a signature whose S component has the group order L added — an otherwise-forgeable malleated
    /// twin — is rejected by the canonical-S check.
    /// </summary>
    [TestMethod]
    public void VerifyData_WhenSignatureSComponentIsMalleated_ShouldReturnFalse()
    {
        using var algorithm = new Ed25519();
        algorithm.GenerateKey();
        var message = new byte[] { 9, 9, 9 };
        var signature = algorithm.SignData(message);

        // S' = S + L: congruent modulo L, so it satisfies the verification equation arithmetically, but RFC 8032
        // requires S < L. Adding the little-endian order with manual carry produces the non-canonical twin.
        var order = Convert.FromHexString("edd3f55c1a631258d69cf7a2def9de1400000000000000000000000000000010");
        var malleated = (byte[])signature.Clone();
        var carry = 0;
        for (var i = 0; i < 32; i++)
        {
            var sum = malleated[32 + i] + order[i] + carry;
            malleated[32 + i] = (byte)sum;
            carry = sum >> 8;
        }

        Assert.IsFalse(algorithm.VerifyData(message, malleated));
    }

    /// <summary>
    /// Verifies that <see cref="Ed25519.VerifyData(ReadOnlySpan{byte}, ReadOnlySpan{byte})" /> throws
    /// <see cref="CryptographicException" /> when no public key is present.
    /// </summary>
    [TestMethod]
    public void VerifyData_WhenNoPublicKeyPresent_ShouldThrowCryptographicException()
    {
        using var algorithm = new Ed25519();

        var ex = Assert.ThrowsExactly<CryptographicException>(() =>
        {
            _ = algorithm.VerifyData(new byte[] { 1 }, new byte[Ed25519.SignatureSizeInBytes]);
        });

        Assert.IsNotNull(ex);
    }
}
