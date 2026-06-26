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
        CryptographicException ex = Assert.ThrowsExactly<CryptographicException>(() =>
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

    /// <summary>
    /// Verifies that <see cref="X25519.IsLowOrderPoint" /> flags every known low-order u-coordinate and that those same
    /// coordinates are rejected by <see cref="X25519.DeriveSharedSecret(ReadOnlySpan{byte})" />, cross-checking the
    /// screening table against the actual all-zero behaviour.
    /// </summary>
    [TestMethod]
    [DataRow("u = 0", "0000000000000000000000000000000000000000000000000000000000000000")]
    [DataRow("u = 1", "0100000000000000000000000000000000000000000000000000000000000000")]
    [DataRow("order 8 (a)", "e0eb7a7c3b41b8ae1656e3faf19fc46ada098deb9c32b1fd866205165f49b800")]
    [DataRow("order 8 (b)", "5f9c95bca3508c24b1d0b1559c83ef5b04445cc4581c8e86d8224eddd09f1157")]
    [DataRow("p - 1", "ecffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f")]
    [DataRow("p", "edffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f")]
    [DataRow("p + 1", "eeffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f")]
    public void IsLowOrderPoint_WhenPointIsLowOrder_ShouldReturnTrueAndDeriveRejects(string testName, string pointHex)
    {
        _ = testName;
        byte[] point = Convert.FromHexString(pointHex);

        Assert.IsTrue(X25519.IsLowOrderPoint(point));

        using var algorithm = new X25519();
        algorithm.GenerateKey();
        Assert.ThrowsExactly<CryptographicException>(() => { _ = algorithm.DeriveSharedSecret(point); });
    }

    /// <summary>
    /// Verifies that <see cref="X25519.IsLowOrderPoint" /> returns <see langword="false" /> for an ordinary generated
    /// public key.
    /// </summary>
    [TestMethod]
    public void IsLowOrderPoint_WhenPointIsValidPublicKey_ShouldReturnFalse()
    {
        using var algorithm = new X25519();
        algorithm.GenerateKey();

        Assert.IsFalse(X25519.IsLowOrderPoint(algorithm.ExportPublicKey()));
    }

    /// <summary>
    /// Verifies that <see cref="X25519.IsLowOrderPoint" /> detects a low-order coordinate even when the ignored high
    /// bit (bit 255) of its encoding is set, matching the RFC 7748 rule that bit 255 is not part of the value.
    /// </summary>
    [TestMethod]
    public void IsLowOrderPoint_WhenHighBitSetOnLowOrderPoint_ShouldStillReturnTrue()
    {
        // u = 1 with bit 255 set; RFC 7748 masks bit 255, so this still encodes the low-order point.
        byte[] point = Convert.FromHexString("0100000000000000000000000000000000000000000000000000000000000080");

        Assert.IsTrue(X25519.IsLowOrderPoint(point));
    }

    /// <summary>
    /// Verifies that the X25519 ladder ignores bit 255 of the peer u-coordinate, so a peer key with that bit set
    /// derives the same shared secret as the same key with it cleared (RFC 7748 §5).
    /// </summary>
    [TestMethod]
    public void DeriveSharedSecret_WhenPeerHighBitSet_ShouldMatchClearedHighBit()
    {
        using var algorithm = new X25519();
        algorithm.ImportPrivateKey(Convert.FromHexString("77076d0a7318a57d3c16c17251b26645df4c2f87ebc0992ab177fba51db92c2a"));

        byte[] peer = Convert.FromHexString("de9edb7d7b7dc1b4d35b61c2ece435373f8343c85b78674dadfc7e146f882b4f");
        byte[] peerHighBitSet = (byte[])peer.Clone();
        peerHighBitSet[31] |= 0x80;

        CollectionAssert.AreEqual(algorithm.DeriveSharedSecret(peer), algorithm.DeriveSharedSecret(peerHighBitSet));
    }
}
