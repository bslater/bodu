// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Hc128Tests.KnownAnswerTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

using System.Reflection;
using System.Security.Cryptography;

/// <summary>
/// Locks the <see cref="Hc128" /> stream cipher against the published eSTREAM known-answer test vectors, and inherits
/// the shared <see cref="StreamCipherAlgorithmTests{TTest, TAlgorithm}" /> behavioural contract.
/// </summary>
[TestClass]
public sealed class Hc128Tests
    : StreamCipherAlgorithmTests<Hc128Tests, Hc128>
{
    /// <summary>
    /// Represents one HC-128 keystream known-answer test row, recovered as the ciphertext of an all-zero plaintext.
    /// </summary>
    private sealed record Hc128KeystreamKat
    {
        public required string Name { get; init; }

        public required string KeyHex { get; init; }

        public required string IvHex { get; init; }

        public required string KeystreamHex { get; init; }
    }

    // ── eSTREAM HC-128 keystream known-answer tests ──────────────────────────────────────────
    //
    // Source: Hongjun Wu's HC-128 eSTREAM submission test vectors, reproduced by Crypto++. The key = 0 / IV = 0
    // S[0] block matches the canonical published vector byte-for-byte.
    private static readonly Hc128KeystreamKat[] KeystreamKnownAnswers =
    [
        new Hc128KeystreamKat
        {
            Name = "eSTREAM HC-128 key = 0, IV = 0 (64 bytes)",
            KeyHex = "00000000000000000000000000000000",
            IvHex = "00000000000000000000000000000000",
            KeystreamHex =
                "82001573a003fd3b7fd72ffb0eaf63aac62f12deb629dca72785a66268ec758b" +
                "1edb36900560898178e0ad009abf1f491330dc1c246e3d6cb264f6900271d59c",
        },
        new Hc128KeystreamKat
        {
            Name = "eSTREAM HC-128 key = 0x80.., IV = 0 (64 bytes)",
            KeyHex = "80000000000000000000000000000000",
            IvHex = "00000000000000000000000000000000",
            KeystreamHex =
                "378602b98f32a74847515654ae0de7ed8f72bc34776a065103e51595521ffe47" +
                "f9af0a4cb47999cfa26d33bf809545989d53debfe7a9efd8b9109ca6efaddf83",
        },
    ];

    /// <summary>
    /// Yields the HC-128 keystream known-answer vectors as <see cref="DynamicDataAttribute" /> rows.
    /// </summary>
    /// <returns>One row per vector.</returns>
    private static IEnumerable<object[]> Hc128KeystreamKatData()
    {
        foreach (Hc128KeystreamKat kat in KeystreamKnownAnswers)
            yield return new object[] { kat.KeyHex, kat.IvHex, kat.KeystreamHex, kat.Name };
    }

    /// <summary>
    /// Produces a human-readable display name for an HC-128 KAT row.
    /// </summary>
    /// <param name="testMethod">The executing test method.</param>
    /// <param name="data">The row data; the final element carries the scenario name.</param>
    /// <returns>The test method name followed by the row's scenario label.</returns>
    public static string GetKatDisplayName(MethodInfo testMethod, object[] data) =>
        $"{testMethod.Name} — {data[^1]}";

    /// <summary>
    /// Verifies that <see cref="Hc128" /> reproduces each published eSTREAM keystream vector, recovered as the
    /// ciphertext of an all-zero plaintext.
    /// </summary>
    /// <param name="keyHex">The 128-bit key, in hex.</param>
    /// <param name="ivHex">The 128-bit IV, in hex.</param>
    /// <param name="keystreamHex">The expected keystream, in hex.</param>
    /// <param name="displayName">The scenario label.</param>
    [TestMethod]
    [DynamicData(nameof(Hc128KeystreamKatData), DynamicDataDisplayName = nameof(GetKatDisplayName))]
    public void CreateEncryptor_WhenGivenEstreamVector_ShouldMatchExpected(
        string keyHex, string ivHex, string keystreamHex, string displayName)
    {
        byte[] key = Convert.FromHexString(keyHex);
        byte[] iv = Convert.FromHexString(ivHex);
        byte[] expected = Convert.FromHexString(keystreamHex);

        using var cipher = new Hc128();
        using ICryptoTransform encryptor = cipher.CreateEncryptor(key, iv);
        byte[] keystream = encryptor.TransformFinalBlock(new byte[expected.Length], 0, expected.Length);

        CollectionAssert.AreEqual(expected, keystream, $"HC-128 keystream mismatch for {displayName}.");
    }
}
