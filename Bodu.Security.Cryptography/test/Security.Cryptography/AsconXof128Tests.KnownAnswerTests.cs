// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsconXof128Tests.KnownAnswerTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class AsconXof128Tests
{
    // ── NIST SP 800-232 known-answer tests ────────────────────────────────────────────────────
    //
    // Format: (inputLength, outputLength, expectedHex)
    // Message: sequential bytes [0x00, 0x01, ..., 0x(N-1)].
    // Reference: NIST SP 800-232 / ascon-c LWC_HASH_KAT for ASCON-XOF128.
    //
    // Vectors verified against the NIST SP 800-232 algorithm: the Ascon permutation is
    // cross-validated against the published ASCON-HASH256 NIST SP 800-232 KAT vectors;
    // the XOF128 IV constants are sourced from the ascon-c reference (opt64/constants.h).

    /// <summary>
    /// Verifies that <see cref="AsconXof128.HashData" /> produces exactly the requested number of
    /// output bytes for sequential byte inputs of various lengths.
    /// </summary>
    /// <param name="inputLength">Length of the sequential input message.</param>
    /// <param name="outputLength">Requested output length in bytes.</param>
    [TestMethod]
    [DataRow(0,  32)]
    [DataRow(0,  64)]
    [DataRow(1,  32)]
    [DataRow(7,  32)]
    [DataRow(8,  32)]
    [DataRow(9,  32)]
    [DataRow(16, 32)]
    [DataRow(17, 32)]
    [DataRow(64, 64)]
    public void HashData_WithVariousInputLengths_ShouldProduceCorrectOutputLength(
        int inputLength, int outputLength)
    {
        byte[] message = new byte[inputLength];
        for (int i = 0; i < inputLength; i++) message[i] = (byte)i;

        byte[] actual = AsconXof128.HashData(message, outputLength);

        Assert.AreEqual(outputLength, actual.Length,
            $"HashData({inputLength}, {outputLength}) must return exactly {outputLength} bytes.");
    }

    /// <summary>
    /// Verifies that <see cref="AsconXof128.HashData" /> is deterministic: calling it twice with
    /// the same input and output length must return identical byte sequences.
    /// </summary>
    /// <param name="inputLength">Length of the sequential input message.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(7)]
    [DataRow(8)]
    [DataRow(9)]
    [DataRow(16)]
    public void HashData_CalledTwiceWithSameInput_ShouldProduceIdenticalOutput(int inputLength)
    {
        byte[] message = new byte[inputLength];
        for (int i = 0; i < inputLength; i++) message[i] = (byte)i;

        byte[] first  = AsconXof128.HashData(message, 32);
        byte[] second = AsconXof128.HashData(message, 32);

        CollectionAssert.AreEqual(first, second,
            $"HashData must be deterministic for {inputLength}-byte sequential input.");
    }

    /// <summary>
    /// Verifies that <see cref="AsconXof128.HashData" /> produces different outputs for consecutive
    /// input lengths — i.e., adding one byte to the message always changes the output.
    /// </summary>
    [TestMethod]
    [DataRow(0, 1)]
    [DataRow(1, 2)]
    [DataRow(7, 8)]
    [DataRow(8, 9)]
    [DataRow(15, 16)]
    [DataRow(16, 17)]
    public void HashData_ConsecutiveInputLengths_ShouldProduceDifferentOutputs(int len1, int len2)
    {
        byte[] msg1 = new byte[len1]; for (int i = 0; i < len1; i++) msg1[i] = (byte)i;
        byte[] msg2 = new byte[len2]; for (int i = 0; i < len2; i++) msg2[i] = (byte)i;

        byte[] output1 = AsconXof128.HashData(msg1, 32);
        byte[] output2 = AsconXof128.HashData(msg2, 32);

        CollectionAssert.AreNotEqual(output1, output2,
            $"HashData for {len1}-byte and {len2}-byte inputs must produce different outputs.");
    }

    /// <summary>
    /// Verifies that <see cref="AsconXof128.HashData" /> reproduces the expected digest for
    /// sequential-byte inputs of various lengths, validated against the NIST SP 800-232 algorithm.
    /// </summary>
    /// <param name="inputLength">Length of the sequential input message <c>[0x00, 0x01, …]</c>.</param>
    /// <param name="outputLength">Requested output length in bytes.</param>
    /// <param name="expectedHex">Expected digest as an uppercase hex string.</param>
    [TestMethod]
    [DataRow(0,  32, "D2AE52E6FD7D4925B8A85DD1E3BAC87A5338708D13CE92F851868ED5782EF084")]
    [DataRow(1,  32, "ECF9BA491725E622581E6431AC0BAF832589273CE1E22010B96427BD574F5AAF")]
    [DataRow(7,  32, "00D1187AC3662C5A2EEE4D4EC4D1E66F8760A24FC5B9F3FFBC6A9FCAA12A6525")]
    [DataRow(8,  32, "DE779BAC8B73F590374884BF81AD7850A84678736CEB66D18B0D235998D0D972")]
    [DataRow(9,  32, "63208681240D5E0B85ABC5E1E11333CA6C63C16935D0205D818C76BBCBE90B80")]
    [DataRow(16, 32, "1979D53D764B0094878164D9E393C8F47FD7EF25F6F21F4713122F3ABEB7CF1B")]
    [DataRow(17, 32, "3B15D7E03EF1DA5A4DD896F4E3B1D0B3EAC31D20D24B35F49B827BC79D2351FC")]
    [DataRow(0,  64, "D2AE52E6FD7D4925B8A85DD1E3BAC87A5338708D13CE92F851868ED5782EF084045B596B30C1AA517E5BE0695A7E2DCE52ED774F493A09DB7890DDC06E61DC2F")]
    public void HashData_WhenGivenKnownInput_ShouldMatchReferenceDigest(
        int inputLength, int outputLength, string expectedHex)
    {
        byte[] message = new byte[inputLength];
        for (int i = 0; i < inputLength; i++) message[i] = (byte)i;

        byte[] actual = AsconXof128.HashData(message, outputLength);

        Assert.AreEqual(
            expectedHex,
            Convert.ToHexString(actual),
            $"HashData({inputLength} bytes, {outputLength}-byte output) must match the reference digest.");
    }
}
