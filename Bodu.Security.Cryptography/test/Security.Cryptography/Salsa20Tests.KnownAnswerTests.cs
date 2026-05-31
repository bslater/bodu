// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Salsa20Tests.KnownAnswerTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

using System.Reflection;
using System.Security.Cryptography;

/// <summary>
/// Locks the <see cref="Salsa20" /> stream cipher against the published eSTREAM / ECRYPT and Bernstein-specification
/// known-answer test vectors, and inherits the shared
/// <see cref="StreamCipherAlgorithmTests{TTest, TAlgorithm}" /> behavioural contract.
/// </summary>
[TestClass]
public sealed class Salsa20Tests
    : StreamCipherAlgorithmTests<Salsa20Tests, Salsa20>
{
    /// <summary>
    /// Represents one Salsa20 keystream known-answer test row, recovered as the ciphertext of an all-zero plaintext.
    /// </summary>
    private sealed record Salsa20KeystreamKat
    {
        public required string Name { get; init; }

        public required string Source { get; init; }

        public required int KeySizeBits { get; init; }

        public required string KeyHex { get; init; }

        public required string NonceHex { get; init; }

        public required string KeystreamHex { get; init; }
    }

    // ── eSTREAM / ECRYPT Salsa20/20 keystream known-answer tests ─────────────────────────────
    //
    // Sources: eSTREAM verified vectors (RustCrypto salsa20 corpus) and the Crypto++ TestVectors/salsa.txt
    // ECRYPT set, all confirmed against this implementation.
    private static readonly Salsa20KeystreamKat[] KeystreamKnownAnswers =
    [
        new Salsa20KeystreamKat
        {
            Name = "eSTREAM Salsa20/256 KEY1 IV0 (first 64 bytes)",
            Source = "eSTREAM verified vectors",
            KeySizeBits = 256,
            KeyHex = "8000000000000000000000000000000000000000000000000000000000000000",
            NonceHex = "0000000000000000",
            KeystreamHex =
                "e3be8fdd8beca2e3ea8ef9475b29a6e7003951e1097a5c38d23b7a5fad9f6844" +
                "b22c97559e2723c7cbbd3fe4fc8d9a0744652a83e72a9c461876af4d7ef1a117",
        },
        new Salsa20KeystreamKat
        {
            Name = "eSTREAM Salsa20/256 KEY0 IV1 (first 64 bytes)",
            Source = "eSTREAM verified vectors",
            KeySizeBits = 256,
            KeyHex = "0000000000000000000000000000000000000000000000000000000000000000",
            NonceHex = "8000000000000000",
            KeystreamHex =
                "2aba3dc45b4947007b14c851cd694456b303ad59a465662803006705673d6c3e" +
                "29f1d3510dfc0405463c03414e0e07e359f1f1816c68b2434a19d3eee0464873",
        },
        new Salsa20KeystreamKat
        {
            Name = "ECRYPT Salsa20/128 Set 1 vector 0 (first 64 bytes)",
            Source = "Crypto++ TestVectors/salsa.txt (ECRYPT Set 1 #0)",
            KeySizeBits = 128,
            KeyHex = "80000000000000000000000000000000",
            NonceHex = "0000000000000000",
            KeystreamHex =
                "4dfa5e481da23ea09a31022050859936da52fcee218005164f267cb65f5cfd7f" +
                "2b4f97e0ff16924a52df269515110a07f9e460bc65ef95da58f740b7d1dbb0aa",
        },
    ];

    /// <summary>
    /// Yields the Salsa20 keystream known-answer vectors as <see cref="DynamicDataAttribute" /> rows.
    /// </summary>
    /// <returns>One row per vector.</returns>
    private static IEnumerable<object[]> Salsa20KeystreamKatData()
    {
        foreach (Salsa20KeystreamKat kat in KeystreamKnownAnswers)
            yield return new object[] { kat.KeySizeBits, kat.KeyHex, kat.NonceHex, kat.KeystreamHex, kat.Name };
    }

    /// <summary>
    /// Produces a human-readable display name for a Salsa20 KAT row.
    /// </summary>
    /// <param name="testMethod">The executing test method.</param>
    /// <param name="data">The row data; the final element carries the scenario name.</param>
    /// <returns>The test method name followed by the row's scenario label.</returns>
    public static string GetKatDisplayName(MethodInfo testMethod, object[] data) =>
        $"{testMethod.Name} — {data[^1]}";

    /// <summary>
    /// Verifies that <see cref="Salsa20" /> reproduces each published keystream vector, recovered as the ciphertext of
    /// an all-zero plaintext, for both 128-bit and 256-bit keys.
    /// </summary>
    /// <param name="keySizeBits">The key size, in bits.</param>
    /// <param name="keyHex">The key, in hex.</param>
    /// <param name="nonceHex">The 64-bit nonce, in hex.</param>
    /// <param name="keystreamHex">The expected keystream, in hex.</param>
    /// <param name="displayName">The scenario label.</param>
    [TestMethod]
    [DynamicData(nameof(Salsa20KeystreamKatData), DynamicDataDisplayName = nameof(GetKatDisplayName))]
    public void CreateEncryptor_WhenGivenKeystreamVector_ShouldMatchExpected(
        int keySizeBits, string keyHex, string nonceHex, string keystreamHex, string displayName)
    {
        byte[] key = Convert.FromHexString(keyHex);
        byte[] nonce = Convert.FromHexString(nonceHex);
        byte[] expected = Convert.FromHexString(keystreamHex);
        byte[] zeros = new byte[expected.Length];

        using var cipher = new Salsa20 { KeySize = keySizeBits };
        using ICryptoTransform encryptor = cipher.CreateEncryptor(key, nonce);
        byte[] keystream = encryptor.TransformFinalBlock(zeros, 0, zeros.Length);

        CollectionAssert.AreEqual(expected, keystream, $"Salsa20 keystream mismatch for {displayName}.");
    }

    /// <summary>
    /// Verifies that <see cref="Salsa20" /> reproduces the eSTREAM long-stream digest vector — the XOR of all 2,048
    /// keystream blocks over 131,072 bytes — confirming correct 64-bit block-counter progression across many blocks.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void CreateEncryptor_WhenGeneratingLongStream_ShouldMatchEcryptXorDigest()
    {
        byte[] key = Convert.FromHexString("0053a6f94c9ff24598eb3e91e4378add3083d6297ccf2275c81b6ec11467ba0d");
        byte[] nonce = Convert.FromHexString("0d74db42a91077de");
        byte[] expected = Convert.FromHexString(
            "c349b6a51a3ec9b712eaed3f90d8bcee69b7628645f251a996f55260c62ef31f" +
            "d6c6b0aea94e136c9d984ad2df3578f78e457527b03a0450580dd874f63b1ab9");

        const int total = 131072;
        byte[] keystream;
        using (var cipher = new Salsa20())
        using (ICryptoTransform encryptor = cipher.CreateEncryptor(key, nonce))
            keystream = encryptor.TransformFinalBlock(new byte[total], 0, total);

        byte[] digest = new byte[64];
        for (int offset = 0; offset < total; offset += 64)
            for (int i = 0; i < 64; i++)
                digest[i] ^= keystream[offset + i];

        CollectionAssert.AreEqual(expected, digest, "Salsa20 long-stream XOR digest mismatch.");
    }
}
