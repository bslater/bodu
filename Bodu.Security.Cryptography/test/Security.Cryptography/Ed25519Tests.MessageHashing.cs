// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Ed25519Tests.MessageHashing.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains the tests of how <see cref="Ed25519" /> hashes a message, across signing and verification: in one call
/// over a copy on the stack up to <see cref="Ed25519.StackHashMaximumMessageLength" /> bytes, and incrementally
/// beyond it.
/// </summary>
public sealed partial class Ed25519Tests
{
    /// <summary>
    /// The RFC 8032 §7.1 TEST 1 private seed, which the message-hashing tests sign under.
    /// </summary>
    private const string MessageHashingSeedHex = "9d61b19deffd5a60ba844af492ec2cc44449c5697b326919703bac031cae7f60";

    /// <summary>
    /// Verifies that signing messages of the lengths either side of
    /// <see cref="Ed25519.StackHashMaximumMessageLength" />, and well beyond it, gives the signature an RFC 8032 signer
    /// built from one-call hashes and the replaced 1.1.0 table gives, through both overloads.
    /// </summary>
    [TestMethod]
    public void SignData_WhenMessageLengthIsAroundTheStackHashLimit_ShouldMatchTheReferenceSigner()
    {
        byte[] seed = Convert.FromHexString(MessageHashingSeedHex);
        using var algorithm = new Ed25519();
        algorithm.ImportPrivateKey(seed);
        byte[] destination = new byte[Ed25519.SignatureSizeInBytes];

        foreach (int length in MessageLengthsAroundTheStackHashLimit())
        {
            byte[] message = MessagePattern(length);
            byte[] expected = SignWithReference(seed, message);

            CollectionAssert.AreEqual(expected, algorithm.SignData(message), $"length {length}");
            algorithm.SignData(message, destination);
            CollectionAssert.AreEqual(expected, destination, $"span overload, length {length}");
        }
    }

    /// <summary>
    /// Verifies that verification accepts the reference signer's signature over messages of the lengths either side of
    /// <see cref="Ed25519.StackHashMaximumMessageLength" />, and well beyond it, and rejects it once the message's last
    /// byte changes, so the whole message is hashed on both sides of the limit.
    /// </summary>
    [TestMethod]
    public void VerifyData_WhenMessageLengthIsAroundTheStackHashLimit_ShouldAcceptTheSignatureAndRejectAChangedLastByte()
    {
        byte[] seed = Convert.FromHexString(MessageHashingSeedHex);
        using var signer = new Ed25519();
        signer.ImportPrivateKey(seed);
        using var verifier = new Ed25519();
        verifier.ImportPublicKey(signer.ExportPublicKey());

        foreach (int length in MessageLengthsAroundTheStackHashLimit())
        {
            byte[] message = MessagePattern(length);
            byte[] signature = SignWithReference(seed, message);

            Assert.IsTrue(verifier.VerifyData(message, signature), $"length {length}");

            if (length > 0)
            {
                message[^1] ^= 0x01;
                Assert.IsFalse(verifier.VerifyData(message, signature), $"changed last byte, length {length}");
            }
        }
    }

    /// <summary>
    /// Verifies that signing a message of at most <see cref="Ed25519.StackHashMaximumMessageLength" /> bytes into a
    /// span allocates nothing on the managed heap.
    /// </summary>
    /// <param name="length">The message's length.</param>
    /// <remarks>
    /// Measured on the calling thread around the one call, so allocations made by tests running in parallel do not
    /// count. A first signature warms the path up.
    /// </remarks>
    [TestMethod]
    [DataRow(0)]
    [DataRow(64)]
    [DataRow(Ed25519.StackHashMaximumMessageLength)]
    public void SignData_WhenMessageFitsTheStackHash_ForSpanOverload_ShouldAllocateNothing(int length)
    {
        using var algorithm = new Ed25519();
        algorithm.ImportPrivateKey(Convert.FromHexString(MessageHashingSeedHex));
        byte[] message = MessagePattern(length);
        byte[] destination = new byte[Ed25519.SignatureSizeInBytes];
        algorithm.SignData(message, destination);

        long before = GC.GetAllocatedBytesForCurrentThread();
        algorithm.SignData(message, destination);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.AreEqual(0L, allocated, $"SignData allocated {allocated} bytes.");
    }

    /// <summary>
    /// Verifies that verifying a signature over a message of at most
    /// <see cref="Ed25519.StackHashMaximumMessageLength" /> bytes allocates nothing on the managed heap.
    /// </summary>
    /// <param name="length">The message's length.</param>
    /// <remarks>
    /// Measured on the calling thread around the one call, as for
    /// <see cref="SignData_WhenMessageFitsTheStackHash_ForSpanOverload_ShouldAllocateNothing" />.
    /// </remarks>
    [TestMethod]
    [DataRow(0)]
    [DataRow(64)]
    [DataRow(Ed25519.StackHashMaximumMessageLength)]
    public void VerifyData_WhenMessageFitsTheStackHash_ShouldAllocateNothing(int length)
    {
        using var algorithm = new Ed25519();
        algorithm.ImportPrivateKey(Convert.FromHexString(MessageHashingSeedHex));
        byte[] message = MessagePattern(length);
        byte[] signature = algorithm.SignData(message);
        Assert.IsTrue(algorithm.VerifyData(message, signature));

        long before = GC.GetAllocatedBytesForCurrentThread();
        bool valid = algorithm.VerifyData(message, signature);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.IsTrue(valid);
        Assert.AreEqual(0L, allocated, $"VerifyData allocated {allocated} bytes.");
    }

    /// <summary>
    /// Yields message lengths either side of <see cref="Ed25519.StackHashMaximumMessageLength" />, around SHA-512's
    /// block boundaries, and well beyond the limit.
    /// </summary>
    /// <returns>The lengths, in bytes.</returns>
    private static IEnumerable<int> MessageLengthsAroundTheStackHashLimit()
    {
        const int Limit = Ed25519.StackHashMaximumMessageLength;

        return [0, 1, 63, 64, 65, 127, 128, Limit - 64, Limit - 1, Limit, Limit + 1, Limit + 64, (4 * Limit) + 3];
    }

    /// <summary>
    /// Returns a message whose bytes follow a fixed pattern.
    /// </summary>
    /// <param name="length">The message's length.</param>
    /// <returns>The message.</returns>
    private static byte[] MessagePattern(int length)
    {
        byte[] message = new byte[length];
        for (int i = 0; i < message.Length; i++)
            message[i] = (byte)((i * 31) + 7);

        return message;
    }

    /// <summary>
    /// Signs a message as RFC 8032 §5.1.6 describes, hashing each input in one call over its concatenation and
    /// multiplying the base point with the replaced 1.1.0 table: an oracle for the signatures <see cref="Ed25519" />
    /// gives.
    /// </summary>
    /// <param name="seed">The 32-byte private seed.</param>
    /// <param name="message">The message.</param>
    /// <returns>The 64-byte signature R ‖ S.</returns>
    private static byte[] SignWithReference(byte[] seed, byte[] message)
    {
        byte[] expanded = SHA512.HashData(seed);
        byte[] s = expanded[..32];
        s[0] &= 248;
        s[31] &= 127;
        s[31] |= 64;

        byte[] publicKey = new byte[32];
        Ed25519PointReference.ScalarMultBase(s).Encode(publicKey);

        byte[] r = new byte[32];
        Ed25519Scalar.Reduce(SHA512.HashData([.. expanded[32..], .. message]), r);
        byte[] rEncoded = new byte[32];
        Ed25519PointReference.ScalarMultBase(r).Encode(rEncoded);

        byte[] k = new byte[32];
        Ed25519Scalar.Reduce(SHA512.HashData([.. rEncoded, .. publicKey, .. message]), k);
        byte[] sEncoded = new byte[32];
        Ed25519Scalar.MulAdd(k, s, r, sEncoded);

        return [.. rEncoded, .. sEncoded];
    }
}
