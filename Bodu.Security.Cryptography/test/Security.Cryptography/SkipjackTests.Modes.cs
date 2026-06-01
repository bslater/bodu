// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SkipjackTests.Modes.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public sealed partial class SkipjackTests
{
    private static readonly byte[] s_modeReferenceKey = Convert.FromHexString("00998877665544332211");

    private static readonly byte[] s_modeReferenceIV = Convert.FromHexString("0000000000000000");

    private static readonly byte[] s_modeSingleBlockPlaintext = Convert.FromHexString("33221100DDCCBBAA");

    private static readonly byte[] s_modeTwoBlockPlaintext = Convert.FromHexString("33221100DDCCBBAA44332211EEDDCCBB");

    /// <summary>
    /// Verifies that the ECB / PKCS7 round trip recovers the original single-block plaintext.
    /// Published fixed-ciphertext vectors for Skipjack in this configuration are sparse, so the
    /// invariant under test is decrypt-after-encrypt rather than ciphertext equality.
    /// </summary>
    [TestMethod]
    public void Modes_EcbPkcs7_WhenEncryptedThenDecrypted_ShouldRecoverSingleBlockPlaintext() =>
        AssertModeRoundTrip(CipherMode.ECB, PaddingMode.PKCS7, s_modeSingleBlockPlaintext);

    /// <summary>
    /// Verifies that the CBC / PKCS7 round trip recovers the original single-block plaintext.
    /// </summary>
    [TestMethod]
    public void Modes_CbcPkcs7_WhenEncryptedThenDecrypted_ShouldRecoverSingleBlockPlaintext() =>
        AssertModeRoundTrip(CipherMode.CBC, PaddingMode.PKCS7, s_modeSingleBlockPlaintext);

    /// <summary>
    /// Verifies that the CBC / PKCS7 round trip recovers the original two-block plaintext.
    /// </summary>
    [TestMethod]
    public void Modes_CbcPkcs7_WhenEncryptedThenDecrypted_ShouldRecoverTwoBlockPlaintext() =>
        AssertModeRoundTrip(CipherMode.CBC, PaddingMode.PKCS7, s_modeTwoBlockPlaintext);

    /// <summary>
    /// Verifies that the CBC / None round trip recovers the original two-block plaintext when
    /// the input is already a whole number of blocks and no padding is applied.
    /// </summary>
    [TestMethod]
    public void Modes_CbcNone_WhenEncryptedThenDecrypted_ShouldRecoverTwoBlockPlaintext() =>
        AssertModeRoundTrip(CipherMode.CBC, PaddingMode.None, s_modeTwoBlockPlaintext);

    /// <summary>
    /// Verifies that the CFB / None round trip recovers the original single-block plaintext.
    /// </summary>
    [TestMethod]
    public void Modes_CfbNone_WhenEncryptedThenDecrypted_ShouldRecoverSingleBlockPlaintext() =>
        AssertModeRoundTrip(CipherMode.CFB, PaddingMode.None, s_modeSingleBlockPlaintext);

    private static void AssertModeRoundTrip(CipherMode mode, PaddingMode padding, byte[] plaintext)
    {
        byte[] ciphertext;
        using (Skipjack encryptor = new() { Mode = mode, Padding = padding })
        using (ICryptoTransform transform = encryptor.CreateEncryptor(s_modeReferenceKey, s_modeReferenceIV))
        {
            ciphertext = transform.TransformFinalBlock(plaintext, 0, plaintext.Length);
        }

        byte[] recovered;
        using (Skipjack decryptor = new() { Mode = mode, Padding = padding })
        using (ICryptoTransform transform = decryptor.CreateDecryptor(s_modeReferenceKey, s_modeReferenceIV))
        {
            recovered = transform.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
        }

        CollectionAssert.AreEqual(
            plaintext,
            recovered,
            $"Round-trip Skipjack {mode}/{padding}: decrypt of encrypted plaintext did not yield original.");
    }
}
