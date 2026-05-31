// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RabbitTests.KnownAnswerTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

using System.Reflection;
using System.Security.Cryptography;

/// <summary>
/// Locks the <see cref="Rabbit" /> stream cipher against the published RFC 4503 Appendix A known-answer test vectors
/// (little-endian octet convention, as used by the RFC reference implementation and reproduced by Crypto++ and
/// libtomcrypt), and inherits the shared <see cref="StreamCipherAlgorithmTests{TTest, TAlgorithm}" /> behavioural
/// contract.
/// </summary>
[TestClass]
public sealed class RabbitTests
    : StreamCipherAlgorithmTests<RabbitTests, Rabbit>
{
    /// <summary>
    /// Represents one Rabbit keystream known-answer test row, recovered as the ciphertext of an all-zero plaintext.
    /// </summary>
    private sealed record RabbitKeystreamKat
    {
        public required string Name { get; init; }

        public required string KeyHex { get; init; }

        public string? IvHex { get; init; }

        public required string KeystreamHex { get; init; }
    }

    // ── RFC 4503 Appendix A keystream known-answer tests ─────────────────────────────────────
    //
    // Source: RFC 4503 Appendix A.1 (without IV setup) and A.2 (with IV setup), confirmed against the Crypto++
    // and libtomcrypt reference implementations. The all-zero-key A.1 S[0] block matches the Crypto++ vector
    // byte-for-byte.
    private static readonly RabbitKeystreamKat[] KeystreamKnownAnswers =
    [
        new RabbitKeystreamKat
        {
            Name = "RFC 4503 A.1 without IV, key = 0 (48 bytes)",
            KeyHex = "00000000000000000000000000000000",
            IvHex = null,
            KeystreamHex =
                "02f74a1c26456bf5ecd6a536f05457b1a78ac689476c697b390c9cc515d8e888" +
                "96d6731688d168da51d40c70c3a116f4",
        },
        new RabbitKeystreamKat
        {
            Name = "RFC 4503 A.1 without IV, non-zero key (48 bytes)",
            KeyHex = "c21fcf3881cd5ee8628accb0a9890df8",
            IvHex = null,
            KeystreamHex =
                "3d02e0c730559112b473b790dee018dfcd6d730ce54e19f0c35ec4790eb6c74a" +
                "b0bb1bb7860a685abf9c8faf263cca09",
        },
        new RabbitKeystreamKat
        {
            Name = "RFC 4503 A.2 with IV, key = 0, IV = 0 (48 bytes)",
            KeyHex = "00000000000000000000000000000000",
            IvHex = "0000000000000000",
            KeystreamHex =
                "edb70567375dcd7cd89554f85e27a7c68d4adc7032298f7bd4eff504aca6295f" +
                "668fbf478adb2be51e6cde292b82de2a",
        },
        new RabbitKeystreamKat
        {
            Name = "RFC 4503 A.2 with IV, key = 0, IV = 597e26c175f573c3 (48 bytes)",
            KeyHex = "00000000000000000000000000000000",
            IvHex = "597e26c175f573c3",
            KeystreamHex =
                "6d7d012292ccdce0e2120058b94ecd1f2e6f93edff99247b012521d1104e5fa7" +
                "a79b0212d0bd56233938e793c312c1eb",
        },
    ];

    /// <summary>
    /// Yields the Rabbit keystream known-answer vectors as <see cref="DynamicDataAttribute" /> rows.
    /// </summary>
    /// <returns>One row per vector.</returns>
    private static IEnumerable<object?[]> RabbitKeystreamKatData()
    {
        foreach (RabbitKeystreamKat kat in KeystreamKnownAnswers)
            yield return new object?[] { kat.KeyHex, kat.IvHex, kat.KeystreamHex, kat.Name };
    }

    /// <summary>
    /// Produces a human-readable display name for a Rabbit KAT row.
    /// </summary>
    /// <param name="testMethod">The executing test method.</param>
    /// <param name="data">The row data; the final element carries the scenario name.</param>
    /// <returns>The test method name followed by the row's scenario label.</returns>
    public static string GetKatDisplayName(MethodInfo testMethod, object?[] data) =>
        $"{testMethod.Name} — {data[^1]}";

    /// <summary>
    /// Verifies that <see cref="Rabbit" /> reproduces each published RFC 4503 keystream vector, recovered as the
    /// ciphertext of an all-zero plaintext, for both the with-IV and (via the engine) the without-IV setups.
    /// </summary>
    /// <param name="keyHex">The 128-bit key, in hex.</param>
    /// <param name="ivHex">The 64-bit IV, in hex, or <see langword="null" /> for the without-IV setup.</param>
    /// <param name="keystreamHex">The expected keystream, in hex.</param>
    /// <param name="displayName">The scenario label.</param>
    [TestMethod]
    [DynamicData(nameof(RabbitKeystreamKatData), DynamicDataDisplayName = nameof(GetKatDisplayName))]
    public void Keystream_WhenGivenRfc4503Vector_ShouldMatchExpected(
        string keyHex, string? ivHex, string keystreamHex, string displayName)
    {
        byte[] key = Convert.FromHexString(keyHex);
        byte[] expected = Convert.FromHexString(keystreamHex);

        byte[] keystream;
        if (ivHex is null)
        {
            // Without-IV setup (RFC 4503 A.1): drive the engine directly, since the public surface always applies an IV.
            using var engine = new RabbitStreamCipher(key);
            keystream = new byte[expected.Length];
            for (int offset = 0; offset < keystream.Length; offset += RabbitStreamCipher.BlockSizeBytes)
                engine.NextKeystreamBlock(keystream.AsSpan(offset, RabbitStreamCipher.BlockSizeBytes));
        }
        else
        {
            byte[] iv = Convert.FromHexString(ivHex);
            using var cipher = new Rabbit();
            using ICryptoTransform encryptor = cipher.CreateEncryptor(key, iv);
            keystream = encryptor.TransformFinalBlock(new byte[expected.Length], 0, expected.Length);
        }

        CollectionAssert.AreEqual(expected, keystream, $"Rabbit keystream mismatch for {displayName}.");
    }
}
