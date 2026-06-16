// ---------------------------------------------------------------------------------------------------------------
// <copyright file="X25519Tests.DeriveSharedSecret.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains X25519-specific tests for <see cref="X25519.DeriveSharedSecret(ReadOnlySpan{byte})" /> — the span
/// overload and the strict RFC 7748 §6.1 low-order rejection; the agreement, determinism, missing-key, and
/// peer-length contracts are inherited from the key-agreement base.
/// </summary>
public sealed partial class X25519Tests
{
    /// <summary>
    /// Verifies that the span-writing overload produces the same shared secret as the allocating overload and
    /// rejects destinations of any other length.
    /// </summary>
    [TestMethod]
    public void DeriveSharedSecret_WhenUsingSpanOverload_ShouldMatchAllocatingOverloadAndRejectWrongDestinationLengths()
    {
        using var algorithm = new X25519();
        algorithm.ImportPrivateKey(Convert.FromHexString("77076d0a7318a57d3c16c17251b26645df4c2f87ebc0992ab177fba51db92c2a"));
        byte[] peer = Convert.FromHexString("de9edb7d7b7dc1b4d35b61c2ece435373f8343c85b78674dadfc7e146f882b4f");

        byte[] allocating = algorithm.DeriveSharedSecret(peer);
        byte[] spanResult = new byte[X25519.SharedSecretSizeInBytes];
        algorithm.DeriveSharedSecret(peer, spanResult);
        CollectionAssert.AreEqual(allocating, spanResult);

        Assert.ThrowsExactly<ArgumentException>(() => { algorithm.DeriveSharedSecret(peer, new byte[31]); });
        Assert.ThrowsExactly<ArgumentException>(() => { algorithm.DeriveSharedSecret(peer, new byte[33]); });
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

        byte[] destination = new byte[X25519.SharedSecretSizeInBytes];
        Array.Fill(destination, (byte)0xAA);

        _ = Assert.ThrowsExactly<CryptographicException>(() =>
        {
            algorithm.DeriveSharedSecret(new byte[X25519.KeySizeInBytes], destination);
        });

        CollectionAssert.AreEqual(new byte[X25519.SharedSecretSizeInBytes], destination);
    }
}
