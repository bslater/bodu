// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsconXof128Tests.KnownAnswerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class AsconXof128Tests
{
    // ── NIST SP 800-232 known-answer tests ────────────────────────────────────────────────────
    //
    // Format: (inputLength, outputLength, expectedHex)
    // Message: sequential bytes [0x00, 0x01, ..., 0x(N-1)].
    // Reference: ascon-c LWC_XOF_KAT_128_512 (NIST SP 800-232 Ascon-XOF128).
    //
    // These digests are the exact ascon-c reference outputs: for an N-byte sequential message the
    // XOF output is the prefix of the file's 512-bit MD for the row whose Msg is that sequence.
    // Because XOF output is a prefix, a 32-byte digest is the first 32 bytes of the 64-byte MD.

    /// <summary>
    /// Verifies that <see cref="AsconXof128.HashData" /> produces exactly the requested number of
    /// output bytes for sequential byte inputs of various lengths.
    /// </summary>
    /// <param name="inputLength">Length of the sequential input message.</param>
    /// <param name="outputLength">Requested output length in bytes.</param>
    [TestMethod]

    [DataRow(0, 32)]
    [DataRow(0, 64)]
    [DataRow(1, 32)]
    [DataRow(7, 32)]
    [DataRow(8, 32)]
    [DataRow(9, 32)]
    [DataRow(16, 32)]
    [DataRow(17, 32)]
    [DataRow(64, 64)]
    public void HashData_WithVariousInputLengths_ShouldProduceCorrectOutputLength(
        int inputLength, int outputLength)
    {
        byte[] message = new byte[inputLength];
        for (int i = 0; i < inputLength; i++) message[i] = (byte)i;

        byte[] actual = AsconXof128.HashData(message, outputLength);

        Assert.HasCount(outputLength, actual,
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

        byte[] first = AsconXof128.HashData(message, 32);
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

    [DataRow(0, 32, "473D5E6164F58B39DFD84AACDB8AE42EC2D91FED33388EE0D960D9B3993295C6")]
    [DataRow(1, 32, "51430E0438ECDF642B393630D977625F5F337656BA58AB1E960784AC32A16E0D")]
    [DataRow(7, 32, "7AE562DB37212A9ACD2673ECFD5B4F1C5CB2E6F64EBF00AA7F6EF8DC82C448D5")]
    [DataRow(8, 32, "8D1886F5D3EC4AF8D15B44BC62B74DA6EA91BC28FB82F9C34079B5ED6E38B6C9")]
    [DataRow(9, 32, "DB3013BFBBD132DC1D3152FD955ED48F7CBB675E9AD2A2FECF92B74C957592E0")]
    [DataRow(16, 32, "10BFEDC5F6442D3E1D8C324878CE1DDF73B01CAFC365589283AC4CBB98E48DE3")]
    [DataRow(17, 32, "233AF64F97CA9BD97BAE06270571E57215C5CB5BA4038536C5C128DA1D3A379A")]
    [DataRow(0, 64, "473D5E6164F58B39DFD84AACDB8AE42EC2D91FED33388EE0D960D9B3993295C6AD77855A5D3B13FE6AD9E6098988373AF7D0956D05A8F1665D2C67D1A3AD10FF")]
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

    /// <summary>
    /// Verifies that <see cref="AsconXof128.HashData" /> reproduces, byte-for-byte, the exact 512-bit
    /// output published in the ascon-c <c>LWC_XOF_KAT_128_512.txt</c> reference file for the given
    /// message — pinning the full data path to an external published KAT rather than a derived oracle.
    /// </summary>
    /// <param name="messageHex">The message bytes as taken from the reference file's <c>Msg</c> field.</param>
    /// <param name="expectedHex">The reference file's 512-bit <c>MD</c> output.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("", "473D5E6164F58B39DFD84AACDB8AE42EC2D91FED33388EE0D960D9B3993295C6AD77855A5D3B13FE6AD9E6098988373AF7D0956D05A8F1665D2C67D1A3AD10FF")]
    [DataRow("00", "51430E0438ECDF642B393630D977625F5F337656BA58AB1E960784AC32A16E0D446405551F5469384F8EA283CF12E64FA72C426BFEBAEA3AA1529E2C4AB23A2F")]
    [DataRow("00010203040506", "7AE562DB37212A9ACD2673ECFD5B4F1C5CB2E6F64EBF00AA7F6EF8DC82C448D5FE11CD91F4368C37690D79E5DE0CA8AD419E1918CE8DAB2D42363E9476638A7B")]
    [DataRow("0001020304050607", "8D1886F5D3EC4AF8D15B44BC62B74DA6EA91BC28FB82F9C34079B5ED6E38B6C951803D7DFB3C5E512A0EF5E4060062A6FD067F9C73EF9BEE527411BDA67FC896")]
    [DataRow("000102030405060708", "DB3013BFBBD132DC1D3152FD955ED48F7CBB675E9AD2A2FECF92B74C957592E0C89959E81C16FD07EAD9EEB8E40359C497AA20258B43D87EC69AD0BB0993FD38")]
    [DataRow("000102030405060708090A0B0C0D0E0F", "10BFEDC5F6442D3E1D8C324878CE1DDF73B01CAFC365589283AC4CBB98E48DE3CEDA8A41BB0983D539E4D90F6458C5C781724FAD641ED3CDB4779931097440B3")]
    [DataRow("000102030405060708090A0B0C0D0E0F10", "233AF64F97CA9BD97BAE06270571E57215C5CB5BA4038536C5C128DA1D3A379AE13DA3E54546A1499014CA03F2EEE10B7AA930FAA58A3994FD4BCC71F6CB1927")]
    [DataRow("000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F", "2E5F3403F4171471CC7934B51982CECE8D6628435DB70E89880F3BE4E0B7B05232DFE63C44A836D771337C9C5A2688D1B71ECABE0D5C2006FEF36EF3186138AD")]
    [DataRow("000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F20", "FEF74B7EBD183BA1D87BF414000B29258D6A2233A2A03ED519C646B351BC008464CB725C2922E77A5E2B71F2D48E8D1AB34B45C3DA91F5D46C9C3D9AE9057E02")]
    public void HashData_WithAsconCReferenceVector_ShouldMatchPublishedOutput(string messageHex, string expectedHex)
    {
        byte[] message = Convert.FromHexString(messageHex);

        byte[] actual = AsconXof128.HashData(message, expectedHex.Length / 2);

        Assert.AreEqual(expectedHex, Convert.ToHexString(actual),
            "Ascon-XOF128 output must match the ascon-c LWC_XOF_KAT_128_512 published vector.");
    }
}
