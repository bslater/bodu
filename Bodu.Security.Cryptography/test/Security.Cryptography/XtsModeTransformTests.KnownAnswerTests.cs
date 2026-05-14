// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XtsModeTransformTests.KnownAnswerTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Security.Cryptography;

public sealed partial class XtsModeTransformTests
{
    // ── IEEE Std 1619-2007 Section 7 / NIST SP 800-38E — AES-128-XTS ──────────────────────────
    //
    // Each vector provides: Key1 (data cipher), Key2 (tweak cipher), sector number (tweak),
    // plaintext, and expected ciphertext. All are 16-byte (128-bit) blocks.
    //
    // Vector 1 (Section 7, first entry):
    //   Key1 = 00000000000000000000000000000000
    //   Key2 = 00000000000000000000000000000000
    //   Sector (tweak) = 00000000000000000000000000000000
    //   PT   = 00000000000000000000000000000000
    //   CT   = 917cf69ebd68b2ec9b9fe9a3eadda692
    //
    // Vector 2 (second entry):
    //   Key1 = 11111111111111111111111111111111
    //   Key2 = 22222222222222222222222222222222
    //   Sector (tweak, LE 64-bit sector#=0x3333333333) = 33333333330000000000000000000000
    //   PT   = 4444444444444444444444444444444444444444444444444444444444444444
    //   CT   = d75b96e7429fbf9f6b6d5e9c2bbb4a4c (two blocks)
    //          Wait — use the verified IEEE vector below.
    //
    // Note: sector number is stored as 128-bit little-endian (low 8 bytes = sector index LE64).

    private static IEnumerable<object[]> XtsKatVectors()
    {
        // IEEE 1619-2007 Vector 1: both keys all-zero, sector 0, plaintext all-zero (16 bytes).
        yield return new object[]
        {
            "00000000000000000000000000000000", // Key1 (dataCipher)
            "00000000000000000000000000000000", // Key2 (tweakCipher)
            "00000000000000000000000000000000", // sector number as 128-bit LE
            "00000000000000000000000000000000", // plaintext
            "917cf69ebd68b2ec9b9fe9a3eadda692"  // expected ciphertext
        };
        // IEEE 1619-2007 Vector 2: distinct keys, sector 0x3333333333.
        // Sector as 128-bit LE: 33 33 33 33 33 00 00 00 00 00 00 00 00 00 00 00
        yield return new object[]
        {
            "11111111111111111111111111111111",
            "22222222222222222222222222222222",
            "33333333330000000000000000000000",
            "44444444444444444444444444444444",
            "c454185e6a16936e39334038acef838b"
        };
    }

    /// <summary>
    /// Verifies that <see cref="XtsModeTransform.Transform" />, with Ieee1619Vector, returns the expected value.
    /// </summary>
    [TestMethod]

    [DynamicData(nameof(XtsKatVectors))]
    public void Transform_WithIeee1619Vector_ShouldEncryptCorrectly(
        string key1Hex, string key2Hex, string tweakHex, string ptHex, string expectedCtHex)
    {
        using var dataCipher = new AesBlockCipherFixture(Convert.FromHexString(key1Hex));
        using var tweakCipher = new AesBlockCipherFixture(Convert.FromHexString(key2Hex));
        var tweak = Convert.FromHexString(tweakHex);
        var plaintext = Convert.FromHexString(ptHex);
        var expected = Convert.FromHexString(expectedCtHex);

        var transform = new XtsModeTransform(dataCipher, tweakCipher, tweak);
        var output = new byte[plaintext.Length];
        transform.Transform(plaintext, output, encrypt: true);

        CollectionAssert.AreEqual(expected, output,
            $"XTS encrypt mismatch for IEEE 1619 vector (Key1={key1Hex[..8]}…).");
    }

    /// <summary>
    /// Verifies that <see cref="XtsModeTransform.Transform" />, with Ieee1619Vector, returns the expected value.
    /// </summary>
    [TestMethod]

    [DynamicData(nameof(XtsKatVectors))]
    public void Transform_WithIeee1619Vector_ShouldDecryptToOriginalPlaintext(
        string key1Hex, string key2Hex, string tweakHex, string ptHex, string expectedCtHex)
    {
        using var dataCipher = new AesBlockCipherFixture(Convert.FromHexString(key1Hex));
        using var tweakCipher = new AesBlockCipherFixture(Convert.FromHexString(key2Hex));
        var tweak = Convert.FromHexString(tweakHex);
        var ciphertext = Convert.FromHexString(expectedCtHex);
        var expected = Convert.FromHexString(ptHex);

        var transform = new XtsModeTransform(dataCipher, tweakCipher, tweak);
        var output = new byte[ciphertext.Length];
        transform.Transform(ciphertext, output, encrypt: false);

        CollectionAssert.AreEqual(expected, output,
            $"XTS decrypt mismatch for IEEE 1619 vector (Key1={key1Hex[..8]}…).");
    }
}
