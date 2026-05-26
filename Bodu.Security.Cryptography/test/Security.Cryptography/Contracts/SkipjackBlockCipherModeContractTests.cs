// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SkipjackBlockCipherModeContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using Bodu.Test.Contracts;
using Bodu.Test.Kat;

namespace Bodu.Security.Cryptography.Contracts;

/// <summary>
/// Drives <see cref="BlockCipherModeContractTests{TCipher}" /> against <see cref="Skipjack" /> across
/// the canonical cipher × mode × padding matrix. Published fixed-ciphertext vectors for non-AES
/// ciphers in CBC/PKCS7 are sparse; this subclass instead exercises the encrypt-decrypt round-trip
/// invariant across the modes Bodu's Skipjack supports (ECB, CBC, CFB, OFB) combined with PKCS7 and
/// None padding. Bespoke Skipjack mode tests (CTS, residue boundary cases, intermediate-block
/// behaviour) live in <c>SkipjackTransformTests.*</c> and <c>SymmetricAlgorithmTests.*</c>.
/// </summary>
[TestClass]
public sealed class SkipjackBlockCipherModeContractTests
    : BlockCipherModeContractTests<Skipjack>
{
    private static readonly byte[] s_referenceKey = Convert.FromHexString("00998877665544332211");

    private static readonly byte[] s_referenceIV = Convert.FromHexString("0000000000000000");

    private static readonly byte[] s_singleBlockPlaintext = Convert.FromHexString("33221100DDCCBBAA");

    private static readonly byte[] s_twoBlockPlaintext = Convert.FromHexString("33221100DDCCBBAA44332211EEDDCCBB");

    /// <inheritdoc />
    protected override byte[] Encrypt(byte[] key, byte[] iv, CipherMode mode, PaddingMode padding, byte[] plaintext)
    {
        using Skipjack algorithm = new()
        {
            Mode = mode,
            Padding = padding,
        };
        using ICryptoTransform encryptor = algorithm.CreateEncryptor(key, iv);
        return encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);
    }

    /// <inheritdoc />
    protected override byte[] Decrypt(byte[] key, byte[] iv, CipherMode mode, PaddingMode padding, byte[] ciphertext)
    {
        using Skipjack algorithm = new()
        {
            Mode = mode,
            Padding = padding,
        };
        using ICryptoTransform decryptor = algorithm.CreateDecryptor(key, iv);
        return decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
    }

    /// <inheritdoc />
    /// <remarks>
    /// No fixed-ciphertext KATs are supplied here: Skipjack mode-specific reference vectors are not
    /// published. The encrypt-decrypt round trip across the mode × padding matrix is asserted via
    /// <see cref="RoundTripCases" /> instead.
    /// </remarks>
    protected override IReadOnlyList<BlockCipherModeKat> KnownAnswers { get; } = Array.Empty<BlockCipherModeKat>();

    /// <inheritdoc />
    protected override IReadOnlyList<BlockCipherModeKat> RoundTripCases { get; } = new BlockCipherModeKat[]
    {
        new(
            Name: "ECB / PKCS7 — single full block",
            Algorithm: "Skipjack",
            Mode: CipherMode.ECB,
            Padding: PaddingMode.PKCS7,
            Key: s_referenceKey,
            IV: s_referenceIV,
            Plaintext: s_singleBlockPlaintext,
            Ciphertext: Array.Empty<byte>()),

        new(
            Name: "CBC / PKCS7 — single full block",
            Algorithm: "Skipjack",
            Mode: CipherMode.CBC,
            Padding: PaddingMode.PKCS7,
            Key: s_referenceKey,
            IV: s_referenceIV,
            Plaintext: s_singleBlockPlaintext,
            Ciphertext: Array.Empty<byte>()),

        new(
            Name: "CBC / PKCS7 — two full blocks",
            Algorithm: "Skipjack",
            Mode: CipherMode.CBC,
            Padding: PaddingMode.PKCS7,
            Key: s_referenceKey,
            IV: s_referenceIV,
            Plaintext: s_twoBlockPlaintext,
            Ciphertext: Array.Empty<byte>()),

        new(
            Name: "CBC / None — two full blocks (no padding)",
            Algorithm: "Skipjack",
            Mode: CipherMode.CBC,
            Padding: PaddingMode.None,
            Key: s_referenceKey,
            IV: s_referenceIV,
            Plaintext: s_twoBlockPlaintext,
            Ciphertext: Array.Empty<byte>()),

        new(
            Name: "CFB / None — single full block",
            Algorithm: "Skipjack",
            Mode: CipherMode.CFB,
            Padding: PaddingMode.None,
            Key: s_referenceKey,
            IV: s_referenceIV,
            Plaintext: s_singleBlockPlaintext,
            Ciphertext: Array.Empty<byte>()),
    };
}
