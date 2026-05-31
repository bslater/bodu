// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RabbitTests.KnownAnswerTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

using System.Reflection;
using System.Security.Cryptography;
using Bodu.Security.Cryptography.Contracts;

/// <summary>
/// Locks the <see cref="Rabbit" /> stream cipher against the published RFC 4503 Appendix A conformance vectors and
/// Appendix B internal-state debugging vectors, and inherits the shared
/// <see cref="SymmetricStreamAlgorithmContractTests{TCipher}" /> behavioural contract.
/// </summary>
/// <remarks>
/// RFC 4503 octet strings follow the I2OSP (big-endian) convention: the key and IV are big-endian integers and each
/// 16-byte keystream block <c>S[i]</c> is the big-endian representation of the extraction value. These vectors are
/// therefore byte-reversed relative to the self-consistent little-endian convention used by some implementations
/// (Crypto++, libtomcrypt).
/// </remarks>
[TestClass]
public sealed partial class RabbitTests
    : SymmetricStreamAlgorithmContractTests<Rabbit>
{
    /// <inheritdoc />
    protected override int ExpectedKeySizeBits => 128;

    /// <inheritdoc />
    protected override int ExpectedNonceSizeBits => 64;

    /// <summary>
    /// Represents one RFC 4503 keystream conformance vector (Appendix A), recovered as the ciphertext of an all-zero
    /// plaintext.
    /// </summary>
    private sealed record RabbitKeystreamKat
    {
        public required string Name { get; init; }

        public required string Section { get; init; }

        public required string KeyHex { get; init; }

        public string? IvHex { get; init; }

        public required string KeystreamHex { get; init; }
    }

    // ── RFC 4503 Appendix A keystream conformance vectors (I2OSP / big-endian) ────────────────
    private static readonly RabbitKeystreamKat[] KeystreamKnownAnswers =
    [
        new RabbitKeystreamKat
        {
            Name = "RFC 4503 Appendix A.1 no-IV zero key",
            Section = "Appendix A.1",
            KeyHex = "00000000000000000000000000000000",
            IvHex = null,
            KeystreamHex =
                "B15754F036A5D6ECF56B45261C4AF702" +
                "88E8D815C59C0C397B696C4789C68AA7" +
                "F416A1C3700CD451DA68D1881673D696",
        },
        new RabbitKeystreamKat
        {
            // The Appendix B.1 printed key has a typo (byte 2 = ED); the expanded state proves it is 3D, and this
            // corrected key reproduces both the Appendix A.1 second vector and the Appendix B.1 output.
            Name = "RFC 4503 Appendix A.1 no-IV key 912813292E3D36FE3BFC62F1DC51C3AC (corrected B.1 key byte 3D)",
            Section = "Appendix A.1",
            KeyHex = "912813292E3D36FE3BFC62F1DC51C3AC",
            IvHex = null,
            KeystreamHex =
                "3D2DF3C83EF627A1E97FC38487E2519C" +
                "F576CD61F4405B8896BF53AA8554FC19" +
                "E5547473FBDB43508AE53B20204D4C5E",
        },
        new RabbitKeystreamKat
        {
            Name = "RFC 4503 Appendix A.1 no-IV key 8395741587E0C733E9E9AB01C09B0043",
            Section = "Appendix A.1",
            KeyHex = "8395741587E0C733E9E9AB01C09B0043",
            IvHex = null,
            KeystreamHex =
                "0CB10DCDA041CDAC32EB5CFD02D0609B" +
                "95FC9FCA0F17015A7B7092114CFF3EAD" +
                "9649E5DE8BFC7F3F924147AD3A947428",
        },
        new RabbitKeystreamKat
        {
            Name = "RFC 4503 Appendix A.2 zero key zero IV",
            Section = "Appendix A.2",
            KeyHex = "00000000000000000000000000000000",
            IvHex = "0000000000000000",
            KeystreamHex =
                "C6A7275EF85495D87CCD5D376705B7ED" +
                "5F29A6AC04F5EFD47B8F293270DC4A8D" +
                "2ADE822B29DE6C1EE52BDB8A47BF8F66",
        },
        new RabbitKeystreamKat
        {
            Name = "RFC 4503 Appendix A.2 zero key IV C373F575C1267E59",
            Section = "Appendix A.2",
            KeyHex = "00000000000000000000000000000000",
            IvHex = "C373F575C1267E59",
            KeystreamHex =
                "1FCD4EB9580012E2E0DCCC9222017D6D" +
                "A75F4E10D12125017B2499FFED936F2E" +
                "EBC112C393E738392356BDD012029BA7",
        },
        new RabbitKeystreamKat
        {
            Name = "RFC 4503 Appendix A.2 zero key IV A6EB561AD2F41727",
            Section = "Appendix A.2",
            KeyHex = "00000000000000000000000000000000",
            IvHex = "A6EB561AD2F41727",
            KeystreamHex =
                "445AD8C805858DBF70B6AF23A151104D" +
                "96C8F27947F42C5BAEAE67C6ACC35B03" +
                "9FCBFC895FA71C17313DF034F01551CB",
        },
    ];

    /// <summary>
    /// Yields the RFC 4503 Appendix A keystream vectors as <see cref="DynamicDataAttribute" /> rows.
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
    /// Verifies that <see cref="Rabbit" /> reproduces each RFC 4503 Appendix A keystream vector, recovered as the
    /// ciphertext of an all-zero plaintext, for both the with-IV and (via the engine) the without-IV setups.
    /// </summary>
    /// <param name="keyHex">The 128-bit key, in hex.</param>
    /// <param name="ivHex">The 64-bit IV, in hex, or <see langword="null" /> for the without-IV setup.</param>
    /// <param name="keystreamHex">The expected keystream, in hex.</param>
    /// <param name="displayName">The scenario label.</param>
    [TestMethod]
    [DynamicData(nameof(RabbitKeystreamKatData), DynamicDataDisplayName = nameof(GetKatDisplayName))]
    public void Keystream_WhenGivenRfc4503AppendixAVector_ShouldMatchExpected(
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
