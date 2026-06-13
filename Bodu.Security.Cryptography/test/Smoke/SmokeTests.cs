// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SmokeTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using System.Text;

namespace Bodu.Smoke;

/// <summary>
/// Provides smoke-tier coverage for the primary public types in <c>Bodu.Security.Cryptography</c>. Each test
/// exercises one happy-path entry point so that catastrophic breakage (key-schedule corruption, block-mode
/// dispatch fault, digest length error) is caught by the smallest possible build run.
/// </summary>
[TestClass]
public sealed class SmokeTests
{
    /// <summary>
    /// Verifies that <see cref="Bodu.Security.Cryptography.Threefish256" /> performs a successful
    /// encrypt / decrypt round-trip in CBC mode under a freshly generated key and IV.
    /// </summary>
    [TestMethod]
    public void Threefish256_EncryptDecryptRoundTrip_ShouldRecoverPlaintext()
    {
        using var algorithm = Bodu.Security.Cryptography.Threefish256.Create();
        algorithm.Mode = CipherMode.CBC;
        algorithm.Padding = PaddingMode.PKCS7;
        algorithm.GenerateKey();
        algorithm.GenerateIV();
        algorithm.GenerateTweak();

        var plaintext = Encoding.UTF8.GetBytes("Smoke test for Threefish-256 in CBC + PKCS7.");

        byte[] ciphertext;
        using (ICryptoTransform encryptor = algorithm.CreateEncryptor())
            ciphertext = encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);

        byte[] roundTrip;
        using (ICryptoTransform decryptor = algorithm.CreateDecryptor())
            roundTrip = decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);

        CollectionAssert.AreEqual(plaintext, roundTrip);
    }

    /// <summary>
    /// Verifies that <see cref="Bodu.Security.Cryptography.Skipjack" /> performs a successful
    /// encrypt / decrypt round-trip in ECB mode with PKCS7 padding under a freshly generated key.
    /// </summary>
    [TestMethod]
    public void Skipjack_EncryptDecryptRoundTrip_ShouldRecoverPlaintext()
    {
        using Bodu.Security.Cryptography.Skipjack algorithm = new();
        algorithm.Mode = CipherMode.ECB;
        algorithm.Padding = PaddingMode.PKCS7;
        algorithm.GenerateKey();
        algorithm.GenerateIV();

        var plaintext = Encoding.UTF8.GetBytes("Skipjack smoke.");

        byte[] ciphertext;
        using (ICryptoTransform encryptor = algorithm.CreateEncryptor())
            ciphertext = encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);

        byte[] roundTrip;
        using (ICryptoTransform decryptor = algorithm.CreateDecryptor())
            roundTrip = decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);

        CollectionAssert.AreEqual(plaintext, roundTrip);
    }

    /// <summary>
    /// Verifies that <see cref="Bodu.Security.Cryptography.Tiger" /> produces a 192-bit (24-byte) digest from
    /// a non-empty input.
    /// </summary>
    [TestMethod]
    public void Tiger_ComputeHash_ShouldProduce192BitDigest()
    {
        using Bodu.Security.Cryptography.Tiger tiger = new();
        var digest = tiger.ComputeHash(Encoding.ASCII.GetBytes("smoke"));

        Assert.AreEqual(24, digest.Length);
    }

    /// <summary>
    /// Verifies that <see cref="Bodu.Security.Cryptography.SipHash64" /> produces an 8-byte digest from a
    /// non-empty input under a freshly generated key.
    /// </summary>
    [TestMethod]
    public void SipHash64_ComputeHash_ShouldProduceEightByteDigest()
    {
        var key = new byte[16];
        for (var i = 0; i < key.Length; i++) key[i] = (byte)i;

        using Bodu.Security.Cryptography.SipHash64 hash = new() { Key = key };
        var digest = hash.ComputeHash(Encoding.ASCII.GetBytes("smoke"));

        Assert.AreEqual(8, digest.Length);
    }

    /// <summary>
    /// Verifies that <see cref="Bodu.Security.Cryptography.ChaCha20" /> performs a successful encrypt / decrypt
    /// round-trip under a freshly generated key and nonce.
    /// </summary>
    [TestMethod]
    public void ChaCha20_EncryptDecryptRoundTrip_ShouldRecoverPlaintext()
    {
        using var algorithm = Bodu.Security.Cryptography.ChaCha20.Create();
        algorithm.GenerateKey();
        algorithm.GenerateNonce();

        var plaintext = Encoding.UTF8.GetBytes("Smoke test for ChaCha20.");

        byte[] ciphertext;
        using (ICryptoTransform encryptor = algorithm.CreateEncryptor())
            ciphertext = encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);

        byte[] roundTrip;
        using (ICryptoTransform decryptor = algorithm.CreateDecryptor())
            roundTrip = decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);

        CollectionAssert.AreEqual(plaintext, roundTrip);
    }

    /// <summary>
    /// Verifies that <see cref="Bodu.Security.Cryptography.XChaCha20" /> performs a successful encrypt / decrypt
    /// round-trip under a freshly generated key and 192-bit nonce.
    /// </summary>
    [TestMethod]
    public void XChaCha20_EncryptDecryptRoundTrip_ShouldRecoverPlaintext()
    {
        using var algorithm = Bodu.Security.Cryptography.XChaCha20.Create();
        algorithm.GenerateKey();
        algorithm.GenerateNonce();

        var plaintext = Encoding.UTF8.GetBytes("Smoke test for XChaCha20.");

        byte[] ciphertext;
        using (ICryptoTransform encryptor = algorithm.CreateEncryptor())
            ciphertext = encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);

        byte[] roundTrip;
        using (ICryptoTransform decryptor = algorithm.CreateDecryptor())
            roundTrip = decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);

        CollectionAssert.AreEqual(plaintext, roundTrip);
    }

    /// <summary>
    /// Verifies that <see cref="Bodu.Security.Cryptography.Salsa20" /> performs a successful encrypt / decrypt
    /// round-trip under a freshly generated key and nonce.
    /// </summary>
    [TestMethod]
    public void Salsa20_EncryptDecryptRoundTrip_ShouldRecoverPlaintext()
    {
        using var algorithm = Bodu.Security.Cryptography.Salsa20.Create();
        algorithm.GenerateKey();
        algorithm.GenerateNonce();

        var plaintext = Encoding.UTF8.GetBytes("Smoke test for Salsa20.");

        byte[] ciphertext;
        using (ICryptoTransform encryptor = algorithm.CreateEncryptor())
            ciphertext = encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);

        byte[] roundTrip;
        using (ICryptoTransform decryptor = algorithm.CreateDecryptor())
            roundTrip = decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);

        CollectionAssert.AreEqual(plaintext, roundTrip);
    }

    /// <summary>
    /// Verifies that <see cref="Bodu.Security.Cryptography.XSalsa20" /> performs a successful encrypt / decrypt
    /// round-trip under a freshly generated key and 192-bit nonce.
    /// </summary>
    [TestMethod]
    public void XSalsa20_EncryptDecryptRoundTrip_ShouldRecoverPlaintext()
    {
        using var algorithm = Bodu.Security.Cryptography.XSalsa20.Create();
        algorithm.GenerateKey();
        algorithm.GenerateNonce();

        var plaintext = Encoding.UTF8.GetBytes("Smoke test for XSalsa20.");

        byte[] ciphertext;
        using (ICryptoTransform encryptor = algorithm.CreateEncryptor())
            ciphertext = encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);

        byte[] roundTrip;
        using (ICryptoTransform decryptor = algorithm.CreateDecryptor())
            roundTrip = decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);

        CollectionAssert.AreEqual(plaintext, roundTrip);
    }

    /// <summary>
    /// Verifies that <see cref="Bodu.Security.Cryptography.Rabbit" /> performs a successful encrypt / decrypt
    /// round-trip under a freshly generated key and 64-bit IV.
    /// </summary>
    [TestMethod]
    public void Rabbit_EncryptDecryptRoundTrip_ShouldRecoverPlaintext()
    {
        using var algorithm = Bodu.Security.Cryptography.Rabbit.Create();
        algorithm.GenerateKey();
        algorithm.GenerateNonce();

        var plaintext = Encoding.UTF8.GetBytes("Smoke test for Rabbit.");

        byte[] ciphertext;
        using (ICryptoTransform encryptor = algorithm.CreateEncryptor())
            ciphertext = encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);

        byte[] roundTrip;
        using (ICryptoTransform decryptor = algorithm.CreateDecryptor())
            roundTrip = decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);

        CollectionAssert.AreEqual(plaintext, roundTrip);
    }

    /// <summary>
    /// Verifies that <see cref="Bodu.Security.Cryptography.Hc128" /> performs a successful encrypt / decrypt
    /// round-trip under a freshly generated key and 128-bit IV.
    /// </summary>
    [TestMethod]
    public void Hc128_EncryptDecryptRoundTrip_ShouldRecoverPlaintext()
    {
        using var algorithm = Bodu.Security.Cryptography.Hc128.Create();
        algorithm.GenerateKey();
        algorithm.GenerateNonce();

        var plaintext = Encoding.UTF8.GetBytes("Smoke test for HC-128.");

        byte[] ciphertext;
        using (ICryptoTransform encryptor = algorithm.CreateEncryptor())
            ciphertext = encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);

        byte[] roundTrip;
        using (ICryptoTransform decryptor = algorithm.CreateDecryptor())
            roundTrip = decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);

        CollectionAssert.AreEqual(plaintext, roundTrip);
    }

    /// <summary>
    /// Verifies that two freshly generated <see cref="Bodu.Security.Cryptography.X25519" /> parties derive the same
    /// shared secret from each other's public keys.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void X25519_DeriveSharedSecret_BothSidesShouldAgree()
    {
        using var alice = Bodu.Security.Cryptography.X25519.Create();
        using var bob = Bodu.Security.Cryptography.X25519.Create();
        alice.GenerateKey();
        bob.GenerateKey();

        var aliceShared = alice.DeriveSharedSecret(bob.ExportPublicKey());
        var bobShared = bob.DeriveSharedSecret(alice.ExportPublicKey());

        CollectionAssert.AreEqual(aliceShared, bobShared);
    }

    /// <summary>
    /// Verifies that <see cref="Bodu.Security.Cryptography.Ed25519" /> performs a successful sign / verify
    /// round-trip under a freshly generated key pair.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void Ed25519_SignVerifyRoundTrip_ShouldAcceptGenuineSignature()
    {
        using var algorithm = Bodu.Security.Cryptography.Ed25519.Create();
        algorithm.GenerateKey();

        var message = Encoding.UTF8.GetBytes("Smoke test for Ed25519.");
        var signature = algorithm.SignData(message);

        Assert.IsTrue(algorithm.VerifyData(message, signature));
    }
}
