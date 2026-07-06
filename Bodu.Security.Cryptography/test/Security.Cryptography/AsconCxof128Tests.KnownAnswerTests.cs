// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsconCxof128Tests.KnownAnswerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class AsconCxof128Tests
{
    // ── NIST SP 800-232 known-answer tests ────────────────────────────────────────────────────
    //
    // Format: (customisationLength, messageLength, outputLength)
    // Customisation: sequential bytes [0x00, 0x01, ..., 0x(CL-1)].
    // Message:       sequential bytes [0x00, 0x01, ..., 0x(ML-1)].
    // Reference: NIST SP 800-232 Ascon-CXOF128, cross-checked against the ascon-c
    // LWC_CXOF_KAT_128_512 reference file (all 1089 rows reproduce; the empty-customisation rows
    // here equal the file's empty-Z counterparts truncated to the requested output length).

    /// <summary>
    /// Verifies that <see cref="AsconCxof128" /> produces exactly the requested number of output
    /// bytes for various combinations of customisation and message lengths.
    /// </summary>
    /// <param name="customizationLength">Length of the sequential customisation string.</param>
    /// <param name="messageLength">Length of the sequential message.</param>
    /// <param name="outputLength">Requested output length in bytes.</param>
    [TestMethod]

    [DataRow(0, 0, 32)]
    [DataRow(0, 1, 32)]
    [DataRow(1, 0, 32)]
    [DataRow(1, 1, 32)]
    [DataRow(0, 8, 32)]
    [DataRow(8, 0, 32)]
    [DataRow(8, 8, 32)]
    [DataRow(9, 9, 64)]
    [DataRow(16, 16, 32)]
    [DataRow(17, 17, 32)]
    public void GetHash_WithVariousLengths_ShouldProduceCorrectOutputLength(
        int customizationLength, int messageLength, int outputLength)
    {
        byte[] customization = new byte[customizationLength];
        for (int i = 0; i < customizationLength; i++) customization[i] = (byte)i;

        byte[] message = new byte[messageLength];
        for (int i = 0; i < messageLength; i++) message[i] = (byte)i;

        using var cxof = new AsconCxof128();
        cxof.Customize(customization);
        cxof.Absorb(message);
        byte[] actual = cxof.GetHash(outputLength);

        Assert.HasCount(outputLength, actual,
            $"GetHash must return {outputLength} bytes for customize={customizationLength}, message={messageLength}.");
    }

    /// <summary>
    /// Verifies that <see cref="AsconCxof128" /> is deterministic: the same customisation and
    /// message always produce the same output.
    /// </summary>
    /// <param name="customizationLength">Length of the sequential customisation string.</param>
    /// <param name="messageLength">Length of the sequential message.</param>
    [TestMethod]

    [DataRow(0, 0)]
    [DataRow(0, 8)]
    [DataRow(8, 0)]
    [DataRow(8, 8)]
    [DataRow(16, 16)]
    public void GetHash_CalledTwiceWithSameInputs_ShouldProduceIdenticalOutput(
        int customizationLength, int messageLength)
    {
        byte[] customization = new byte[customizationLength];
        for (int i = 0; i < customizationLength; i++) customization[i] = (byte)i;

        byte[] message = new byte[messageLength];
        for (int i = 0; i < messageLength; i++) message[i] = (byte)i;

        using var first = new AsconCxof128();
        first.Customize(customization);
        first.Absorb(message);
        byte[] output1 = first.GetHash(32);

        using var second = new AsconCxof128();
        second.Customize(customization);
        second.Absorb(message);
        byte[] output2 = second.GetHash(32);

        CollectionAssert.AreEqual(output1, output2,
            $"CXOF128 must be deterministic for customize={customizationLength}, message={messageLength}.");
    }

    /// <summary>
    /// Verifies that incrementing the message length by one byte always changes the output.
    /// </summary>
    [TestMethod]

    [DataRow(0, 0, 1)]
    [DataRow(0, 7, 8)]
    [DataRow(0, 8, 9)]
    [DataRow(4, 0, 1)]
    [DataRow(4, 7, 8)]
    public void GetHash_ConsecutiveMessageLengths_ShouldProduceDifferentOutputs(
        int customizationLength, int msgLen1, int msgLen2)
    {
        byte[] customization = new byte[customizationLength];
        for (int i = 0; i < customizationLength; i++) customization[i] = (byte)i;

        byte[] msg1 = new byte[msgLen1]; for (int i = 0; i < msgLen1; i++) msg1[i] = (byte)i;
        byte[] msg2 = new byte[msgLen2]; for (int i = 0; i < msgLen2; i++) msg2[i] = (byte)i;

        using var c1 = new AsconCxof128();
        c1.Customize(customization);
        c1.Absorb(msg1);
        byte[] output1 = c1.GetHash(32);

        using var c2 = new AsconCxof128();
        c2.Customize(customization);
        c2.Absorb(msg2);
        byte[] output2 = c2.GetHash(32);

        CollectionAssert.AreNotEqual(output1, output2,
            $"CXOF128 with message lengths {msgLen1} and {msgLen2} must produce different outputs.");
    }

    /// <summary>
    /// Verifies that <see cref="AsconCxof128" /> reproduces the expected digest for various
    /// combinations of customisation and message lengths, validated against the NIST SP 800-232
    /// algorithm.
    /// </summary>
    /// <param name="customizationLength">Length of the sequential customisation string.</param>
    /// <param name="messageLength">Length of the sequential message.</param>
    /// <param name="outputLength">Requested output length in bytes.</param>
    /// <param name="expectedHex">Expected digest as an uppercase hex string.</param>
    [TestMethod]

    [DataRow(0, 0, 32, "4F50159EF70BB3DAD8807E034EAEBD44C4FA2CBBC8CF1F05511AB66CDCC52990")]
    [DataRow(0, 1, 32, "7F0C0DDD4BC9603DEED19510CDB954D65CF254F59234BFBF5A730D03D2712DAA")]
    [DataRow(1, 0, 32, "6A6FDABD0ACD0B7F98084ADC7EC592789D670305C3B030BAB7F590353515EA95")]
    [DataRow(1, 1, 32, "FBAB1C477798DF70A260AA9067422A13F30781F2700BFDAEFAC44FC1C1E20E16")]
    [DataRow(0, 8, 32, "2C076D8A559299E39D9C42D271B40CFD1072BEBFAC53C939B931508885887440")]
    [DataRow(8, 0, 32, "18A2BD4477B9CDE1614D05B4613653B277D930F8CC92783CB30E2E272C062A6A")]
    [DataRow(8, 8, 32, "3C151CDD72BE71A0CBAF99EF101B04D23F10C633ABBBF5A8900E4860B90F419A")]
    [DataRow(16, 16, 32, "30B0682E8BEC6515DB72978A32F0A43ACC0C119B5225405551F17C532451581C")]
    [DataRow(17, 17, 32, "A0798B54C14B1BD947B5525376857E8C5AA98C3ACCD8593C427A84489BB20B4A")]
    public void GetHash_WhenGivenKnownInputs_ShouldMatchReferenceDigest(
        int customizationLength, int messageLength, int outputLength, string expectedHex)
    {
        byte[] customization = new byte[customizationLength];
        for (int i = 0; i < customizationLength; i++) customization[i] = (byte)i;

        byte[] message = new byte[messageLength];
        for (int i = 0; i < messageLength; i++) message[i] = (byte)i;

        using var cxof = new AsconCxof128();
        cxof.Customize(customization);
        cxof.Absorb(message);
        byte[] actual = cxof.GetHash(outputLength);

        Assert.AreEqual(
            expectedHex,
            Convert.ToHexString(actual),
            $"GetHash(cust={customizationLength}, msg={messageLength}, out={outputLength}) must match the reference digest.");
    }

    /// <summary>
    /// Verifies that <see cref="AsconCxof128" /> reproduces, byte-for-byte, the exact 512-bit output
    /// published in the ascon-c <c>LWC_CXOF_KAT_128_512.txt</c> reference file for the given message and
    /// customisation string. The reference file's customisation <c>Z</c> begins at <c>0x10</c>, so these
    /// rows exercise the SP 800-232 length-prefixed customisation-absorb path against an external
    /// published KAT rather than a derived oracle.
    /// </summary>
    /// <param name="messageHex">The message bytes as taken from the reference file's <c>Msg</c> field.</param>
    /// <param name="customizationHex">The customisation string as taken from the reference file's <c>Z</c> field.</param>
    /// <param name="expectedHex">The reference file's 512-bit <c>MD</c> output.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("", "", "4F50159EF70BB3DAD8807E034EAEBD44C4FA2CBBC8CF1F05511AB66CDCC529905CA12083FC186AD899B270B1473DC5F7EC88D1052082DCDFE69FB75D269E7B74")]
    [DataRow("", "10", "0C93A483E7D574D49FE52CCE03EE646117977D57A8AA57704AB4DAF44B501430FF6AC11A5D1FD6F2154B5C65728268270C8BB578508487B8965718ADA6272FD6")]
    [DataRow("", "1011", "D1106C7622E79FE955BD9D79E03B918E770FE0E0CDDDE28BEB924B02C5FC936B33ACCA299C89ECA5D71886CBBFA4D54A21C55FDE2B679F5E2488063A1719DC32")]
    [DataRow("", "101112131415161718191A", "ECDAB5B15324F99A1709BE26FC329D305BD475E5F39BC2B63788792166AD08FE720CCD14E0A4DE7D83EDE1C7744929DC509C73748D6661A3D3215995357D3F88")]
    [DataRow("00", "", "7F0C0DDD4BC9603DEED19510CDB954D65CF254F59234BFBF5A730D03D2712DAAB9161C6553F65FA72A25B3174AC13A33218C393577A85B6D6F4319D1EF8A7541")]
    [DataRow("00", "101112131415161718191A", "415786EEE2ED178E369B3963358D707154297441395633269780A69842293D8C59A17BA50F98C5612FFBEC7B56C9E4997B5A5BF1255DFC98B472B14CAB7F2819")]
    [DataRow("0001", "101112131415161718", "FC759DE351AE4A8C6F10C6C9C48E993E468311B1521FF644FECC300BD331F117B84CF48243D562A0996F7A6E5943CA599658325BFBD2277B7A7376575E4B3078")]
    [DataRow("000102", "101112131415161718", "51CE5CFE5F3886CB4127A3FC4C899E696D72CE04D9BE5BF03C36E79C1C9A5351A38A4D268F1DE5BDCAB7B04253B717A4D094D74BFCF3760F6651850979CD8D66")]
    [DataRow("00010203", "101112131415161718191A1B1C", "3AF3D645ADA6F40E339CFD8B3F5BDD1B7E452E7A04FE1E236E7D19DB4D12B1D05A41735A58C8332EE2378555E084215CC15B79C4851680DF7191E6E463879CF0")]
    [DataRow("000102030405060708090A0B0C0D0E0F", "", "5BD8386B8CB8B2191CA0AC4034DB620121A97F7DA099E91E6208DC5C196E5194583611208D67D60070E6280A871B001DD366C0DBB6DE05FC07FCB5B82CB641AA")]
    [DataRow("000102030405060708090A0B0C0D0E0F1011", "", "A5688309F9BB3794DD4BADFC622D6AB6FD4A49AEACB0E44B895740566B2A9AB88AC29E315C7BAFD4D1992DA5459AC2E02D0573122A91F675CE027CBF4646B746")]
    public void GetHash_WithAsconCReferenceVector_ShouldMatchPublishedOutput(
        string messageHex, string customizationHex, string expectedHex)
    {
        byte[] message = Convert.FromHexString(messageHex);
        byte[] customization = Convert.FromHexString(customizationHex);

        using var cxof = new AsconCxof128();
        cxof.Customize(customization);
        cxof.Absorb(message);
        byte[] actual = cxof.GetHash(expectedHex.Length / 2);

        Assert.AreEqual(expectedHex, Convert.ToHexString(actual),
            "Ascon-CXOF128 output must match the ascon-c LWC_CXOF_KAT_128_512 published vector.");
    }
}
