// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Hc128Tests.KnownAnswerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------


using System.Buffers.Binary;
using System.Security.Cryptography;
using Bodu.Security.Cryptography.Infrastructure;
using Bodu.Test.Kat;

namespace Bodu.Security.Cryptography;
/// <summary>
/// Locks the <see cref="Hc128" /> stream cipher against the official Appendix A test vectors from Hongjun Wu's
/// specification paper <c>The Stream Cipher HC-128</c> (the eSTREAM-author vectors; HC-128 has no RFC), and inherits the
/// shared <see cref="SymmetricStreamAlgorithmTests{TTest, TAlgorithm}" /> behavioural contract.
/// </summary>
/// <remarks>
/// HC-128 serializes each 32-bit output word least-significant-byte first, so a printed word such as <c>73150082</c>
/// appears in the byte keystream as <c>82 00 15 73</c>. The vectors below carry the expected byte keystream directly.
/// </remarks>
[TestClass]
public sealed partial class Hc128Tests
    : SymmetricStreamAlgorithmTests<Hc128Tests, Hc128>
{
    /// <inheritdoc />
    protected override SymmetricStreamAlgorithmSpecification GetSpecification() =>
        new()
        {
            DefaultKeySizeBits = 128,
            NonceSizeBits = 128,
            LegalKeySizesBits = [128],
            KnownAnswers = KeystreamKnownAnswers,
        };

    // ── HC-128 specification Appendix A keystream vectors (first 512 bits) ────────────────────
    //
    // Loaded dynamically from the embedded HC-128 specification Appendix A vector file (Hongjun Wu, "The Stream Cipher
    // HC-128"). Words are emitted little-endian per the spec, so the reader serializes each printed 32-bit word
    // little-endian to recover the byte keystream.
    private const string Hc128SpecResourceName = "Bodu.Security.Cryptography.Hc128.spec-appendix-a.txt";

    private static readonly StreamCipherKnownAnswer[] KeystreamKnownAnswers = LoadKeystreamKnownAnswers();

    /// <summary>Loads the HC-128 Appendix A keystream vectors from the embedded specification vector file.</summary>
    /// <returns>The three Appendix A keystream vectors.</returns>
    /// <exception cref="InvalidOperationException">The embedded resource cannot be located.</exception>
    private static StreamCipherKnownAnswer[] LoadKeystreamKnownAnswers()
    {
        using Stream stream = typeof(Hc128Tests).Assembly.GetManifestResourceStream(Hc128SpecResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{Hc128SpecResourceName}' is missing.");

        return [.. Hc128SpecKatReader.Read(stream)];
    }

    /// <summary>
    /// Yields the HC-128 keystream known-answer vectors as <see cref="DynamicDataAttribute" /> rows.
    /// </summary>
    /// <returns>One row per vector.</returns>
    private static IEnumerable<object[]> Hc128KeystreamKatData()
    {
        foreach (StreamCipherKnownAnswer kat in new Hc128Tests().GetSpecification().KnownAnswers)
            yield return new object[] { kat };
    }

    /// <summary>
    /// Verifies that <see cref="Hc128" /> reproduces each HC-128 specification Appendix A keystream vector, recovered as
    /// the ciphertext of an all-zero plaintext.
    /// </summary>
    /// <param name="vector">The keystream KAT vector under test.</param>
    [TestMethod]
    [DynamicData(
        nameof(Hc128KeystreamKatData),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void CreateEncryptor_WhenGivenSpecificationVector_ShouldMatchExpected(StreamCipherKnownAnswer vector)
    {
        byte[] expected = vector.Ciphertext;

        using var cipher = new Hc128();
        using ICryptoTransform encryptor = cipher.CreateEncryptor(vector.Key, vector.Nonce);
        byte[] keystream = encryptor.TransformFinalBlock(new byte[expected.Length], 0, expected.Length);

        CollectionAssert.AreEqual(expected, keystream, $"HC-128 keystream mismatch for {vector.Name}.");
    }

    /// <summary>
    /// Verifies that <see cref="Hc128" /> reproduces the HC-128 specification Appendix A.4 long-run accumulator: under
    /// an all-zero key and IV, <c>A[i] = XOR over j = 0..2^20-1 of word(16*j + i)</c> for <c>i = 0..15</c>. This
    /// exercises 2^20 keystream blocks (64 MiB), confirming correct long-stream generation well beyond the first block.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void CreateEncryptor_WhenGeneratingLongStream_ShouldMatchAppendixA4Accumulator()
    {
        // Expected accumulator words A[0..15] (HC-128 spec Appendix A.4), serialized little-endian per word.
        string[] expectedWords =
        [
            "a4eac026", "7e491126", "6a2a384f", "5c4e1329",
            "da407fa1", "55e6b1ae", "05c6fdf3", "bbdc8a86",
            "7a699aa0", "1a4dc117", "63658ccc", "d3e62474",
            "9cf8236f", "0131be21", "c3a51de9", "d12290de",
        ];

        uint[] expected = new uint[16];
        for (int i = 0; i < 16; i++)
            expected[i] = uint.Parse(expectedWords[i], System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture);

        byte[] key = new byte[16];
        byte[] iv = new byte[16];
        byte[] zero = new byte[64];
        byte[] block = new byte[64];
        uint[] accumulator = new uint[16];

        const int repetitions = 1 << 20;
        using var cipher = new Hc128();
        using ICryptoTransform encryptor = cipher.CreateEncryptor(key, iv);

        for (int j = 0; j < repetitions; j++)
        {
            encryptor.TransformBlock(zero, 0, 64, block, 0);
            for (int i = 0; i < 16; i++)
                accumulator[i] ^= BinaryPrimitives.ReadUInt32LittleEndian(block.AsSpan(i * 4, 4));
        }

        CollectionAssert.AreEqual(expected, accumulator, "HC-128 Appendix A.4 long-run accumulator mismatch.");
    }
}
