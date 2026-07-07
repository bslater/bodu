// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RabbitTests.KnownAnswerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------


using System.Security.Cryptography;
using Bodu.Security.Cryptography.Infrastructure;
using Bodu.Test.Kat;

namespace Bodu.Security.Cryptography;
/// <summary>
/// Locks the <see cref="Rabbit" /> stream cipher against the published RFC 4503 Appendix A conformance vectors and
/// Appendix B internal-state debugging vectors, and inherits the shared
/// <see cref="SymmetricStreamAlgorithmTests{TTest, TAlgorithm}" /> behavioural contract.
/// </summary>
/// <remarks>
/// RFC 4503 octet strings follow the I2OSP (big-endian) convention: the key and IV are big-endian integers and each
/// 16-byte keystream block <c>S[i]</c> is the big-endian representation of the extraction value. These vectors are
/// therefore byte-reversed relative to the self-consistent little-endian convention used by some implementations
/// (Crypto++, libtomcrypt).
/// </remarks>
[TestClass]
public sealed partial class RabbitTests
    : SymmetricStreamAlgorithmTests<RabbitTests, Rabbit>
{
    /// <inheritdoc />
    protected override SymmetricStreamAlgorithmSpecification GetSpecification() =>
        new()
        {
            DefaultKeySizeBits = 128,
            NonceSizeBits = 64,
            LegalKeySizesBits = [128],
            KnownAnswers = KeystreamKnownAnswers,
        };

    // ── RFC 4503 Appendix A keystream conformance vectors (I2OSP / big-endian) ────────────────
    // Loaded dynamically from the embedded RFC 4503 Appendix A octet-form vector file. An empty Nonce marks the
    // without-IV setup (Appendix A.1); a populated Nonce is the with-IV setup (Appendix A.2). Appendix A prints the
    // second A.1 key correctly as 91 28 13 29 2E 3D 36 FE ... (the ED typo is confined to the Appendix B.1 walk-through).
    private const string RabbitRfc4503ResourceName = "Bodu.Security.Cryptography.Rabbit.rfc4503-appendix-a.txt";

    private static readonly StreamCipherKnownAnswer[] KeystreamKnownAnswers = LoadKeystreamKnownAnswers();

    /// <summary>Loads the RFC 4503 Appendix A keystream vectors from the embedded octet-form vector file.</summary>
    /// <returns>Every Appendix A.1 and A.2 keystream vector.</returns>
    /// <exception cref="InvalidOperationException">The embedded resource cannot be located.</exception>
    private static StreamCipherKnownAnswer[] LoadKeystreamKnownAnswers()
    {
        using Stream stream = typeof(RabbitTests).Assembly.GetManifestResourceStream(RabbitRfc4503ResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{RabbitRfc4503ResourceName}' is missing.");

        return [.. Rfc4503RabbitKatReader.Read(stream)];
    }

    /// <summary>
    /// Yields the RFC 4503 Appendix A keystream vectors as <see cref="DynamicDataAttribute" /> rows.
    /// </summary>
    /// <returns>One row per vector.</returns>
    private static IEnumerable<object[]> RabbitKeystreamKatData()
    {
        foreach (StreamCipherKnownAnswer kat in new RabbitTests().GetSpecification().KnownAnswers)
            yield return new object[] { kat };
    }

    /// <summary>
    /// Verifies that <see cref="Rabbit" /> reproduces each RFC 4503 Appendix A keystream vector, recovered as the
    /// ciphertext of an all-zero plaintext, for both the with-IV and (via the engine) the without-IV setups.
    /// </summary>
    /// <param name="vector">The keystream KAT vector under test.</param>
    [TestMethod]
    [DynamicData(
        nameof(RabbitKeystreamKatData),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Keystream_WhenGivenRfc4503AppendixAVector_ShouldMatchExpected(StreamCipherKnownAnswer vector)
    {
        byte[] expected = vector.Ciphertext;

        byte[] keystream;
        if (vector.Nonce.Length == 0)
        {
            // Without-IV setup (RFC 4503 A.1): drive the engine directly, since the public surface always applies an IV.
            using var engine = new RabbitStreamCipher(vector.Key);
            keystream = new byte[expected.Length];
            for (int offset = 0; offset < keystream.Length; offset += RabbitStreamCipher.BlockSizeBytes)
                engine.NextKeystreamBlock(keystream.AsSpan(offset, RabbitStreamCipher.BlockSizeBytes));
        }
        else
        {
            using var cipher = new Rabbit();
            using ICryptoTransform encryptor = cipher.CreateEncryptor(vector.Key, vector.Nonce);
            keystream = encryptor.TransformFinalBlock(new byte[expected.Length], 0, expected.Length);
        }

        CollectionAssert.AreEqual(expected, keystream, $"Rabbit keystream mismatch for {vector.Name}.");
    }
}
