// ---------------------------------------------------------------------------------------------------------------
// <copyright file="X25519Tests.DeriveSharedSecret.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains unit tests for <see cref="X25519.DeriveSharedSecret(ReadOnlySpan{byte})" /> and its span-writing
/// overload.
/// </summary>
public partial class X25519Tests
{
    /// <summary>
    /// Verifies that two freshly generated parties derive the same shared secret from each other's public keys.
    /// </summary>
    [TestMethod]
    public void DeriveSharedSecret_WhenBothPartiesExchangeKeys_ShouldAgree()
    {
        using X25519 alice = X25519.Create();
        using X25519 bob = X25519.Create();
        alice.GenerateKey();
        bob.GenerateKey();

        var aliceShared = alice.DeriveSharedSecret(bob.ExportPublicKey());
        var bobShared = bob.DeriveSharedSecret(alice.ExportPublicKey());

        CollectionAssert.AreEqual(aliceShared, bobShared);
        Assert.AreEqual(X25519.SharedSecretSizeInBytes, aliceShared.Length);
    }

    /// <summary>
    /// Verifies that the span-writing overload produces the same shared secret as the allocating overload.
    /// </summary>
    [TestMethod]
    public void DeriveSharedSecret_WhenUsingSpanOverload_ShouldMatchAllocatingOverload()
    {
        using var algorithm = new X25519();
        algorithm.ImportPrivateKey(Convert.FromHexString("77076d0a7318a57d3c16c17251b26645df4c2f87ebc0992ab177fba51db92c2a"));
        var peer = Convert.FromHexString("de9edb7d7b7dc1b4d35b61c2ece435373f8343c85b78674dadfc7e146f882b4f");

        var allocating = algorithm.DeriveSharedSecret(peer);
        var spanResult = new byte[X25519.SharedSecretSizeInBytes];
        algorithm.DeriveSharedSecret(peer, spanResult);

        CollectionAssert.AreEqual(allocating, spanResult);
    }

    /// <summary>
    /// Verifies that <see cref="X25519.DeriveSharedSecret(ReadOnlySpan{byte})" /> throws
    /// <see cref="CryptographicException" /> when no private key is present.
    /// </summary>
    [TestMethod]
    public void DeriveSharedSecret_WhenNoPrivateKeyPresent_ShouldThrowCryptographicException()
    {
        using var algorithm = new X25519();
        algorithm.ImportPublicKey(new byte[X25519.KeySizeInBytes]);

        var ex = Assert.ThrowsExactly<CryptographicException>(() =>
        {
            _ = algorithm.DeriveSharedSecret(Convert.FromHexString("de9edb7d7b7dc1b4d35b61c2ece435373f8343c85b78674dadfc7e146f882b4f"));
        });

        Assert.IsNotNull(ex);
    }

    /// <summary>
    /// Verifies that <see cref="X25519.DeriveSharedSecret(ReadOnlySpan{byte})" /> throws
    /// <see cref="ArgumentException" /> when the peer public key is not exactly 32 bytes.
    /// </summary>
    [TestMethod]
    public void DeriveSharedSecret_WhenPeerKeyLengthIsInvalid_ShouldThrowArgumentException()
    {
        using var algorithm = new X25519();
        algorithm.GenerateKey();

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = algorithm.DeriveSharedSecret(new byte[16]);
        });

        Assert.AreEqual("peerPublicKey", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the span overload throws <see cref="ArgumentException" /> when the destination is not exactly
    /// 32 bytes.
    /// </summary>
    [TestMethod]
    public void DeriveSharedSecret_WhenDestinationLengthIsInvalid_ShouldThrowArgumentException()
    {
        using var algorithm = new X25519();
        algorithm.GenerateKey();
        var peer = Convert.FromHexString("de9edb7d7b7dc1b4d35b61c2ece435373f8343c85b78674dadfc7e146f882b4f");

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            algorithm.DeriveSharedSecret(peer, new byte[31]);
        });

        Assert.AreEqual("destination", ex.ParamName);
    }

    /// <summary>
    /// Verifies that deriving against a low-order peer point throws <see cref="CryptographicException" /> under the
    /// strict RFC 7748 §6.1 all-zero check.
    /// </summary>
    [TestMethod]
    public void DeriveSharedSecret_WhenPeerKeyIsLowOrderPoint_ShouldThrowCryptographicException()
    {
        using var algorithm = new X25519();
        algorithm.GenerateKey();

        // u = 0 is a low-order point: every scalar maps it to the all-zero shared secret.
        var ex = Assert.ThrowsExactly<CryptographicException>(() =>
        {
            _ = algorithm.DeriveSharedSecret(new byte[X25519.KeySizeInBytes]);
        });

        Assert.IsNotNull(ex);
    }

    /// <summary>
    /// Verifies that the span overload zeroes the destination before throwing for a low-order peer point, so the
    /// caller never observes attacker-predictable output.
    /// </summary>
    [TestMethod]
    public void DeriveSharedSecret_WhenPeerKeyIsLowOrderPoint_ShouldZeroDestinationBeforeThrowing()
    {
        using var algorithm = new X25519();
        algorithm.GenerateKey();

        var destination = new byte[X25519.SharedSecretSizeInBytes];
        Array.Fill(destination, (byte)0xAA);

        _ = Assert.ThrowsExactly<CryptographicException>(() =>
        {
            algorithm.DeriveSharedSecret(new byte[X25519.KeySizeInBytes], destination);
        });

        CollectionAssert.AreEqual(new byte[X25519.SharedSecretSizeInBytes], destination);
    }
}
