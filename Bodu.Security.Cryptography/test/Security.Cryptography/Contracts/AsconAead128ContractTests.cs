// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsconAead128ContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Security.Cryptography.Infrastructure;

namespace Bodu.Security.Cryptography.Contracts;

/// <summary>
/// Drives <see cref="AeadContractTests{TAead}" /> against <see cref="AsconAead128" /> with a small
/// representative subset of the NIST SP 800-232 / ASCON-team <c>LWC_AEAD_KAT_128_128</c> vectors,
/// supplemented by every documented tamper-kind so authentication-failure assertions are exercised on
/// the shared contract surface. The full 1089-vector regression suite remains in
/// <c>AsconAead128Tests.KnownAnswerTests.cs</c>.
/// </summary>
[TestClass]
public sealed class AsconAead128ContractTests : AeadContractTests<AsconAead128>
{
    private static readonly byte[] s_referenceKey = Convert.FromHexString("000102030405060708090A0B0C0D0E0F");

    private static readonly byte[] s_referenceNonce = Convert.FromHexString("101112131415161718191A1B1C1D1E1F");

    /// <inheritdoc />
    protected override (byte[] Ciphertext, byte[] Tag) Encrypt(
        byte[] key,
        byte[] nonce,
        byte[] associatedData,
        byte[] plaintext)
    {
        byte[] ciphertextWithTag = new byte[plaintext.Length + AsconAead128.TagBytes];
        using AsconAead128 aead = new(key, nonce);
        aead.ProcessAssociatedData(associatedData);
        aead.Encrypt(plaintext, ciphertextWithTag);

        byte[] ciphertext = ciphertextWithTag.AsSpan(0, plaintext.Length).ToArray();
        byte[] tag = ciphertextWithTag.AsSpan(plaintext.Length, AsconAead128.TagBytes).ToArray();
        return (ciphertext, tag);
    }

    /// <inheritdoc />
    protected override byte[] Decrypt(
        byte[] key,
        byte[] nonce,
        byte[] associatedData,
        byte[] ciphertext,
        byte[] tag)
    {
        byte[] ciphertextWithTag = new byte[ciphertext.Length + tag.Length];
        Buffer.BlockCopy(ciphertext, 0, ciphertextWithTag, 0, ciphertext.Length);
        Buffer.BlockCopy(tag, 0, ciphertextWithTag, ciphertext.Length, tag.Length);

        byte[] plaintext = new byte[ciphertext.Length];
        using AsconAead128 aead = new(key, nonce);
        aead.ProcessAssociatedData(associatedData);
        aead.Decrypt(ciphertextWithTag, plaintext);
        return plaintext;
    }

    /// <inheritdoc />
    protected override IReadOnlyList<AeadKat> KnownAnswers { get; } =
    [
        // NIST LWC_AEAD_KAT_128_128 Count = 1: empty plaintext, empty AD.
        new(
            Name: "NIST #1: empty PT, empty AD",
            Key: s_referenceKey,
            Nonce: s_referenceNonce,
            AssociatedData: Array.Empty<byte>(),
            Plaintext: Array.Empty<byte>(),
            Ciphertext: Array.Empty<byte>(),
            Tag: Convert.FromHexString("4F9C278211BEC9316BF68F46EE8B2EC6")),

        // NIST LWC_AEAD_KAT_128_128 Count = 34: single-byte plaintext, empty AD.
        new(
            Name: "NIST #34: PT=0x20, empty AD",
            Key: s_referenceKey,
            Nonce: s_referenceNonce,
            AssociatedData: Array.Empty<byte>(),
            Plaintext: [0x20],
            Ciphertext: [0xE8],
            Tag: Convert.FromHexString("DD576ABA1CD3E6FC704DE02AEDB79588")),

        // NIST LWC_AEAD_KAT_128_128 Count = 35: single-byte plaintext and single-byte AD.
        new(
            Name: "NIST #35: PT=0x20, AD=0x30",
            Key: s_referenceKey,
            Nonce: s_referenceNonce,
            AssociatedData: [0x30],
            Plaintext: [0x20],
            Ciphertext: [0x96],
            Tag: Convert.FromHexString("2B8016836C75A7D86866588CA245D886")),
    ];

    /// <inheritdoc />
    protected override IReadOnlyList<AeadTamperKat> TamperCases { get; } =
    [
        new(
            Name: "tamper ciphertext byte",
            BaseCase: new AeadKat(
                Name: "NIST #34 base",
                Key: s_referenceKey,
                Nonce: s_referenceNonce,
                AssociatedData: Array.Empty<byte>(),
                Plaintext: [0x20],
                Ciphertext: [0xE8],
                Tag: Convert.FromHexString("DD576ABA1CD3E6FC704DE02AEDB79588")),
            TamperKind: AeadTamperKind.Ciphertext),

        new(
            Name: "tamper tag byte",
            BaseCase: new AeadKat(
                Name: "NIST #34 base",
                Key: s_referenceKey,
                Nonce: s_referenceNonce,
                AssociatedData: Array.Empty<byte>(),
                Plaintext: [0x20],
                Ciphertext: [0xE8],
                Tag: Convert.FromHexString("DD576ABA1CD3E6FC704DE02AEDB79588")),
            TamperKind: AeadTamperKind.Tag),

        new(
            Name: "tamper associated data introduces a byte",
            BaseCase: new AeadKat(
                Name: "NIST #34 base",
                Key: s_referenceKey,
                Nonce: s_referenceNonce,
                AssociatedData: Array.Empty<byte>(),
                Plaintext: [0x20],
                Ciphertext: [0xE8],
                Tag: Convert.FromHexString("DD576ABA1CD3E6FC704DE02AEDB79588")),
            TamperKind: AeadTamperKind.AssociatedData),

        new(
            Name: "tamper nonce byte",
            BaseCase: new AeadKat(
                Name: "NIST #34 base",
                Key: s_referenceKey,
                Nonce: s_referenceNonce,
                AssociatedData: Array.Empty<byte>(),
                Plaintext: [0x20],
                Ciphertext: [0xE8],
                Tag: Convert.FromHexString("DD576ABA1CD3E6FC704DE02AEDB79588")),
            TamperKind: AeadTamperKind.Nonce),
    ];
}
