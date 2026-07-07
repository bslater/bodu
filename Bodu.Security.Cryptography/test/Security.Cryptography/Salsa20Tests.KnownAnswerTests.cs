// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Salsa20Tests.KnownAnswerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------


using System.Security.Cryptography;
using Bodu.Security.Cryptography.Infrastructure;
using Bodu.Test.Kat;
using static Bodu.Security.Cryptography.Infrastructure.KatBytes;

namespace Bodu.Security.Cryptography;
/// <summary>
/// Locks the <see cref="Salsa20" /> stream cipher against the published eSTREAM / ECRYPT and Bernstein-specification
/// known-answer test vectors, and inherits the shared
/// <see cref="SymmetricStreamAlgorithmTests{TTest, TAlgorithm}" /> behavioural contract.
/// </summary>
[TestClass]
public sealed partial class Salsa20Tests
    : SymmetricStreamAlgorithmTests<Salsa20Tests, Salsa20>
{
    /// <inheritdoc />
    protected override SymmetricStreamAlgorithmSpecification GetSpecification() =>
        new()
        {
            DefaultKeySizeBits = 256,
            NonceSizeBits = 64,
            LegalKeySizesBits = [128, 256],
            KnownAnswers = KeystreamKnownAnswers,
        };

    // ── eSTREAM / ECRYPT Salsa20/20 keystream known-answer tests ─────────────────────────────
    //
    // Sources: eSTREAM verified vectors (RustCrypto salsa20 corpus) and the Crypto++ TestVectors/salsa.txt
    // ECRYPT set, all confirmed against this implementation. The key size is taken from the key length.
    private static readonly StreamCipherKnownAnswer[] KeystreamKnownAnswers =
    [
        new StreamCipherKnownAnswer
        {
            Name = "eSTREAM Salsa20/256 KEY1 IV0 (first 64 bytes)",
            Provenance = KatProvenance.ReferenceImplementation("eSTREAM verified vectors"),
            Key = Hex("8000000000000000000000000000000000000000000000000000000000000000"),
            Nonce = Hex("0000000000000000"),
            IsKeystream = true,
            Plaintext = [],
            Ciphertext = Hex(
                "e3be8fdd8beca2e3ea8ef9475b29a6e7003951e1097a5c38d23b7a5fad9f6844" +
                "b22c97559e2723c7cbbd3fe4fc8d9a0744652a83e72a9c461876af4d7ef1a117"),
        },
        new StreamCipherKnownAnswer
        {
            Name = "eSTREAM Salsa20/256 KEY0 IV1 (first 64 bytes)",
            Provenance = KatProvenance.ReferenceImplementation("eSTREAM verified vectors"),
            Key = Hex("0000000000000000000000000000000000000000000000000000000000000000"),
            Nonce = Hex("8000000000000000"),
            IsKeystream = true,
            Plaintext = [],
            Ciphertext = Hex(
                "2aba3dc45b4947007b14c851cd694456b303ad59a465662803006705673d6c3e" +
                "29f1d3510dfc0405463c03414e0e07e359f1f1816c68b2434a19d3eee0464873"),
        },
        new StreamCipherKnownAnswer
        {
            Name = "ECRYPT Salsa20/128 Set 1 vector 0 (first 64 bytes)",
            Provenance = KatProvenance.ReferenceImplementation("Crypto++ TestVectors/salsa.txt (ECRYPT Set 1 #0)"),
            Key = Hex("80000000000000000000000000000000"),
            Nonce = Hex("0000000000000000"),
            IsKeystream = true,
            Plaintext = [],
            Ciphertext = Hex(
                "4dfa5e481da23ea09a31022050859936da52fcee218005164f267cb65f5cfd7f" +
                "2b4f97e0ff16924a52df269515110a07f9e460bc65ef95da58f740b7d1dbb0aa"),
        },
    ];

    /// <summary>
    /// Yields the Salsa20 keystream known-answer vectors as <see cref="DynamicDataAttribute" /> rows.
    /// </summary>
    /// <returns>One row per vector.</returns>
    private static IEnumerable<object[]> Salsa20KeystreamKatData()
    {
        foreach (StreamCipherKnownAnswer kat in new Salsa20Tests().GetSpecification().KnownAnswers)
            yield return new object[] { kat };
    }

    /// <summary>
    /// Verifies that <see cref="Salsa20" /> reproduces each published keystream vector, recovered as the ciphertext of
    /// an all-zero plaintext, for both 128-bit and 256-bit keys.
    /// </summary>
    /// <param name="vector">The keystream KAT vector under test.</param>
    [TestMethod]
    [DynamicData(
        nameof(Salsa20KeystreamKatData),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void CreateEncryptor_WhenGivenKeystreamVector_ShouldMatchExpected(StreamCipherKnownAnswer vector)
    {
        byte[] expected = vector.Ciphertext;
        byte[] zeros = new byte[expected.Length];

        using var cipher = new Salsa20 { KeySize = vector.Key.Length * 8 };
        using ICryptoTransform encryptor = cipher.CreateEncryptor(vector.Key, vector.Nonce);
        byte[] keystream = encryptor.TransformFinalBlock(zeros, 0, zeros.Length);

        CollectionAssert.AreEqual(expected, keystream, $"Salsa20 keystream mismatch for {vector.Name}.");
    }

    /// <summary>Resource name of the embedded ECRYPT verified Salsa20 vector file.</summary>
    private const string Salsa20EcryptResourceName = "Bodu.Security.Cryptography.Salsa20.ecrypt-verified.test-vectors.txt";

    /// <summary>
    /// Yields the official ECRYPT verified Salsa20 vectors (both key-size profiles, all six sets), loaded from the
    /// embedded vector file, as <see cref="DynamicDataAttribute" /> rows.
    /// </summary>
    /// <returns>One row per ECRYPT vector.</returns>
    /// <exception cref="InvalidOperationException">The embedded resource cannot be located.</exception>
    private static IEnumerable<object[]> Salsa20EcryptKatData()
    {
        using Stream stream = typeof(Salsa20Tests).Assembly.GetManifestResourceStream(Salsa20EcryptResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{Salsa20EcryptResourceName}' is missing.");

        foreach (Salsa20EcryptVector vector in Salsa20EcryptVerifiedReader.Read(stream))
            yield return new object[] { vector };
    }

    /// <summary>
    /// Verifies that <see cref="Salsa20" /> reproduces every ECRYPT verified vector: each sampled keystream fragment at
    /// its absolute offset, and the XOR digest folded over the full generated keystream (512 bytes for sets 1-5,
    /// 131,072 bytes for set 6 — confirming correct 64-bit block-counter progression across thousands of blocks).
    /// </summary>
    /// <param name="vector">The ECRYPT vector under test.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(Salsa20EcryptKatData),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void CreateEncryptor_WhenGivenEcryptVerifiedVector_ShouldMatchFragmentsAndDigest(Salsa20EcryptVector vector)
    {
        byte[] keystream;
        using (var cipher = new Salsa20 { KeySize = vector.KeyBits })
        using (ICryptoTransform encryptor = cipher.CreateEncryptor(vector.Key, vector.Iv))
            keystream = encryptor.TransformFinalBlock(new byte[vector.StreamLength], 0, vector.StreamLength);

        foreach ((int offset, byte[] expected) in vector.Fragments)
            CollectionAssert.AreEqual(expected, keystream[offset..(offset + expected.Length)],
                $"Salsa20 keystream fragment mismatch at offset {offset} for {vector.Name}.");

        byte[] digest = new byte[64];
        for (int offset = 0; offset < vector.StreamLength; offset += 64)
            for (int i = 0; i < 64; i++)
                digest[i] ^= keystream[offset + i];

        CollectionAssert.AreEqual(vector.XorDigest, digest, $"Salsa20 XOR digest mismatch for {vector.Name}.");
    }

    /// <summary>
    /// Verifies that <see cref="Salsa20.InitialCounter" /> is captured when the transform is created, so mutating it on
    /// the algorithm afterwards does not change an already-created transform's keystream.
    /// </summary>
    [TestMethod]
    public void CreateEncryptor_WhenInitialCounterChangedAfterCreation_ShouldUseCapturedCounter()
    {
        byte[] key = new byte[32];
        byte[] nonce = new byte[8];
        byte[] plaintext = new byte[128];

        using var cipher = new Salsa20 { InitialCounter = 5 };
        using ICryptoTransform transform = cipher.CreateEncryptor(key, nonce);
        cipher.InitialCounter = 9;
        byte[] actual = transform.TransformFinalBlock(plaintext, 0, plaintext.Length);

        using var reference = new Salsa20 { InitialCounter = 5 };
        using ICryptoTransform referenceTransform = reference.CreateEncryptor(key, nonce);
        byte[] expected = referenceTransform.TransformFinalBlock(plaintext, 0, plaintext.Length);

        CollectionAssert.AreEqual(expected, actual,
            "The transform must use the counter captured at creation, not the algorithm's later value.");
    }
}
