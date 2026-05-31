// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ChaCha20Tests.KnownAnswerTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

using System.Reflection;
using System.Security.Cryptography;

/// <summary>
/// Locks the <see cref="ChaCha20" /> stream cipher against the published RFC 8439 known-answer test vectors, and
/// inherits the shared <see cref="SymmetricStreamAlgorithmTests{TTest, TAlgorithm}" /> behavioural contract.
/// </summary>
[TestClass]
public sealed partial class ChaCha20Tests
    : SymmetricStreamAlgorithmTests<ChaCha20Tests, ChaCha20>
{
    /// <inheritdoc />
    protected override ChaCha20 CreateAlgorithm() => new();

    /// <inheritdoc />
    protected override SymmetricStreamAlgorithmSpecification GetSpecification() =>
        new()
        {
            DefaultKeySizeBits = 256,
            NonceSizeBits = 96,
            LegalKeySizesBits = [256],
        };

    /// <summary>
    /// Represents one ChaCha20 encryption known-answer test row, expressed as continuous hex strings.
    /// </summary>
    private sealed record ChaCha20EncryptionKat
    {
        public required string Name { get; init; }

        public required string Source { get; init; }

        public required string KeyHex { get; init; }

        public required string NonceHex { get; init; }

        public required uint Counter { get; init; }

        public required string PlaintextHex { get; init; }

        public required string CiphertextHex { get; init; }
    }

    // ── RFC 8439 — ChaCha20 cipher known-answer tests ────────────────────────────────────────
    //
    // Source: RFC 8439 — https://www.rfc-editor.org/rfc/rfc8439
    //   §2.4.2 ChaCha20 encryption (the "sunscreen" plaintext).
    //   §2.3.2 ChaCha20 block function (counter 1 keystream, recovered here as the ciphertext of a
    //          64-byte all-zero plaintext under the §2.3.2 key/nonce).
    private static readonly ChaCha20EncryptionKat[] KnownAnswerTests =
    [
        new ChaCha20EncryptionKat
        {
            Name = "RFC8439 2.4.2 ChaCha20 encryption",
            Source = "RFC8439 Section 2.4.2",
            KeyHex = "000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f",
            NonceHex = "000000000000004a00000000",
            Counter = 1,
            PlaintextHex =
                "4c616469657320616e642047656e746c656d656e206f662074686520636c6173" +
                "73206f66202739393a204966204920636f756c64206f6666657220796f75206f" +
                "6e6c79206f6e652074697020666f7220746865206675747572652c2073756e73" +
                "637265656e20776f756c642062652069742e",
            CiphertextHex =
                "6e2e359a2568f98041ba0728dd0d6981e97e7aec1d4360c20a27afccfd9fae0b" +
                "f91b65c5524733ab8f593dabcd62b3571639d624e65152ab8f530c359f0861d8" +
                "07ca0dbf500d6a6156a38e088a22b65e52bc514d16ccf806818ce91ab7793736" +
                "5af90bbf74a35be6b40b8eedf2785e42874d",
        },
        new ChaCha20EncryptionKat
        {
            Name = "RFC8439 2.3.2 ChaCha20 block function (counter 1 keystream)",
            Source = "RFC8439 Section 2.3.2",
            KeyHex = "000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f",
            NonceHex = "000000090000004a00000000",
            Counter = 1,
            PlaintextHex =
                "00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000",
            CiphertextHex =
                "10f1e7e4d13b5915500fdd1fa32071c4c7d1f4c733c068030422aa9ac3d46c4e" +
                "d2826446079faa0914c2d705d98b02a2b5129cd1de164eb9cbd083e8a2503c4e",
        },
    ];

    /// <summary>
    /// Yields the ChaCha20 known-answer vectors as <see cref="DynamicDataAttribute" /> rows.
    /// </summary>
    /// <returns>One row per vector, carrying the hex inputs and expected ciphertext.</returns>
    private static IEnumerable<object[]> ChaCha20KatData()
    {
        foreach (ChaCha20EncryptionKat kat in KnownAnswerTests)
            yield return new object[] { kat.KeyHex, kat.NonceHex, kat.Counter, kat.PlaintextHex, kat.CiphertextHex, kat.Name };
    }

    /// <summary>
    /// Produces a human-readable display name for a ChaCha20 KAT row.
    /// </summary>
    /// <param name="testMethod">The executing test method.</param>
    /// <param name="data">The row data; index 5 carries the scenario name.</param>
    /// <returns>The test method name followed by the row's scenario label.</returns>
    public static string GetKatDisplayName(MethodInfo testMethod, object[] data) =>
        $"{testMethod.Name} — {data[5]}";

    /// <summary>
    /// Verifies that <see cref="ChaCha20" /> encrypts each RFC 8439 plaintext to the published ciphertext.
    /// </summary>
    /// <param name="keyHex">The 256-bit key, in hex.</param>
    /// <param name="nonceHex">The 96-bit nonce, in hex.</param>
    /// <param name="counter">The initial block counter.</param>
    /// <param name="plaintextHex">The plaintext, in hex.</param>
    /// <param name="ciphertextHex">The expected ciphertext, in hex.</param>
    /// <param name="displayName">The scenario label.</param>
    [TestMethod]
    [DynamicData(nameof(ChaCha20KatData), DynamicDataDisplayName = nameof(GetKatDisplayName))]
    public void CreateEncryptor_WhenGivenRfc8439Vector_ShouldMatchExpectedCiphertext(
        string keyHex, string nonceHex, uint counter, string plaintextHex, string ciphertextHex, string displayName)
    {
        var key = Convert.FromHexString(keyHex);
        var nonce = Convert.FromHexString(nonceHex);
        var plaintext = Convert.FromHexString(plaintextHex);
        var expected = Convert.FromHexString(ciphertextHex);

        using var cipher = new ChaCha20 { InitialCounter = counter };
        using ICryptoTransform encryptor = cipher.CreateEncryptor(key, nonce);
        var actual = encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);

        CollectionAssert.AreEqual(expected, actual, $"ChaCha20 ciphertext mismatch for {displayName}.");
    }

    /// <summary>
    /// Verifies that <see cref="ChaCha20" /> decrypts each RFC 8439 ciphertext back to the published plaintext,
    /// confirming the cipher is self-inverse.
    /// </summary>
    /// <param name="keyHex">The 256-bit key, in hex.</param>
    /// <param name="nonceHex">The 96-bit nonce, in hex.</param>
    /// <param name="counter">The initial block counter.</param>
    /// <param name="plaintextHex">The expected plaintext, in hex.</param>
    /// <param name="ciphertextHex">The ciphertext, in hex.</param>
    /// <param name="displayName">The scenario label.</param>
    [TestMethod]
    [DynamicData(nameof(ChaCha20KatData), DynamicDataDisplayName = nameof(GetKatDisplayName))]
    public void CreateDecryptor_WhenGivenRfc8439Vector_ShouldRecoverPlaintext(
        string keyHex, string nonceHex, uint counter, string plaintextHex, string ciphertextHex, string displayName)
    {
        var key = Convert.FromHexString(keyHex);
        var nonce = Convert.FromHexString(nonceHex);
        var ciphertext = Convert.FromHexString(ciphertextHex);
        var expected = Convert.FromHexString(plaintextHex);

        using var cipher = new ChaCha20 { InitialCounter = counter };
        using ICryptoTransform decryptor = cipher.CreateDecryptor(key, nonce);
        var actual = decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);

        CollectionAssert.AreEqual(expected, actual, $"ChaCha20 plaintext recovery mismatch for {displayName}.");
    }

    /// <summary>
    /// Verifies that the ChaCha20 keystream matches the BCL <see cref="ChaCha20Poly1305" /> construction, whose
    /// keystream is RFC 8439 ChaCha20 with the block counter starting at 1.
    /// </summary>
    [TestMethod]
    public void CreateEncryptor_WhenComparedToBclChaCha20Poly1305_ShouldProduceSameKeystream()
    {
        var key = RandomNumberGenerator.GetBytes(32);
        var nonce = RandomNumberGenerator.GetBytes(12);
        var plaintext = RandomNumberGenerator.GetBytes(257);

        var bclCiphertext = new byte[plaintext.Length];
        var tag = new byte[16];
        using (var aead = new ChaCha20Poly1305(key))
            aead.Encrypt(nonce, plaintext, bclCiphertext, tag);

        using var cipher = new ChaCha20 { InitialCounter = 1 };
        using ICryptoTransform encryptor = cipher.CreateEncryptor(key, nonce);
        var actual = encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);

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
        var key = new byte[32];
        var nonce = new byte[12];
        var plaintext = new byte[128];

        using var cipher = new ChaCha20 { InitialCounter = 5 };
        using ICryptoTransform transform = cipher.CreateEncryptor(key, nonce);
        cipher.InitialCounter = 9;
        var actual = transform.TransformFinalBlock(plaintext, 0, plaintext.Length);

        using var reference = new ChaCha20 { InitialCounter = 5 };
        using ICryptoTransform referenceTransform = reference.CreateEncryptor(key, nonce);
        var expected = referenceTransform.TransformFinalBlock(plaintext, 0, plaintext.Length);

        CollectionAssert.AreEqual(expected, actual,
            "The transform must use the counter captured at creation, not the algorithm's later value.");
    }
}
