// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XtsModeTransformTests.KnownAnswerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Security.Cryptography.Infrastructure;
using Bodu.Test.Kat;
using static Bodu.Security.Cryptography.Infrastructure.KatBytes;

namespace Bodu.Security.Cryptography;

public sealed partial class XtsModeTransformTests
{
    // ── IEEE Std 1619-2007 Section 7 / NIST SP 800-38E - AES-128-XTS ──────────────────────────
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
    //          Wait - use the verified IEEE vector below.
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
        byte[] tweak = Convert.FromHexString(tweakHex);
        byte[] plaintext = Convert.FromHexString(ptHex);
        byte[] expected = Convert.FromHexString(expectedCtHex);

        var transform = new XtsModeTransform(dataCipher, tweakCipher, tweak);
        byte[] output = new byte[plaintext.Length];
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
        byte[] tweak = Convert.FromHexString(tweakHex);
        byte[] ciphertext = Convert.FromHexString(expectedCtHex);
        byte[] expected = Convert.FromHexString(ptHex);

        var transform = new XtsModeTransform(dataCipher, tweakCipher, tweak);
        byte[] output = new byte[ciphertext.Length];
        transform.Transform(ciphertext, output, encrypt: false);

        CollectionAssert.AreEqual(expected, output,
            $"XTS decrypt mismatch for IEEE 1619 vector (Key1={key1Hex[..8]}…).");
    }

    /// <summary>The manifest resource name of the IEEE 1619-2007 vectors reproduced by OpenSSL.</summary>
    private const string Ieee1619ResourceName = "Bodu.Security.Cryptography.Xts.ieee1619-2007-xts.txt";

    /// <summary>
    /// Loads every whole-block IEEE 1619-2007 vector - vectors 1 to 14 and 19, AES-128 and AES-256, with data units of
    /// 32 and 512 bytes - as <see cref="DynamicDataAttribute" /> rows. <see cref="KeyedKnownAnswer.Key" /> holds
    /// <c>Key1 || Key2</c> as the standard presents the double-length key, and <see cref="BlockCipherKnownAnswer.Tweak" />
    /// the data unit's tweak.
    /// </summary>
    /// <returns>One row per vector, each holding a single <see cref="BlockCipherKnownAnswer" />.</returns>
    /// <exception cref="InvalidOperationException">The embedded vector file cannot be located.</exception>
    private static IEnumerable<object[]> Ieee1619DataUnitVectors()
    {
        using Stream stream = typeof(XtsModeTransformTests).Assembly.GetManifestResourceStream(Ieee1619ResourceName)
            ?? throw new InvalidOperationException(
                $"Embedded resource '{Ieee1619ResourceName}' is not present in the test assembly. " +
                "Check the <EmbeddedResource> entry in Bodu.Security.Cryptography.Test.csproj.");

        foreach (Dictionary<string, string> record in HexFieldKatReader.Read(stream))
        {
            byte[] plaintext = Hex(HexFieldKatReader.GetRequired(record, "Plaintext"));
            yield return new object[]
            {
                new BlockCipherKnownAnswer
                {
                    Name = $"IEEE 1619-2007 Vector {HexFieldKatReader.GetRequired(record, "Vector")} " +
                        $"({HexFieldKatReader.GetRequired(record, "Cipher")}, {plaintext.Length}-byte data unit)",
                    Provenance = KatProvenance.Standard("IEEE Std 1619-2007, via OpenSSL 3.0.13 evpciph_aes_common.txt"),
                    Key = Hex(HexFieldKatReader.GetRequired(record, "Key")),
                    Tweak = Hex(HexFieldKatReader.GetRequired(record, "IV")),
                    Plaintext = plaintext,
                    Ciphertext = Hex(HexFieldKatReader.GetRequired(record, "Ciphertext")),
                },
            };
        }
    }

    /// <summary>
    /// Verifies that encrypting a whole IEEE 1619-2007 data unit reproduces the published ciphertext. The 512-byte
    /// units double the tweak 31 times, through every shift that carries out of the tweak and needs reduction.
    /// </summary>
    /// <param name="vector">The vector under test.</param>
    [TestMethod]
    [DynamicData(
        nameof(Ieee1619DataUnitVectors),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Transform_WhenEncryptingIeee1619DataUnit_ShouldMatchPublishedCiphertext(BlockCipherKnownAnswer vector)
    {
        byte[] output = TransformDataUnit(vector, vector.Plaintext, encrypt: true);

        CollectionAssert.AreEqual(vector.Ciphertext, output, $"XTS encrypt mismatch for {vector.Name}.");
    }

    /// <summary>
    /// Verifies that decrypting a whole IEEE 1619-2007 data unit recovers the published plaintext.
    /// </summary>
    /// <param name="vector">The vector under test.</param>
    [TestMethod]
    [DynamicData(
        nameof(Ieee1619DataUnitVectors),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Transform_WhenDecryptingIeee1619DataUnit_ShouldRecoverPublishedPlaintext(BlockCipherKnownAnswer vector)
    {
        byte[] output = TransformDataUnit(vector, vector.Ciphertext, encrypt: false);

        CollectionAssert.AreEqual(vector.Plaintext, output, $"XTS decrypt mismatch for {vector.Name}.");
    }

    /// <summary>
    /// Runs one data unit through <see cref="XtsModeTransform" />, splitting the vector's double-length key into the
    /// data key and the tweak key.
    /// </summary>
    /// <param name="vector">The vector supplying the keys and the tweak.</param>
    /// <param name="input">The data unit to transform.</param>
    /// <param name="encrypt"><see langword="true" /> to encrypt; <see langword="false" /> to decrypt.</param>
    /// <returns>The transformed data unit.</returns>
    private static byte[] TransformDataUnit(BlockCipherKnownAnswer vector, byte[] input, bool encrypt)
    {
        byte[] key = vector.Key!;
        using var dataCipher = new AesBlockCipher(key[..(key.Length / 2)]);
        using var tweakCipher = new AesBlockCipher(key[(key.Length / 2)..]);
        using var transform = new XtsModeTransform(dataCipher, tweakCipher, vector.Tweak!);

        byte[] output = new byte[input.Length];
        transform.Transform(input, output, encrypt);
        return output;
    }
}
