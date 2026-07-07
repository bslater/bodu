// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ChaCha20Tests.KnownAnswerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------


using System.Security.Cryptography;
using Bodu.Security.Cryptography.Infrastructure;
using Bodu.Test.Kat;

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
            KnownAnswers = KnownAnswerTests,
        };

    // ── RFC 8439 — ChaCha20 cipher known-answer tests ────────────────────────────────────────
    //
    // Loaded dynamically from the embedded RFC 8439 source text (Appendix A.2, "ChaCha20 Encryption"): the three
    // published encryption test vectors, each carrying its own key, nonce, initial block counter, plaintext, and
    // ciphertext.
    private const string Rfc8439ResourceName = "Bodu.Security.Cryptography.Rfc8439.txt";

    private static readonly StreamCipherKnownAnswer[] KnownAnswerTests = LoadKnownAnswers();

    /// <summary>Loads the RFC 8439 Appendix A.2 ChaCha20 encryption vectors from the embedded RFC text.</summary>
    /// <returns>The three Appendix A.2 encryption vectors.</returns>
    /// <exception cref="InvalidOperationException">The embedded resource cannot be located.</exception>
    private static StreamCipherKnownAnswer[] LoadKnownAnswers()
    {
        using Stream stream = typeof(ChaCha20Tests).Assembly.GetManifestResourceStream(Rfc8439ResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{Rfc8439ResourceName}' is missing.");

        var vectors = new List<StreamCipherKnownAnswer>();
        foreach (Rfc8439TestVector vector in Rfc8439VectorReader.Read(stream, "A.2.  ChaCha20 Encryption"))
        {
            vectors.Add(new StreamCipherKnownAnswer
            {
                Name = $"RFC 8439 Appendix A.2 ChaCha20 encryption #{vector.Number}",
                Provenance = KatProvenance.Rfc("RFC 8439 Appendix A.2"),
                Key = vector.Field("Key"),
                Nonce = vector.Field("Nonce"),
                Counter = vector.InitialBlockCounter ?? 0,
                Plaintext = vector.Field("Plaintext"),
                Ciphertext = vector.Field("Ciphertext"),
            });
        }

        return [.. vectors];
    }

    /// <summary>
    /// Yields the ChaCha20 known-answer vectors as <see cref="DynamicDataAttribute" /> rows.
    /// </summary>
    /// <returns>One row per vector.</returns>
    private static IEnumerable<object[]> ChaCha20KatData()
    {
        foreach (StreamCipherKnownAnswer kat in new ChaCha20Tests().GetSpecification().KnownAnswers)
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
