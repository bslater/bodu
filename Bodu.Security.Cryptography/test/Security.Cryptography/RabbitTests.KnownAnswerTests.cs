// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RabbitTests.KnownAnswerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------


using System.Security.Cryptography;
using Bodu.Security.Cryptography.Infrastructure;
using Bodu.Test.Kat;
using static Bodu.Security.Cryptography.Infrastructure.KatBytes;

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
        };

    // ── RFC 4503 Appendix A keystream conformance vectors (I2OSP / big-endian) ────────────────
    // An empty Nonce marks the without-IV setup (Appendix A.1); a populated Nonce is the with-IV setup (Appendix A.2).
    private static readonly StreamCipherKnownAnswer[] KeystreamKnownAnswers =
    [
        new StreamCipherKnownAnswer
        {
            Name = "RFC 4503 Appendix A.1 no-IV zero key",
            Provenance = KatProvenance.Rfc("RFC 4503 Appendix A.1"),
            Key = Hex("00000000000000000000000000000000"),
            Nonce = [],
            IsKeystream = true,
            Plaintext = [],
            Ciphertext = Hex(
                "B15754F036A5D6ECF56B45261C4AF702" +
                "88E8D815C59C0C397B696C4789C68AA7" +
                "F416A1C3700CD451DA68D1881673D696"),
        },
        new StreamCipherKnownAnswer
        {
            // The Appendix B.1 printed key has a typo (byte 2 = ED); the expanded state proves it is 3D, and this
            // corrected key reproduces both the Appendix A.1 second vector and the Appendix B.1 output.
            Name = "RFC 4503 Appendix A.1 no-IV key 912813292E3D36FE3BFC62F1DC51C3AC (corrected B.1 key byte 3D)",
            Provenance = KatProvenance.Rfc("RFC 4503 Appendix A.1"),
            Key = Hex("912813292E3D36FE3BFC62F1DC51C3AC"),
            Nonce = [],
            IsKeystream = true,
            Plaintext = [],
            Ciphertext = Hex(
                "3D2DF3C83EF627A1E97FC38487E2519C" +
                "F576CD61F4405B8896BF53AA8554FC19" +
                "E5547473FBDB43508AE53B20204D4C5E"),
        },
        new StreamCipherKnownAnswer
        {
            Name = "RFC 4503 Appendix A.1 no-IV key 8395741587E0C733E9E9AB01C09B0043",
            Provenance = KatProvenance.Rfc("RFC 4503 Appendix A.1"),
            Key = Hex("8395741587E0C733E9E9AB01C09B0043"),
            Nonce = [],
            IsKeystream = true,
            Plaintext = [],
            Ciphertext = Hex(
                "0CB10DCDA041CDAC32EB5CFD02D0609B" +
                "95FC9FCA0F17015A7B7092114CFF3EAD" +
                "9649E5DE8BFC7F3F924147AD3A947428"),
        },
        new StreamCipherKnownAnswer
        {
            Name = "RFC 4503 Appendix A.2 zero key zero IV",
            Provenance = KatProvenance.Rfc("RFC 4503 Appendix A.2"),
            Key = Hex("00000000000000000000000000000000"),
            Nonce = Hex("0000000000000000"),
            IsKeystream = true,
            Plaintext = [],
            Ciphertext = Hex(
                "C6A7275EF85495D87CCD5D376705B7ED" +
                "5F29A6AC04F5EFD47B8F293270DC4A8D" +
                "2ADE822B29DE6C1EE52BDB8A47BF8F66"),
        },
        new StreamCipherKnownAnswer
        {
            Name = "RFC 4503 Appendix A.2 zero key IV C373F575C1267E59",
            Provenance = KatProvenance.Rfc("RFC 4503 Appendix A.2"),
            Key = Hex("00000000000000000000000000000000"),
            Nonce = Hex("C373F575C1267E59"),
            IsKeystream = true,
            Plaintext = [],
            Ciphertext = Hex(
                "1FCD4EB9580012E2E0DCCC9222017D6D" +
                "A75F4E10D12125017B2499FFED936F2E" +
                "EBC112C393E738392356BDD012029BA7"),
        },
        new StreamCipherKnownAnswer
        {
            Name = "RFC 4503 Appendix A.2 zero key IV A6EB561AD2F41727",
            Provenance = KatProvenance.Rfc("RFC 4503 Appendix A.2"),
            Key = Hex("00000000000000000000000000000000"),
            Nonce = Hex("A6EB561AD2F41727"),
            IsKeystream = true,
            Plaintext = [],
            Ciphertext = Hex(
                "445AD8C805858DBF70B6AF23A151104D" +
                "96C8F27947F42C5BAEAE67C6ACC35B03" +
                "9FCBFC895FA71C17313DF034F01551CB"),
        },
    ];

    /// <summary>
    /// Yields the RFC 4503 Appendix A keystream vectors as <see cref="DynamicDataAttribute" /> rows.
    /// </summary>
    /// <returns>One row per vector.</returns>
    private static IEnumerable<object[]> RabbitKeystreamKatData()
    {
        foreach (StreamCipherKnownAnswer kat in KeystreamKnownAnswers)
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
