// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ChaCha20Tests.KnownAnswerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------


using System.Security.Cryptography;
using Bodu.Security.Cryptography.Infrastructure;
using Bodu.Test.Kat;
using static Bodu.Security.Cryptography.Infrastructure.KatBytes;

namespace Bodu.Security.Cryptography;
/// <summary>
/// Locks the <see cref="ChaCha20" /> stream cipher against the published RFC 8439 known-answer test vectors, and
/// inherits the shared <see cref="SymmetricStreamAlgorithmTests{TTest, TAlgorithm}" /> behavioural contract.
/// </summary>
[TestClass]
public sealed partial class ChaCha20Tests
    : SymmetricStreamAlgorithmTests<ChaCha20Tests, ChaCha20>
{
    /// <inheritdoc />
    protected override SymmetricStreamAlgorithmSpecification GetSpecification() =>
        new()
        {
            DefaultKeySizeBits = 256,
            NonceSizeBits = 96,
            LegalKeySizesBits = [256],
        };

    // ── RFC 8439 — ChaCha20 cipher known-answer tests ────────────────────────────────────────
    //
    // Source: RFC 8439 — https://www.rfc-editor.org/rfc/rfc8439
    //   §2.4.2 ChaCha20 encryption (the "sunscreen" plaintext).
    //   §2.3.2 ChaCha20 block function (counter 1 keystream, recovered here as the ciphertext of a
    //          64-byte all-zero plaintext under the §2.3.2 key/nonce).
    private static readonly StreamCipherKnownAnswer[] KnownAnswerTests =
    [
        new StreamCipherKnownAnswer
        {
            Name = "RFC8439 2.4.2 ChaCha20 encryption",
            Provenance = KatProvenance.Rfc("RFC 8439 Section 2.4.2"),
            Key = Hex("000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f"),
            Nonce = Hex("000000000000004a00000000"),
            Counter = 1,
            Plaintext = Hex(
                "4c616469657320616e642047656e746c656d656e206f662074686520636c6173" +
                "73206f66202739393a204966204920636f756c64206f6666657220796f75206f" +
                "6e6c79206f6e652074697020666f7220746865206675747572652c2073756e73" +
                "637265656e20776f756c642062652069742e"),
            Ciphertext = Hex(
                "6e2e359a2568f98041ba0728dd0d6981e97e7aec1d4360c20a27afccfd9fae0b" +
                "f91b65c5524733ab8f593dabcd62b3571639d624e65152ab8f530c359f0861d8" +
                "07ca0dbf500d6a6156a38e088a22b65e52bc514d16ccf806818ce91ab7793736" +
                "5af90bbf74a35be6b40b8eedf2785e42874d"),
        },
        new StreamCipherKnownAnswer
        {
            Name = "RFC8439 2.3.2 ChaCha20 block function (counter 1 keystream)",
            Provenance = KatProvenance.Rfc("RFC 8439 Section 2.3.2"),
            Key = Hex("000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f"),
            Nonce = Hex("000000090000004a00000000"),
            Counter = 1,
            Plaintext = Hex(
                "00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000"),
            Ciphertext = Hex(
                "10f1e7e4d13b5915500fdd1fa32071c4c7d1f4c733c068030422aa9ac3d46c4e" +
                "d2826446079faa0914c2d705d98b02a2b5129cd1de164eb9cbd083e8a2503c4e"),
        },
    ];

    /// <summary>
    /// Yields the ChaCha20 known-answer vectors as <see cref="DynamicDataAttribute" /> rows.
    /// </summary>
    /// <returns>One row per vector.</returns>
    private static IEnumerable<object[]> ChaCha20KatData()
    {
        foreach (StreamCipherKnownAnswer kat in KnownAnswerTests)
            yield return new object[] { kat };
    }

    /// <summary>
    /// Verifies that <see cref="ChaCha20" /> encrypts each RFC 8439 plaintext to the published ciphertext.
    /// </summary>
    /// <param name="vector">The known-answer vector under test.</param>
    [TestMethod]
    [DynamicData(
        nameof(ChaCha20KatData),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void CreateEncryptor_WhenGivenRfc8439Vector_ShouldMatchExpectedCiphertext(StreamCipherKnownAnswer vector)
    {
        using var cipher = new ChaCha20 { InitialCounter = vector.Counter };
        using ICryptoTransform encryptor = cipher.CreateEncryptor(vector.Key, vector.Nonce);
        byte[] actual = encryptor.TransformFinalBlock(vector.Plaintext, 0, vector.Plaintext.Length);

        CollectionAssert.AreEqual(vector.Ciphertext, actual, $"ChaCha20 ciphertext mismatch for {vector.Name}.");
    }

    /// <summary>
    /// Verifies that <see cref="ChaCha20" /> decrypts each RFC 8439 ciphertext back to the published plaintext,
    /// confirming the cipher is self-inverse.
    /// </summary>
    /// <param name="vector">The known-answer vector under test.</param>
    [TestMethod]
    [DynamicData(
        nameof(ChaCha20KatData),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void CreateDecryptor_WhenGivenRfc8439Vector_ShouldRecoverPlaintext(StreamCipherKnownAnswer vector)
    {
        using var cipher = new ChaCha20 { InitialCounter = vector.Counter };
        using ICryptoTransform decryptor = cipher.CreateDecryptor(vector.Key, vector.Nonce);
        byte[] actual = decryptor.TransformFinalBlock(vector.Ciphertext, 0, vector.Ciphertext.Length);

        CollectionAssert.AreEqual(vector.Plaintext, actual, $"ChaCha20 plaintext recovery mismatch for {vector.Name}.");
    }

    /// <summary>
    /// Verifies that the ChaCha20 keystream matches the BCL <see cref="ChaCha20Poly1305" /> construction, whose
    /// keystream is RFC 8439 ChaCha20 with the block counter starting at 1.
    /// </summary>
    [TestMethod]
    public void CreateEncryptor_WhenComparedToBclChaCha20Poly1305_ShouldProduceSameKeystream()
    {
        byte[] key = RandomNumberGenerator.GetBytes(32);
        byte[] nonce = RandomNumberGenerator.GetBytes(12);
        byte[] plaintext = RandomNumberGenerator.GetBytes(257);

        byte[] bclCiphertext = new byte[plaintext.Length];
        byte[] tag = new byte[16];
        using (var aead = new ChaCha20Poly1305(key))
            aead.Encrypt(nonce, plaintext, bclCiphertext, tag);

        using var cipher = new ChaCha20 { InitialCounter = 1 };
        using ICryptoTransform encryptor = cipher.CreateEncryptor(key, nonce);
        byte[] actual = encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);

        CollectionAssert.AreEqual(bclCiphertext, actual,
            "ChaCha20 keystream must match the BCL ChaCha20Poly1305 keystream (counter = 1).");
    }

    /// <summary>
    /// Verifies that <see cref="ChaCha20.InitialCounter" /> is captured when the transform is created, so mutating it
    /// on the algorithm afterwards does not change an already-created transform's keystream.
    /// </summary>
    [TestMethod]
    public void CreateEncryptor_WhenInitialCounterChangedAfterCreation_ShouldUseCapturedCounter()
    {
        byte[] key = new byte[32];
        byte[] nonce = new byte[12];
        byte[] plaintext = new byte[128];

        using var cipher = new ChaCha20 { InitialCounter = 5 };
        using ICryptoTransform transform = cipher.CreateEncryptor(key, nonce);
        cipher.InitialCounter = 9;
        byte[] actual = transform.TransformFinalBlock(plaintext, 0, plaintext.Length);

        using var reference = new ChaCha20 { InitialCounter = 5 };
        using ICryptoTransform referenceTransform = reference.CreateEncryptor(key, nonce);
        byte[] expected = referenceTransform.TransformFinalBlock(plaintext, 0, plaintext.Length);

        CollectionAssert.AreEqual(expected, actual,
            "The transform must use the counter captured at creation, not the algorithm's later value.");
    }
}
